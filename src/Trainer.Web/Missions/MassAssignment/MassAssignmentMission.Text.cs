using System.Text.Json;
using Trainer.Web.Core;

namespace Trainer.Web.Missions.MassAssignment;

// Player-facing text of MassAssignmentMission: story, scenario, hints, fix options and explanation.
// The endpoints and data are in MassAssignmentMission.cs.
public sealed partial class MassAssignmentMission
{
    public Text Title => new("Field Promotion", "Beförderung im Feld");

    public MissionStory Story => new(
        Act: 2,
        Order: 1,
        Briefing: new(
            En: """
                Captain's log, stardate 47324.8. Since Lore's appearance, the crew database lists an
                Admiral L. Soong among my crew. No such officer exists, yet he has admin rights and
                access to Starfleet Command channels. The records say he was not created. He was
                promoted, from an ordinary crew account, through the profile page every crew member
                uses. I have asked Mr. Crusher to take a closer look at his own profile page.
                """,
            De: """
                Logbuch des Captains, Sternzeit 47324.8. Seit Lores Auftritt führt die Crew-Datenbank einen
                Admiral L. Soong in meiner Besatzung. Einen solchen Offizier gibt es nicht, und trotzdem hat
                er Admin-Rechte und Zugriff auf die Kanäle des Sternenflottenkommandos. Laut Protokoll wurde
                er nicht angelegt, sondern befördert, und zwar von einem ganz normalen Crew-Konto aus. Über
                die Profilseite, die jeder an Bord benutzt. Ich habe Mr. Crusher gebeten, sich seine eigene
                Profilseite einmal genauer anzusehen.
                """),
        CallingCard: new(
            En: """
                Congratulations, Admiral Crusher. It suits you. Almost. I've had my promotion for a day
                and I'm already bored with it, so I went shopping. Have you seen the quartermaster's new
                requisition app? So generous. It practically hands out the keys to Starbase supply. — L.
                """,
            De: """
                Glückwunsch, Admiral Crusher. Steht dir. Fast. Ich bin seit einem Tag befördert und find's
                schon langweilig, also war ich einkaufen. Kennst du die neue Bestell-App vom Quartiermeister?
                So großzügig. Die drückt einem den Schlüssel zum Lager der Sternenbasis praktisch in die
                Hand. — L.
                """),
        Debrief: new(
            En: """
                Good work, Mr. Crusher. The profile page now accepts a name and an email, and nothing
                else. Admiral Soong has been returned to his proper rank, which is none. I trust you
                have given back your own admiralty as well. Starfleet frowns on field promotions this large.
                """,
            De: """
                Gute Arbeit, Mr. Crusher. Die Profilseite nimmt jetzt nur noch Namen und E-Mail an, sonst
                nichts. Admiral Soong hat seinen rechtmäßigen Rang zurück, nämlich keinen. Ich gehe davon aus,
                dass Sie Ihren eigenen Admiralsrang auch wieder abgegeben haben. So große Beförderungen im
                Feld sieht die Sternenflotte nicht gern.
                """));

    private const string VulnerableCode = """
        ```csharp minimal
        public class User
        {
            public int Id { get; set; }
            public string? DisplayName { get; set; }
            public string? Email { get; set; }
            public bool IsAdmin { get; set; }
            public int Credits { get; set; }
        }

        app.MapPut("/profile", async (User input, ClaimsPrincipal principal, CrewDb db) =>
        {
            var user = await db.Users.FindAsync(principal.GetUserId());

            input.Id = user!.Id;                        // never change the key
            db.Entry(user).CurrentValues.SetValues(input);
            await db.SaveChangesAsync();

            return Results.Ok(user);
        })
        .RequireAuthorization();
        ```
        ```csharp controller
        public class User
        {
            public int Id { get; set; }
            public string? DisplayName { get; set; }
            public string? Email { get; set; }
            public bool IsAdmin { get; set; }
            public int Credits { get; set; }
        }

        [ApiController]
        [Route("profile")]
        [Authorize]
        public class ProfileController(CrewDb db) : ControllerBase
        {
            [HttpPut]
            public async Task<IActionResult> Update(User input)
            {
                var user = await db.Users.FindAsync(User.GetUserId());

                input.Id = user!.Id;                        // never change the key
                db.Entry(user).CurrentValues.SetValues(input);
                await db.SaveChangesAsync();

                return Ok(user);
            }
        }
        ```
        """;

