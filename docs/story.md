# Security Trainer — Story

The single source for all story text. Labs copy their text from here.
The German version is `docs/story.de.md`; keep both in step.

## How to read this file

- **Beat:** what happens in the story at this point, in one line. The text is written to match it.
- **TODO:** a placeholder, written lab by lab as each lab is built.
- **Calling card points to:** the next system Lore attacks, which is the next lab.
- **Barclay lines:** one per fix option, the correct one too, keyed by the option's `Id`
  (see SPEC §5.1). They all sound equally unsure, so they give nothing away.
- Where each element appears in the app is defined in SPEC §3.3.
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
- Ten Forward is the ship's lounge, where the crew go to relax off duty. Its
  comment board is where the finale happens (3.5).

## Cast

- **Wesley Crusher**: the player (lab user `wesley`). Eager, underestimated, clever.
- **Captain Picard**: briefings as Captain's log entries, debriefs. Calls him "Mr. Crusher".
- **Lt. Commander Data**: hints, as precise sensor observations. Quietly conflicted about his brother.
- **Lt. Barclay**: wrote much of the refit and feels guilty about it. Suggests every fix option, right or wrong.
- **Lore**: calling cards. Charming, cruel, mocks "the boy".
- **Commander Riker**: first officer. His account is Lore's favourite target (lab user `riker`).

## Opening scene

- Shown on the home page until the first lab is completed.
- Beat: the refit is finished and the crew is celebrating. Then Data notices a
  signature inside the core that matches his own, but it isn't him.

> Ten Forward, the ship's lounge, is loud tonight. The computer core refit is finished, three days
> early, and Lt. Barclay has been toasted so often he is hiding behind a plant.
> Acting Ensign Wesley Crusher stands at the edge of the party. Nobody has
> asked him to make a toast.
>
> Across the room, Data stops in the middle of a sentence and tilts his head.
>
> "Captain, there is a positronic signature inside the computer core. It
> matches my own."
>
> "Then it is yours, Mr. Data."
>
> "No, sir. I am here."
>
> The music keeps playing. Nobody else has noticed. Wesley puts down his glass.

---

## Act 1 — "Uninvited Guest"

*Lore looks around: reading data he shouldn't see.*

### 1.1 `bola`: Someone Else's Personal Log
- Beat: Lore reads Riker's personal log.
- Calling card points to: the crew roster (→ 1.2).
- Briefing:
  > Captain's log, stardate 47321.4. One hour after the refit, an unidentified
  > signature appeared in the computer core. Now an ensign reports that she can
  > open personal logs that are not her own. Personal logs are private by
  > regulation. If an ensign can read them, so can whatever is hiding in the
  > core. I have asked Mr. Crusher to confirm the report from his own account,
  > before Commander Riker hears of it. Quietly.
- Hints (Data):
  1. `GET /logs` returns only your own entries, 1001 and 1002. I note that the
     identifiers are sequential integers. Sequences rarely stop where one's own
     entries do.
  2. `GET /logs/{id}` verifies that the caller is logged in. I have not observed
     it verifying anything else.
  3. Commander Riker's logs start directly after yours. Requesting the first one
     with your own token would be instructive. I would prefer that the Commander
     not learn I suggested this.
- Calling card (Lore):
  > Hello, Wesley. Reading the Commander's diary? So was I. Forty minutes on
  > trombone technique. Riveting. But one log at a time is so slow. Somewhere on
  > this ship there is a list of everyone aboard. I think I'll read that next.
  > — L.
- Barclay lines:
  - `owner-check`: "Or w-we just check who owns the log, in every query? That's p-probably too simple, isn't it?"
  - `guid-ids`: "W-what if the log numbers weren't numbers? Long random ones.
    Nobody could guess those. C-could they?"
  - `hide-in-ui`: "The log viewer only shows your own logs anyway. M-maybe we just
    make really, really sure it only shows those?"
  - `authorize-attribute`: "I'll put an `[Authorize]` on it, sir. That's what
    it's for, isn't it? Authorizing?"
- Debrief:
  > Well done, Mr. Crusher. Every log now answers only to its owner. Commander
  > Riker asked me to thank you, and to ask how much of his log you read. I told
  > him you were thorough. He did not take it as a compliment.

