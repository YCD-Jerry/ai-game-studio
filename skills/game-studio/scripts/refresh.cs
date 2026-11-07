// game-studio: AssetDatabase.Refresh, refused during Play. Run only while holding editor.lock and after guard returned 0:
// a refresh imports whatever was copied into Assets/ and can start a recompile (domain reload kills the user's Play).
//   unity command script eval_file --file <abs path>/refresh.cs
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "skipped: playing";
UnityEditor.AssetDatabase.Refresh();
return "refreshed";
