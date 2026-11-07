# Editor safety: many sessions, one Unity editor, a human playing inside it

Read this before any recompile, rebuild, reimport, scene open, screenshot eval, or file copy into `Assets/`. The scripts are in `scripts/` (next to `SKILL.md`); the in-project helper is `assets/unity/Editor/EditorStateBeacon.cs`.

## Contents
1. The problem
2. The pieces
3. Setup
4. The guard and its exit codes
5. The lock
6. A round, start to finish
7. Writing evals that clean up after themselves
8. When the editor seems frozen
9. Unverified parts of the scripts

## 1. The problem

The editor is one shared, stateful machine with a human inside it. Whatever one session does (recompile, import, rebuild, open a scene, render) changes what everyone else sees. Every incident in the source project came from treating it like a stateless build server:

- a recompile during the director's Play (twice in one day): NullReferenceExceptions in every Update;
- a file dropped into `Assets/` mid-Play: the domain reload killed every coroutine and froze the story at its climax;
- several sessions' queued rebuilds firing together when the editor came back, 13 recovery backups (96 MB), 40 s recompiles, 90 s to enter Play;
- a job looping "wait until clean" with no timeout while holding the editor for an hour;
- a builder that threw, an Untitled scene, a "save all", and an invisible native Save dialog freezing the editor for 17 minutes;
- a character import racing a chapter rebuild and baking an empty animator controller;
- checks that failed silently and read as passes.

The answer is four mechanisms: **a guard, a lock, a fixed round shape with a deadline, and evals that clean up after themselves.**

## 2. The pieces

| File | What it does |
|---|---|
| `scripts/guard.ps1`, `scripts/guard.sh` | Read-only gate: may this session touch the editor right now? Exit codes below |
| `scripts/playing.cs` | Eval the guard runs: playing / entering Play, paused, compiling, updating, compile failed, open scenes, dirty |
| `scripts/lock.ps1`, `scripts/lock.sh` | Take / refresh / release / status of `editor.lock` (or any named lock): atomic create, jitter, read-back, exact owner match, stale takeover with a message |
| `scripts/round.ps1` | The complete round (§6) with a 25-minute deadline and exit codes 10–14 |
| `scripts/hand_back.cs` | Saves scenes that have a path one by one (never `SaveOpenScenes`), then opens the light scene named in `.game-studio/hand_back.txt` |
| `scripts/refresh.cs` | `AssetDatabase.Refresh()` with a Play check |
| `scripts/review_shots.cs` | Spec-driven edit-mode shots (named cameras or `shot=` lines in `Temp/game-studio/review_shots.txt`), small batches, a flat/cyan sanity check per image |
| `scripts/dialog_watch.ps1` | Background watcher: clicks "Try Again" on "Opening file failed", closes a stray "Save Scene" dialog (cancel), lists Unity's windows when diagnosing a freeze |
| `assets/unity/Editor/EditorStateBeacon.cs` | In-project: writes `Temp/editor_state.txt` every 2 s (read by the guard in beacon mode and by the hook), and **pauses auto-refresh while playing**, so files that land in `Assets/` mid-Play wait until Play ends |
| `scripts/hooks/block_assets_while_playing.py` + `settings.example.json` | A Claude Code PreToolUse hook that refuses any write into `Assets/` while the beacon says Play; a permission allow-list for the guard, lock and CLI so sessions aren't stopped by prompts |

## 3. Setup

