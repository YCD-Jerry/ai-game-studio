# Verify a game change

Start with the player's reproduction path. State the initial save or scene, input sequence, expected outcome, and a condition that fails before the fix when you can reproduce it. Keep the revision, settings, and evidence together.

| Evidence | Run when applicable | What to keep |
|---|---|---|
| Static / data | Compile and run targeted assertions, such as missing references, blocked doors, or localization overflow. | Command, revision, result and failures. |
| Visual | Capture before and after from matched cameras at the same time or pose, resolution, and graphics settings. Review the images. | Images and the discrepancies observed. |
| Runtime | Start from the normal player entry point, exercise the changed path and any affected resume or retry route, and assert the final state. | Initial state, input path, log and test results. |
| Human playtest | Ask the player to judge the changed moment in context. | Build and route, their feedback, and any open decisions. |

For a regression such as a door that opens on first entry but stays locked after loading a save, start the smoke test from both states. Assert the same objective can complete on both routes, with a bounded wait for the state transition. A successful screenshot of the open door is useful visual evidence. The assertion is what detects the progression failure.

## Unity examples

The Key and Door example linked from the distribution repository's README drives the same keyboard input, scene load, and save file that the player uses. Adapt its assertions to the target project's outcome.

- **Static reference check.** From the skill directory, run `python -I scripts/refcheck.py <checkout>` on the checkout that will build. Add `--work` to check on-disk changes, including untracked files Git would normally add. Exit 0 means the scanned references passed, 1 means issues were found, and 2 means the scan could not run. The scan covers scene references of Unity YAML type 2, checks target existence, and compares fileIDs where the target exposes YAML object IDs. Import packages first if their GUIDs are needed. Binary subobjects, nested dependencies and runtime loads need Unity checks.
- **Repeatable screenshots.** Use the project's existing capture tools. For a Unity project without one, adapt `assets/unity/Editor/ReviewShots.cs` for named edit-mode cameras. The optional Windows `scripts/round.ps1` can combine an editor guard, compile/build checks and screenshot verification if the bridge and project are configured. See [Editor safety](editor-safety.md) for prerequisites.
- **Runtime smoke check.** Reuse existing tests in an isolated checkout or run the target build with a test save. Keep the user's editor, save data and concurrent work out of the test. When Unity Test Framework is already present, use the Unity version pinned by the project and run the matching test command. For example, replace the paths and filter below, use fresh output paths, apply a wall-clock limit, and inspect the log and XML. Confirm that the expected test ran and passed. Missing XML, zero matching tests or a timeout are incomplete runs. See [Unity Test Framework command-line reference](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html).

```text
<Unity executable> -batchmode -projectPath "<isolated checkout>" -runTests -testPlatform PlayMode -testFilter "<project smoke test>" -testResults "<fresh results.xml>" -logFile "<fresh unity.log>"
```

Web input, loading or rendering issues need a check in the actual browser build.

The bundled tools can check references and support image capture. A runtime smoke test requires a project-specific entry point, controls, fixtures and assertions. When those are missing, report the remaining runtime case explicitly and gather the evidence the project supports.
