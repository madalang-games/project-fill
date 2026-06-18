#if UNITY_EDITOR
using Game.OutGame.Dev;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.UIColorPalette;

namespace Game.Editor
{
    // DEV cheat overlay prefab builder (partial of UIEditorSetup). Builds the
    // Resources/Prefabs/UI/CheatOverlayView prefab the runtime dynamic-loads. Uses the shared
    // UIEditorSetup helpers (Panel 3-layer / Btn 96px / TMP AutoFontSize / CloseBtnAt) so the overlay
    // follows the palette + layout conventions. Labels are raw dev text (TMP stringId = null →
    // LocalizedText font-only mode), so no client_string.csv / font-subset churn.
    public static partial class UIEditorSetup
    {
        [MenuItem("Tools/UI Setup/Prefabs/CheatOverlay", false, 146)]
        static void CreateCheatOverlaySingle() { EnsureDirs(); CreateCheatOverlay(); AssetDatabase.Refresh(); }

        static void CreateCheatOverlay()
        {
            var root = FullScreen("CheatOverlayView");
            var canvas = Comp<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above all chrome
            ApplyCanvasScaler(root);
            Comp<GraphicRaycaster>(root);
            var view = Comp<CheatOverlayView>(root);

            var content = Panel(root, "Panel", new Vector2(980, 1540), UI_BG_MID);
            var panelRoot = content.transform.parent.gameObject;

            TMP(content, "Header", Center(0, 700, 880, 90), 36, UI_TEXT, "SERVER CHEAT   ( ` to hide )", null, TextCategory.Header);
            var closeBtn = CloseBtnAt(content, new Vector2(445, 705));

            var cmdTab = Btn(content, "CommandTab", new Vector2(-240, 610), new Vector2(460, 96), UI_PRIMARY, "Command");
            var btnTab = Btn(content, "ButtonsTab", new Vector2(240, 610), new Vector2(460, 96), UI_BG_DEEP, "Buttons");

            // Log area (masked; newest line at bottom). Fixed font (dev log, like the InputField exception).
            var logArea = Child(content, "LogArea");
            Fixed(logArea, new Vector2(0, 250), new Vector2(900, 640));
            Img(logArea, Hex("05060A"));
            Comp<RectMask2D>(logArea);
            var logTmp = TMP(logArea, "LogText", Center(0, 0, 860, 600), 28, Hex("D2E6C7"), "", null, TextCategory.Normal);
            logTmp.enableAutoSizing = false; logTmp.fontSize = 28f;
            logTmp.alignment = TextAlignmentOptions.BottomLeft;
            logTmp.enableWordWrapping = true;

            // ── Command pane ───────────────────────────────────────────────────────────────────
            var cmdPane = Child(content, "CommandPane");
            Fixed(cmdPane, new Vector2(0, -440), new Vector2(940, 560));
            var commandInput = CheatInputField(cmdPane, "CommandInput", new Vector2(-130, 150), new Vector2(620, 100), "/gold set 99999");
            var sendBtn = Btn(cmdPane, "SendButton", new Vector2(330, 150), new Vector2(220, 100), UI_SUCCESS, "Send");
            var docsBtn = Btn(cmdPane, "DocsButton", new Vector2(-330, 20), new Vector2(280, 100), UI_PRIMARY, "Docs");
            var clearBtn = Btn(cmdPane, "ClearButton", new Vector2(-30, 20), new Vector2(280, 100), UI_BG_DEEP, "Clear Log");

            // ── Button pane ────────────────────────────────────────────────────────────────────
            var btnPane = Child(content, "ButtonPane");
            Fixed(btnPane, new Vector2(0, -440), new Vector2(940, 560));

            var domains = (CheatDomain[])System.Enum.GetValues(typeof(CheatDomain));
            float[] xs = { -345f, -115f, 115f, 345f };
            var domainButtons = new Button[domains.Length];
            for (int i = 0; i < domains.Length; i++)
            {
                float y = i < 4 ? 210f : 110f;
                float x = xs[i % 4];
                var tab = Btn(btnPane, "DomainTab_" + domains[i], new Vector2(x, y), new Vector2(225, 90), UI_BG_DEEP, domains[i].ToString());
                domainButtons[i] = tab.GetComponent<Button>();
            }

            var targetInput = CheatInputField(btnPane, "TargetInput", new Vector2(-235, 0), new Vector2(440, 90), "id | all");
            var amountInput = CheatInputField(btnPane, "AmountInput", new Vector2(235, 0), new Vector2(440, 90), "amount / day", TMP_InputField.ContentType.IntegerNumber);

            var actionButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var act = Btn(btnPane, "Action_" + i, new Vector2(xs[i], -120f), new Vector2(220, 100), UI_PRIMARY, "-");
                actionButtons[i] = act.GetComponent<Button>();
            }