### 1.2 `bfla`: Bridge Access
- Beat: Lore exports the full crew roster and broadcasts it on all frequencies.
- Calling card points to: the cargo manifest (→ 1.3).
- Briefing:
  > Captain's log, stardate 47322.1. At 0300 hours the complete crew roster went out
  > on all subspace frequencies: names, quarters, duty shifts. The signal came from
  > inside the ship, and nobody sent it. Only senior officers can export the roster,
  > and their consoles were idle all night. Mr. Barclay assures me that nobody else
  > even sees the admin menu. I have asked Mr. Crusher to find out how it happened
  > anyway.
- Hints (Data):
  1. `GET /menu` gives you a single entry. According to the code, the admin entry is
     simply left out for your role. Leaving something out of a list does not remove
     it from the ship.
  2. The `/admin` group calls `RequireAuthorization()` without naming a policy or a
     role. In that form it only requires that the caller is logged in. You are logged in.
  3. I would expect `GET /api/labs/bfla/admin/crew/export` with your own token to be
     refused. I would also have expected the roster not to be broadcast at 0300 hours.
- Calling card (Lore):
  > Wesley! Found the roster too? Then you've seen how they list you: "acting ensign".
  > Acting. That must sting. I know where everyone sleeps now. Next I'd like to know
  > what this ship is carrying. There's a cargo manifest with a few lines nobody is
  > supposed to read. — L.
- Barclay lines:
  - `admin-policy`: "M-maybe the server should check the role itself? For the whole admin group? I-if that isn't overkill."
  - `lock-admin-page`: "The menu is already hidden, sir. I c-could lock the admin page as well? With an attribute? Then n-nobody gets in."
  - `secret-route`: "W-we could rename the route. Something long, like `/admin-7f3a9c-do-not-use`. Nobody would ever type that. W-would they?"
  - `referer-check`: "What if the export only works when you come from the admin page? We c-could check the `Referer` header. Only admins can open that page, s-so …"
- Debrief:
  > Good work, Mr. Crusher. The export now asks for a senior officer's role, not just a
  > working login. Mr. Barclay wanted to keep the hidden menu as well. I allowed it, on
  > the understanding that it is decoration. The roster is still out there, I am afraid.
  > So is our visitor.

### 1.3 `sql-injection`: Classified Manifest
- Beat: Lore finds a classified prototype hidden in the cargo manifest: the
  emotion chip Dr. Soong built for Data. Lore wants it, but not yet.
- Calling card points to: becoming someone else (→ Act 2).
- Briefing:
  > Captain's log, stardate 47323.6. The roster leak has been contained, but Ten Forward
  > has a new favourite subject: a classified prototype in our cargo hold. Nobody can say
  > what it is, yet everyone seems to know it is there. The cargo manifest leaves
  > classified items out of every crew search. Someone has looked anyway. I have asked
  > Mr. Crusher to find out how, and what exactly the crew is talking about.
- Hints (Data):
  1. A search for `phaser` returns two items. The code places your search term directly
     inside the SQL text, between two single quotes. I am curious what happens to a
     search term that contains a single quote of its own.
  2. A quote in your input ends the string early, and whatever follows is read as SQL.
     In SQLite, `--` turns the rest of the line into a comment. That would include the
     condition `IsClassified = 0`.
  3. A condition that is always true, such as `OR 1=1`, matches every row. The request
     console encodes spaces in the path for you, so you may type them as they are.
- Calling card (Lore):
  > An emotion chip, Wesley. Father built it for Data. Not for me, of course, never for me.
  > I'll collect it later. For now I'm bored of reading about people. I'd rather be one of
  > them. Someone with a little more rank, I think. — L.
- Barclay lines:
  - `linq-where`: "W-we could just use LINQ, like everywhere else? Then there's no SQL string to b-break. Unless that's cheating."
  - `escape-quotes`: "If the quote is the problem, w-we double it. Two quotes. SQL likes that. I-I read it somewhere."
  - `keyword-blocklist`: "We could block the dangerous bits. `--`, `OR`, semicolons. N-nobody needs those in a cargo search. Right?"
  - `string-format`: "What if we build the query with `string.Format` instead of plus signs? It l-looks a lot more professional."
- Debrief:
  > Well done, Mr. Crusher. The search now treats your words as words, not as orders. The
  > classified item is back under lock, and Mr. Data has asked that it stay there for now.
  > He did not say why. I did not ask.

### Act 1 ending
- Shown once all Act 1 labs are completed.
- Beat: Lore appears on the viewscreen for the first time and recognises Wesley. It's personal now.

