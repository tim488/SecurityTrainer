# Security Trainer — Specification

## 1. Purpose

A self-contained web app for learning common vulnerabilities in **.NET, Blazor
and Web APIs**. Each lab is a chapter of a short story: exploit a real
vulnerable endpoint in the browser, capture the flag, then pick the correct fix.

- **Goals:** learn by doing (every lab has a real exploit) · everything happens
  in the browser · zero setup (`git clone` → `dotnet run`) · new lab = new folder
  · fun to play through.
- **Non-goals:** in-browser code editing, user accounts, hosting, leaderboards,
  Docker, server-side progress storage.

## 2. Running

Requires the .NET 10 SDK and a modern browser.

```bash
git clone <repo> && cd securityTrainer
dotnet run --project src/Trainer.Web   # http://localhost:5000
```

The app is **deliberately vulnerable** and binds to `localhost` only. Never
deploy it or expose it to a network.

## 3. Story

### 3.1 Setting

You are the new security chief aboard the **USS Enterprise-D**. Starfleet
rushed the computer core's software refit to make the launch date, and it
shows. Systems misbehave, crew data leaks between decks, and someone keeps
leaving calling cards in the ship's computer, signed with a single letter: **Q**.

### 3.2 Cast

| Character | Role | Used for |
|---|---|---|
| **Captain Picard** | Your commanding officer. Measured, expects results. "Make it so." | Briefings as Captain's log entries, debriefs |
| **Lt. Barclay** | Nervous engineer who wrote much of the refit. Means well. | Wrong fix options ("C-couldn't we just hide the button, sir?") |
| **Lt. Commander Data** | Science officer. Precise, literal, slightly too thorough. | Hints ("I have observed 47 requests to `/logs/*` in the last 3.2 hours…") |
| **Q** | Omnipotent trickster, delighted by human incompetence. | Calling card next to each flag, teasing the next chapter |
| **Geordi, Riker** | Crew accounts used as lab users. You play as Geordi. | — |

### 3.3 How the story appears

- **Briefing** — Captain's log entry from Picard (≤ 100 words) before the technical scenario.
- **Hints** — written as Data's sensor observations.
- **Calling card** — the flag sits next to a note from Q.
- **Fix quiz** — wrong options are presented as Barclay's suggestions.
- **Debrief** — short epilogue (≤ 60 words) after the lab is completed.
- **Act epilogues** — completing an act unlocks a hearing in Q's courtroom (see §3.4).
- **Rank** — derived from completed labs: Cadet → Ensign →
  Lieutenant → Commander → Captain. Shown on the home page.

Rules: the story is flavour. Scenario, goal and explanation must stand on their
own, and a **"Skip story"** toggle hides all story elements. The story never
gives away the exploit. The Star Trek theme is for personal use only.

### 3.4 Story arc

Q arrives on the bridge and puts humanity on trial: if the crew can't secure
their own ship, humans are barred from the stars. Each of Q's moves is a step
an attacker would really take: **look around → take over identities → reach
the core**. Difficulty rises with it. Every calling card points at the next
system Q will attack, which becomes the next lab.

| # | Lab | What Q does | His calling card points to… |
|---|---|---|---|
| 1.1 | `bola` | Reads Riker's personal log | "Such a *public* ship. Who else is on board?" → crew roster |
| 1.2 | `bfla` | Exports the full crew roster | "I wonder what's in the cargo hold." → manifest |
| 1.3 | `sql-injection` | Finds the classified prototype | "Reading is dull. Let's try *being* someone." |
| — | Act 1 hearing | Q: "You let a stranger read your diaries. Guilty, so far." | |
| 2.1 | `mass-assignment` | Promotes himself to admiral | "Rank is nice. Keys are nicer." → storefront config |
| 2.2 | `client-secrets` | Places requisitions with a leaked key | "Riker's access code has only four digits. *Four.*" |
| 2.3 | `brute-force` | Resets Riker's access code | "He'll change it, of course. Unless he gives me his session." |
| 2.4 | `open-redirect` | Phishes Riker's session | "Now, where do you keep the *really* interesting things?" |
| — | Act 2 hearing | Q, wearing Riker's uniform: "Identity is just a string to you." | |
| 3.1 | `path-traversal` | Climbs into the ship's archive | "Your computer can't even tell its users apart." |
| 3.2 | `shared-state` | Reads command codes across sessions | "Who needs codes when the badge checker doesn't look?" |
| 3.3 | `jwt` | Forges an admiral's clearance | "The computer core trusts anyone who asks nicely." |
| 3.4 | `ssrf` | Pulls the core credentials | "One last trial. Your officers read every review…" |
| 3.5 | `blazor-xss` | Hides in a review to steal the moderator's token. You catch *his* token instead. | — |
| — | Verdict | Q, grudgingly: "The court finds humanity… adequate. For now." | |