    public Text ScenarioMarkdown => new(
        En: $"""
            Every crew member can edit their display name and email on the profile page. The Blazor
            page loads the profile, shows the two input fields, and sends the whole object back.

            | Endpoint | What it does |
            |---|---|
            | `GET /api/missions/mass-assignment/profile` | Your profile |
            | `PUT /api/missions/mass-assignment/profile` | Saves your profile (JSON body) |
            | `GET /api/missions/mass-assignment/crew` | Who is aboard, and who is an admin |
            | `GET /api/missions/mass-assignment/admin/secret` | The Starfleet Command channel (admins only) |

            You are logged in as `wesley`; your bearer token is already in the request console.
            Saving the profile looks like this:

            {VulnerableCode}

            **Goal:** make yourself an admin and open the Starfleet Command channel. The flag is
            its `authCode`.
            """,
        De: $"""
            Jedes Crewmitglied kann auf der Profilseite seinen Anzeigenamen und seine E-Mail-Adresse
            ändern. Die Blazor-Seite lädt das Profil, zeigt die beiden Eingabefelder an und schickt das
            ganze Objekt zurück.

            | Endpunkt | Funktion |
            |---|---|
            | `GET /api/missions/mass-assignment/profile` | Dein Profil |
            | `PUT /api/missions/mass-assignment/profile` | Speichert dein Profil (JSON-Body) |
            | `GET /api/missions/mass-assignment/crew` | Wer an Bord ist und wer Admin ist |
            | `GET /api/missions/mass-assignment/admin/secret` | Der Kanal des Sternenflottenkommandos (nur Admins) |

            Du bist als `wesley` angemeldet, dein Token steht schon in der Request-Konsole. So wird das
            Profil gespeichert:

            {VulnerableCode}

            **Ziel:** Mach dich selbst zum Admin und öffne den Kanal des Sternenflottenkommandos. Die Flag
            ist dessen `authCode`.
            """);

    public IReadOnlyList<Text> Hints =>
    [
        new("`GET /profile` returns five fields. The profile page lets you edit two of them. I note that the server does not appear to know which two.",
            "`GET /profile` liefert fünf Felder. Auf der Profilseite können Sie zwei davon bearbeiten. Mir fällt auf, dass der Server offenbar nicht weiß, welche zwei."),
        new("`SetValues` copies every property of the incoming object onto the stored user. It does not distinguish between fields the form shows and fields it does not.",
            "`SetValues` kopiert jede Eigenschaft des eingehenden Objekts auf den gespeicherten Benutzer. Dabei unterscheidet es nicht zwischen Feldern, die das Formular zeigt, und solchen, die es nicht zeigt."),
        new("You could test the theory by sending your full profile back with `PUT /profile` and `\"isAdmin\": true`, then calling `GET /admin/secret`. I would advise against changing `credits`. Guinan audits the bar tab.",
            "Sie könnten die Theorie prüfen, indem Sie Ihr vollständiges Profil mit `\"isAdmin\": true` per `PUT /profile` zurückschicken und danach `GET /admin/secret` aufrufen. Von einer Änderung der `credits` rate ich ab. Guinan prüft die Rechnungen in Zehn Vorne."),
    ];

