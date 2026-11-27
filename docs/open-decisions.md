# Open decisions

Things I decided on my own while building the missions, or wasn't sure about.
Each item says what I did, so you can keep it or change it.

## Story and text

- **1.2 quiz option "hide the menu" replaced.** SPEC listed "hide the menu" as a
  wrong fix, but the app already hides it, so it made a poor quiz option. It is now
  `lock-admin-page`: `[Authorize(Roles = "admin")]` on the Blazor page, which protects
  the page but not the API. SPEC §6 and both story files are updated.
- **"Acting ensign" in German** is "Fähnrich auf Probe" (roster in 1.2, Lore mocks
  it). The dub may have used something else; I didn't check.
- **Data's position in German** is "Zweiter Offizier, Einsatzoffizier". Check that
  it reads well to you.

## Game mechanics

- **Admin user in 1.2** is `riker`. Picard would fit better as the admin, but Riker is
  the lab user the whole story is built around.
- **1.3 shows SQL errors** in the response (500 with the SQLite message). That helps
  players see their quote broke the query. Real apps shouldn't do this; the explanation
  doesn't mention it yet.
- **1.3 emotion chip.** The classified prototype is Soong's emotion chip, and Lore says
  he'll "collect it later". Nothing picks this up yet. It could pay off in the finale or
  stay as a sequel hook.
- **2.1 profile update** copies *every* field, like EF's `SetValues`. If a player sends
  only `displayName`, their email becomes null and credits drop to 0. That is faithful to
  the bug but may confuse; hint 3 says to send the full profile back.
- **2.2 default request** has an empty `X-Api-Key:` header, so players see where the key
  goes. That makes the mission easier; remove it if you want more searching.

- **2.4 Riker only opens ship links** (`enterprise.local`, `localhost` or a relative
  path). Links to other hosts get "Nice try". The attacker host is `evil.example`.
- **2.4 replaced a quiz option.** SPEC had "URL-encode `returnUrl`" as a wrong fix,
  but encoding the whole URL actually breaks the redirect, so it was a weak option. It
  is now "remove `https://` and `http://`", which `//evil.example` still bypasses.
- **3.2 Riker's bot orders every 4 seconds.** Wesley's own order is visible until
  the next one. If that feels too fast or too slow, change `BotInterval` in `SharedStateLab`.
- **3.3 JWT validation is simulated** in the lab instead of using the JwtBearer
  package, to keep the app light. It behaves like `RequireSignedTokens = false`: an
  empty signature is accepted, a non-empty one must verify.
- **3.4 the fake network** has a few extra hosts (`redirect.example`,
  `metadata.internal`, decimal IP) so that the blocklist feedback can be tried for real.
- **3.5 can be cheated with the browser's developer tools:** the moderator frame's
  URL (with its key) and `modToken` are visible there. A real fix would need a
  separate origin. I think it's fine for a training game.
- **3.5 runs real JavaScript** that players write, inside a sandboxed iframe
  (`allow-scripts` only, no same-origin), so it can't touch the app itself. I checked
  the exploit end to end in headless Firefox.
- **No separate Act 3 ending.** The finale follows 3.5 directly; `story.md` never
  planned one. `Acts.cs` has room for it if you want one.
- **The finale and act endings** show once all *registered* missions are done. With
  2.3 missing, Act 2 needs three missions.
- **Lab APIs skip the friendly "not found" page** now. Before, a POST that got a
  401 was re-routed to the HTML page and showed up as a confusing 400 in the console.

## Not built yet

- **Mission 2.3 `brute-force` ("Four-Digit Airlock") is left for you to write.** While
  I was writing it, an automatic safety check stopped my response, and I'm not allowed
  to produce that content again. The slot is kept: SPEC §6 and `docs/story.md` still
  describe it, and Lore's calling card in 2.2 points to it. Until it exists, Act 2 has
  three missions, and the Act 2 ending shows once those three are done. The Repeater
  tool from SPEC §5.2 was only needed for 2.3, so it isn't built either.
