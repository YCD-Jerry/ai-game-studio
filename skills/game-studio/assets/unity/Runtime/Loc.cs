using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// String tables from TSV: first row "key&lt;TAB&gt;zh-Hans&lt;TAB&gt;en", '#' lines are comments, "\n" in a cell is a line break.
    /// Keys are global, so prefix them by table (ch2_..., ui_..., spk_...). Never hard-code player-facing text.
    /// </summary>
    public static class Loc
    {
        const string k_Folder = "Localization";   // Resources/Localization/*.tsv.txt
        static readonly Dictionary<string, Dictionary<string, string>> s_Tables = new();   // language -> key -> text
        static readonly HashSet<string> s_Reported = new();
        static string s_Language;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()   // Enter Play Mode without domain reload keeps statics alive
        {
            s_Tables.Clear();
            s_Reported.Clear();
            s_Language = null;
            Changed = null;
        }

        public static IEnumerable<string> Languages { get { EnsureLoaded(); return s_Tables.Keys; } }

        public static string Language
        {
            get { EnsureLoaded(); return s_Language; }
            set
            {
                EnsureLoaded();
                if (!s_Tables.ContainsKey(value)) throw new ArgumentException("no strings for language " + value);
                if (s_Language == value) return;
                s_Language = value;
                Changed?.Invoke();
            }
        }

        public static string Get(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (s_Language != null && s_Tables.TryGetValue(s_Language, out var table) && table.TryGetValue(key, out var text) && text.Length > 0)
                return text;
            if (s_Reported.Add(key)) Debug.LogWarning($"[Loc] missing '{key}' ({s_Language})");
            return "#" + key;   // visible in game, easy to grep in screenshots
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        static void EnsureLoaded()
        {
            if (s_Tables.Count > 0) return;
            foreach (var asset in Resources.LoadAll<TextAsset>(k_Folder))
                Parse(asset.name, asset.text, s_Tables);
            if (s_Tables.Count == 0) { Debug.LogError("[Loc] no tables under Resources/" + k_Folder); return; }
            s_Language ??= PickDefault();
        }

        static string PickDefault()
        {
            bool chinese = Application.systemLanguage is SystemLanguage.Chinese or SystemLanguage.ChineseSimplified or SystemLanguage.ChineseTraditional;
            string wanted = chinese ? "zh-Hans" : "en";   // on the web this reads the browser's language
            if (s_Tables.ContainsKey(wanted)) return wanted;
            foreach (var language in s_Tables.Keys) return language;
            return wanted;
        }

        /// <summary>Parses one table into language -> key -> text. Throws on a bad header or a duplicate key (also used by the editor validator).</summary>
        public static int Parse(string tableName, string tsv, Dictionary<string, Dictionary<string, string>> into)
        {
            var lines = tsv.Replace("\r\n", "\n").Split('\n');
            var header = lines[0].TrimStart('﻿').Split('\t');
            if (header.Length < 2 || header[0].Trim() != "key")
                throw new FormatException($"{tableName}: first row must be key<TAB><lang>...");
            var seen = new HashSet<string>();
            int rows = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var cells = line.Split('\t');
                string key = cells[0].Trim();
                if (!seen.Add(key)) throw new FormatException($"{tableName}:{i + 1}: duplicate key {key}");
                for (int c = 1; c < header.Length; c++)
                {
                    string language = header[c].Trim();
                    if (!into.TryGetValue(language, out var table)) into[language] = table = new Dictionary<string, string>();
                    table[key] = c < cells.Length ? cells[c].Replace("\\n", "\n") : string.Empty;
                }
                rows++;
            }
            return rows;
        }
    }
}