            // ── Wire SerializeFields ───────────────────────────────────────────────────────────
            var so = new SerializedObject(view);
            so.FindProperty("_panel").objectReferenceValue = panelRoot;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("_commandTabButton").objectReferenceValue = cmdTab.GetComponent<Button>();
            so.FindProperty("_buttonsTabButton").objectReferenceValue = btnTab.GetComponent<Button>();
            so.FindProperty("_commandPane").objectReferenceValue = cmdPane;
            so.FindProperty("_buttonPane").objectReferenceValue = btnPane;
            so.FindProperty("_commandInput").objectReferenceValue = commandInput;
            so.FindProperty("_targetInput").objectReferenceValue = targetInput;
            so.FindProperty("_amountInput").objectReferenceValue = amountInput;
            so.FindProperty("_sendButton").objectReferenceValue = sendBtn.GetComponent<Button>();
            so.FindProperty("_docsButton").objectReferenceValue = docsBtn.GetComponent<Button>();
            so.FindProperty("_clearButton").objectReferenceValue = clearBtn.GetComponent<Button>();
            so.FindProperty("_logText").objectReferenceValue = logTmp;

            var tabsProp = so.FindProperty("_domainTabButtons");
            tabsProp.arraySize = domainButtons.Length;
            for (int i = 0; i < domainButtons.Length; i++)
                tabsProp.GetArrayElementAtIndex(i).objectReferenceValue = domainButtons[i];

            var actProp = so.FindProperty("_actionButtons");
            actProp.arraySize = actionButtons.Length;
            for (int i = 0; i < actionButtons.Length; i++)
                actProp.GetArrayElementAtIndex(i).objectReferenceValue = actionButtons[i];

            so.ApplyModifiedProperties();

            Save(root, "CheatOverlayView");
        }

        // TMP_InputField builder mirroring the AccountPopup nickname input (fixed 32 text/placeholder,
        // TextArea + RectMask2D). Find-or-create children → idempotent re-runs.
        static TMP_InputField CheatInputField(GameObject parent, string name, Vector2 pos, Vector2 size,
            string placeholder, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
        {
            var go = Child(parent, name);
            Fixed(go, pos, size);
            Img(go, UI_BG_DEEP);
            var input = Comp<TMP_InputField>(go);

            var area = Child(go, "TextArea");
            Stretch(area);
            var areaRt = RT(area);
            areaRt.offsetMin = new Vector2(15, 8);
            areaRt.offsetMax = new Vector2(-15, -8);
            Comp<RectMask2D>(area);

            var textGo = Child(area, "TextComponent");
            Stretch(textGo);
            var textTmp = Comp<TextMeshProUGUI>(textGo);
            textTmp.enableAutoSizing = true; textTmp.fontSizeMin = 32f; textTmp.fontSizeMax = 32f; textTmp.fontSize = 32f;
            textTmp.color = UI_TEXT;
            textTmp.alignment = TextAlignmentOptions.Left;

            var phGo = Child(area, "Placeholder");
            Stretch(phGo);
            var phTmp = Comp<TextMeshProUGUI>(phGo);
            phTmp.enableAutoSizing = true; phTmp.fontSizeMin = 32f; phTmp.fontSizeMax = 32f; phTmp.fontSize = 32f;
            phTmp.color = new Color(UI_TEXT.r, UI_TEXT.g, UI_TEXT.b, 0.5f);
            phTmp.text = placeholder;
            phTmp.alignment = TextAlignmentOptions.Left;

            input.textViewport = areaRt;
            input.textComponent = textTmp;
            input.placeholder = phTmp;
            input.contentType = contentType;
            return input;
        }
    }
}
#endif