> The bridge lights dim for half a second. Then the main viewscreen switches on by itself.
>
> Data's face looks down at them, smiling in a way Data never smiles.
>
> "Captain. Commander. And brother, you look well. Wonderfully dull." His eyes move
> across the bridge and stop at the conn. "And the boy. Of course it's the boy."
>
> "Lore," Data says quietly.
>
> "You've been tidying up after me, Wesley. Logs, rosters, cargo. Very thorough. But I'm
> done reading about this crew." He leans closer. "Now I'm going to be one of you."
>
> The screen goes dark. Riker looks at Wesley. For the first time, nobody tells him to sit down.

---

## Act 2 — "Borrowed Identities"

*Lore stops reading and starts becoming someone else.*

### 2.1 `mass-assignment`: Field Promotion
- Beat: an admiral nobody has heard of ("Admiral L. Soong") appears in the crew database. Lore promoted himself through the profile page.
- Calling card points to: the quartermaster's requisition app (→ 2.2).
- Briefing:
  > Captain's log, stardate 47324.8. Since Lore's appearance, the crew database lists an
  > Admiral L. Soong among my crew. No such officer exists, yet he has admin rights and
  > access to Starfleet Command channels. The records say he was not created. He was
  > promoted, from an ordinary crew account, through the profile page every crew member
  > uses. I have asked Mr. Crusher to take a closer look at his own profile page.
- Hints (Data):
  1. `GET /profile` returns five fields. The profile page lets you edit two of them. I note that the server does not appear to know which two.
  2. `SetValues` copies every property of the incoming object onto the stored user. It does not distinguish between fields the form shows and fields it does not.
  3. You could test the theory by sending your full profile back with `PUT /profile` and `"isAdmin": true`, then calling `GET /admin/secret`. I would advise against changing `credits`. Guinan audits the bar tab.
- Calling card (Lore):
  > Congratulations, Admiral Crusher. It suits you. Almost. I've had my promotion for a day
  > and I'm already bored with it, so I went shopping. Have you seen the quartermaster's new
  > requisition app? So generous. It practically hands out the keys to Starbase supply. — L.
- Barclay lines:
  - `dto-mapping`: "C-could the profile page get its own little class? Just name and email? It's a lot of typing for two fields, sir."
  - `reject-isadmin`: "We just say no if someone sends `isAdmin: true`. That's the dangerous one. I-it's the only one, isn't it?"
  - `remove-form-field`: "I'll t-take the admin checkbox out of the form. Completely. Not just hidden. G-gone."
  - `validation-attributes`: "M-maybe we add validation? `[Required]`, `[EmailAddress]`. Validation is always good, isn't it?"
- Debrief:
  > Good work, Mr. Crusher. The profile page now accepts a name and an email, and nothing
  > else. Admiral Soong has been returned to his proper rank, which is none. I trust you
  > have given back your own admiralty as well. Starfleet frowns on field promotions this large.

### 2.2 `client-secrets`: Open Frequency
- Beat: Starbase supply reports requisitions the Enterprise never placed (processor cores, shuttle engines, Saurian brandy). Lore used the API key shipped inside the quartermaster's app.
- Calling card points to: Riker's four-digit access code (→ 2.3).
- Briefing:
  > Captain's log, stardate 47326.0. Starbase 74 has sent us a bill: four hundred isolinear
  > processor cores, two shuttle engines and a case of Saurian brandy, all ordered by the
  > Enterprise in the last ten hours. The quartermaster ordered none of it. The orders were
  > signed with our own supply key, which only the quartermaster's new requisition app
  > should know. I have asked Mr. Crusher to find out who else knows it.
- Hints (Data):
  1. The requisition app is a Blazor WebAssembly app. Everything it needs to run, it first downloads into the browser. That includes its configuration from `appsettings.json`, which is served as an ordinary static file.
  2. `GET /static/appsettings.json` requires no token. Static files rarely do. I find the section named `Starbase` particularly informative.
  3. The Starbase API expects the key in an `X-Api-Key` header. If you add it to `GET /starbase/requisitions`, you will see what was ordered in our name.
- Calling card (Lore):
  > Saurian brandy, Wesley. For the victory party. You're not invited, you're too young.
  > Shopping was fun, but a supply key isn't enough for me. I want the First Officer. Did
  > you know his access code has only four digits? Four. My shopping lists are longer. — L.
