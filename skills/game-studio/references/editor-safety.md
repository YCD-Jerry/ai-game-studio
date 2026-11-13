# Shared Unity editor safety

Use this workflow when several sessions can change the same open Unity project or a person is playtesting in it. For automated PlayMode tests and builds, prefer a separate project copy with its own `Library`, test saves, and output. A running editor can continue a command after the client times out.

## Install and check

From the skill folder, with the target project closed and Python 3.10 or later:

```text
python scripts/setup_unity.py install --project <project> --editor-closed
python scripts/setup_unity.py doctor --project <project>
```

The installer copies the optional tools and beacon. It reports differing files for review. In an existing installation, update all participating sessions and wrappers together with the editor closed and the old jobs stopped. Older lock records remain blocked until recovered. Add `.game-studio/locks/` to the game's ignore rules. Keep that directory on a local disk, outside synchronized storage, and use the same directory in every cooperating session. Never remove its `coordination.mutex` while clients may be using it.

The current verification covers Windows PowerShell and Git Bash sharing locks, three concurrent contenders, and a live Unity 6000.3.27f1 beacon through Edit and Play. The CLI probe is covered by stub tests. Live CLI bridge combinations, the complete round with editor mutations, and native Linux and macOS runs still need verification.

Open the project, let import finish, and run `doctor` again. The default beacon needs no CLI. For a project with a working live Unity CLI bridge, set `GAME_STUDIO_PLAY_PROBE=cli` and validate the probe against the actual editor. Set `UNITY_CLI` if the executable is off PATH. Set `GAME_STUDIO_EVAL_CMD` if the bridge registers a different eval command. The Windows `round.ps1` workflow still needs a compatible CLI to execute its editor actions.

## Guard and hook

Set `UNITY_PROJECT_PATH` to the project root and give each worker a distinct `GAME_STUDIO_SESSION` token. Use `GAME_STUDIO_LOCK_DIR` only when all workers share the same override. Use `GAME_STUDIO_PYTHON` when the wrappers need a specific Python executable. Run the guard directly before each copy into `Assets`, package or project settings change, refresh, recompile, scene open, or editor eval. Mutation code that runs inside Unity should also check editor state immediately before its action.

```text
python tools/game-studio/editor_guard.py guard
```

`guard.ps1 [owner]` and `bash guard.sh [owner]` use the same implementation.
Use the direct Python command or Bash if local PowerShell policy blocks scripts.

| Exit | Condition |
|---|---|
| 0 | Idle Edit mode and cooperative locks are available to this session |
| 3 | Playing or entering Play |
| 4 | State unavailable, expired, malformed, ambiguous or timed out |
| 5 | Another session holds the import lock |
| 6 | Another session holds the editor lock |
| 7 | Compiling, importing, or reloading |

Every nonzero result stops the pending editor mutation. The beacon publishes state at Play transitions and every two seconds. A sample older than ten seconds is unknown. The guard is a point-in-time check. A user can start Play between the check and the action. Coordinate the edit window, and use an isolated project for work that needs a guarantee against that race.

The optional Claude Code hook in `scripts/hooks/settings.example.json` applies to the host's matched tool calls. It checks direct edits of `Assets`, `Packages`, and `ProjectSettings`. It gates shell commands unless they match a small read-only allowlist, because shell programs can write through relative paths and subprocesses. Missing state blocks gated calls, including with the editor closed. Work outside the configured project or use a separate checkout for offline changes. Merge the example manually only where this policy fits. Tools outside the host's matcher, external processes and other editors can still change the project. The hook adds no tool permissions. Unity's beacon also delays automatic refresh in Play. Explicit Unity operations still need their own state check.

## Lock, heartbeat and recovery

Acquire the cooperative lock before any shared editor or global import operation. Use either `editor` or `import` according to the operation. Both lock names participate in the same acquisition transaction and cannot overlap. Status is available while another worker owns the lock.

```text
python tools/game-studio/editor_lock.py take --owner chapter-3 --wait-minutes 2 --note "Rebuild scene"
python tools/game-studio/editor_lock.py refresh --owner chapter-3
python tools/game-studio/editor_lock.py release --owner chapter-3
python tools/game-studio/editor_lock.py status
```

