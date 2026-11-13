using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameStudio.KeyDoor
{
    public sealed class KeyDoorPlayModeTests
    {
        string saveDirectory;
        Keyboard keyboard;
        InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;
        InputSettings.BackgroundBehavior backgroundBehavior;

        [SetUp]
        public void SetUp()
        {
            saveDirectory = Path.Combine(Path.GetTempPath(), "key-door-test-" + Guid.NewGuid().ToString("N"));
            KeyDoorDemo.SaveDirectory = saveDirectory;
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            KeyDoorDemo.SaveDirectory = null;
            if (Directory.Exists(saveDirectory))
                Directory.Delete(saveDirectory, recursive: true);
        }

        [UnityTest]
        public IEnumerator KeyAndDoorSurviveSceneReloadAndPlayerCanFinish()
        {
            yield return SceneManager.LoadSceneAsync("KeyDoor");
            yield return null;

            var firstRun = UnityEngine.Object.FindFirstObjectByType<KeyDoorDemo>();
            Assert.That(firstRun, Is.Not.Null);
            Assert.That(CurrentTile(), Is.EqualTo("tile0"));
            Assert.That(Text("objective"), Is.EqualTo("Stand on the key and press F."));

            yield return Press(Key.RightArrow);
            yield return Press(Key.F);
            Assert.That(TileText("tile1"), Is.Empty);
            Assert.That(Text("feedback"), Is.EqualTo("Key picked up."));

            yield return Press(Key.RightArrow);
            yield return Press(Key.F);
            Assert.That(TileText("tile3"), Is.EqualTo("Open door"));

            yield return Press(Key.S);
            Assert.That(File.Exists(Path.Combine(saveDirectory, "checkpoint.json")), Is.True);
            Assert.That(Text("feedback"), Is.EqualTo("Checkpoint saved."));

            yield return Press(Key.R);
            Assert.That(firstRun == null, Is.True, "Reload must destroy the previous scene's controller.");
            Assert.That(CurrentTile(), Is.EqualTo("tile2"));
            Assert.That(Text("feedback"), Is.EqualTo("Checkpoint loaded."));
            Assert.That(TileText("tile1"), Is.Empty);
            Assert.That(TileText("tile3"), Is.EqualTo("Open door"));

            yield return Press(Key.RightArrow);
            yield return Press(Key.RightArrow);
            Assert.That(CurrentTile(), Is.EqualTo("tile4"));
            Assert.That(Text("objective"), Is.EqualTo("You reached the exit."));
        }

        [UnityTest]
        public IEnumerator ClosedDoorBlocksProgressWithoutKey()
        {
            yield return SceneManager.LoadSceneAsync("KeyDoor");
            yield return null;

            yield return Press(Key.RightArrow);
            yield return Press(Key.RightArrow);
            yield return Press(Key.F);
            Assert.That(Text("feedback"), Is.EqualTo("Find the key first."));
            Assert.That(TileText("tile3"), Is.EqualTo("Closed door"));

            yield return Press(Key.RightArrow);
            Assert.That(CurrentTile(), Is.EqualTo("tile2"));
            Assert.That(Text("feedback"), Is.EqualTo("The door is closed."));
        }

        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        static VisualElement Root() =>
            UnityEngine.Object.FindFirstObjectByType<KeyDoorDemo>()
                .GetComponent<UIDocument>().rootVisualElement;

        static string Text(string name) => Root().Q<Label>(name).text;
        static string TileText(string name) => Root().Q(name).Q<Label>().text;
        static string CurrentTile() => Root().Q(className: "player-tile").name;
    }
}
