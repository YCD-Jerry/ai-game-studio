using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameStudio.KeyDoor
{
    static class CreateKeyDoorScene
    {
        const string ScenePath = "Assets/KeyDoor.unity";
        const string PanelPath = "Assets/KeyDoorPanel.asset";

        // Recreate the scene if extending the fixture. The shipped scene is already playable.
        public static void Generate()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var screen = new GameObject("Key and Door");
            var document = screen.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/KeyDoor.uxml");
            screen.AddComponent<KeyDoorDemo>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
