# Security Trainer — Story

The single source for all story text. Labs copy their text from here.

## How to read this file

- **Beat:** what happens in the story at this point, in one line. The text is written to match it.
- **TODO:** a placeholder, written lab by lab as each lab is built.
- **Calling card points to:** the next system Lore attacks, which is the next lab.
- **Barclay lines:** one per wrong fix option, keyed by the option's `Id` (see SPEC §5.1).
- Where each element appears in the app, and what "Skip story" hides, is defined in SPEC §3.3.
- Limits: opening scene ≤ 120 words, briefing ≤ 100 words, debrief ≤ 60 words.
  The story never gives away the exploit.

## Premise

- Setting: USS Enterprise-D, right after a rushed software refit of the computer core.
- Hero: **Wesley Crusher**, acting ensign and teenage prodigy. Nobody on the
  bridge takes him fully seriously. He grows into the job lab by lab.
- Villain: **Lore**, Data's android brother. He has slipped into the ship's
  computer, holds a grudge against Wesley and wants the ship.
- Lore's path follows a real attack: **look around → take over identities → reach the core**.
- Every flag sits next to a calling card from Lore that points at the next lab.
- Tone: real tension, with comic relief from Barclay's nerves, Data's
  precision and Lore's wit.
- Only one antagonist: Lore. No second intruder.

### Background (for readers who don't know the show)

- Lore and Data were built by the same scientist and look identical. Data is
  curious and kind; Lore is vain, charming and ruthless.
- Wesley once helped expose Lore and stop him. Lore has not forgiven "the boy".
- Data can't use contractions ("I do not", never "I don't"). Lore can. That's
  how Wesley unmasks him at the end of Act 2.

## Cast

- **Wesley Crusher**: the player (lab user `wesley`). Eager, underestimated, clever.
- **Captain Picard**: briefings as Captain's log entries, debriefs. Calls him "Mr. Crusher".
- **Lt. Commander Data**: hints, as precise sensor observations. Quietly conflicted about his brother.
- **Lt. Barclay**: wrote much of the refit and feels guilty about it. Voices the wrong fix options.
- **Lore**: calling cards. Charming, cruel, mocks "the boy".
- **Commander Riker**: first officer. His account is Lore's favourite target (lab user `riker`).

## Opening scene

- Shown on the home page until the first lab is completed.
- Beat: the refit is finished and the crew is celebrating. Then Data notices a
  signature inside the core that matches his own, but it isn't him.
- TODO: text

---

## Act 1 — "Uninvited Guest"

*Lore looks around: reading data he shouldn't see.*

### 1.1 `bola`: Someone Else's Personal Log
- Beat: Lore reads Riker's personal log.
- Calling card points to: the crew roster (→ 1.2).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `guid-ids` TODO · `hide-in-ui` TODO · `authorize-attribute` TODO
- Debrief: TODO

### 1.2 `bfla`: Bridge Access
- Beat: Lore exports the full crew roster and broadcasts it on all frequencies.
- Calling card points to: the cargo manifest (→ 1.3).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `hide-menu` TODO · `secret-route` TODO · `referer-check` TODO
- Debrief: TODO

### 1.3 `sql-injection`: Classified Manifest
- Beat: Lore finds a classified prototype hidden in the cargo manifest.
- Calling card points to: becoming someone else (→ Act 2).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `escape-quotes` TODO · `keyword-blocklist` TODO · `string-format` TODO
- Debrief: TODO

### Act 1 ending
- Shown once all Act 1 labs are completed.
- Beat: Lore appears on the viewscreen for the first time and recognises Wesley. It's personal now.
- TODO: text

---

## Act 2 — "Borrowed Identities"

*Lore stops reading and starts becoming someone else.*

### 2.1 `mass-assignment`: Field Promotion
- Beat: an admiral nobody has heard of appears in the crew database. Lore promoted himself.
- Calling card points to: the quartermaster's requisition app (→ 2.2).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `reject-isadmin` TODO · `remove-form-field` TODO · `validation-attributes` TODO
- Debrief: TODO

### 2.2 `client-secrets`: Open Frequency
- Beat: Starbase supply reports requisitions the Enterprise never placed. Lore
  used the API key shipped inside the quartermaster's app.
- Calling card points to: Riker's four-digit access code (→ 2.3).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `base64-key` TODO · `csharp-constant` TODO · `cors` TODO
- Debrief: TODO

### 2.3 `brute-force`: Four-Digit Airlock
- Beat: Lore resets Riker's access code without touching his communicator.
- Calling card points to: stealing Riker's session (→ 2.4).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `forwarded-for-limit` TODO · `form-captcha` TODO · `delay-on-failure` TODO
- Debrief: TODO

### 2.4 `open-redirect`: Wrong Coordinates
- Beat: Riker follows a link from "Starfleet Support" and Lore takes over his account.
- Calling card points to: the ship's archive (→ Act 3).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `starts-with-slash` TODO · `contains-domain` TODO · `url-encode` TODO
- Debrief: TODO

### Act 2 ending
- Shown once all Act 2 labs are completed.
- Beat: Lore impersonates Data on comms and asks the bridge to lower the core
  firewall. Wesley notices he used a contraction, which Data never does.
- TODO: text

---

## Act 3 — "Into the Core"

*Lore goes for the core of the ship.*

### 3.1 `path-traversal`: Jefferies Tube
- Beat: Lore's latest note quotes confidential crew evaluations. They only exist
  in the restricted archive, next to the public mission reports.
- Flag file: `/data/restricted/crew-evaluations.pdf`.
- Calling card points to: crew data getting mixed up between users (→ 3.2).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `strip-dot-dot` TODO · `require-pdf` TODO · `reject-leading-slash` TODO
- Debrief: TODO

### 3.2 `shared-state`: Crossed Beams
- Beat: crew members receive each other's replicator orders, and with Riker's
  order comes his command code.
- Calling card points to: a clearance check that doesn't check signatures (→ 3.3).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `add-lock` TODO · `clear-after-order` TODO · `immutable-record` TODO
- Debrief: TODO

### 3.3 `jwt`: Forged Clearance
- Beat: Lore logs into the bridge console as an admiral who doesn't exist.
- Calling card points to: the core fetching anything it's asked to (→ 3.4).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `reject-alg-none` TODO · `shorter-lifetime` TODO · `role-from-db` TODO
- Debrief: TODO

### 3.4 `ssrf`: Ping the Core
- Beat: someone is offering the ship's core credentials to a Ferengi trader.
  The sender's signature is Lore's. He pulled them through the core itself.
- Calling card points to: the Ten Forward comment board, which Lore now moderates (→ 3.5).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `string-blocklist` TODO · `require-https` TODO · `title-only` TODO
- Debrief: TODO

### 3.5 `blazor-xss`: Subspace Message (finale)
- Beat: the sting. Lore hides in the comment moderator account and reads every
  comment. Wesley plants a comment that steals Lore's own `modToken`.
- The token reveals where in the core Lore is hiding. Barclay recognises the
  partition, and Data locks his brother out.
- Calling card (Lore, as he's locked out): TODO
- Briefing: TODO
- Hints (Data): TODO ×3
- Barclay lines: `regex-strip-script` TODO · `browser-validation` TODO · `block-onerror` TODO
- Debrief: TODO. Picard credits Barclay as well as Wesley.

### Finale
- Shown once all labs are completed.
- Beat: the ship is secure and Wesley earns the bridge's respect. Lore escapes into the dark.
- Sequel stinger: a final, faint signature in the core. TODO: text
