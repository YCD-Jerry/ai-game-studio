using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.EditorTools
{
    /// <summary>
    /// One-click web build: WebGPU first with a WebGL2 fallback, Brotli with a decompression fallback (works on hosts
    /// that can't set Content-Encoding, e.g. GitHub Pages and itch.io), data caching, hashed file names, stripped
    /// engine code, then a size report next to the build. A full web build holds the editor for 10-30 minutes:
    /// never start it on a shared editor while other sessions work or the director plays.
    /// If you use Addressables (Unity Localization tables are Addressables), set "Build Addressables on Player Build"
    /// in the Addressables settings, or the web build shows keys instead of text.
    /// </summary>
    public static class WebBuild
    {
        public const string DoneMarker = "[Game] Web build ";

        [MenuItem("Game/Build/Web Player")]
        public static void BuildMenu() => Build("Builds/Web");

        public static BuildReport Build(string outputFolder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Web build refused: the editor is in Play mode.");

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes in Build Settings.");

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.WebGPU, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.stripEngineCode = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            WriteSizeReport(outputFolder, report);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"{DoneMarker}FAILED: {report.summary.result}, {report.summary.totalErrors} errors (see {outputFolder}/size_report.txt)");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"{DoneMarker}OK {outputFolder} {report.summary.totalSize / (1024f * 1024f):0.0} MB in {report.summary.totalTime.TotalMinutes:0.0} min");
            }
            return report;
        }

        /// <summary>Every file sorted by size, the total, and the build's errors: the first thing to read when the build is too big or broken.</summary>
        static void WriteSizeReport(string folder, BuildReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"result {report.summary.result}  errors {report.summary.totalErrors}  warnings {report.summary.totalWarnings}");
            if (Directory.Exists(folder))
            {
                var files = new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories).OrderByDescending(f => f.Length).ToArray();
                sb.AppendLine($"total {files.Sum(f => f.Length) / (1024f * 1024f):0.0} MB in {files.Length} files");
                foreach (var f in files)
                    sb.AppendLine($"{f.Length / (1024f * 1024f),9:0.00} MB  {Path.GetRelativePath(folder, f.FullName)}");
            }
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        sb.AppendLine($"ERROR [{step.name}] {m.content}");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "size_report.txt"), sb.ToString());
        }
    }
}