## 4. User flow

1. **Home** — a case board grouped by act. Each card shows title, category,
   difficulty and status (new / exploited / solved). All labs are open; acts
   are a suggested order.
2. **Lab page** (`/labs/{slug}`), three steps:
   1. **Read** — briefing, scenario, goal, hints revealed one at a time.
   2. **Exploit** — attack the endpoint with the request console. A successful
      exploit reveals `FLAG{...}`; submit it to complete the step.
   3. **Fix** — unlocked after the flag. Pick the correct fix from 3–4 code
      snippets. Wrong answers can be retried. After the correct answer: feedback
      for every option, an explanation with links (OWASP, Microsoft Docs) and
      the debrief.
3. A lab is **completed** when the flag is submitted and the correct fix chosen.
4. **Reset** per lab reseeds its data and clears its progress.

## 5. Architecture

One ASP.NET Core project: a **Blazor Web App** (Interactive Server) plus the
vulnerable lab endpoints (Minimal APIs under `/api/labs/{slug}`).

```
securityTrainer/
├── SPEC.md · README.md · docs/writing-a-lab.md
├── tests/Trainer.Web.Tests/
└── src/Trainer.Web/
    ├── Program.cs              host setup, registers labs
    ├── Core/
    │   ├── ILab.cs · LabRegistry.cs · FlagService.cs · DemoAuth.cs
    │   ├── Story/              cast, acts, epilogues, ranks
    │   └── Simulation/         virtual filesystem, fake network, victim bots
    ├── Components/
    │   ├── Pages/Home.razor · Pages/LabPage.razor
    │   ├── RequestConsole.razor · FlagInput.razor · FixQuiz.razor
    │   ├── HintList.razor · StoryPanel.razor
    │   └── Tools/              JwtEditor.razor, Repeater.razor
    └── Labs/<Name>/            one folder per lab
```

### 5.1 Lab contract

```csharp
public interface ILab
{
    string Slug { get; }                     // "bola"
    string Title { get; }                    // "Someone Else's Personal Log"
    string Category { get; }                 // "API", "Injection", "Blazor", ...
    Difficulty Difficulty { get; }           // Easy, Medium, Hard
    LabStory Story { get; }
    string ScenarioMarkdown { get; }         // technical scenario + goal
    IReadOnlyList<string> Hints { get; }
    RequestTemplate DefaultRequest { get; }
    IReadOnlyList<FixOption> FixOptions { get; }
    string ExplanationMarkdown { get; }

    void MapEndpoints(IEndpointRouteBuilder app);
    void Reset();
}

public record LabStory(int Act, int Order, string Briefing, string CallingCard, string Debrief);
public record FixOption(string Id, string Code, bool IsCorrect, string Feedback);
public record RequestTemplate(string Method, string Path, Dictionary<string,string> Headers, string? Body);
```

Labs are registered explicitly in `LabRegistry` (no reflection). Content lives
in the lab folder as C# constants or embedded `.md` files.

### 5.2 Request console

- Method (GET/POST/PUT/DELETE), path, headers, JSON body; prefilled from
  `DefaultRequest` including Geordi's token.
- Sent server-side via `HttpClient` to the app's own base address. Shows status,
  headers and formatted body. Redirects are **not** followed.
- Only paths under `/api/labs/` are allowed. Last 10 requests per lab are kept
  in memory.
- Optional per-lab tools: **JWT editor** (decode, edit, re-encode) and
  **Repeater** (send a request for each value in a number range).

### 5.3 Demo auth

Each lab seeds users (`geordi`, `riker`, …) with random opaque tokens generated at
startup; `DemoAuth` maps tokens to users. Tokens are unguessable, so the only
way in is the vulnerability being taught. (The JWT lab uses JWTs instead.)

### 5.4 Flags

Random per lab on every app start (`FLAG{bola-7f3a9c...}`), so they can't be
read from source. Placed only where a successful exploit reveals them. Checked
server-side with constant-time comparison.