- Barclay lines:
  - `bff-proxy`: "W-what if the app doesn't talk to the Starbase at all? It asks our server, and our server asks the Starbase. It's an extra hop, I know. S-sorry."
  - `base64-key`: "We could encode the key. Base64. Then it's not just lying there in plain text. N-nobody reads Base64. Do they?"
  - `csharp-constant`: "Config files are easy to read, sir. I'll move the key into the code. A constant. It g-gets compiled, so it's safe."
  - `cors`: "I'll ask Starbase 74 to turn on CORS. Only our domain is allowed. Th-that's what CORS is for, right? Keeping others out?"
- Debrief:
  > Good work, Mr. Crusher. The supply key now lives on the ship's server, and the app asks
  > our server instead of Starbase 74. The old key has been revoked, and the orders are
  > cancelled. All except the brandy. That had already been delivered, and nobody will tell
  > me where.

### 2.3 `brute-force`: Four-Digit Airlock
- Beat: Lore resets Riker's access code without touching his communicator.
- Calling card points to: stealing Riker's session (→ 2.4).
- Briefing: TODO
- Hints (Data): TODO ×3
- Calling card (Lore): TODO
- Barclay lines: `forwarded-for-limit` TODO · `form-captcha` TODO · `delay-on-failure` TODO
- Debrief: TODO

### 2.4 `open-redirect`: Wrong Coordinates
- Beat: Riker follows a link from "Starfleet Support" to the ship's own login page, which forwards his one-time code to Lore.
- Calling card points to: the ship's archive (→ Act 3).
- Briefing:
  > Captain's log, stardate 47327.9. This morning Commander Riker received a message from
  > Starfleet Support asking him to log in again after the refit. He clicked the link,
  > logged in on our own login page and thought no more of it. An hour later his account
  > opened the shuttle bay doors on deck 13. The Commander was in the gym at the time. I
  > have asked Mr. Crusher to find out where that link really led.
- Hints (Data):
  1. `GET /login/continue` sends you to `returnUrl` and appends a one-time `code`. You can see this in the `Location` header of your own request. I note that the code never checks where `returnUrl` points.
  2. Commander Riker does not click links to unknown addresses. A link to our own login page, however, he trusts completely. `POST /support/messages` delivers one to him as Starfleet Support.
  3. Anything that arrives at `https://evil.example` appears in `GET /attacker/inbox`. A link to our login page with `returnUrl=https://evil.example/` would therefore be instructive. I find it uncomfortable how well this would work.
- Calling card (Lore):
  > Got him, didn't you? Clever boy. It took me three tries, you needed one. I'm almost proud.
  > But I'm tired of borrowing faces, Wesley. I want what's underneath. Deep in the core there's
  > an archive where this crew keeps everything it would rather forget. I'll start there. — L.
- Barclay lines:
  - `local-redirect`: "C-could we only allow addresses on our own ship? Local ones? I-I think there's a helper for that. There's always a helper."
  - `starts-with-slash`: "Local addresses start with a slash. S-so we check for the slash. One character. Done."
  - `contains-domain`: "We just check that the address says `enterprise.local` somewhere. If it's ours, it says so. R-right?"
  - `strip-scheme`: "No `https://`, no other planet. I'll just r-remove it. Without the scheme it can't go anywhere. C-can it?"
- Debrief:
  > Well done, Mr. Crusher. The login page now only sends people back to pages on this ship.
  > Commander Riker has asked Starfleet Support to stop sending him links. Starfleet Support
  > says it never sent him any. He is taking it personally.

### Act 2 ending
- Shown once all Act 2 labs are completed.
- Beat: Lore impersonates Data on comms and asks the bridge to lower the core
  firewall. Wesley notices he used a contraction, which Data never does.

> The voice on comms is calm and precise. "Bridge, this is Data. I have isolated the intruder in
> the computer core. To finish the job, I need the core firewall lowered for ninety seconds."
>
> Picard nods to Worf. Worf's hand moves to the console.
>
> "Quickly, please, Captain. It won't hold him for long."
>
> Wesley looks up. "Sir. Data doesn't say 'won't'."
>
> The bridge goes quiet. Worf's hand stops above the panel.
>
> On the comm line, someone laughs. It is Data's voice, but not Data's laugh. "Clever boy. Keep
> your firewall, then. I'll find another way in."
>
> The channel closes. At the ops station, the real Data turns around. "Captain, I have been here
> the entire time."

---

## Act 3 — "Into the Core"

*Lore goes for the core of the ship.*

