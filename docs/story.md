# Security Trainer — Story

Outline of the story arc. Text marked `TODO` is a placeholder to be written.
Limits from SPEC.md §3.3: briefing ≤ 100 words, debrief ≤ 60 words, the
story never gives away the exploit.

## Premise

- Setting: USS Enterprise-D, right after a rushed software refit of the computer core.
- Hero: **Wesley Crusher**, acting ensign and teenage prodigy. Nobody on the
  bridge takes him fully seriously. He grows into the job lab by lab
  (rank Cadet → Captain).
- Villain: **Lore**, Data's android brother. He has slipped into the ship's
  computer, holds a grudge against Wesley and wants the ship.
- Lore's path follows a real attack: **look around → take over identities → reach the core**.
- Every flag sits next to a calling card from Lore that points at the next system he attacks (the next lab).
- Tone: real tension, with comic relief from Barclay's nerves, Data's
  precision and Lore's wit.
- Only one antagonist: Lore. No second intruder.

## Cast

- **Wesley Crusher**: the player (lab user `wesley`). Eager, underestimated, clever.
- **Captain Picard**: briefings as Captain's log entries, debriefs. Calls him "Mr. Crusher".
- **Lt. Commander Data**: hints, as precise sensor observations. Quietly conflicted about his brother.
- **Lt. Barclay**: wrote much of the refit and feels guilty about it. Voices the wrong fix options.
- **Lore**: calling cards. Charming, cruel, mocks "the boy".
- **Commander Riker**: Lore's favourite victim account (lab user `riker`).

## Ranks

- Cadet: TODO (Lore's reaction)
- Ensign: TODO
- Lieutenant: TODO
- Commander: TODO
- Captain: TODO

## Opening scene

- Shown on first visit to the home page (≤ 120 words).
- Beat: the refit is finished and the crew is celebrating. Then a signature Data
  doesn't recognise appears inside the core: his own, but it isn't him.
- TODO: text

---

## Act 1 — "Uninvited Guest"

*Lore looks around: reading data he shouldn't see.*

### 1.1 `bola`: Someone Else's Personal Log
- Beat: Lore reads Riker's personal log.
- Calling card points to: the crew roster.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: GUID IDs · TODO: hide in UI · TODO: `[Authorize]`
- Debrief: TODO

### 1.2 `bfla`: Bridge Access
- Beat: Lore exports the full crew roster.
- Calling card points to: the cargo manifest.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: hide the menu · TODO: unguessable route · TODO: `Referer` check
- Debrief: TODO

### 1.3 `sql-injection`: Classified Manifest
- Beat: Lore finds the classified prototype in the cargo manifest.
- Calling card points to: becoming someone else (Act 2).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: escape quotes · TODO: keyword blocklist · TODO: `string.Format`
- Debrief: TODO

### Act 1 ending
- Beat: Lore appears on the viewscreen for the first time and recognises Wesley. It's personal now.
- TODO: text

---

## Act 2 — "Borrowed Identities"

*Lore stops reading and starts becoming someone else.*

### 2.1 `mass-assignment`: Field Promotion
- Beat: Lore promotes himself to admiral in the crew database.
- Calling card points to: the storefront config.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: reject `IsAdmin` · TODO: remove the form field · TODO: validation attributes
- Debrief: TODO

### 2.2 `client-secrets`: Open Frequency
- Beat: Lore places requisitions with the leaked partner key.
- Calling card points to: Riker's four-digit access code.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: Base64 · TODO: C# constant · TODO: CORS
- Debrief: TODO

### 2.3 `brute-force`: Four-Digit Airlock
- Beat: Lore resets Riker's access code.
- Calling card points to: Riker's session.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: `X-Forwarded-For` rate limit · TODO: CAPTCHA in the form · TODO: `Task.Delay`
- Debrief: TODO

### 2.4 `open-redirect`: Wrong Coordinates
- Beat: Riker follows a "Starfleet Support" link and Lore takes over his session.
- Calling card points to: the ship's archive (Act 3).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: `StartsWith("/")` · TODO: `Contains(...)` · TODO: URL-encode
- Debrief: TODO

### Act 2 ending
- Beat: Lore impersonates Data on comms and asks the bridge to lower the core
  firewall. Wesley notices: Data never uses contractions.
- TODO: text

---

## Act 3 — "Into the Core"

*Lore goes for the core of the ship.*

### 3.1 `path-traversal`: Jefferies Tube
- Beat: Lore climbs into the ship's archive.
- Calling card points to: the computer can't tell its users apart.
- Open question: the flag file is `payroll.pdf`, but the briefing talks about crew
  evaluations. Rename it (e.g. `crew-evaluations.pdf`).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: strip `../` · TODO: require `.pdf` · TODO: reject leading `/`
- Debrief: TODO

### 3.2 `shared-state`: Crossed Beams
- Beat: replicator orders, and Riker's command code, leak across sessions.
- Calling card points to: the badge checker that doesn't check.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: `lock` · TODO: clear after checkout · TODO: immutable record
- Debrief: TODO

### 3.3 `jwt`: Forged Clearance
- Beat: Lore logs into the bridge console as an admiral who doesn't exist.
- Calling card points to: the computer core trusts anyone who asks nicely.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: reject `alg: none` · TODO: shorter lifetime · TODO: role from DB
- Debrief: TODO

### 3.4 `ssrf`: Ping the Core
- Beat: Lore pulls the core credentials.
- Calling card points to: the finale. Lore brags that he now moderates the Ten Forward reviews.
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay (wrong fixes): TODO: string blocklist · TODO: require `https://` · TODO: title only
- Debrief: TODO

### 3.5 `blazor-xss`: Subspace Message (finale)
- Beat: the sting. Lore hides in the review moderator account. Wesley hides a
  payload in a review and steals Lore's own `modToken`.
- The token reveals where in the core Lore is hiding. Barclay recognises the
  partition, and Data locks his brother out.
- Calling card (Lore, as he's locked out): TODO. Required by SPEC §8.
- Briefing: TODO
- Hints (Data): TODO ×3
- Barclay (wrong fixes): TODO: regex-strip `<script>` · TODO: browser validation · TODO: block `onerror`
- Debrief: TODO. Picard credits Barclay as well as Wesley.

### Finale
- Beat: the ship is secure and Wesley earns the bridge's respect. Lore escapes into the dark.
- Sequel stinger: a final, faint signature in the core. TODO: text

---

## Open points

- SPEC §10 "Season 2: a new antagonist" clashes with Lore escaping. Decide which way to go.
- Fix option IDs aren't defined in the spec yet. Barclay's lines will map to them.
- Rank thresholds aren't defined (suggested: Cadet 0–2, Ensign 3–6, Lieutenant 7–9, Commander 10–12, Captain 13).