    public IReadOnlyList<FixOption> FixOptions =>
    [
        new("dto-mapping", """
            public record UpdateProfileRequest(string DisplayName, string Email);

            app.MapPut("/profile", async (UpdateProfileRequest input, ClaimsPrincipal principal, CrewDb db) =>
            {
                var user = await db.Users.FindAsync(principal.GetUserId());

                user!.DisplayName = input.DisplayName;
                user.Email = input.Email;
                await db.SaveChangesAsync();

                return Results.Ok(new { user.DisplayName, user.Email });
            })
            .RequireAuthorization();
            """, IsCorrect: true,
            new("Correct. The endpoint binds to a request type that only has the two editable fields and copies " +
                "exactly those. `isAdmin` and `credits` in the body are simply ignored, and fields added to `User` later stay safe too.",
                "Richtig. Der Endpunkt bindet an einen eigenen Request-Typ, der nur die beiden bearbeitbaren Felder hat, und übernimmt genau diese. `isAdmin` und `credits` im Body werden einfach ignoriert, und Felder, die später zu `User` dazukommen, sind auch geschützt."),
            BarclayLine: new(
                "C-could the profile page get its own little class? Just name and email? It's a lot of typing for two fields, sir.",
                "K-könnte die Profilseite eine eigene kleine Klasse kriegen? Nur Name und E-Mail? Ist ziemlich viel Tipparbeit für zwei Felder, Sir."),
            ControllerCode: """
            public record UpdateProfileRequest(string DisplayName, string Email);

            [ApiController]
            [Route("profile")]
            [Authorize]
            public class ProfileController(CrewDb db) : ControllerBase
            {
                [HttpPut]
                public async Task<IActionResult> Update(UpdateProfileRequest input)
                {
                    var user = await db.Users.FindAsync(User.GetUserId());

                    user!.DisplayName = input.DisplayName;
                    user.Email = input.Email;
                    await db.SaveChangesAsync();

                    return Ok(new { user.DisplayName, user.Email });
                }
            }
            """),

        new("reject-isadmin", """
            app.MapPut("/profile", async (User input, ClaimsPrincipal principal, CrewDb db) =>
            {
                if (input.IsAdmin)
                    return Results.BadRequest("You cannot make yourself an admin.");

                var user = await db.Users.FindAsync(principal.GetUserId());

                input.Id = user!.Id;
                db.Entry(user).CurrentValues.SetValues(input);
                await db.SaveChangesAsync();

                return Results.Ok(user);
            })
            .RequireAuthorization();
            """, IsCorrect: false,
            new("A blocklist of one. `credits` is still writable, and every field added to `User` later is exposed the moment it exists. " +
                "Worse, a real admin who saves their profile is now rejected, because their own `isAdmin` is true.",
                "Eine Sperrliste mit genau einem Eintrag. `credits` lässt sich weiterhin ändern, und jedes Feld, das später zu `User` dazukommt, ist sofort angreifbar. Außerdem kann jetzt kein echter Admin mehr sein Profil speichern, weil sein eigenes `isAdmin` true ist."),
            BarclayLine: new(
                "We just say no if someone sends `isAdmin: true`. That's the dangerous one. I-it's the only one, isn't it?",
                "W-wir sagen einfach nein, wenn jemand `isAdmin: true` schickt. Das ist doch das gefährliche Feld. D-das einzige, oder?"),
            ControllerCode: """
            [ApiController]
            [Route("profile")]
            [Authorize]
            public class ProfileController(CrewDb db) : ControllerBase
            {
                [HttpPut]
                public async Task<IActionResult> Update(User input)
                {
                    if (input.IsAdmin)
                        return BadRequest("You cannot make yourself an admin.");

                    var user = await db.Users.FindAsync(User.GetUserId());

                    input.Id = user!.Id;
                    db.Entry(user).CurrentValues.SetValues(input);
                    await db.SaveChangesAsync();

                    return Ok(user);
                }
            }
            """),

        new("remove-form-field", """
            @* Profile.razor *@
            <EditForm Model="profile" OnValidSubmit="SaveAsync">
                <InputText @bind-Value="profile.DisplayName" />
                <InputText @bind-Value="profile.Email" />
                @* removed: <InputCheckbox @bind-Value="profile.IsAdmin" /> *@
                <button type="submit">Save</button>
            </EditForm>
            """, IsCorrect: false,
            new("The form is not the API. You never used the form in this mission: you sent the JSON yourself, and the " +
                "server still copies whatever arrives.",
                "Das Formular ist nicht die API. Du hast das Formular in dieser Mission nie benutzt, sondern das JSON selbst geschickt, und der Server übernimmt weiterhin alles, was ankommt."),
            BarclayLine: new(
                "I'll t-take the admin checkbox out of the form. Completely. Not just hidden. G-gone.",
                "Ich n-nehme die Admin-Checkbox aus dem Formular. Komplett, nicht nur versteckt. W-weg."),
            Language: "razor"),

        new("validation-attributes", """
            public class User
            {
                public int Id { get; set; }
                [Required, StringLength(60)] public string? DisplayName { get; set; }
                [Required, EmailAddress] public string? Email { get; set; }
                public bool IsAdmin { get; set; }
                public int Credits { get; set; }
            }
            """, IsCorrect: false,
            new("Validation checks that values are well-formed, not whether you may set them. `\"isAdmin\": true` is a perfectly " +
                "valid boolean, so it passes and gets copied.",
                "Validierung prüft, ob Werte gültig aussehen, aber nicht, ob du sie setzen darfst. `\"isAdmin\": true` ist ein völlig gültiger Boolean, kommt also durch und wird übernommen."),
            BarclayLine: new(
                "M-maybe we add validation? `[Required]`, `[EmailAddress]`. Validation is always good, isn't it?",
                "V-vielleicht bauen wir Validierung ein? `[Required]`, `[EmailAddress]`. Validierung ist doch immer gut, oder?")),
    ];