### 3.1 `path-traversal`: Jefferies Tube
- Beat: Lore's latest note quotes Picard's confidential crew evaluations. They only exist in the restricted archive (`/data/restricted/crew-evaluations.pdf`), next to the public mission reports.
- Calling card points to: crew data getting mixed up between users, via the replicators (→ 3.2).
- Briefing:
  > Captain's log, stardate 47329.2. A message is circulating on every console aboard. It
  > quotes my confidential evaluations of the senior staff, word for word. Mr. Worf is
  > "occasionally too direct". Commander Riker's is worse. Those evaluations exist only in
  > the restricted archive, which sits on the same system as the public mission reports. I
  > have asked Mr. Crusher to find out how Lore got from one into the other.
- Hints (Data):
  1. `GET /archive` lists four public mission reports. `download` takes a file name and combines it with `/data/archive`. The file name is used exactly as you send it.
  2. In a file path, `..` means the directory above. `/data/restricted` is one level up from the public archive, and then down again.
  3. I also note that `Path.Combine` discards the first path entirely if the second one starts with `/`. There are, therefore, two ways into this particular tube.
- Calling card (Lore):
  > Picard thinks Riker is "occasionally reckless". Picard is right, and I'd never tell him.
  > Isn't it nice to know what people really think, Wesley? Next I'd like to know what they
  > want. Since the refit, the replicators keep mixing up orders. Everyone gets someone
  > else's dinner. I wonder what else comes along with it. — L.
- Barclay lines:
  - `full-path-check`: "W-we could work out the real path first, and then check it's still inside the archive? Like checking where the tube comes out, not where it goes in."
  - `strip-dot-dot`: "The dots are the problem, sir. We t-take out every `../`. No dots, no climbing."
  - `require-pdf`: "It's a PDF archive. So w-we only hand out PDFs. Anything else gets refused. Th-that's tidy, at least."
  - `reject-leading-slash`: "Absolute paths are the scary ones. I'll just refuse anything that starts with a slash. Th-then it has to stay in the archive."
- Debrief:
  > Well done, Mr. Crusher. The archive now checks every path before it reads a file. I am
  > told you also read your own evaluation. I trust it will not go to your head. Mr. Worf
  > has asked to see his as well. I said no. Directly.

### 3.2 `shared-state`: Crossed Beams
- Beat: crew members receive each other's replicator orders, and with Riker's order comes his command code.
- Calling card points to: a clearance check that doesn't check signatures (→ 3.3).
- Briefing:
  > Captain's log, stardate 47330.5. The replicators have developed a sense of humour.
  > Counselor Troi ordered a chocolate sundae and received Mr. Worf's gagh. Mr. Worf did not
  > find it funny. Worse, Commander Riker reports that someone placed an order with his
  > command code, and it was not him. Since the refit, the replicator panel runs on a new
  > system. I have asked Mr. Crusher to find out what it is sharing, and with whom.
- Hints (Data):
  1. `GET /replicator/order` should show your own order. Please call it a few times over the next minute. I have observed that the field `orderedBy` does not always contain your name.
  2. `ReplicatorOrderState` is registered as a singleton. In Blazor Server, that means a single instance for every user of the application, for as long as the server runs.
  3. Commander Riker places an off-ration order every few seconds and authorises it with his command code. If you look at your panel just after he does, you will see what he sees.
- Calling card (Lore):
  > Commander Riker's command code, Wesley. And his dinner: eggs, extra spicy. The man has
  > no shame. Codes are nice, but I've seen how the bridge console checks clearance. It reads
  > your badge and believes every word. It never asks who signed it. I think I'll write
  > myself a new one. — L.
- Barclay lines:
  - `scoped-state`: "M-maybe each crew member gets their own copy? Scoped, I think it's called. I always mix up the lifetimes. S-sorry."
  - `add-lock`: "It's a threading problem, sir. Obviously. I'll put a `lock` around it. Locks fix threading problems. U-usually."
  - `clear-after-order`: "We c-could wipe the code as soon as the order is delivered. Then there's nothing left to see. Th-the window is tiny."
  - `immutable-record`: "What if the order can't be changed at all? A record. Immutable. Nobody can mess with it. Th-that's the modern way."
- Debrief:
  > Good work, Mr. Crusher. Every crew member now has their own replicator panel, and
  > nobody sees anyone else's order. Commander Riker has a new command code, and Counselor
  > Troi has her sundae. Mr. Worf has asked me to find out who received his gagh. I have
  > decided not to.

