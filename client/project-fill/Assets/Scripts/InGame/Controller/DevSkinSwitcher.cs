#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Game.InGame.View;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.InGame.Controller
{
    // DEV-ONLY in-game skin/stage switcher. Entire file is compiled out of release builds (#if guard),
    // so it ships nothing. Spawned by InGameController.Begin in the editor / development builds. Builds
    // a runtime screen-space Canvas overlay (big readable uGUI buttons — same UiUtil runtime-UI pattern
    // BoardView's fallback overlay uses, so no prefab and no UIEditorSetup/Unity-menu step) that cycles
    // the active Board / Chip / Lane cosmetics and the campaign stage LIVE for skin preview. The
    // backquote (`) key toggles the panel; it starts hidden.
    //
    // Board  → BoardView.RebuildSkins (re-resolves the theme + rebuilds pieces) + scene background.
    // Stage  → InGameController.DevLoadStage (reloads the board at a new stage index).
    public class DevSkinSwitcher : MonoBehaviour
    {
        // Preview id lists — a DEV tool, NOT gameplay branching. Mirror BoardTheme.Board / ApplyChip /
        // ApplyLane switch arms; the "*_default" entry reproduces the original procedural look.
        static readonly string[] Boards = { "board_default", "board_void", "board_retro_dos", "board_vintage", "board_circuit", "board_collector", "board_quantum", "board_challenge" };
        static readonly string[] Chips  = { "chip_default", "chip_hex", "chip_retro", "chip_crystal", "chip_platinum", "chip_ghost", "chip_neon", "chip_daily" };
        static readonly string[] Lanes  = { "lane_default", "lane_bronze", "lane_holo", "lane_terminal", "lane_crystal", "lane_ghost" };

        InGameController _controller;
        BoardView        _board;
        int  _bi, _ci, _li, _si;
        bool _open; // hidden by default; the backquote (`) key toggles the whole panel

        GameObject       _panel;
        TextMeshProUGUI  _boardVal, _chipVal, _laneVal, _stageVal;
        Sprite           _panelSprite, _btnSprite;
        bool             _built;

        // Creates (or refreshes the refs on) the single switcher instance + builds the overlay once.
        public static void Ensure(InGameController controller, BoardView board)
        {
            var go = GameObject.Find("__DevSkinSwitcher") ?? new GameObject("__DevSkinSwitcher");
            var sw = go.GetComponent<DevSkinSwitcher>() ?? go.AddComponent<DevSkinSwitcher>();
            sw._controller = controller;
            sw._board      = board;
            sw.Build();
        }

        // Backquote (`) toggles the panel. New Input System (project default) — null-guard the keyboard.
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                _open = !_open;
                if (_panel != null) _panel.SetActive(_open);
            }
        }

        void Build()
        {
            if (_built) return;
            _built = true;
            _panelSprite = TextureFactory.RoundedRect(48, 0.18f);
            _btnSprite   = TextureFactory.RoundedRect(48, 0.30f);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above all gameplay chrome
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight  = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Dark panel down the left side; blocks taps to the board behind it.
            var bg = UiUtil.Image(transform, "Panel", _panelSprite, new Color(0.05f, 0.06f, 0.10f, 0.92f));
            bg.raycastTarget = true;
            UiUtil.Anchors(bg.rectTransform, 0.03f, 0.14f, 0.97f, 0.62f);
            _panel = bg.gameObject;

            var header = UiUtil.Label(_panel.transform, "Header", "DEV SKINS    ( ` to hide )", 40f, TextAlignmentOptions.Center);
            header.enableAutoSizing = true; header.fontSizeMax = 46f;
            header.color = new Color(0.55f, 0.85f, 1f);
            UiUtil.Anchors(header.rectTransform, 0.04f, 0.86f, 0.96f, 1.0f);

            Row("Board", 0.66f, 0.84f, out _boardVal, () => Cycle(ref _bi, Boards.Length, -1), () => Cycle(ref _bi, Boards.Length, 1));
            Row("Chip",  0.46f, 0.64f, out _chipVal,  () => Cycle(ref _ci, Chips.Length,  -1), () => Cycle(ref _ci, Chips.Length,  1));
            Row("Lane",  0.26f, 0.44f, out _laneVal,  () => Cycle(ref _li, Lanes.Length,  -1), () => Cycle(ref _li, Lanes.Length,  1));
            Row("Stage", 0.05f, 0.23f, out _stageVal, () => Stage(-1), () => Stage(1));

            RefreshLabels();
            _panel.SetActive(_open); // hidden until toggled
        }

        // One row: name label | value label | big < and > buttons.
        void Row(string name, float yMin, float yMax, out TextMeshProUGUI value, System.Action prev, System.Action next)
        {
            var nl = UiUtil.Label(_panel.transform, name + "Name", name, 38f, TextAlignmentOptions.Left);
            nl.enableAutoSizing = true; nl.fontSizeMax = 42f;
            UiUtil.Anchors(nl.rectTransform, 0.05f, yMin, 0.28f, yMax);

            value = UiUtil.Label(_panel.transform, name + "Val", "", 34f, TextAlignmentOptions.Center);
            value.enableAutoSizing = true; value.fontSizeMax = 40f;
            value.color = new Color(0.95f, 0.95f, 0.7f);
            UiUtil.Anchors(value.rectTransform, 0.28f, yMin, 0.66f, yMax);

            MakeButton("<", 0.66f, yMin, 0.81f, yMax, prev);
            MakeButton(">", 0.83f, yMin, 0.98f, yMax, next);
        }

        void MakeButton(string text, float xMin, float yMin, float xMax, float yMax, System.Action onClick)
        {
            var img = UiUtil.Image(_panel.transform, "Btn" + text, _btnSprite, new Color(0.18f, 0.24f, 0.40f, 0.98f));
            img.raycastTarget = true;
            UiUtil.Anchors(img.rectTransform, xMin, yMin, xMax, yMax);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            var l = UiUtil.Label(img.transform, "L", text, 60f, TextAlignmentOptions.Center);
            l.enableAutoSizing = true; l.fontSizeMin = 20f; l.fontSizeMax = 110f;
            l.fontStyle = FontStyles.Bold;
            UiUtil.Stretch(l.rectTransform);
        }

        void Cycle(ref int idx, int len, int dir) { idx = (idx + dir + len) % len; ApplySkins(); }

        void Stage(int dir) { _si = Mathf.Max(0, _si + dir); _controller?.DevLoadStage(_si); RefreshLabels(); }

        void ApplySkins()
        {
            CosmeticState.DevOverride(Boards[_bi], Chips[_ci], Lanes[_li]);
            if (_board != null) _board.RebuildSkins();
            var bgView = FindObjectOfType<InGameSceneBackgroundView>();
            if (bgView != null) bgView.Apply(Boards[_bi]); // keep the scene background in sync with the board skin
            RefreshLabels();
        }

        void RefreshLabels()
        {
            if (_boardVal != null) _boardVal.text = Boards[_bi];
            if (_chipVal  != null) _chipVal.text  = Chips[_ci];
            if (_laneVal  != null) _laneVal.text  = Lanes[_li];
            if (_stageVal != null) _stageVal.text = (_si + 1).ToString();
        }
    }
}
#endif
