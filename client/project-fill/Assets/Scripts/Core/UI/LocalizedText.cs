using Game.Services;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    /// <summary>
    /// Attach to any TMP_Text to enable localization.
    /// - stringId set   → updates text + font when language changes (static text)
    /// - stringId empty → updates font only (dynamic text; caller sets .text at runtime)
    /// Editor: OnValidate previews EN text without Play mode. CSV reimport auto-refreshes all.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        private const string EditorCsvPath = "Assets/Resources/Data/string/client_string.csv";

        [SerializeField] private string _stringId;

        private TMP_Text _tmp;

        void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
            Apply();
        }

        public void SetStringId(string id)
        {
            _stringId = id;
            Apply();
        }

        void OnEnable()
        {
            if (LocalizationService.Instance != null)
            {
                LocalizationService.Instance.OnLanguageChanged += Apply;
                Apply();
            }
        }

        void OnDisable()
        {
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.OnLanguageChanged -= Apply;
        }

        private void Apply()
        {
            if (LocalizationService.Instance == null) return;

            if (!string.IsNullOrEmpty(_stringId))
                _tmp.text = LocalizationService.Instance.Get(_stringId);

            var font = LocalizationService.Instance.GetFont(LocalizationService.Instance.CurrentLanguage);
            if (font != null)
                _tmp.font = font;

            if (TryGetComponent<UITextStyle>(out var style))
            {
                style.ApplyStyle();
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying) return;
            // Defer to avoid AssetDatabase access during serialization
            UnityEditor.EditorApplication.delayCall += ApplyEditorPreview;
        }

        internal void ApplyEditorPreview()
        {
            if (this == null) return;
            if (_tmp == null) TryGetComponent(out _tmp);
            if (_tmp == null || string.IsNullOrEmpty(_stringId)) return;

            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(EditorCsvPath);
            if (asset == null) return;

            var val = FindValue(asset.text, _stringId, "EN");
            if (!string.IsNullOrEmpty(val) && _tmp.text != val)
                _tmp.text = val;
        }

        // Called by StringCsvPostprocessor after CSV reimport
        public static void RefreshAllInEditor()
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(EditorCsvPath);
            if (asset == null) return;

            var all = FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lt in all)
            {
                if (lt._tmp == null) lt.TryGetComponent(out lt._tmp);
                if (lt._tmp == null || string.IsNullOrEmpty(lt._stringId)) continue;
                var val = FindValue(asset.text, lt._stringId, "EN");
                if (!string.IsNullOrEmpty(val) && lt._tmp.text != val)
                    lt._tmp.text = val;
            }

            // Also refresh open prefab stage
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                foreach (var lt in stage.prefabContentsRoot.GetComponentsInChildren<LocalizedText>(true))
                    lt.ApplyEditorPreview();
        }

        static string FindValue(string csv, string key, string langCode)
        {
            var lines = csv.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return null;
            var headers = SplitLine(lines[0]);
            int langCol = -1;
            for (int i = 0; i < headers.Length; i++)
                if (headers[i] == langCode) { langCol = i; break; }
            if (langCol < 0) return null;
            for (int r = 1; r < lines.Length; r++)
            {
                var cols = SplitLine(lines[r]);
                if (cols.Length > 0 && cols[0].Trim() == key && langCol < cols.Length)
                    return cols[langCol].Trim();
            }
            return null;
        }

        static string[] SplitLine(string line)
        {
            var fields  = new System.Collections.Generic.List<string>();
            var cur     = string.Empty;
            bool inQ    = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQ && i + 1 < line.Length && line[i + 1] == '"') { cur += '"'; i++; }
                    else inQ = !inQ;
                }
                else if (c == ',' && !inQ) { fields.Add(cur.Trim()); cur = string.Empty; }
                else cur += c;
            }
            fields.Add(cur.Trim());
            return fields.ToArray();
        }
#endif
    }
}
