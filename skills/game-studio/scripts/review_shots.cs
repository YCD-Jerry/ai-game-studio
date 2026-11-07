// game-studio: edit-mode review shots. Renders named scene cameras and/or ad-hoc camera specs to PNG, without Play,
// without saving anything. Run while holding editor.lock (it may open a scene).
//
//   1. write the spec to <project>/Temp/game-studio/review_shots.txt   (round.ps1 -ShotSpec does this for you)
//   2. unity command script eval_file --file <abs path>/review_shots.cs --timeout 600
//
// Eval files cannot take arguments, so the spec travels in a side file. Temp/ is outside Assets/, so writing it
// never triggers an import, even while someone plays.
//
// Spec, one key=value per line, '#' comments. Coordinates are world space (or offsets from anchor= when given):
//   out=<absolute folder for PNGs>            output folder (default <project>/Temp/game-studio/shots); created if missing
//   scene=Assets/Scenes/Ch1.unity  open this scene first (refused if any loaded scene has unsaved changes)
//   size=1280x720
//   post=1                         post-processing on the ad-hoc camera (URP, set by reflection; ignored elsewhere)
//   copyfrom=Main Camera           ad-hoc camera copies this camera's settings (default: Camera.main, if any)
//   sample=3.0                     seconds of Animator time to sample before shooting (0 = off). Without it,
//                                  characters render in bind/T-pose in edit mode.
//   step=0.1                       Animator.Update step for sampling
//   float=Speed 0                  set an Animator float parameter before sampling (repeatable; skipped where absent)
//   anchor=Dais                    following shot= lines are offsets from this GameObject's position (anchor= alone clears)
//   camera=front Shot Cam A        shot "front" through the scene camera named "Shot Cam A" (name may contain spaces)
//   shot=eye -3.6 1.6 -2.4 0 0.3 0 55      shot "eye": camera at (x y z) looking at (tx ty tz), fov 55 (default 50)
//   max=8                          shots rendered per eval (keep batches small: one long eval blocks the editor)
//   first=0                        index of the first shot of this batch; the reply's "more=N" says what is left
//
// Lessons built in (each one cost a real incident):
//   * ShaderUtil.allowAsyncCompilation=false while shooting: otherwise variants not compiled yet render flat cyan
//     (a placeholder that looks like a broken shader or 100% fog). Restored in finally. Cyan share is reported.
//   * forceMatrixRecalculationPerRender=true on every SkinnedMeshRenderer: otherwise Camera.Render in one eval keeps
//     the OLD skinned pose while bone data already moved. Restored in finally.
//   * Animators sampled (Rebind + fixed Update steps) so people stand in their idle, not their bind pose; every bone
//     transform is recorded first and restored afterwards, so the sampled pose is never saved into the scene.
//   * GPU wait: one render + a one-pixel ReadPixels into a reused 1x1 texture before the batch. Never
//     AsyncGPUReadback in a loop inside one eval (frames do not advance in a blocked eval; it crashed an editor).
//   * One RenderTexture and one Texture2D reused for the whole batch; small batches (max=).
//   * Every image is checked: mean, spread and cyan share; FLAT (uniform image) and CYAN warnings, so a broken shot
//     cannot read as a pass.
//   * Everything created is HideAndDontSave and destroyed in finally; scene dirty state is reported before/after.
// Face close-ups of characters with tiny bone scales can show white specular patches in edit-mode skinning; shoot a
// BakeMesh still copy for those instead of "fixing" the material (observed once; Unity 6000.3 URP).
//
// Reply: one line per shot ("OK name path mean= sd= cyan=%" or "FAIL name why"), then
// "shots=<ok> failed=<n> more=<left> dirtyBefore= dirtyAfter=". Callers should check the PNG files exist and are newer
// than the run start rather than trusting the reply alone.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "skipped: playing";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var projectRoot = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath).Replace('\\', '/');
var specPath = projectRoot + "/Temp/game-studio/review_shots.txt";
if (!System.IO.File.Exists(specPath)) return "ERROR no spec at " + specPath;

