# Security Trainer

Learn common vulnerabilities in .NET, Blazor and Web APIs by exploiting them in
the browser. Each mission is a chapter of a short Star Trek story: you play Wesley
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

The smoke test plays all 12 missions in a real browser, in English and German, and catches
what unit tests can't, such as a page that crashes when it re-renders. It runs through
Claude Code with the Playwright MCP server: ask Claude for a smoke test, or run the
`/smoke-test` skill (`.claude/skills/smoke-test/SKILL.md`). The playthrough itself is
`tests/smoke/playthrough.js`.

## Terms

One word everywhere: a **mission** is one exercise, with its story, vulnerable API, flag,
fix options and tests. The UI, the page addresses (`/missions/{slug}`), the API paths
(`/api/missions/{slug}/...`) and the code (`IMission`, `MissionRegistry`, `Missions/<Name>/`)
all use it.

## Layout

- `src/Trainer.Web/`: the Blazor Web App and the vulnerable endpoints (`/api/missions/{slug}`)
  - `Core/`: mission contract, registry, flags, demo auth, progress
  - `Components/`: home page, mission page, request console, quiz
  - `Core/Story/`: act titles, opening scene, act endings, finale
  - `Core/Simulation/`: virtual filesystem and fake network, so missions never touch the host
  - `Components/Tools/`: extra mission tools (JWT editor, moderator view)
  - `Missions/<Name>/`: one folder per mission; `<Name>Mission.cs` has the endpoints and data,
    `<Name>Mission.Text.cs` the story, scenario, hints, fix options and explanation
- `tests/Trainer.Web.Tests/`: xUnit tests using `WebApplicationFactory`
- `docs/story-guide.md`: premise, cast, the arc and the writing rules (English and German)
- `docs/writing-a-mission.md`: how to add a mission

Progress is stored in your browser's `localStorage`. Use **Reset** on a mission to start it over.
It belongs to the exact address you opened: `localhost:5000`, `127.0.0.1:5000` and another
port each start from zero, and so does a private window. Stick to one address.
Mission data lives in the running app and is shared by everyone who uses that instance, so
**Reset** also resets the mission for them. Each player should run their own copy.

## Known limitations

These are deliberate, to keep the missions playable and the app light:

- **1.3 returns raw SQL errors**, so players see that their quote broke the query. The
  explanation says why real apps shouldn't.
- **3.3 simulates JWT validation** instead of using the JwtBearer package. It behaves like
  `RequireSignedTokens = false`: an empty signature is accepted, a non-empty one must verify.
- **3.5 runs the JavaScript players write** in a sandboxed iframe (`allow-scripts` only, no
  same-origin), so it can't touch the app. It can be cheated with the browser's developer
  tools, because the moderator frame's URL and `modToken` are visible there. A real fix
  would need a separate origin.

## License

The code is [MIT licensed](LICENSE). highlight.js and the Antonio font come with their own
licenses in `src/Trainer.Web/wwwroot/lib/`.

Star Trek and its characters belong to their rights holders (Paramount). This is a
non-commercial fan project for teaching, not affiliated with or endorsed by them.
