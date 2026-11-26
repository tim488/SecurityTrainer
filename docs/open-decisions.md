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

## Not built yet

- Missions 2.3 `brute-force` through 3.5 `blazor-xss`, the Act 2 and Act 3 endings, and
  the finale. Also the tools they need: Repeater (2.3), JWT editor (3.3), moderator
  view (3.5), and the victim bots.
- `docs/writing-a-lab.md` (SPEC §7).