string outDir = projectRoot + "/Temp/game-studio/shots";
string wantScene = null, copyFrom = null, anchor = null;
int width = 1280, height = 720, maxShots = 8, first = 0;
bool post = true;
float sampleSeconds = 0f, sampleStep = 0.1f;
var floatNames = new System.Collections.Generic.List<string>();
var floatValues = new System.Collections.Generic.List<float>();
// shot entries: kind ("camera"|"shot"), name, camera name or numbers, anchor
var kinds = new System.Collections.Generic.List<string>();
var names = new System.Collections.Generic.List<string>();
var camNames = new System.Collections.Generic.List<string>();
var nums = new System.Collections.Generic.List<float[]>();
var anchors = new System.Collections.Generic.List<string>();
var sep = new char[] { ' ', '\t' };
foreach (var raw in System.IO.File.ReadAllLines(specPath)) {
    var line = raw.Trim();
    if (line.Length == 0 || line.StartsWith("#")) continue;
    int eq = line.IndexOf('=');
    if (eq < 0) return "ERROR bad spec line: " + line;
    var key = line.Substring(0, eq).Trim().ToLowerInvariant();
    var val = line.Substring(eq + 1).Trim();
    var parts = val.Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
    if (key == "out") outDir = val.Replace('\\', '/');
    else if (key == "scene") wantScene = val.Replace('\\', '/');
    else if (key == "size") { var wh = val.ToLowerInvariant().Split('x'); width = int.Parse(wh[0], inv); height = int.Parse(wh[1], inv); }
    else if (key == "post") post = val == "1" || val.ToLowerInvariant() == "true";
    else if (key == "copyfrom") copyFrom = val;
    else if (key == "sample") sampleSeconds = float.Parse(val, inv);
    else if (key == "step") sampleStep = UnityEngine.Mathf.Max(0.001f, float.Parse(val, inv));
    else if (key == "float") { floatNames.Add(parts[0]); floatValues.Add(float.Parse(parts[1], inv)); }
    else if (key == "anchor") anchor = val.Length == 0 ? null : val;
    else if (key == "max") maxShots = UnityEngine.Mathf.Max(1, int.Parse(val, inv));
    else if (key == "first") first = UnityEngine.Mathf.Max(0, int.Parse(val, inv));
    else if (key == "camera") {
        if (parts.Length < 2) return "ERROR camera= needs <shot name> <camera name>: " + line;
        kinds.Add("camera"); names.Add(parts[0]); camNames.Add(val.Substring(val.IndexOf(parts[0]) + parts[0].Length).Trim());
        nums.Add(null); anchors.Add(null);
    }
    else if (key == "shot") {
        if (parts.Length < 7) return "ERROR shot= needs <name> x y z tx ty tz [fov]: " + line;
        var n = new float[7];
        for (int k = 0; k < 6; k++) n[k] = float.Parse(parts[k + 1], inv);
        n[6] = parts.Length > 7 ? float.Parse(parts[7], inv) : 50f;
        kinds.Add("shot"); names.Add(parts[0]); camNames.Add(null); nums.Add(n); anchors.Add(anchor);
    }
    else return "ERROR unknown spec key: " + key;
}
if (kinds.Count == 0) return "ERROR spec has no camera= or shot= lines";
if (width < 16 || height < 16 || width > 8192 || height > 8192) return "ERROR size out of range";

// Scene: check what is really open (another session's hand-back may have switched it).
if (wantScene != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != wantScene) {
    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
        var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
        if (s.isDirty) return "ERROR not opening " + wantScene + ": " + (string.IsNullOrEmpty(s.path) ? "<untitled>" : s.path) + " has unsaved changes";
    }
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(wantScene, UnityEditor.SceneManagement.OpenSceneMode.Single);
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (wantScene != null && scene.path != wantScene) return "ERROR active scene is " + scene.path + ", wanted " + wantScene;
bool dirtyBefore = scene.isDirty;
System.IO.Directory.CreateDirectory(outDir);

UnityEngine.Camera FindCamera(string camName) {
    foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
        if (c.name == camName && (c.hideFlags & UnityEngine.HideFlags.DontSave) == 0) return c;
    return null;
}
void SetPost(UnityEngine.Camera c, bool on) {
    var t = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
    if (t == null) return;
    var d = c.GetComponent(t);
    if (d == null) d = c.gameObject.AddComponent(t);
    var p = t.GetProperty("renderPostProcessing");
    if (p != null) p.SetValue(d, on, null);
}