0. **How sessions reach the editor.** The scripts assume a command-line bridge into the running editor: the Unity CLI (`unity`) plus its editor package in the project, which lets a shell run `unity command <group> <cmd>` (run a C# snippet, recompile, invoke a menu item, read the console) against the open editor. An MCP bridge to the editor does the same jobs from inside Claude; then run the guard in **beacon mode** (`GAME_STUDIO_PLAY_PROBE=beacon`), which needs no CLI at all. Check your command names once (`unity command --help`); they changed between versions, so set `GAME_STUDIO_EVAL_CMD` if yours isn't `command script eval_file`.
1. Copy `scripts/` into the project as `tools/game-studio/` (outside `Assets/`). Environment for every session: `UNITY_PROJECT_PATH=<project root>`, `GAME_STUDIO_SESSION=<one ASCII token, e.g. ch3>`. Optional: `GAME_STUDIO_LOCK_DIR` (default `<project>/.game-studio/locks`, on a local disk), `GAME_STUDIO_EVAL_DIR` (where `playing.cs` etc. live; it must be readable by the Unity process, not a sandboxed temp folder), `UNITY_CLI`, `GAME_STUDIO_PLAY_PROBE` (`cli` or `beacon`), `GAME_STUDIO_EVAL_CMD`.
   On Windows, run the `.ps1` scripts as `powershell -NoProfile -ExecutionPolicy Bypass -File ...` (process scope only; it changes no system setting), or use the bash twins from Git Bash.
2. Add `.game-studio/locks/` to `.gitignore`; commit `.game-studio/hand_back.txt` naming the light scene.
3. Copy `EditorStateBeacon.cs` into an Editor folder.
4. In Unity's preferences, set script changes while playing to "recompile after finished playing".
5. Decide who may start the editor (one cheap custodian session). Everyone else reports a closed editor instead of opening their own. On "restore scene backup?", choose don't restore: scenes are generated from code.
6. Install the hard stop and the allow-list: merge `scripts/hooks/settings.example.json` into `.claude/settings.json` (adjust the paths) and restart the sessions. Now even a session that forgot every rule cannot write into `Assets/` while the director plays.
7. The custodian opens the editor (`unity open <project>` or the Hub) once, waits for the import to finish, and opens the light scene.

## 4. The guard and its exit codes

```powershell
& scripts/guard.ps1 ch3; if ($LASTEXITCODE -ne 0) { return }
```
```bash
scripts/guard.sh ch3 || exit
```

| Code | Meaning | Do |
|---|---|---|
| 0 | Not playing, no import running, editor lock free or yours | Proceed |
| 3 | Play mode or entering it: the director is playtesting | Do other work; come back later. Never stop the director's Play yourself; if a rebuild is urgent, ask them |
| 4 | State unknown: editor closed, CLI failed, eval timed out, modal dialog | Look before retrying (§8) |
| 5 | Import lock younger than 30 min: a global import is running | Wait |
| 6 | Another owner's editor lock younger than 20 min | Wait your turn |

Run it before every recompile, rebuild, reimport, `AssetDatabase.Refresh`, and **every copy into `Assets/`**. The rule protects a state ("the project does not change while the director plays"), not one action; written as "don't recompile", it let a texture drop through.

## 5. The lock

```powershell
& scripts/lock.ps1 -Action take -Name editor -Note "ch3 rebuild"   # bounded wait, then 0 or 1
& scripts/lock.ps1 -Action refresh -Name editor                      # before each long step; 1 = you lost it
& scripts/lock.ps1 -Action release -Name editor                      # only releases your own
```

- A lock not rewritten for 20 minutes is stale and may be taken over; so refresh it between steps (a 40-minute sequence once lost the editor halfway).
- Never wait while holding it. Wait first (bounded), then take, then work.
- Global operations that regenerate shared assets (importing all characters) use their own lock name (`import`) so the guard returns 5 to everyone else.
- Owner names are matched exactly (a prefix match once let `ch1` pass a lock held by `ch10`).

## 6. A round, start to finish

`scripts/round.ps1` does all of this; read it before writing your own.

```
wait for the lock (bounded, holding nothing) → take it, read it back
deadline = now + 25 min; start dialog_watch
bring in: refresh lock → guard → copy only changed files (top level of -StageDir only; retry 6×) → mirror deletes with .meta
refresh → settle: compiling=False and updating=False three polls in a row
compile failed? → list `error CS` from the console → STOP (never rebuild on old code)
build: refresh lock → guard → t = now → invoke the builder menu → settle
       → VERIFY the builder's "Built" line or the scene file written after t (the menu reply lies)
shots: refresh lock → write the spec → review_shots (small batch) → VERIFY PNG count and times > t
finally: hand_back (save named scenes, open the light scene) → release the lock if still yours → stop the watcher
```

Exit codes: 0 done; 1 no lock; 2 bad arguments or missing proof; 3 guard refused mid-round; 10 compile errors; 11 build not verified; 12 shots failed; 13 deadline or lost lock; 14 never settled.

Rules around it:
- Decide from your own eval replies and file times, never from the CLI's human-readable text: it is localized (a Chinese UI printed "个错误" for "errors"), and English matching fails silently.
- Killing a CLI call at its timeout does not stop work already running inside the editor; the hand-back queues behind it.
- A stopped background shell script can keep running. After stopping a job, check its output, the lock and file times before reporting it stopped. Queued jobs check a "still wanted?" marker right after taking the lock.
- Background jobs are checked often enough to notice a dead editor.

## 7. Writing evals that clean up after themselves

- An eval file is a C# statement body ending in `return "<string>";`, kept **outside** `Assets/`. No arguments: parameters go in a side file the eval reads. Fully qualify names.
- Any eval that changes something starts with `if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "skipped: playing";` (this also catches the moment Play is starting).
- Objects it creates are `HideAndDontSave` and destroyed in `finally`. Globals, editor settings (`ShaderUtil.allowAsyncCompilation`), skinning flags and bone poses are restored in `finally`. Report "dirty before / after".
- Check the active scene inside the eval: another session's hand-back may have switched it.
- Shots: async shader compilation off (flat cyan = still compiling); `forceMatrixRecalculationPerRender` on skinned meshes; sample Animators (`Rebind(); Update(t)`) or people stand in T-pose; one reused RenderTexture; wait for the GPU with a one-pixel `ReadPixels` (a looped `AsyncGPUReadback` crashed the editor); a few images per eval. Log a measured number beside each picture.
- Never `SaveOpenScenes` (an Untitled scene turns it into a modal dialog). Save scenes with a path, one by one.
- Never swap a scene file on disk while it is open (git checkout, copy): a modal "changed externally" dialog blocks everyone. Hand back first, then restore, then refresh.
- Copying over a model the editor has memory-mapped can fail (Win32 error 1224) and pop "Opening file failed"; copy only changed files, retry, and keep `dialog_watch` running.

## 8. When the editor seems frozen

Symptoms: every eval times out, the process shows "Responding" at ~0% CPU. Almost always a modal dialog.
1. `scripts/dialog_watch.ps1 -Mode List` to see Unity's windows; a `#32770` class window is a native dialog.
2. "Save Scene" → close it (WM_CLOSE = cancel, writes nothing). "Opening file failed" → Try Again.
3. If the permission system won't let you act on the process, don't work around it: put it first in the director's decisions file.
4. Afterwards: find who left the Untitled scene or the locked file, and fix the cause.

## 9. Unverified parts of the scripts

Tested against a stub CLI and a fake project (all guard and lock exit codes, stale locks, BOM/CRLF lock files, two takers at once, round success and failure paths); C# evals compile against Unity 6000.3. Not yet verified against a live editor: the eval wrapper's handling of `using` and non-string returns; `review_shots.cs` rendering inactive cameras under URP and its flat/cyan thresholds; `OpenScene(Single)` dropping dirty generated scenes without a dialog; `isPlayingOrWillChangePlaymode` during a real Play transition; `dialog_watch.ps1` against non-English editor dialogs; the bash scripts on macOS/Linux. Treat the first live run as a test and read every output.