### 5.5 Data and simulation

- Each lab owns its data and reseeds it at startup and on `Reset()`.
  In-memory collections; SQLite in-memory (EF Core) for SQL injection.
- Labs never touch the host: file access uses a **virtual filesystem**, outbound
  HTTP uses a **fake network** (`HttpMessageHandler` with simulated internal hosts).
- **Victim bots**: in-process simulations of other users (e.g. Riker clicking a
  link, Riker using the replicator). No real browsers.

### 5.6 Progress

`localStorage`, per lab slug: `{ hintsRevealed, flagSubmitted,
fixAnsweredCorrectly, attempts }`, plus global `{ skipStory }`. Accessed via JS
interop in try/catch; the app works without it (progress is lost on reload).

### 5.7 Fix quiz

Options in random order, highlighted with highlight.js served from `wwwroot`
(offline). Wrong options must be **plausible** real-world mistakes: incomplete,
client-side only, obscurity, or blocklists.

## 6. Labs

Endpoint paths are relative to `/api/labs/{slug}`. ✅ = correct fix, ❌ = Barclay's
suggestions.

### Act 1 — "Uninvited Guest" (v1)

*Q looks around: reading data he shouldn't see.*

**1.1 Someone Else's Personal Log** — `bola` · API · Easy
- *Briefing:* Q appeared on the bridge, called our computer "a toddler's toy" and put
  humanity on trial. An hour later, an ensign reports she can read anyone's personal log.
- *Vulnerable:* `GET /logs/{id}` returns any log, no ownership check
  (`GET /logs` correctly lists only your own).
- *Flag:* Geordi owns logs 1001, 1002. Riker's log 1003 has the flag in `Notes`.
- *Exploit:* `GET /logs/1003` with Geordi's token.
- ✅ Query by `Id` **and** `OwnerId == currentUser.Id`, `404` if not found
- ❌ GUID IDs (obscurity; IDs leak via URLs, logs, shared links) · ❌ Hide other
  logs in the UI (API still open) · ❌ `[Authorize]` (authentication ≠ authorization)

**1.2 Bridge Access** — `bfla` · API · Easy
- *Briefing:* Q broadcast the full crew roster on all frequencies. "Lovely bridge."
- *Vulnerable:* `GET /admin/crew/export` only requires an authenticated
  user. The Blazor UI hides the admin menu for non-admins.
- *Flag:* in the export. *Exploit:* call it with Geordi's token.
- ✅ `RequireAuthorization` with an `Admin` role policy on the admin route group
- ❌ Hide the menu (already done, didn't help) · ❌ Unguessable route name
  (obscurity) · ❌ Check the `Referer` is the admin page (client-controlled)

**1.3 Classified Manifest** — `sql-injection` · Injection · Medium
- *Briefing:* A classified prototype in the cargo manifest is the talk of Ten Forward.
- *Vulnerable:* `GET /products?search=` uses `FromSqlRaw("SELECT * FROM Products
  WHERE Name LIKE '%" + search + "%' AND IsPublished = 1")`.
- *Flag:* in the `Description` of the unpublished product.
- *Exploit:* `search=' OR 1=1 --`
- ✅ LINQ `Where(p => p.Name.Contains(search) && p.IsPublished)` or `FromSql($"...{search}...")`
- ❌ `search.Replace("'", "''")` (fragile, context-dependent) · ❌ Reject `--`,
  `OR`, `;` (bypassable, breaks real searches) · ❌ `string.Format` (still concatenation)

### Act 2 — "Borrowed Identities" (v1.1)

*Q stops reading and starts becoming someone else.*

**2.1 Field Promotion** — `mass-assignment` · API · Easy
- *Briefing:* The crew database now lists Q as an admiral. He promoted himself.
- *Vulnerable:* `PUT /profile` binds the body straight to the `User` entity
  (`Id`, `DisplayName`, `Email`, `IsAdmin`, `Credits`) and copies everything.
- *Flag:* `GET /admin/secret` returns it if `IsAdmin`.
- *Exploit:* `PUT /profile` with `"isAdmin": true`, then `GET /admin/secret`.
- ✅ Bind to `UpdateProfileRequest { DisplayName, Email }` and map only those fields
- ❌ Reject `IsAdmin == true` (blocklist; `Credits` still writable, future fields
  exposed) · ❌ Remove the field from the Blazor form (client-side) ·
  ❌ `[Required]`/`[EmailAddress]` (validation ≠ authorization)

**2.2 Open Frequency** — `client-secrets` · Blazor · Easy
- *Briefing:* Starbase supply reports requisitions the Enterprise never placed.
- *Vulnerable:* the Blazor WASM storefront ships `wwwroot/appsettings.json` with
  the partner API key (simulated at `GET /static/appsettings.json`).
- *Flag:* `GET /partner/orders` with `X-Api-Key: <key>`.
- ✅ Keep the key on the server; the client calls the ship's backend, which calls
  the partner (BFF)
- ❌ Base64/obfuscate the key · ❌ Move it into a C# constant (the WASM DLL is
  downloadable and decompilable) · ❌ Restrict callers with CORS (non-browser
  clients ignore CORS)

