---
name: smoke-test
description: Play all 12 missions in a real browser (English and German) with the Playwright MCP server and report what broke. Use before showing the trainer to people, after UI or mission changes, or when asked for a smoke test or a playthrough.
---

# Smoke test: play the whole game in the browser

`tests/smoke/playthrough.js` plays every mission the way a player would: request
console, Repeater, JWT editor, flag, fix quiz. It clears the browser's saved progress,
plays in story mode, records page crashes (Blazor's "unhandled exception on the current
circuit") and every other browser console error or warning (`consoleIssues`), and returns
one result per mission. A clean run has no crashes and no console issues. A mission that
fails switches the run to free play, so the rest still gets tested. One run takes about a
minute.

The unit tests (`dotnet test`) don't render pages twice, so they miss bugs like a
component that crashes on re-render. This test catches them.

## Prerequisites

The Playwright MCP server must be connected (tools named `mcp__playwright__*`). If it
isn't, tell the user to run these, then restart Claude Code and continue:

```
claude mcp add playwright -- npx -y @playwright/mcp@latest --browser chromium
npx -y playwright install chromium
```

It runs in its own browser profile, so clearing progress never touches the user's own browser.

## Steps

1. **Start the app** on `http://localhost:5000`, unless something already answers there
   (`curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/`). Prefer the current
   source, in the background, with detailed errors:
   ```
   dotnet build -warnaserror && dotnet run --project src/Trainer.Web --no-build -- --DetailedErrors=true > <scratchpad>/smoke-server.log 2>&1
   ```
   To test the Docker image instead: `podman build -t security-trainer .` and
   `podman run -d --rm --name st-smoke -p 127.0.0.1:5000:5000 security-trainer`.
2. **English run:** `browser_navigate` to `http://localhost:5000/?lang=en`, then
   `browser_run_code_unsafe` with `filename: tests/smoke/playthrough.js`.
3. **German run:** the same with `?lang=de`.
4. **Report** to the user as a short table per language: mission, ok or failed, seconds,
   error. Name any crashes and console issues, and check the server log too:
   `grep -E '^(fail|crit|warn)' <scratchpad>/smoke-server.log` should find nothing.
5. **If something failed,** find the cause before guessing:
   - the server log (with `--DetailedErrors=true` it has the full exception),
   - the console log the Playwright tool points to under `.playwright-mcp/`,
   - a `browser_snapshot` of the page where it stopped.
6. **Clean up** what you started: `browser_close` first, then stop the server or container.
   In the other order the open page logs reconnect errors.

## When missions change

Each mission is one entry in the `missions` list in `tests/smoke/playthrough.js`: its
slug, the id of the correct fix option, and an `exploit` function that returns the flag.
When you add or change a mission, update its entry, and run the test once in each
language. UI labels are matched in both languages (the `L` object at the top).
