# Writing a lab

A lab (shown to players as a "mission") is one folder with one class. This guide
walks through adding one, for example the missing 2.3 `brute-force`.

## 1. Story first

Write the text in `docs/story.md` (English) and `docs/story.de.md` (German) first.
Follow "How to read this file" at the top of `story.md` and the writing rules at the
top of `story.de.md`. You need:

- a **briefing**: a Captain's log from Picard, at most 100 words, without giving away the exploit
- three **hints** from Data: precise, formal, no contractions, each a bit more concrete
- a **calling card** from Lore that points at the next lab
- a **Barclay line** for every fix option, the correct one too, all equally unsure
- a **debrief** from Picard, at most 60 words

## 2. The lab class

Create `src/Trainer.Web/Labs/<Name>/<Name>Lab.cs` and implement `ILab`
(`Core/ILab.cs`). `Labs/Bfla/BflaLab.cs` is a small, complete example to copy from.

- `Slug`: kebab-case, also the API prefix (`/api/labs/{slug}`) and the page URL.
- `Story`: `Act` and `Order` must be unique across labs; they decide the place on the home page.
- `ScenarioMarkdown`: what the system does, a table of endpoints, the vulnerable code,
  and a bold **Goal** line that says where the flag is.
- `DefaultRequest`: what the request console starts with, usually a harmless request
  with Wesley's token: `auth.GetToken(Slug, DemoAuth.Player)`.
- `FixOptions`: at least three. Exactly one has `IsCorrect: true`. Wrong options must
  be mistakes real developers make. `Id` is a short kebab-case name of the approach.
- `Tools`: optional extra tools (`LabTool.JwtEditor`, `LabTool.ModeratorView`).
- Every player-facing string is a `Text(En, De)`. Code snippets stay English.
- Endpoint code comes in two styles, Minimal API and controller; players pick one with
  the switch on the lab page (display only, the lab's real endpoints don't change).
  In Markdown, write each endpoint snippet twice, as a `` ```csharp minimal `` block
  followed by a `` ```csharp controller `` block. Untagged blocks show in both styles.
  A fix option that maps an endpoint sets `ControllerCode` as well. Keep prose
  style-neutral. The contract tests check that both styles are there.

## 3. Endpoints and data

In `MapEndpoints`, map the lab's endpoints on the group you are given (it is already
under `/api/labs/{slug}`), then call `Reset()`.

- Authenticate with `auth.FindUser(Slug, request)`; return `Results.Unauthorized()` if it is null.
- Read the language with `Languages.FromRequest(request)` and return lab data in it.
- Put `flags.GetFlag(Slug)` only where a successful exploit reveals it.
- Keep all state in the lab and guard it with a `Lock`. `Reset()` restores the start state.
- Never touch the host: use `Core/Simulation/VirtualFileSystem` for files and
  `Core/Simulation/FakeNetwork` for outbound HTTP. A victim bot is a method (or a
  `Timer`, see `SharedStateLab`) inside the lab. A lab that owns a timer or a
  connection implements `IDisposable`.
- Mark the vulnerable endpoint with a `// VULNERABLE:` comment.

## 4. Register it

Add one line to the array in `Core/LabRegistry.cs`.

## 5. Tests

Add `tests/Trainer.Web.Tests/<Name>LabTests.cs` deriving from `LabTestBase`
(see `BflaLabTests.cs`): the exploit returns the flag, normal use still works,
anonymous callers are rejected, and `Reset()` restores the data. The contract tests
in `LabContractTests.cs` check story completeness, both languages and the fix options
for every registered lab automatically.

## 6. Check by hand

`dotnet run --project src/Trainer.Web`, open the mission, and play it once in each
language: exploit, flag, all fix options, debrief.