**2.3 Four-Digit Airlock** — `brute-force` · API · Medium
- *Briefing:* Q reset Riker's access code without ever touching his communicator.
- *Vulnerable:* `POST /password-reset/confirm { user, code }` with a 4-digit code
  and no attempt limit.
- *Flag:* returned on success. *Exploit:* request a reset for Riker, then use the
  Repeater over `0000–9999`.
- ✅ Invalidate the code after a few failed attempts, use a long random code,
  rate-limit per account
- ❌ Rate limiter partitioned by `X-Forwarded-For` (attacker sets it) ·
  ❌ CAPTCHA in the Blazor form only · ❌ `Task.Delay(500)` on failure
  (parallel requests)

**2.4 Wrong Coordinates** — `open-redirect` · API · Medium
- *Briefing:* Riker logged in via a link from "Starfleet Support". Now Q has his account.
- *Vulnerable:* `GET /login/continue?returnUrl=` redirects to `returnUrl` with a
  one-time `code` appended (mobile app handoff).
- *Victim bot:* `POST /support/messages { "link": ... }` makes Riker follow the
  link. Anything landing on `https://evil.example` shows up in `GET /attacker/inbox`.
- *Flag:* Riker's `code`. *Exploit:* send Riker
  `/api/labs/open-redirect/login/continue?returnUrl=https://evil.example/`.
- ✅ `Url.IsLocalUrl(returnUrl)` / `Results.LocalRedirect`, fallback to `/`
- ❌ `StartsWith("/")` (`//evil.example`) · ❌ `Contains("enterprise.local")`
  (`enterprise.local.evil.example`) · ❌ URL-encode `returnUrl`

### Act 3 — "The Trial" (v1.2)

*Q goes for the core of the ship.*

**3.1 Jefferies Tube** — `path-traversal` · API · Medium
- *Briefing:* Data found crew evaluations in Q's latest note. They only exist in the ship's archive.
- *Vulnerable:* `GET /receipts/download?file=1001.pdf` uses
  `Path.Combine("/data/receipts", file)` over the virtual filesystem.
- *Flag:* in `/data/exports/payroll.pdf`.
- *Exploit:* `file=../exports/payroll.pdf` (or an absolute path: `Path.Combine`
  discards the root).
- ✅ Look up receipts by ID; or `Path.GetFullPath` and verify the result is inside the root
- ❌ `file.Replace("../", "")` (`....//`) · ❌ Require a `.pdf` extension (the
  target is a PDF) · ❌ Reject paths starting with `/` (`../` still works)

**3.2 Crossed Beams** — `shared-state` · Blazor · Medium
- *Briefing:* Crew members get each other's replicator orders. Randomly.
- *Vulnerable:* `CheckoutState` is registered as a **Singleton** in Blazor Server.
  A Riker bot places an order every few seconds, storing his command code in it.
- *Flag:* Riker's command code. *Exploit:* `GET /checkout/summary` as Geordi.
- ✅ Register as Scoped (per circuit) and keep data keyed to the user
- ❌ `lock` around access (thread safety ≠ isolation) · ❌ Clear the state after
  each checkout (race) · ❌ Make it an immutable record (the reference is still shared)

**3.3 Forged Clearance** — `jwt` · API · Hard
- *Briefing:* Q logged into the bridge console as an admiral who doesn't exist.
- *Vulnerable:* JWT bearer config with `RequireSignedTokens = false` and
  `ValidateIssuerSigningKey = false`.
