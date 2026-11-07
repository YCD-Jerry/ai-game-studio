using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// Renders every camera named "ReviewCam_*" in the open scene to PNG, in edit mode, for look-dev comparison against
    /// reference images. Not a playtest. Restores everything it touches; saves nothing in the scene.
    /// </summary>
    public static class ReviewShots
    {
        const string k_Prefix = "ReviewCam_";
        const string k_ClockGlobal = "_ReviewClock";   // shaders that animate can use this instead of _Time when > 0 (edit-mode _Time keeps running)

        [MenuItem("Game/Review/Render Review Cameras")]
        static void Menu() => Debug.Log(Render(1600, 900, 1f));

        public static string Render(int width, int height, float sampleTime)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "skipped: playing";
            var scene = SceneManager.GetActiveScene();
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.name.StartsWith(k_Prefix)).OrderBy(c => c.name).ToArray();
            if (cameras.Length == 0) return $"no {k_Prefix}* cameras in {scene.name}";

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Review", scene.name, System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folder);   // outside Assets: writing into Assets triggers an import (and, mid-Play, a reload)

            var skinned = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var skinnedWas = skinned.Select(s => s.forceMatrixRecalculationPerRender).ToArray();
            bool asyncWas = ShaderUtil.allowAsyncCompilation;
            float clockWas = Shader.GetGlobalFloat(k_ClockGlobal);
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var log = new StringBuilder($"{scene.name} t={sampleTime}\n");
            try
            {
                ShaderUtil.allowAsyncCompilation = false;            // otherwise uncompiled variants render flat cyan
                foreach (var s in skinned) s.forceMatrixRecalculationPerRender = true;   // otherwise skinned meshes keep a stale pose
                Shader.SetGlobalFloat(k_ClockGlobal, sampleTime);
                foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                    if (a.runtimeAnimatorController != null) { a.Rebind(); a.Update(sampleTime); }   // otherwise T-pose

                foreach (var cam in cameras)
                {
                    bool wasActive = cam.gameObject.activeSelf, wasEnabled = cam.enabled;
                    var targetWas = cam.targetTexture;
                    try
                    {
                        cam.gameObject.SetActive(true);
                        cam.targetTexture = rt;
                        cam.ResetAspect();
                        cam.Render();
                        cam.Render();                                  // the first render after a change can be stale
                        RenderTexture.active = rt;
                        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);   // blocking read; never loop AsyncGPUReadback in an editor script
                        pixels.Apply();
                        RenderTexture.active = null;
                        string file = Path.Combine(folder, cam.name.Substring(k_Prefix.Length) + ".png");
                        File.WriteAllBytes(file, pixels.EncodeToPNG());
                        log.AppendLine($"{cam.name}: pos {cam.transform.position} fov {cam.fieldOfView:0} -> {file}");
                    }
                    finally
                    {
                        cam.targetTexture = targetWas;
                        cam.enabled = wasEnabled;
                        cam.gameObject.SetActive(wasActive);
                    }
                }
            }
            finally
            {
                RenderTexture.active = null;
                ShaderUtil.allowAsyncCompilation = asyncWas;
                for (int i = 0; i < skinned.Length; i++) if (skinned[i] != null) skinned[i].forceMatrixRecalculationPerRender = skinnedWas[i];
                Shader.SetGlobalFloat(k_ClockGlobal, clockWas);    // shader globals outlive edit mode and leak into Play
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(pixels);
            }
            return log.ToString();
        }
    }
}