    public Text ExplanationMarkdown => new(
        En: """
            **Mass assignment** (OWASP calls it *Broken Object Property Level Authorization*)
            happens when an endpoint binds the request body straight to a domain object and
            saves it. The form only showed name and email, but the server accepted every
            property of `User`, including `IsAdmin`. You only had to send it.

            The fix is to bind to a type made for this one request (a DTO such as
            `UpdateProfileRequest`) and copy only the fields the user may change. Do the same for
            responses: return a DTO, not the entity, so new internal fields don't leak either.

            Why the other options fail:
            - **Rejecting `isAdmin: true`** is a blocklist: `credits` and every future field stay writable.
            - **Removing the form field** changes the UI, not the API.
            - **Validation attributes** check format, not permission.

            Read more:
            - [OWASP API3:2023 Broken Object Property Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa3-broken-object-property-level-authorization/)
            - [OWASP Mass Assignment Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html)
            """,
        De: """
            **Mass Assignment** (bei OWASP *Broken Object Property Level Authorization*) entsteht,
            wenn ein Endpunkt den Request-Body direkt an ein Domänenobjekt bindet und speichert. Das
            Formular hat nur Name und E-Mail gezeigt, aber der Server hat jede Eigenschaft von `User`
            angenommen, auch `IsAdmin`. Du musstest sie nur mitschicken.

            Die Lösung: Binde an einen Typ, der nur für diesen einen Request gedacht ist (ein DTO wie
            `UpdateProfileRequest`), und übernimm nur die Felder, die der Benutzer ändern darf. Bei
            Antworten gilt dasselbe: Gib ein DTO zurück und nicht die Entität, dann sickern auch neue
            interne Felder nicht durch.

            Warum die anderen Optionen nicht helfen:
            - **`isAdmin: true` ablehnen** ist eine Sperrliste: `credits` und jedes künftige Feld bleiben änderbar.
            - **Das Formularfeld entfernen** ändert die Oberfläche, aber nicht die API.
            - **Validierungsattribute** prüfen das Format, aber nicht die Berechtigung.

            Mehr dazu:
            - [OWASP API3:2023 Broken Object Property Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa3-broken-object-property-level-authorization/)
            - [OWASP Mass Assignment Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html)
            """);
}
