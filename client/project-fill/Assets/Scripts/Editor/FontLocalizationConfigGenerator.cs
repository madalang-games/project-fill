#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class FontLocalizationConfigGenerator
    {
        private const string AssetPath  = "Assets/Resources/Localization/FontLocalizationConfig.asset";
        private const string ConfigPath = "tools/subset_tool/config.json";

        [MenuItem("Tools/Localization/Generate Font Config from subset_tool/config.json", false, 300)]
        static void Generate()
        {
            var repoRoot   = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            var configFull = Path.Combine(repoRoot, ConfigPath);
            if (!File.Exists(configFull))
            {
                Debug.LogError($"[FontLocalizationConfigGenerator] config.json not found at {configFull}");
                return;
            }

            var json   = File.ReadAllText(configFull);
            var config = JsonUtility.FromJson<SubsetConfig>(json);

            var silver  = FindFont("Silver SDF");
            var galmuri = FindFont("Galmuri11 SDF");
            var unifont = FindFont("unifont-17.0.04 SDF");

            if (silver == null || galmuri == null || unifont == null)
            {
                Debug.LogError("[FontLocalizationConfigGenerator] One or more SDF font assets not found. Run font subset + reimport first.");
                return;
            }

            // Set fallback fonts on primary fonts so TMP auto-falls back for missing glyphs
            SetFallback(silver,  unifont);
            SetFallback(galmuri, unifont);

            // Build per-language primary font mapping from config.json
            var langToFont = new Dictionary<string, TMP_FontAsset>();
            foreach (var entry in config.fonts)
            {
                if (entry.source == "unifont-17.0.04.otf") continue; // unifont is fallback only
                TMP_FontAsset asset = entry.source switch
                {
                    "Silver.ttf"   => silver,
                    "Galmuri11.ttf" => galmuri,
                    _ => null,
                };
                if (asset == null) continue;
                foreach (var lang in entry.languages)
                    langToFont[lang] = asset;
            }

            // Ensure directories
            EnsureDir("Assets/Resources");
            EnsureDir("Assets/Resources/Localization");

            // Load or create asset
            var cfg = AssetDatabase.LoadAssetAtPath<FontLocalizationConfig>(AssetPath);
            bool isNew = cfg == null;
            if (isNew) cfg = ScriptableObject.CreateInstance<FontLocalizationConfig>();

            // Populate entries
            var so       = new SerializedObject(cfg);
            var arrProp  = so.FindProperty("_entries");
            var languages = (Language[])Enum.GetValues(typeof(Language));
            arrProp.arraySize = languages.Length;
            for (int i = 0; i < languages.Length; i++)
            {
                var lang     = languages[i];
                var langStr  = lang.ToString();
                var primary  = langToFont.TryGetValue(langStr, out var f) ? f : unifont;
                var elem     = arrProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("language").enumValueIndex = i;
                elem.FindPropertyRelative("font").objectReferenceValue = primary;
            }
            so.ApplyModifiedProperties();

            if (isNew)
                AssetDatabase.CreateAsset(cfg, AssetPath);
            else
                EditorUtility.SetDirty(cfg);

            AssetDatabase.SaveAssets();
            Debug.Log($"[FontLocalizationConfigGenerator] Done → {AssetPath}");
        }

        private static TMP_FontAsset FindFont(string name)
        {
            var guids = AssetDatabase.FindAssets($"{name} t:TMP_FontAsset");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void SetFallback(TMP_FontAsset font, TMP_FontAsset fallback)
        {
            if (font.fallbackFontAssetTable == null)
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!font.fallbackFontAssetTable.Contains(fallback))
                font.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(font);
        }

        private static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var cur   = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // Mirrors structure of subset_tool/config.json
        [Serializable] private class SubsetConfig  { public FontEntry[] fonts; }
        [Serializable] private class FontEntry     { public string source; public string target; public string[] languages; }
    }
}
#endif
