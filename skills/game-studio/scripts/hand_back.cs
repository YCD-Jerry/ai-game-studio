// game-studio: hand the shared editor back after a round. Run it last, while you still hold editor.lock, then release the lock.
//
//   unity command script eval_file --file <abs path>/hand_back.cs --timeout 300
//
// What it does, in order:
//   1. Refuses during Play (never touches the user's session).
//   2. Saves every loaded scene that has a path and unsaved changes, ONE scene at a time with SaveScene.
//      It never calls EditorSceneManager.SaveOpenScenes: on an Untitled scene (what a scene builder leaves behind
//      when it throws) that opens the native "Save Scene" dialog, which is modal and freezes the editor for everyone.
//   3. Scenes whose path starts with a discard_prefix are NOT saved; their unsaved changes are dropped when the
//      light scene is opened (use this for code-generated scenes that are rebuilt from code and committed anyway).
//   4. Untitled scenes are never saved. With untitled=refuse (default) a dirty Untitled scene stops the hand-back
//      and is reported; with untitled=drop it is discarded (right for projects where every scene is generated,
//      so an Untitled scene can only be the debris of a failed build).
//   5. AssetDatabase.SaveAssets().
//   6. Opens the configured light scene alone (OpenSceneMode.Single), so the next Play starts fast and a crash
//      or restart has no big unsaved scenes to "restore from backup". Without a light_scene it only saves.
//
// Config (optional), one key=value per line, '#' comments, in <project>/.game-studio/hand_back.txt :
//   light_scene=Assets/Scenes/Light.unity
//   discard_prefix=Assets/Scenes/Generated/      (repeatable)
//   untitled=refuse                              (refuse | drop)
//
// Output starts with "OK" on success, "REFUSED" when it would lose someone's work, "skipped: playing" during Play.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "skipped: playing";
var root_ = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
var cfgPath = System.IO.Path.Combine(root_, ".game-studio", "hand_back.txt");
string light = null; bool dropUntitled = false;
var discard = new System.Collections.Generic.List<string>();
if (System.IO.File.Exists(cfgPath)) {
    foreach (var raw in System.IO.File.ReadAllLines(cfgPath)) {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith("#")) continue;
        int eq = line.IndexOf('=');
        if (eq < 0) continue;
        var key = line.Substring(0, eq).Trim().ToLowerInvariant();
        var val = line.Substring(eq + 1).Trim().Replace('\\', '/');
        if (key == "light_scene") light = val;
        else if (key == "discard_prefix") discard.Add(val);
        else if (key == "untitled") dropUntitled = val.ToLowerInvariant() == "drop";
    }
}
var said = "";
bool untitledDirty = false;
var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
for (int i = 0; i < count; i++) {
    var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    if (!s.isLoaded) continue;
    if (string.IsNullOrEmpty(s.path)) {
        if (s.isDirty) untitledDirty = true;
        said += " untitled(dirty=" + s.isDirty + ")";
        continue;
    }
    if (!s.isDirty) continue;
    bool drop = false;
    foreach (var p in discard) if (p.Length > 0 && s.path.StartsWith(p)) drop = true;
    if (drop) { said += " not-saved(discard_prefix) " + s.path; continue; }
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s)) return "REFUSED: could not save " + s.path + said;
    said += " saved " + s.path;
}
if (untitledDirty && !dropUntitled)
    return "REFUSED: an Untitled scene has unsaved changes (a builder that threw?). Look at it, or set untitled=drop in .game-studio/hand_back.txt." + said;
UnityEditor.AssetDatabase.SaveAssets();
if (string.IsNullOrEmpty(light)) {
    if (untitledDirty) {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        return "OK saved; dropped the Untitled scene (no light_scene configured)" + said;
    }
    return "OK saved (no light_scene configured, nothing opened)" + said;
}
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(light) == null) return "REFUSED: light_scene not found: " + light + said;
var act = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (UnityEngine.SceneManagement.SceneManager.sceneCount == 1 && act.path == light && !act.isDirty) return "OK already " + light + said;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(light, UnityEditor.SceneManagement.OpenSceneMode.Single);
var now = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (now.path != light) return "REFUSED: tried to open " + light + " but active is " + now.path + said;
return "OK " + light + " only, clean" + said;