The PowerShell and Bash lock wrappers preserve the familiar take, refresh, release, and status commands. Refresh between bounded steps. For automated rounds, pass `--holder-pid` with the same long-lived local worker PID to take, refresh, and release, or let `round.ps1` do it. A heartbeat older than the configured diagnostic threshold (20 minutes for editor, 30 for import) prompts inspection. Age alone never releases a lock. A stopped client can leave work queued in Unity.

If a job crashed or a legacy lock remains, inspect its status and the Unity console. Verify no relevant job is running or queued. Use the record fingerprint printed by status to recover that exact version under the same OS mutex:

```text
python tools/game-studio/editor_lock.py recover --name editor --fingerprint <value> --confirm-no-work
```

Recovery checks the recorded PID when present and refuses an alive or indeterminate PID. It also requires idle Edit mode. For a verified closed project, add `--editor-closed`; the command refuses evidence of an open editor. For a record with no PID, confirm that its owner has stopped before recovering. If the fingerprint changed, inspect again. A PID can be reused. Recovery treats a reused live PID conservatively, so confirm the original process is gone before planning how to clear the lock.

## A bounded round

`scripts/round.ps1` does all of this; read it before writing your own.

```
wait for the lock (bounded, holding nothing) → acquire it
deadline = now + 25 min; opt in to dialog_watch only with the target editor PID
bring in: refresh lock → guard → copy only changed files (top level of -StageDir only; retry 6×) → mirror deletes with .meta
refresh → settle: compiling=False and updating=False three polls in a row
compile failed? → list `error CS` from the console → STOP (never rebuild on old code)
build: refresh lock → guard → t = now → invoke the builder menu → settle
       → VERIFY the builder's "Built" line or the scene file written after t (the menu reply lies)
shots: refresh lock → write the spec → review_shots (small batch) → VERIFY PNG count and times > t
finally: check guard → hand back when safe → release on known completion; retain lock after uncertain CLI failure
```

Exit codes: 0 done; 1 no lock; 2 bad arguments or missing proof; 3 guard refused mid-round; 10 compile errors; 11 build not verified; 12 shots failed; 13 deadline or lost lock; 14 never settled.

Rules around it:
- Decide from your own eval replies and file times, never from the CLI's human-readable text: it is localized (a Chinese UI printed "个错误" for "errors"), and English matching fails silently.
- Killing a CLI call at its timeout does not stop work already running inside the editor; the hand-back queues behind it.
- A stopped background shell script can keep running. After stopping a job, check its output, the lock and file times before reporting it stopped. Queued jobs check a "still wanted?" marker right after taking the lock.
- Background jobs are checked often enough to notice a dead editor.

## Writing evals that clean up after themselves

- An eval file is a C# statement body ending in `return "<string>";`, kept **outside** `Assets/`. No arguments: parameters go in a side file the eval reads. Fully qualify names.
- Any eval that changes something starts with `if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "skipped: playing";` (this also catches the moment Play is starting).
- Objects it creates are `HideAndDontSave` and destroyed in `finally`. Globals, editor settings (`ShaderUtil.allowAsyncCompilation`), skinning flags and bone poses are restored in `finally`. Report "dirty before / after".
- Check the active scene inside the eval: another session's hand-back may have switched it.
- Shots: async shader compilation off (flat cyan = still compiling); `forceMatrixRecalculationPerRender` on skinned meshes; sample Animators (`Rebind(); Update(t)`) or people stand in T-pose; one reused RenderTexture; wait for the GPU with a one-pixel `ReadPixels` (a looped `AsyncGPUReadback` crashed the editor); a few images per eval. Log a measured number beside each picture.
- Never `SaveOpenScenes` (an Untitled scene turns it into a modal dialog). Save scenes with a path, one by one.
- Never swap a scene file on disk while it is open (git checkout, copy): a modal "changed externally" dialog blocks everyone. Hand back first, then restore, then refresh.
- Copying over a model the editor has memory-mapped can fail (Win32 error 1224) and pop "Opening file failed". Copy only changed files and use bounded retries. The optional Windows watcher can retry the dialog for a specific editor PID.

## When the editor seems frozen

Symptoms: every eval times out, the process shows "Responding" at ~0% CPU. Almost always a modal dialog.
1. `scripts/dialog_watch.ps1 -Mode List` to see Unity's windows; a `#32770` class window is a native dialog.
2. Check which editor owns the dialog and inspect the pending work. The optional watcher matches English captions and requires the target editor PID for actions.
3. Trace the dialog to the builder, save, or import operation that left it open.