var info = "scene " + scene.path + "\n";
var wasAsync = UnityEditor.ShaderUtil.allowAsyncCompilation;
var smrs = UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);
var smrWas = new bool[smrs.Length];
var bones = new System.Collections.Generic.List<UnityEngine.Transform>();
var bonePos = new System.Collections.Generic.List<UnityEngine.Vector3>();
var boneRot = new System.Collections.Generic.List<UnityEngine.Quaternion>();
var boneScale = new System.Collections.Generic.List<UnityEngine.Vector3>();
UnityEngine.GameObject camGo = null;
UnityEngine.RenderTexture rt = null;
UnityEngine.Texture2D tex = null, px = null;
int ok = 0, failed = 0, last = UnityEngine.Mathf.Min(kinds.Count, first + maxShots);
try {
    UnityEditor.ShaderUtil.allowAsyncCompilation = false;
    for (int i = 0; i < smrs.Length; i++) { smrWas[i] = smrs[i].forceMatrixRecalculationPerRender; smrs[i].forceMatrixRecalculationPerRender = true; }

    if (sampleSeconds > 0f) {
        int sampled = 0;
        int steps = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(sampleSeconds / sampleStep));
        foreach (var an in UnityEngine.Object.FindObjectsByType<UnityEngine.Animator>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None)) {
            if (an.runtimeAnimatorController == null || !an.isActiveAndEnabled) continue;
            foreach (var t in an.GetComponentsInChildren<UnityEngine.Transform>(true)) { bones.Add(t); bonePos.Add(t.localPosition); boneRot.Add(t.localRotation); boneScale.Add(t.localScale); }
            an.Rebind();
            for (int f = 0; f < floatNames.Count; f++)
                foreach (var p in an.parameters)
                    if (p.name == floatNames[f] && p.type == UnityEngine.AnimatorControllerParameterType.Float) an.SetFloat(floatNames[f], floatValues[f]);
            for (int k = 0; k < steps; k++) an.Update(sampleStep);
            sampled++;
        }
        info += "animators sampled " + sampled + " for " + (steps * sampleStep).ToString("F2", inv) + " s\n";
    }

    rt = new UnityEngine.RenderTexture(width, height, 24);
    rt.Create();
    tex = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
    px = new UnityEngine.Texture2D(1, 1, UnityEngine.TextureFormat.RGB24, false);
    camGo = new UnityEngine.GameObject("game-studio ReviewCamera") { hideFlags = UnityEngine.HideFlags.HideAndDontSave };
    var cam = camGo.AddComponent<UnityEngine.Camera>();
    cam.enabled = false;
    var template = copyFrom != null ? FindCamera(copyFrom) : UnityEngine.Camera.main;
    if (copyFrom != null && template == null) info += "copyfrom camera not found: " + copyFrom + "\n";
    if (template != null) cam.CopyFrom(template);
    cam.targetTexture = null;
    SetPost(cam, post);

    // GPU wait / warm-up: one render and a one-pixel synchronous read.
    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
    UnityEngine.RenderTexture.active = rt;
    px.ReadPixels(new UnityEngine.Rect(0, 0, 1, 1), 0, 0); px.Apply();
    UnityEngine.RenderTexture.active = null;

    for (int s = first; s < last; s++) {
        var name = names[s];
        UnityEngine.Camera use = cam;
        if (kinds[s] == "camera") {
            use = FindCamera(camNames[s]);
            if (use == null) { info += "FAIL " + name + " camera not found: " + camNames[s] + "\n"; failed++; continue; }
            if (!use.gameObject.activeInHierarchy) info += "note " + name + ": camera's GameObject is inactive, render may be empty\n";
        } else {
            var n = nums[s];
            var basePos = UnityEngine.Vector3.zero;
            if (anchors[s] != null) {
                var ag = UnityEngine.GameObject.Find(anchors[s]);
                if (ag == null) { info += "FAIL " + name + " anchor not found: " + anchors[s] + "\n"; failed++; continue; }
                basePos = ag.transform.position;
            }
            var at = basePos + new UnityEngine.Vector3(n[0], n[1], n[2]);
            var look = basePos + new UnityEngine.Vector3(n[3], n[4], n[5]);
            cam.fieldOfView = n[6];
            cam.transform.position = at;
            if ((look - at).sqrMagnitude > 1e-8f) cam.transform.LookAt(look);
        }
        var prevTarget = use.targetTexture;
        try {
            use.targetTexture = rt;
            use.Render();
        } finally { use.targetTexture = prevTarget; }
        UnityEngine.RenderTexture.active = rt;
        tex.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        UnityEngine.RenderTexture.active = null;

        // Sanity: mean, spread, flat-cyan share (async-shader placeholder), on a sparse sample.
        var pix = tex.GetPixels32();
        int stride = UnityEngine.Mathf.Max(1, pix.Length / 20000), nS = 0, cyan = 0;
        double sum = 0, sum2 = 0;
        for (int k = 0; k < pix.Length; k += stride) {
            var c = pix[k];
            double l = 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b;
            sum += l; sum2 += l * l; nS++;
            if (c.r < 40 && c.g > 200 && c.b > 200) cyan++;
        }
        double mean = sum / nS, sd = System.Math.Sqrt(System.Math.Max(0, sum2 / nS - mean * mean));
        double cyanPct = 100.0 * cyan / nS;
        var path = outDir.TrimEnd('/') + "/" + name + ".png";
        try { System.IO.File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(tex)); }
        catch (System.Exception e) { info += "FAIL " + name + " save: " + e.Message + "\n"; failed++; continue; }
        var flags = "";
        if (sd < 2.0) flags += " FLAT";
        if (cyanPct > 2.0) flags += " CYAN";
        info += "OK " + name + " " + path + " mean=" + mean.ToString("F0", inv) + " sd=" + sd.ToString("F1", inv) + " cyan=" + cyanPct.ToString("F1", inv) + "%" + flags + "\n";
        ok++;
    }
} finally {
    for (int i = 0; i < bones.Count; i++) if (bones[i] != null) { bones[i].localPosition = bonePos[i]; bones[i].localRotation = boneRot[i]; bones[i].localScale = boneScale[i]; }
    for (int i = 0; i < smrs.Length; i++) if (smrs[i] != null) smrs[i].forceMatrixRecalculationPerRender = smrWas[i];
    UnityEngine.RenderTexture.active = null;
    if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
    if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
    if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
    if (px != null) UnityEngine.Object.DestroyImmediate(px);
    UnityEditor.ShaderUtil.allowAsyncCompilation = wasAsync;
}
return info + "shots=" + ok + " failed=" + failed + " more=" + (kinds.Count - last) + " dirtyBefore=" + dirtyBefore + " dirtyAfter=" + scene.isDirty;
