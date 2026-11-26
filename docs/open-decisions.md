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