- *Flag:* `GET /admin/bridge`. *Exploit:* in the JWT editor set
  `"role": "admin"`, `alg: none`, drop the signature.
- ✅ `RequireSignedTokens = true`, `ValidateIssuerSigningKey = true`,
  `ValidAlgorithms = ["HS256"]`, validate issuer and audience
- ❌ Reject tokens whose `alg` equals `"none"` (blocklist, signature still not
  required) · ❌ Shorter token lifetime · ❌ Look the role up in the DB (the
  forged `sub` still impersonates any user)

**3.4 Ping the Core** — `ssrf` · API · Hard
- *Briefing:* The ship's core credentials are up for auction on a Ferengi trading post.
- *Vulnerable:* `POST /link-preview { "url": ... }` fetches any URL to build a
  subspace message preview.
- *Fake network:* `http://169.254.169.254/latest/credentials` (cloud metadata),
  `http://inventory.internal/`.
- *Flag:* in the metadata credentials. *Exploit:* preview the metadata URL.
- ✅ Hostname allowlist; resolve and reject private/link-local addresses; no redirects
- ❌ Block `localhost`, `127.0.0.1`, `169.254.169.254` as strings (decimal IPs,
  DNS names, redirects) · ❌ Require `https://` · ❌ Return only the page title
  (still reaches internal hosts and leaks data)

**3.5 Subspace Message** — `blazor-xss` · Blazor · Hard (finale)
- *Briefing:* Q's last note: "Your officers read every Ten Forward review. So do I."
  Time to turn his own trick against him.
- *Vulnerable:* reviews render with `@((MarkupString)review.Text)` so the crew
  can use bold text. `POST /reviews` stores them.
- *Simulation:* a sandboxed iframe on the lab page shows the moderator's view,
  which holds a `modToken`, and offers a collect URL that logs to `GET /collect`.
- *Flag:* the `modToken`. *Exploit:* `<img src=x onerror="...">` that sends it to
  the collect URL. (`<script>` does not run via `MarkupString`, which is a useful hint.)
- ✅ Render `@review.Text` (auto-encoded), or sanitize with an allowlist sanitizer
  if formatting is needed
- ❌ Strip `<script>` with a regex · ❌ Validate in the browser before submit ·
  ❌ Block `onerror` and `javascript:` (`onload`, `onfocus`, …)

## 7. Adding a lab

See `docs/writing-a-lab.md`:

1. Create `src/Trainer.Web/Labs/<Name>/` and implement `ILab`.
2. Put the `FlagService` flag where only the exploit reveals it.
3. Register the lab in `LabRegistry` with its act and order.
4. Check manually: exploit works, flag is accepted, quiz feedback is clear.

Guidelines:
- One vulnerability per lab; vulnerable code looks like real production code.
- At least 3 fix options; wrong ones are mistakes developers actually make.
- Host-affecting labs use the simulation layer (§5.5).
- Story: briefing as Picard's log, hints in Data's voice, wrong fixes in
  Barclay's, one calling card from Q. Keep it short and never spoil the exploit.

## 8. Testing

xUnit project `tests/Trainer.Web.Tests` using `WebApplicationFactory`. Per lab:
- The exploit returns the flag; normal use still works.
- Exactly one fix option is correct.
- `Reset()` restores the initial data.
- Story is complete (briefing, calling card, debrief) and `(Act, Order)` is unique.

## 9. Acceptance criteria

- `dotnet run` starts the app on `http://localhost:5000`; not reachable from other machines.
- Home shows the labs grouped by act, with status and rank.
- Every lab is solvable end-to-end in the browser: read → exploit → flag → fix → debrief.
- Progress survives a reload; Reset clears it. "Skip story" hides all story elements.
- Flags differ on every start.
- `dotnet test` passes. README explains running; `docs/writing-a-lab.md` explains adding a lab.

**Releases:** v1 = core app, story framework, Act 1 · v1.1 = Act 2 (victim bots,
Repeater) · v1.2 = Act 3 (virtual filesystem, JWT editor, fake network, moderator iframe).

## 10. Future ideas

- More labs: CORS misconfiguration, CSRF / missing antiforgery, secrets in logs
  (Data's logs as evidence), insecure deserialization, verbose error pages.
- "Spot the bug" code review mode.
- In-browser code editing with real compilation and tests.
- Season 2: a new company, a new antagonist.
