using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;                 // com.unity.ai.navigation
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Game.World;                          // RuntimeNavMeshBaker (Runtime/RuntimeNavMeshBaker.cs)

namespace Game.EditorTools
{
    /// <summary>Generates Level01 from code. Never hand-edit the .unity: change this file and re-run the menu.
    /// Success is ONLY the "[Game] Built" line in the console; automation must check for it.</summary>
    public static class Level01Builder
    {
        public const string BuiltMarker = "[Game] Built ";
        const string k_ScenePath = "Assets/Game/Scenes/Level01.unity";
        const string k_MaterialDir = "Assets/Game/Generated/Materials";
        const int k_IgnoreRaycast = 2;     // moving figures, carried props, invisible walls: excluded from NavMesh and ground probes

        public enum Solid { Yes, No }      // every placement must decide; no default

        static readonly Dictionary<string, Material> s_Materials = new();
        static int s_Solid, s_Passable;

        [MenuItem("Game/Build/Level 01")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Game] BUILD REFUSED: the editor is in Play mode.");
                return;
            }
            s_Materials.Clear();
            s_Solid = s_Passable = 0;
            try
            {
                EnsureFolder(Path.GetDirectoryName(k_ScenePath)!.Replace('\\', '/'));
                EnsureFolder(k_MaterialDir);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // no save prompt

                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                RenderSettings.sun = sun;

                var env = new GameObject("Environment");
                var surface = env.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;  // colliders decide what is walkable
                surface.layerMask = ~(1 << k_IgnoreRaycast);
                env.AddComponent<RuntimeNavMeshBaker>();                          // bakes the NavMesh when the scene loads

                Box("Ground", env.transform, new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f), "Ground", Solid.Yes);
                Box("Rock", env.transform, new Vector3(4f, 0.5f, 6f), new Vector3(1.5f, 1f, 1.2f), "Rock", Solid.Yes);
                Box("Rug", env.transform, new Vector3(-3f, 0.01f, 2f), new Vector3(2f, 0.02f, 3f), "Rug", Solid.No);
                InvisibleWall(env.transform, new Vector3(0f, 2f, 40f), new Vector3(80f, 4f, 0.5f));
                // Place("Assets/Game/Art/Trees/Oak.fbx", env.transform, new Vector3(8f, 0f, -4f), 30f, 1f, Solid.Yes, 3f, 25f);
                MovingFigure("Guide", env.transform, new Vector3(2f, 0.9f, 2f), 0.9f);

                if (!EditorSceneManager.SaveScene(scene, k_ScenePath))
                    throw new IOException("SaveScene returned false for " + k_ScenePath);
                AddToBuildSettings(k_ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log($"{BuiltMarker}{k_ScenePath} (solid {s_Solid}, passable {s_Passable})");   // last line, always
            }
            catch (Exception e)
            {
                Debug.LogError($"[Game] BUILD FAILED {k_ScenePath}: {e}");
                // Never leave an Untitled scene open: a later "save open scenes" pops a modal dialog that freezes the editor.
                if (File.Exists(k_ScenePath))
                    EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); // first build failed: this scene is still Untitled, so open your light scene here if you have one
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        // ---- placement helpers: each one takes a Solid decision ----

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, Solid solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            ApplySolid(go, solid);
            return go;
        }

        /// <summary>Instances a model ON TOP of its own root transform (FBX roots can carry x100 and a -90 tilt) and checks its real height.</summary>
        static GameObject Place(string assetPath, Transform parent, Vector3 position, float yaw, float scale, Solid solid, float minHeight, float maxHeight)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) ?? throw new FileNotFoundException("missing model " + assetPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * asset.transform.rotation);
            go.transform.localScale = asset.transform.localScale * scale;
            float height = WorldBounds(go).size.y;
            if (height < minHeight || height > maxHeight)
                throw new InvalidOperationException($"{assetPath}: real height {height:0.00} m, expected {minHeight}-{maxHeight} m (import scale?)");
            ApplySolid(go, solid);
            return go;
        }

        /// <summary>A figure that moves: solid for the player, steered round by agents, never baked into the NavMesh.</summary>
        static GameObject MovingFigure(string name, Transform parent, Vector3 position, float halfHeight)
        {
            var go = new GameObject(name) { layer = k_IgnoreRaycast };
            go.transform.SetParent(parent);
            go.transform.position = position;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.28f;
            capsule.height = 2f * halfHeight;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = 0.3f;
            obstacle.height = 2f * halfHeight;
            obstacle.carving = false;      // moving: avoidance only; carve only static blockers such as a shut door
            s_Solid++;
            return go;
        }

        static void InvisibleWall(Transform parent, Vector3 center, Vector3 size)
        {
            var wall = new GameObject("InvisibleWall") { layer = k_IgnoreRaycast };   // not baked: agents path along it, not round it
            wall.transform.SetParent(parent);
            wall.transform.position = center;
            wall.AddComponent<BoxCollider>().size = size;
            s_Solid++;
        }

        static void ApplySolid(GameObject go, Solid solid)
        {
            if (solid == Solid.No)
            {
                foreach (var c in go.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(c);
                s_Passable++;
                return;
            }
            if (go.GetComponentInChildren<Collider>(true) == null)
            {
                var b = WorldBounds(go);
                var box = go.AddComponent<BoxCollider>();
                var s = go.transform.lossyScale;
                box.center = go.transform.InverseTransformPoint(b.center);
                box.size = new Vector3(b.size.x / Mathf.Abs(s.x), b.size.y / Mathf.Abs(s.y), b.size.z / Mathf.Abs(s.z)); // world-space bounds: a loose fit when the object is rotated; add a fitted collider by hand for those
            }
            s_Solid++;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException(go.name + " has no renderer");
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        static Material Mat(string name)
        {
            if (s_Materials.TryGetValue(name, out var cached)) return cached;
            string path = $"{k_MaterialDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("URP Lit shader not found");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            return s_Materials[name] = material;
        }

        /// <summary>Wires a private [SerializeField]; a wrong name is a build failure, not a silent null.</summary>
        public static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new MissingFieldException(target.GetType().Name, field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != path))
                EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(path, true)).ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
