using System;
using System.Collections.Generic;
using Game.Core;
using Game.Localization;
using TMPro;
using UnityEngine;

namespace Game.Services
{
    public class LocalizationService : MonoBehaviour
    {
        private const string PrefsKey           = "lang";
        private const string StringResourcePath = "Data/string/client_string";
        private const string ErrorResourcePath  = "Data/string/error_messages";
        private const string FontConfigPath     = "Localization/FontLocalizationConfig";

        public static LocalizationService Instance { get; private set; }

        public Language CurrentLanguage { get; private set; }
        public event Action OnLanguageChanged;

        private Dictionary<string, string> _strings = new();
        private Dictionary<string, string> _errors  = new();
        private FontLocalizationConfig _fontConfig;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _fontConfig = Resources.Load<FontLocalizationConfig>(FontConfigPath);
            if (_fontConfig == null)
                Debug.LogWarning("[LocalizationService] FontLocalizationConfig not found at " + FontConfigPath);
            CurrentLanguage = LoadLanguage();
            LoadTables(CurrentLanguage);
        }

        public void SetLanguage(Language lang)
        {
            if (lang == CurrentLanguage) return;
            CurrentLanguage = lang;
            PlayerPrefs.SetString(PrefsKey, lang.ToString());
            LoadTables(lang);
            Game.Core.UI.UIFontMaterialCache.Clear();
            OnLanguageChanged?.Invoke();
        }

        public string Get(string key)
        {
            if (!string.IsNullOrEmpty(key) && _strings.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val))
                return val;
            return key;
        }

        public string GetError(string errorCode)
        {
            if (!string.IsNullOrEmpty(errorCode) && _errors.TryGetValue(errorCode, out var val) && !string.IsNullOrEmpty(val))
                return val;
            return errorCode;
        }

        public string GetErrorFromResponse(string errOrJson)
        {
            if (string.IsNullOrEmpty(errOrJson)) return errOrJson;
            try
            {
                var parsed = JsonUtility.FromJson<ErrorResponseJson>(errOrJson);
                if (parsed != null && !string.IsNullOrEmpty(parsed.code))
                    return GetError(parsed.code);
            }
            catch { }
            return GetError(errOrJson);
        }

        public TMP_FontAsset GetFont(Language lang)
        {
            return _fontConfig != null ? _fontConfig.GetFont(lang) : null;
        }

        private void LoadTables(Language lang)
        {
            _strings = ParseTable(StringResourcePath, lang.ToString());
            _errors  = ParseTable(ErrorResourcePath,  lang.ToString());
        }

        private static Dictionary<string, string> ParseTable(string resourcePath, string langCode)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[LocalizationService] Missing resource: {resourcePath}");
                return new Dictionary<string, string>();
            }

            var lines = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return new Dictionary<string, string>();

            var headers = ParseLine(lines[0]);
            int langCol = -1, enCol = -1, keyCol = 0;
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] == langCode)     langCol = i;
                if (headers[i] == "EN")         enCol   = i;
                if (headers[i].EndsWith("_key")) keyCol = i;
            }
            if (langCol == -1) langCol = enCol;

            var result = new Dictionary<string, string>(lines.Length);
            for (int r = 1; r < lines.Length; r++)
            {
                var cols = ParseLine(lines[r]);
                if (cols.Length == 0) continue;
                var key = keyCol < cols.Length ? cols[keyCol].Trim() : "";
                if (string.IsNullOrEmpty(key)) continue;
                var val = langCol >= 0 && langCol < cols.Length ? cols[langCol].Trim() : "";
                if (string.IsNullOrEmpty(val) && enCol >= 0 && enCol < cols.Length)
                    val = cols[enCol].Trim();
                result[key] = val;
            }
            return result;
        }

        private static string[] ParseLine(string line)
        {
            var fields  = new List<string>();
            var cur     = string.Empty;
            bool inQuote = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '"') { cur += '"'; i++; }
                    else inQuote = !inQuote;
                }
                else if (c == ',' && !inQuote) { fields.Add(cur.Trim()); cur = string.Empty; }
                else cur += c;
            }
            fields.Add(cur.Trim());
            return fields.ToArray();
        }

        private static Language LoadLanguage()
        {
            var saved = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse<Language>(saved, out var parsed))
                return parsed;
            return DetectSystemLanguage();
        }

        private static Language DetectSystemLanguage() =>
            Application.systemLanguage switch
            {
                SystemLanguage.Korean              => Language.KO,
                SystemLanguage.ChineseSimplified   => Language.ZH_CN,
                SystemLanguage.Chinese             => Language.ZH_CN,
                SystemLanguage.ChineseTraditional  => Language.ZH_TW,
                SystemLanguage.Japanese            => Language.JA,
                SystemLanguage.Russian             => Language.RU,
                SystemLanguage.Spanish             => Language.ES,
                SystemLanguage.Portuguese          => Language.PT,
                SystemLanguage.French              => Language.FR,
                SystemLanguage.German              => Language.DE,
                SystemLanguage.Thai                => Language.TH,
                SystemLanguage.Arabic              => Language.AR,
                SystemLanguage.Italian             => Language.IT,
                SystemLanguage.Turkish             => Language.TR,
                SystemLanguage.Indonesian          => Language.ID,
                _                                  => Language.EN,
            };
    }
}
