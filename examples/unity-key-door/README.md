# Key and Door

A small Unity project for practicing a repeatable gameplay check. It uses a saved scene, UI Toolkit, the Input System, and a JSON checkpoint.

## Play

Open this folder as a project in Unity 6000.3.27f1. Open `Assets/KeyDoor.unity`, click **Play**, and focus the Game view.

1. Use the arrow keys to move right onto the key and press **F**.
2. Move one tile right to stand beside the door and press **F** to open it.
3. Press **S** to save. Press **R** to reload the scene.
4. Walk right through the open door to the exit. The objective changes to “You reached the exit.”

The demo reads its checkpoint on entry. For a fresh manual run, remove `checkpoint.json` from the demo's `Application.persistentDataPath`. The automated tests use a temporary folder.

## Run the PlayMode tests

Close this example in Unity before starting a batch run. Use the installed Unity editor executable for the pinned version and fresh absolute log and XML paths.

```text
<Unity executable> -batchmode -nographics -projectPath "<absolute path to examples/unity-key-door>" -runTests -testPlatform PlayMode -testResults "<absolute path to key-door-results.xml>" -logFile "<absolute path to key-door-unity.log>"
```

The [Unity test command](https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/test-framework-introduction/running-tests/run-tests-from-command-line) exits after the run. Require two passing cases in the XML.

- `KeyAndDoorSurviveSceneReloadAndPlayerCanFinish` drives the normal keyboard route, checks that the checkpoint exists, reloads the saved scene, confirms the old controller was destroyed, and checks the restored board and completion.
- `ClosedDoorBlocksProgressWithoutKey` tries the interaction without a key and checks that the player remains on the approach tile.

You can also run the two cases from the **PlayMode** tab in Unity's Test Runner. These assertions verify input, scene wiring, rules, and persistence in the editor. A manual playthrough can assess the rendered UI and how the controls feel.