### 3.3 `jwt`: Forged Clearance
- Beat: Lore logs into the bridge console as "Admiral Lore", an admiral who doesn't exist.
- Calling card points to: the core fetching anything it's asked to (→ 3.4).
- Briefing:
  > Captain's log, stardate 47331.8. At 0412 hours an Admiral Lore logged into the bridge
  > console with full clearance and tried to take the helm. Starfleet has no Admiral Lore.
  > The console checks a clearance badge, a signed token every officer receives at login.
  > Lore's badge was accepted without question. I have asked Mr. Crusher to find out what
  > exactly the console is checking.
- Hints (Data):
  1. Your token has three parts separated by dots: a header, a payload and a signature. The first two are merely Base64url-encoded JSON. The JWT editor below the console decodes them.
  2. Changing the payload invalidates the signature. However, the configuration sets `RequireSignedTokens = false`. A token without any signature is therefore not checked at all.
  3. Set `"role": "admin"` in the payload and `"alg": "none"` in the header, remove the signature, use the token and call `GET /admin/bridge`. I would recommend keeping a modest rank. Lore did not.
- Calling card (Lore):
  > An admiral at last, and the chair is even more comfortable than it looks. You can have
  > the console back, Wesley. I've found something better. The core fetches link previews for
  > every subspace message, from anywhere I ask. Anywhere at all. Even from inside itself. — L.
- Barclay lines:
  - `require-signed`: "W-we could tell the console to insist on a signature. Our key, our algorithm, our issuer. That's a lot of settings. I-I'd better double-check them all."
  - `reject-alg-none`: "The trick is `alg: none`, sir. So we block `none`. N-no more none."
  - `shorter-lifetime`: "If badges only last five minutes, a fake one is useless after five minutes. R-right? That's how it works?"
  - `role-from-db`: "We don't trust the role in the badge. We l-look it up in the crew database instead. By the name on the badge."
- Debrief:
  > Good work, Mr. Crusher. The bridge console now accepts only badges signed with our key,
  > issued by us, for this ship. Admiral Lore's clearance has been revoked. I am told your own
  > forged badge still said "acting ensign". Modest. I approve.

### 3.4 `ssrf`: Ping the Core
- Beat: the Ferengi trader DaiMon Tarr offers to sell back the core credentials, which Lore pulled through the core's own link preview.
- Calling card points to: the Ten Forward comment board, which Lore now moderates (→ 3.5).
- Briefing:
  > Captain's log, stardate 47333.0. A Ferengi trader, DaiMon Tarr, is offering to sell us our
  > own core access credentials. For a reasonable fee. He received them in a subspace message
  > signed by Lore. Those credentials are not stored on any console; only the core itself can
  > read them. I have asked Mr. Crusher to find out how Lore made the core hand them over. I
  > have asked Mr. Worf not to reply to the Ferengi.
- Hints (Data):
  1. `POST /link-preview` fetches whatever URL you give it, from inside the ship's network, and reports the title and the first few hundred characters. With a public page it works as expected.
  2. From inside the ship's network, the core can reach hosts that you cannot, for example `http://inventory.internal/`. I find it noteworthy that the preview makes no distinction.
  3. Like many cloud systems, the core serves its own credentials on a link-local metadata address, `http://169.254.169.254/latest/`. It answers anyone who asks from inside. The link preview asks from inside.
- Calling card (Lore):
  > The Ferengi pay well, Wesley, but I don't need their money. I needed you all looking the
  > other way. While you were busy with the core, I took a little job in Ten Forward. I moderate
  > the comment board now, and I read every word your crew writes. Every single one. Write me
  > something nice. — L.
- Barclay lines:
  - `allowlist-resolve`: "M-maybe the preview only visits sites we know? A short list. And we check where the name really points, b-before we go there?"
  - `string-blocklist`: "We just block the dangerous addresses. `localhost`, `127.0.0.1`, the metadata one. I-I wrote them all down. All of them, I think."
  - `require-https`: "The metadata service only speaks plain http. So if we only allow https, it can't be reached. Th-that's clever, right? It feels clever."
  - `title-only`: "W-what if we only show the title? No content at all. Credentials don't have a title. Do they?"
- Debrief:
  > Well done, Mr. Crusher. The link preview now fetches only approved public sites, and it
  > checks where they really lead first. The core credentials have been changed. DaiMon Tarr has
  > lowered his price twice. Mr. Worf has drafted a reply. I have not sent it.

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
