# Security Trainer

Learn common vulnerabilities in .NET, Blazor and Web APIs by exploiting them in
the browser. Each lab is a chapter of a short Star Trek story: you play Wesley
Crusher, chasing Lore through the Enterprise's freshly refitted computer core.

> ⚠️ This app is **deliberately vulnerable**. It binds to `localhost` only.
> Never deploy it or expose it to a network.

## Run

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a modern browser.

```bash
git clone <repo> && cd securityTrainer
dotnet run --project src/Trainer.Web   # http://localhost:5000
```

### Without the .NET SDK (Docker or Podman)

```bash
docker build -t security-trainer .
docker run --rm -p 127.0.0.1:5000:5000 security-trainer   # http://localhost:5000
```

Keep the `127.0.0.1:` prefix. Without it, Docker opens the port to your whole network, and
this app is deliberately vulnerable. Options work the same way as with `dotnet run`, e.g.
`docker run --rm -p 127.0.0.1:5000:5000 security-trainer --Trainer:StoryMode=false`.
`podman` takes the same commands.

Missions open one after another (story mode). Players can switch to free play on the
mission overview. For a workshop where everyone should start in free play, run with
`--Trainer:StoryMode=false`.

## Test

```bash
dotnet test
```

## Layout

- `src/Trainer.Web/`: the Blazor Web App and the vulnerable lab endpoints (`/api/labs/{slug}`)
  - `Core/`: lab contract, registry, flags, demo auth, progress
  - `Components/`: home page, lab page, request console, quiz
  - `Core/Story/`: act titles, opening scene, act endings, finale
  - `Core/Simulation/`: virtual filesystem and fake network, so labs never touch the host
  - `Components/Tools/`: extra lab tools (JWT editor, moderator view)
  - `Labs/<Name>/`: one folder per lab
- `tests/Trainer.Web.Tests/`: xUnit tests using `WebApplicationFactory`
- `SPEC.md`: the full specification
- `docs/story.md`: the story outline; `docs/story.de.md`: the German version
- `docs/writing-a-lab.md`: how to add a lab
- `docs/open-decisions.md`: choices made during development that are still open

Progress is stored in your browser's `localStorage`. Use **Reset** on a lab to start it over.
