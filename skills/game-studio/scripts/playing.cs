// game-studio: editor state probe. Read-only; safe to run at any time, even while someone is playing.
//
//   unity command script eval_file --file <abs path>/playing.cs
//
// Prints one line, e.g.
//   playing=False paused=False compiling=False updating=False compileFailed=False scenes=1 dirty=False active=Assets/Scenes/Light.unity
//
// guard.ps1 / guard.sh look only for "playing=True" / "playing=False"; anything else means "unknown state" (exit 4).
// round.ps1 also reads compiling/updating ("settled" = both False several polls in a row) and compileFailed.
//
// "playing" uses isPlayingOrWillChangePlaymode, so the moment between pressing Play and entering Play mode
// already counts as playing (a recompile or import fired into that moment breaks the user's session just the same).
// "active" is last because a scene path may contain spaces.
//
// Eval files are C# statement bodies that end in `return <string>;`. Names are fully qualified on purpose
// (whether `using` directives are allowed depends on the Pipeline package's eval wrapper; not verified).
var a = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool anyDirty = false;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) anyDirty = true;
return "playing=" + UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
    + " paused=" + UnityEditor.EditorApplication.isPaused
    + " compiling=" + UnityEditor.EditorApplication.isCompiling
    + " updating=" + UnityEditor.EditorApplication.isUpdating
    + " compileFailed=" + UnityEditor.EditorUtility.scriptCompilationFailed
    + " scenes=" + UnityEngine.SceneManagement.SceneManager.sceneCount
    + " dirty=" + anyDirty
    + " active=" + (string.IsNullOrEmpty(a.path) ? "<untitled>" : a.path);
