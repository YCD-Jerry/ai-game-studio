using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameStudio.KeyDoor
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class KeyDoorDemo : MonoBehaviour
    {
        const int KeyTile = 1;
        const int DoorTile = 3;
        const int ExitTile = 4;
        const string SaveName = "checkpoint.json";

        [Serializable]
        sealed class Checkpoint
        {
            public int version = 1;
            public int position;
            public bool hasKey;
            public bool doorOpen;
        }

        // Tests redirect the same persistence path used by the player's S and R keys.
        internal static string SaveDirectory { get; set; }

        static string SavePath =>
            Path.Combine(SaveDirectory ?? Application.persistentDataPath, SaveName);

        Checkpoint state;
        Label objective;
        Label feedback;
        VisualElement[] tiles;
        bool reloading;

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            objective = root.Q<Label>("objective");
            feedback = root.Q<Label>("feedback");
            tiles = new VisualElement[ExitTile + 1];
            for (var index = 0; index < tiles.Length; index++)
                tiles[index] = root.Q<VisualElement>($"tile{index}");

            state = new Checkpoint();
            if (File.Exists(SavePath))
                Restore();
            Draw();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || reloading)
                return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                reloading = true;
                SceneManager.LoadScene(gameObject.scene.name);
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame)
                Move(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                Move(1);
            if (keyboard.fKey.wasPressedThisFrame)
                Interact();
            if (keyboard.sKey.wasPressedThisFrame)
                Save();

            Draw();
        }

        void Move(int direction)
        {
            var next = state.position + direction;
            if (next < 0 || next > ExitTile)
                return;

            if (next == DoorTile && !state.doorOpen)
            {
                feedback.text = "The door is closed.";
                return;
            }

            state.position = next;
            feedback.text = "";
        }

        void Interact()
        {
            if (state.position == KeyTile && !state.hasKey)
            {
                state.hasKey = true;
                feedback.text = "Key picked up.";
            }
            else if (state.position == DoorTile - 1 && !state.doorOpen)
            {
                state.doorOpen = state.hasKey;
                feedback.text = state.doorOpen ? "Door opened." : "Find the key first.";
            }
        }

        void Save()
        {
            try
            {
                var path = SavePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(state));
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
                feedback.text = "Checkpoint saved.";
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                feedback.text = "Could not save the checkpoint.";
            }
        }

        void Restore()
        {
            try
            {
                var saved = JsonUtility.FromJson<Checkpoint>(File.ReadAllText(SavePath));
                if (saved == null || saved.version != 1 || saved.position < 0 || saved.position > ExitTile ||
                    (saved.doorOpen && !saved.hasKey) || (saved.position >= DoorTile && !saved.doorOpen))
                {
                    feedback.text = "Checkpoint is invalid. Started a new run.";
                    return;
                }

                state = saved;
                feedback.text = "Checkpoint loaded.";
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                feedback.text = "Could not load the checkpoint. Started a new run.";
            }
        }

        void Draw()
        {
            for (var index = 0; index < tiles.Length; index++)
                tiles[index].EnableInClassList("player-tile", state.position == index);

            tiles[KeyTile].Q<Label>().text = state.hasKey ? "" : "Key";
            tiles[DoorTile].Q<Label>().text = state.doorOpen ? "Open door" : "Closed door";

            if (state.position == ExitTile)
                objective.text = "You reached the exit.";
            else if (state.doorOpen)
                objective.text = "Reach the exit.";
            else if (state.hasKey)
                objective.text = "Stand beside the door and press F.";
            else
                objective.text = "Stand on the key and press F.";
        }
    }
}
