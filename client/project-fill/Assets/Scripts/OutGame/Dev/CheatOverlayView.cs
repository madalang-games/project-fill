#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Text;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.OutGame.Dev
{
    // DEV-ONLY server-cheat overlay. Entire file is compiled out of release builds (#if guard), so it
    // ships nothing. The prefab (Resources/Prefabs/UI/CheatOverlayView) is authored by
    // UIEditorSetup.CreateCheatOverlay() (UIEditorSetup convention — palette / Panel 3-layer / Btn 96px /
    // TMP AutoFontSize) and is dynamic-loaded once at startup via RuntimeInitializeOnLoadMethod, so the
    // overlay is reachable from every scene (in-game AND out-game). The backquote (`) key toggles the
    // panel; it starts hidden.
    //
    // Two input paths, both assembling the same command string and posting to the same endpoint
    // (server/parser do not distinguish path — single source of truth):
    //   Command mode — free text "/gold set 99999" → Send.
    //   Button mode  — domain tab + target/amount inputs + preset action buttons (no typing).
    // POST /api/dev/cheat/command {command}; the response message + success go to the log, and a
    // prefix-based local refresh re-pulls the touched domain (gold/inventory/cosmetics).
    public class CheatOverlayView : MonoBehaviour
    {
        const string PrefabResourcePath = "Prefabs/UI/CheatOverlayView";
        const string CommandEndpoint = "/api/dev/cheat/command";
        const string DocsEndpoint = "/api/dev/cheat/docs";
        const int MaxLogLines = 14;

        [SerializeField] GameObject _panel;
        [SerializeField] Button _closeButton;
        [SerializeField] Button _commandTabButton;
        [SerializeField] Button _buttonsTabButton;
        [SerializeField] GameObject _commandPane;
        [SerializeField] GameObject _buttonPane;
        [SerializeField] TMP_InputField _commandInput;
        [SerializeField] TMP_InputField _targetInput;
        [SerializeField] TMP_InputField _amountInput;
        [SerializeField] Button _sendButton;
        [SerializeField] Button _docsButton;
        [SerializeField] Button _clearButton;
        [SerializeField] TextMeshProUGUI _logText;
        [SerializeField] Button[] _domainTabButtons; // index = CheatDomain order
        [SerializeField] Button[] _actionButtons;    // pooled; relabelled/rewired per domain

        static bool _spawned;
        bool _open;
        bool _buttonMode;
        CheatDomain _domain = CheatDomain.Gold;
        readonly StringBuilder _log = new StringBuilder();

        static readonly Color TabOn = new Color(0.30f, 0.62f, 0.92f, 1f);
        static readonly Color TabOff = new Color(0.16f, 0.20f, 0.32f, 1f);

        readonly struct Preset
        {
            public readonly string Label;
            public readonly Func<string> Build;
            public Preset(string label, Func<string> build) { Label = label; Build = build; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (_spawned) return;
            if (!Debug.isDebugBuild && !Application.isEditor) return; // belt-and-suspenders; #if already gates compilation
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null) { Debug.LogWarning("[CheatOverlay] prefab missing at Resources/" + PrefabResourcePath); return; }
            _spawned = true;
            var go = Instantiate(prefab);
            go.name = "__CheatOverlay";
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            WireListeners();
            SetMode(false);
            SelectDomain(CheatDomain.Gold);
        }

        void WireListeners()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(() => Toggle(false));
            if (_commandTabButton != null) _commandTabButton.onClick.AddListener(() => SetMode(false));
            if (_buttonsTabButton != null) _buttonsTabButton.onClick.AddListener(() => SetMode(true));
            if (_sendButton != null) _sendButton.onClick.AddListener(() => { if (_commandInput != null) SendCommand(_commandInput.text); });
            if (_docsButton != null) _docsButton.onClick.AddListener(OpenDocs);
            if (_clearButton != null) _clearButton.onClick.AddListener(ClearLog);

            var domains = (CheatDomain[])Enum.GetValues(typeof(CheatDomain));
            if (_domainTabButtons != null)
                for (int i = 0; i < _domainTabButtons.Length && i < domains.Length; i++)
                {
                    var d = domains[i];
                    if (_domainTabButtons[i] != null) _domainTabButtons[i].onClick.AddListener(() => SelectDomain(d));
                }
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
                Toggle(!_open);
        }

        void Toggle(bool open)
        {
            _open = open;
            if (_panel != null) _panel.SetActive(open);
            if (open && !_buttonMode && _commandInput != null) _commandInput.ActivateInputField();
        }

        // ---- modes / domains -------------------------------------------------------------------

        void SetMode(bool buttonMode)
        {
            _buttonMode = buttonMode;
            if (_commandPane != null) _commandPane.SetActive(!buttonMode);
            if (_buttonPane != null) _buttonPane.SetActive(buttonMode);
            Tint(_commandTabButton, buttonMode ? TabOff : TabOn);
            Tint(_buttonsTabButton, buttonMode ? TabOn : TabOff);
        }

        void SelectDomain(CheatDomain d)
        {
            _domain = d;
            var domains = (CheatDomain[])Enum.GetValues(typeof(CheatDomain));
            if (_domainTabButtons != null)
                for (int i = 0; i < _domainTabButtons.Length && i < domains.Length; i++)
                    Tint(_domainTabButtons[i], domains[i] == d ? TabOn : TabOff);
            RebuildActions();
        }

        // Relabel + rewire the pooled action buttons for the selected domain. Branching is on the
        // CheatDomain enum (the discriminator); target/amount are read from the shared inputs at click.
        void RebuildActions()
        {
            if (_actionButtons == null) return;
            var presets = PresetsFor(_domain);
            for (int i = 0; i < _actionButtons.Length; i++)
            {
                var btn = _actionButtons[i];
                if (btn == null) continue;
                if (i < presets.Length)
                {
                    btn.gameObject.SetActive(true);
                    SetLabel(btn, presets[i].Label);
                    var build = presets[i].Build;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SendCommand(build()));
                }
                else
                {
                    btn.onClick.RemoveAllListeners();
                    btn.gameObject.SetActive(false);
                }
            }
        }

        Preset[] PresetsFor(CheatDomain domain)
        {
            string t = domain.ToString().ToLowerInvariant();
            switch (domain)
            {
                case CheatDomain.Gold:
                    return new[]
                    {
                        new Preset("+10k", () => $"/{t} add 10000"),
                        new Preset("+100k", () => $"/{t} add 100000"),
                        new Preset("Max", () => $"/{t} set 999999999"),
                        new Preset("Set", () => $"/{t} set {Amount()}"),
                    };
                case CheatDomain.Item:
                    return new[]
                    {
                        new Preset("Add", () => $"/{t} {Target("all")} add {Amount()}"),
                        new Preset("Reduce", () => $"/{t} {Target("all")} red {Amount()}"),
                        new Preset("Set", () => $"/{t} {Target("all")} set {Amount()}"),
                    };
                case CheatDomain.Stage:
                    return new[] { new Preset("Set Stage", () => $"/{t} set {Amount()}") };
                case CheatDomain.Tutorial:
                    return new[]
                    {
                        new Preset("Seen", () => $"/{t} {Target("all")} true"),
                        new Preset("Unseen", () => $"/{t} {Target("all")} false"),
                    };
                case CheatDomain.Ad:
                    return new[]
                    {
                        new Preset("Bypass On", () => $"/{t} true"),
                        new Preset("Bypass Off", () => $"/{t} false"),
                    };
                case CheatDomain.Cosmetic:
                    return new[]
                    {
                        new Preset("Unlock", () => $"/{t} {Target("all")} unlock"),
                        new Preset("Lock", () => $"/{t} {Target("all")} lock"),
                    };
                case CheatDomain.Achievement:
                    return new[]
                    {
                        new Preset("Complete", () => $"/{t} {Target("all")} complete"),
                        new Preset("Reset", () => $"/{t} {Target("all")} reset"),
                    };
                case CheatDomain.Attendance:
                    return new[]
                    {
                        new Preset("Set Day", () => $"/{t} setday {Amount()}"),
                        new Preset("Reset", () => $"/{t} reset"),
                    };
                default:
                    return Array.Empty<Preset>();
            }
        }

        string Target(string fallback)
        {
            var v = _targetInput != null ? _targetInput.text.Trim() : string.Empty;
            return string.IsNullOrEmpty(v) ? fallback : v;
        }

        string Amount()
        {
            var v = _amountInput != null ? _amountInput.text.Trim() : string.Empty;
            return string.IsNullOrEmpty(v) ? "0" : v;
        }

        // ---- networking ------------------------------------------------------------------------

        void SendCommand(string command)
        {
            command = (command ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(command)) { AppendLog("<color=#FF9>empty command</color>"); return; }
            if (NetworkService.Instance == null) { AppendLog("<color=#F88>no NetworkService (not booted?)</color>"); return; }

            AppendLog("> " + command);
            string body = "{\"command\":\"" + Escape(command) + "\"}";
            NetworkService.Instance.Post(CommandEndpoint, body, (ok, result) =>
            {
                if (!ok) { AppendLog("<color=#F88>FAIL " + Escape(result) + "</color>"); return; }
                var json = JsonUtility.FromJson<CheatCmdResponseJson>(result);
                string msg = json != null && !string.IsNullOrEmpty(json.message) ? json.message : "ok";
                AppendLog("<color=#9F9>" + Escape(msg) + "</color>");
                RefreshLocalState(command);
            });
        }

        // Re-sync local state so the lobby/HUD reflects the cheat without a manual refresh.
        // gold/item/cosmetic re-fetch from the server. /stage progress is NOT exposed by any lobby
        // refetch endpoint (the stage map is local-cache driven, fed only by stage start/clear), so we
        // apply the campaign reach locally — exactly as the real clear flow does — then re-render.
        static void RefreshLocalState(string command)
        {
            var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            switch (tokens[0])
            {
                case "/gold":
                    CurrencyApiService.Instance?.FetchGold();
                    break;
                case "/item":
                    InventoryApiService.Instance?.FetchInventory();
                    break;
                case "/cosmetic":
                    CosmeticApiService.Instance?.FetchCosmetics();
                    break;
                case "/stage":
                    // "/stage set {id}" → mirror StageApiService's ApplyMaxClearedStage (unlocks 1..id+1).
                    if (tokens.Length >= 3 && int.TryParse(tokens[2], out var stageId))
                        PlayerProgressService.Instance?.ApplyMaxClearedStage(stageId);
                    var home = UnityEngine.Object.FindObjectOfType<Game.OutGame.Lobby.HomeTabView>();
                    if (home != null) home.Refresh();
                    break;
                case "/tutorial":
                    // Re-pull (clear-then-fetch) so both see/un-see directions reflect; tutorials are
                    // evaluated on board entry, so no live re-render is needed.
                    Game.Services.Tutorial.TutorialManager.ReloadFromServer();
                    break;
            }
        }

        void OpenDocs()
        {
            if (NetworkService.Instance == null) { AppendLog("<color=#F88>no NetworkService</color>"); return; }
            NetworkService.Instance.Get(DocsEndpoint, (ok, result) =>
            {
                if (!ok) { AppendLog("<color=#F88>docs FAIL " + Escape(result) + "</color>"); return; }
                try
                {
                    string path = System.IO.Path.Combine(Application.persistentDataPath, "cheat_docs.html");
                    System.IO.File.WriteAllText(path, result);
                    Application.OpenURL("file://" + path);
                    AppendLog("<color=#9CF>docs → " + path + "</color>");
                }
                catch (Exception e)
                {
                    AppendLog("<color=#F88>docs write failed: " + e.Message + "</color>");
                }
            });
        }

        // ---- log + tiny ui helpers -------------------------------------------------------------

        void AppendLog(string line)
        {
            _log.AppendLine(line);
            var lines = _log.ToString().Split('\n');
            if (lines.Length > MaxLogLines)
            {
                _log.Clear();
                for (int i = lines.Length - MaxLogLines; i < lines.Length; i++)
                    if (i >= 0) _log.AppendLine(lines[i]);
            }
            if (_logText != null) _logText.text = _log.ToString();
        }

        void ClearLog()
        {
            _log.Clear();
            if (_logText != null) _logText.text = string.Empty;
        }

        static void Tint(Button b, Color c)
        {
            if (b != null && b.targetGraphic != null) b.targetGraphic.color = c;
        }

        static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.text = text;
        }

        static string Escape(string value)
            => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        class CheatCmdResponseJson
        {
            public bool success;
            public string command;
            public string message;
        }
    }
}
#endif
