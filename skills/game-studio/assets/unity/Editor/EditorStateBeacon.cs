using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// Writes the editor's state to Temp/editor_state.txt every 2 s and on every play-mode change, so a guard script can
    /// refuse to recompile, rebuild or write into Assets while the user plays. A stale file means the editor is closed,
    /// busy, or stuck behind a modal dialog: treat it as unsafe.
    /// </summary>
    [InitializeOnLoad]
    static class EditorStateBeacon
    {
        static readonly string k_Path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "editor_state.txt");
        static readonly int k_Pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        static double s_Next;

        static EditorStateBeacon()
        {
            EditorApplication.playModeStateChanged += _ => Write();
            CompilationPipeline.compilationStarted += _ => Write("reloading");
            AssemblyReloadEvents.beforeAssemblyReload += () => Write("reloading");
            EditorApplication.quitting += Remove;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup < s_Next) return;
                s_Next = EditorApplication.timeSinceStartup + 2.0;
                Write();
            };
            Write();
        }

        /// <summary>One line, also usable as the body of an eval: return EditorStateBeacon.Describe();</summary>
        public static string Describe(string phase = null)
        {
            bool dirty = false, untitled = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                dirty |= s.isDirty;
                untitled |= string.IsNullOrEmpty(s.path);
            }
            return $"playing={EditorApplication.isPlaying} willPlay={EditorApplication.isPlayingOrWillChangePlaymode} " +
                   $"compiling={EditorApplication.isCompiling} updating={EditorApplication.isUpdating} " +
                   $"scene={SceneManager.GetActiveScene().path} dirty={dirty} untitled={untitled} " +
                   $"phase={phase ?? "idle"} utc={DateTime.UtcNow:o} pid={k_Pid}";
        }

        static void Write(string phase = null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(k_Path)!);
                var temporary = k_Path + ".tmp";
                File.WriteAllText(temporary, Describe(phase));
                if (File.Exists(k_Path)) File.Replace(temporary, k_Path, null);
                else File.Move(temporary, k_Path);
            }
            catch (IOException) { s_Next = 0; }   // retry on the next editor update
            catch (UnauthorizedAccessException) { }   // readers stop trusting the previous sample when it expires
        }

        static void Remove()
        {
            try { File.Delete(k_Path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>While playing, nothing written into Assets is imported (an import or recompile mid-Play kills every coroutine).</summary>
    [InitializeOnLoad]
    static class PlayModeRefreshLock
    {
        const string k_Held = "game.playRefreshLock";   // SessionState survives domain reloads; DisallowAutoRefresh is counted

        static PlayModeRefreshLock()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.EnteredPlayMode) Hold();
                else if (change == PlayModeStateChange.EnteredEditMode) Release();
            };
            if (EditorApplication.isPlayingOrWillChangePlaymode) Hold(); else Release();
        }

        static void Hold()
        {
            if (SessionState.GetBool(k_Held, false)) return;
            SessionState.SetBool(k_Held, true);
            AssetDatabase.DisallowAutoRefresh();
        }

        static void Release()
        {
            if (!SessionState.GetBool(k_Held, false)) return;
            SessionState.SetBool(k_Held, false);
            AssetDatabase.AllowAutoRefresh();
        }
    }
}
