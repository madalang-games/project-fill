using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class BoardView : MonoBehaviour
    {
        // Serialized fields matching InGame.unity scene YAML
        [SerializeField] private GameObject      _cellPrefab;
        [SerializeField] private BoardBackground _background;
        [SerializeField] private RectTransform   _hudRectTransform;
        [SerializeField] private RectTransform   _itemTrayRectTransform;
        [SerializeField] private float _minMarginPx       = 24f;
        [SerializeField] private float _tapFeedbackDuration = 0.13f;
        [SerializeField] private float _groupPulseDuration  = 0.12f;
        [SerializeField] private float _removeDuration      = 0.24f;
        [SerializeField] private float _protectorHitDuration= 0.18f;
        [SerializeField] private float _dropDuration        = 0.26f;
        [SerializeField] private float _staggerDelay        = 0.025f;
        [SerializeField] private int   _burstCount          = 7;
        [SerializeField] private float _rotateDuration      = 0.42f;
        [SerializeField] private float _rotateScalePulse    = 0.035f;

        // Runtime
        private Board _board;
        private Canvas _canvas;
        private bool _inputBlocked;

        // UI root references
        private RectTransform _lanesContainer;
        private TextMeshProUGUI _movesText;
        private TextMeshProUGUI _stageLabel;
        private readonly List<Image>    _signalIndicators = new();
        private readonly List<LaneView> _laneViews        = new();

        // Overlay panels (created on demand)
        private GameObject _stuckPanel;
        private GameObject _clearPanel;
        private GameObject _hintOverlay;

        public bool IsInputBlocked => _inputBlocked;

        public event Action<int>         OnLaneTapped;
        public event Action<BoosterType> OnBoosterTapped;

        private void Awake()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080, 1920);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Called by InGameController
        public void Init(Board board, int stageId = 1)
        {
            _board = board;
            _inputBlocked = false;
            BuildUI(stageId);
            RefreshAll();
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildUI(int stageId)
        {
            // Clear old runtime children
            foreach (Transform t in transform)
                Destroy(t.gameObject);
            _laneViews.Clear();
            _signalIndicators.Clear();
            _stuckPanel = null;
            _clearPanel = null;
            _hintOverlay = null;

            // Full-screen root panel with dark bg
            var root = MakePanel(transform, "Root");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = new Color(0.076f, 0.076f, 0.118f);

            // ── HUD (top 10%) ──────────────────────────────────────────────
            var hud = MakePanel(root, "HUD");
            SetAnchors(hud, 0f, 0.90f, 1f, 1f);
            hud.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f);
            BuildHUD(hud, stageId);

            // ── Signal Panel (10%-18%) ─────────────────────────────────────
            var signalPanel = MakePanel(root, "SignalPanel");
            SetAnchors(signalPanel, 0f, 0.82f, 1f, 0.90f);
            signalPanel.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.11f, 0.18f);
            BuildSignalPanel(signalPanel);

            // ── Lanes area (18%-82%) ───────────────────────────────────────
            _lanesContainer = MakePanel(root, "Lanes");
            SetAnchors(_lanesContainer, 0f, 0.18f, 1f, 0.82f);

            // ── Booster bar (0%-18%) ───────────────────────────────────────
            var boosterBar = MakePanel(root, "BoosterBar");
            SetAnchors(boosterBar, 0f, 0f, 1f, 0.18f);
            boosterBar.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f);
            BuildBoosterBar(boosterBar);

            // Build lane views
            BuildLanes();
        }

        private void BuildHUD(RectTransform parent, int stageId)
        {
            // Back button
            var backBtn = MakeButton(parent, "Back", "←", 56f, () => GoToLobby());
            SetAnchors(backBtn, 0f, 0f, 0.12f, 1f);
            SetPadding(backBtn, 16, 0, 0, 0);

            // Stage label (center-left)
            _stageLabel = MakeLabel(parent, "Stage", $"STAGE  {stageId}", 32f, TextAlignmentOptions.Midline);
            SetAnchors((RectTransform)_stageLabel.transform, 0.12f, 0f, 0.50f, 1f);

            // Moves (center-right)
            _movesText = MakeLabel(parent, "Moves", "0", 52f, TextAlignmentOptions.Midline);
            SetAnchors((RectTransform)_movesText.transform, 0.50f, 0f, 0.80f, 1f);
            _movesText.color = new Color(0.85f, 0.92f, 1f);

            // Moves sub-label
            var moveLbl = MakeLabel(parent, "MovesLbl", "MOVES", 22f, TextAlignmentOptions.Midline);
            var moveLblRt = (RectTransform)moveLbl.transform;
            SetAnchors(moveLblRt, 0.50f, 0f, 0.80f, 0.35f);
            moveLbl.color = new Color(0.5f, 0.55f, 0.7f);
        }

        private void BuildSignalPanel(RectTransform parent)
        {
            int n = _board.TotalSets;
            float gap = 0.015f;
            float total = 1f - 0.04f - gap * (n - 1);
            float w = total / n;

            for (int i = 0; i < n; i++)
            {
                float x = 0.02f + i * (w + gap);
                var bar = MakePanel(parent, $"Signal_{i}");
                SetAnchors(bar, x, 0.15f, x + w, 0.85f);
                var img = bar.gameObject.AddComponent<Image>();
                img.color = new Color(0.18f, 0.18f, 0.28f);
                _signalIndicators.Add(img);
            }
        }

        private void BuildBoosterBar(RectTransform parent)
        {
            string[]      icons  = { "↩", "?", "⟳", "+" };
            string[]      names  = { "UNDO", "HINT", "SHUFFLE", "ADD LANE" };
            BoosterType[] types  = { BoosterType.Undo, BoosterType.Hint, BoosterType.Shuffle, BoosterType.AddLane };

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float x0 = 0.03f + i * 0.24f;
                float x1 = x0 + 0.21f;

                var btn = MakeButton(parent, $"Booster_{names[i]}", icons[i], 52f,
                    () => OnBoosterTapped?.Invoke(types[idx]));
                SetAnchors(btn, x0, 0.10f, x1, 0.90f);
                btn.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);

                // Sub label
                var subLbl = MakeLabel(btn, $"Lbl_{i}", names[i], 18f, TextAlignmentOptions.Bottom);
                SetAnchors((RectTransform)subLbl.transform, 0f, 0f, 1f, 0.30f);
                subLbl.color = new Color(0.5f, 0.55f, 0.7f);
            }
        }

        private void BuildLanes()
        {
            foreach (Transform t in _lanesContainer) Destroy(t.gameObject);
            _laneViews.Clear();

            int n     = _board.Lanes.Count;
            float gap = Mathf.Max(0.005f, 0.04f / n);
            float total = 1f - 0.04f - gap * (n - 1);
            float lw  = total / n;

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                float x0 = 0.02f + i * (lw + gap);
                float x1 = x0 + lw;

                var laneGO = new GameObject($"Lane_{i}");
                laneGO.transform.SetParent(_lanesContainer, false);
                var laneRt = laneGO.AddComponent<RectTransform>();
                SetAnchors(laneRt, x0, 0.01f, x1, 0.99f);

                var laneBg = laneGO.AddComponent<Image>();
                laneBg.color = new Color(0.14f, 0.14f, 0.22f);

                // Tap button on the entire lane
                var btn = laneGO.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    if (!_inputBlocked) OnLaneTapped?.Invoke(idx);
                });

                // Chip slots (bottom = slot 0, top = slot 3)
                var chipImages = new Image[SlotLane.Capacity];
                for (int s = 0; s < SlotLane.Capacity; s++)
                {
                    float yMin = 0.03f + s * (0.94f / SlotLane.Capacity);
                    float yMax = yMin + (0.94f / SlotLane.Capacity) - 0.015f;

                    var chipGO = new GameObject($"Chip_{s}");
                    chipGO.transform.SetParent(laneGO.transform, false);
                    var chipRt = chipGO.AddComponent<RectTransform>();
                    SetAnchors(chipRt, 0.07f, yMin, 0.93f, yMax);

                    var chipImg = chipGO.AddComponent<Image>();
                    chipImg.color = new Color(0.2f, 0.2f, 0.32f); // empty slot color
                    chipImages[s] = chipImg;

                    // Chip label (single letter)
                    var labelGO = new GameObject("ChipLabel");
                    labelGO.transform.SetParent(chipGO.transform, false);
                    var labelRt = labelGO.AddComponent<RectTransform>();
                    SetAnchors(labelRt, 0f, 0f, 1f, 1f);
                    var lbl = labelGO.AddComponent<TextMeshProUGUI>();
                    lbl.fontSize  = 26f;
                    lbl.color     = new Color(0f, 0f, 0f, 0.6f);
                    lbl.alignment = TextAlignmentOptions.Midline;
                    lbl.text      = "";
                }

                _laneViews.Add(new LaneView(laneGO, laneRt, laneBg, chipImages));
            }
        }

        // ── Refresh ──────────────────────────────────────────────────────────

        public void RefreshAll()
        {
            // Handle lane count mismatch (after AddLane)
            if (_laneViews.Count != _board.Lanes.Count)
                BuildLanes();

            for (int i = 0; i < _laneViews.Count; i++)
                RefreshLaneInternal(i);

            if (_movesText != null) _movesText.text = _board.MoveCount.ToString();
        }

        public void RefreshLane(int index)
        {
            if (index < 0 || index >= _laneViews.Count) return;
            RefreshLaneInternal(index);
        }

        private void RefreshLaneInternal(int index)
        {
            var lv   = _laneViews[index];
            var lane = _board.Lanes[index];

            for (int s = 0; s < SlotLane.Capacity; s++)
            {
                var img = lv.ChipImages[s];
                var lbl = img.transform.GetChild(0)?.GetComponent<TextMeshProUGUI>();
                if (s < lane.Count)
                {
                    var type = lane.Chips[s];
                    img.color = type.ToColor();
                    if (lbl != null) lbl.text = type.ToLabel();
                }
                else
                {
                    img.color = new Color(0.2f, 0.2f, 0.32f);
                    if (lbl != null) lbl.text = "";
                }
            }
        }

        public void UpdateMoves(int moves)
        {
            if (_movesText != null) _movesText.text = moves.ToString();
        }

        public void UpdateSignalPanel(int completedSets)
        {
            for (int i = 0; i < _signalIndicators.Count; i++)
                _signalIndicators[i].color = i < completedSets
                    ? new Color(0.2f, 0.9f, 0.5f)
                    : new Color(0.18f, 0.18f, 0.28f);
        }

        // ── Selection / Highlight ────────────────────────────────────────────

        public void SetSelection(int index)
        {
            ClearHighlights();
            if (index >= 0 && index < _laneViews.Count)
                _laneViews[index].Background.color = new Color(0.26f, 0.32f, 0.50f);
        }

        public void ClearSelection()
        {
            ClearHighlights();
        }

        public void SetHighlightValidLanes(int fromLane)
        {
            if (fromLane < 0 || fromLane >= _board.Lanes.Count) return;
            var top = _board.Lanes[fromLane].TopChip;
            if (top == null) return;

            for (int i = 0; i < _laneViews.Count; i++)
            {
                if (i == fromLane) continue;
                if (_board.Lanes[i].CanAccept(top.Value))
                    _laneViews[i].Background.color = new Color(0.18f, 0.28f, 0.20f);
                else
                    _laneViews[i].Background.color = new Color(0.14f, 0.14f, 0.22f);
            }
        }

        public void ClearHighlights()
        {
            foreach (var lv in _laneViews)
                lv.Background.color = new Color(0.14f, 0.14f, 0.22f);
        }

        public void SetHint(int fromLane, int toLane)
        {
            ClearHighlights();
            if (fromLane >= 0 && fromLane < _laneViews.Count)
                _laneViews[fromLane].Background.color = new Color(0.40f, 0.38f, 0.10f);
            if (toLane >= 0 && toLane < _laneViews.Count)
                _laneViews[toLane].Background.color = new Color(0.20f, 0.38f, 0.15f);
        }

        // ── Effects ──────────────────────────────────────────────────────────

        public void PlayInvalidEffect(int laneIndex, Action onDone = null)
        {
            if (laneIndex < 0 || laneIndex >= _laneViews.Count)
            {
                onDone?.Invoke();
                return;
            }
            StartCoroutine(ShakeCoroutine(_laneViews[laneIndex].Rt, onDone));
        }

        public void PlayCompleteSetEffect(int laneIndex, SignalType type, Action onDone = null)
        {
            StartCoroutine(CompleteCoroutine(laneIndex, type, onDone));
        }

        private IEnumerator ShakeCoroutine(RectTransform rt, Action onDone)
        {
            var origColor = rt.GetComponent<Image>()?.color ?? Color.white;
            var img = rt.GetComponent<Image>();
            if (img != null) img.color = new Color(0.6f, 0.15f, 0.15f);

            var orig = rt.anchoredPosition;
            float elapsed = 0f, dur = _tapFeedbackDuration * 1.5f;
            while (elapsed < dur)
            {
                float t = elapsed / dur;
                rt.anchoredPosition = orig + new Vector2(Mathf.Sin(t * Mathf.PI * 10f) * 18f * (1f - t), 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.anchoredPosition = orig;
            if (img != null) img.color = new Color(0.14f, 0.14f, 0.22f);
            onDone?.Invoke();
        }

        private IEnumerator CompleteCoroutine(int laneIndex, SignalType type, Action onDone)
        {
            if (laneIndex >= 0 && laneIndex < _laneViews.Count)
            {
                var lv = _laneViews[laneIndex];
                var origColor = lv.Background.color;
                lv.Background.color = type.ToColor();

                float elapsed = 0f;
                while (elapsed < _removeDuration)
                {
                    float t = elapsed / _removeDuration;
                    float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                    lv.Root.transform.localScale = Vector3.one * scale;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                lv.Root.transform.localScale = Vector3.one;
                lv.Background.color = new Color(0.14f, 0.14f, 0.22f);
            }
            onDone?.Invoke();
        }

        // ── Overlays ─────────────────────────────────────────────────────────

        public void BlockInput(bool block) => _inputBlocked = block;

        public void AddLaneView()
        {
            if (_laneViews.Count != _board.Lanes.Count)
                BuildLanes();
            RefreshAll();
        }

        public void ShowStuckPanel(Action onShuffle, Action onRestart, Action onLobby)
        {
            DismissPanel(ref _stuckPanel);

            _stuckPanel = MakeOverlayPanel("StuckPanel").gameObject;

            var rt = (RectTransform)_stuckPanel.transform;
            SetAnchors(rt, 0.05f, 0.25f, 0.95f, 0.75f);
            _stuckPanel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f, 0.97f);

            // Title
            var title = MakeLabel(rt, "Title", "교착 상태!", 52f, TextAlignmentOptions.Midline);
            SetAnchors((RectTransform)title.transform, 0.05f, 0.72f, 0.95f, 0.95f);

            // Body
            var body = MakeLabel(rt, "Body", "더 이상 이동 가능한 칩이 없습니다.", 28f, TextAlignmentOptions.Midline);
            SetAnchors((RectTransform)body.transform, 0.05f, 0.55f, 0.95f, 0.72f);
            body.color = new Color(0.6f, 0.65f, 0.8f);

            // Shuffle button
            var btnShuffle = MakeButton(rt, "BtnShuffle", "⟳  셔플  (100G)", 32f, () =>
            {
                DismissPanel(ref _stuckPanel);
                _inputBlocked = false;
                onShuffle?.Invoke();
            });
            SetAnchors(btnShuffle, 0.05f, 0.37f, 0.95f, 0.54f);
            btnShuffle.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.40f);

            // Restart button
            var btnRestart = MakeButton(rt, "BtnRestart", "↺  다시 시작", 32f, () =>
            {
                DismissPanel(ref _stuckPanel);
                onRestart?.Invoke();
            });
            SetAnchors(btnRestart, 0.05f, 0.21f, 0.95f, 0.36f);
            btnRestart.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.30f);

            // Lobby button
            var btnLobby = MakeButton(rt, "BtnLobby", "↩  로비로", 32f, () =>
            {
                DismissPanel(ref _stuckPanel);
                onLobby?.Invoke();
            });
            SetAnchors(btnLobby, 0.05f, 0.05f, 0.95f, 0.20f);
            btnLobby.GetComponent<Image>().color = new Color(0.25f, 0.12f, 0.12f);
        }

        public void ShowClearPanel(int stageId = 1, Action onNext = null, Action onLobby = null)
        {
            DismissPanel(ref _clearPanel);

            _clearPanel = MakeOverlayPanel("ClearPanel").gameObject;
            var rt = (RectTransform)_clearPanel.transform;
            SetAnchors(rt, 0.05f, 0.25f, 0.95f, 0.75f);
            _clearPanel.GetComponent<Image>().color = new Color(0.08f, 0.18f, 0.10f, 0.97f);

            var title = MakeLabel(rt, "Title", "스테이지 클리어!", 56f, TextAlignmentOptions.Midline);
            title.color = new Color(0.3f, 1f, 0.6f);
            SetAnchors((RectTransform)title.transform, 0.05f, 0.70f, 0.95f, 0.95f);

            var moves = MakeLabel(rt, "Moves", $"이동 횟수: {_board?.MoveCount ?? 0}", 34f, TextAlignmentOptions.Midline);
            SetAnchors((RectTransform)moves.transform, 0.05f, 0.50f, 0.95f, 0.70f);
            moves.color = new Color(0.7f, 0.85f, 1f);

            var btnNext = MakeButton(rt, "BtnNext", "다음 스테이지  →", 36f, () =>
            {
                DismissPanel(ref _clearPanel);
                onNext?.Invoke();
            });
            SetAnchors(btnNext, 0.05f, 0.22f, 0.95f, 0.45f);
            btnNext.GetComponent<Image>().color = new Color(0.10f, 0.35f, 0.18f);

            var btnLobby = MakeButton(rt, "BtnLobby", "로비로  ↩", 32f, () =>
            {
                DismissPanel(ref _clearPanel);
                onLobby?.Invoke();
            });
            SetAnchors(btnLobby, 0.05f, 0.05f, 0.95f, 0.21f);
            btnLobby.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
        }

        private RectTransform MakeOverlayPanel(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);  // child of Board canvas
            var rt = go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            return rt;
        }

        private void DismissPanel(ref GameObject panel)
        {
            if (panel != null) { Destroy(panel); panel = null; }
        }

        // ── Scene nav ────────────────────────────────────────────────────────

        private void GoToLobby()
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeToScene("Lobby");
            else
                SceneManager.LoadScene("Lobby");
        }

        // ── UI Helpers ───────────────────────────────────────────────────────

        private RectTransform MakePanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
            float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = text;
            tmp.fontSize   = size;
            tmp.color      = new Color(0.85f, 0.90f, 1f);
            tmp.alignment  = align;
            return tmp;
        }

        private RectTransform MakeButton(Transform parent, string name, string label,
            float fontSize, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.30f);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRt = lblGO.AddComponent<RectTransform>();
            SetAnchors(lblRt, 0f, 0f, 1f, 1f);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = new Color(0.88f, 0.92f, 1f);
            tmp.alignment = TextAlignmentOptions.Midline;

            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin  = new Vector2(xMin, yMin);
            rt.anchorMax  = new Vector2(xMax, yMax);
            rt.offsetMin  = rt.offsetMax = Vector2.zero;
        }

        private static void SetPadding(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public CellView GetCellView(int row, int col) => null;

        // ── Inner types ──────────────────────────────────────────────────────

        private class LaneView
        {
            public readonly GameObject   Root;
            public readonly RectTransform Rt;
            public readonly Image         Background;
            public readonly Image[]       ChipImages;

            public LaneView(GameObject root, RectTransform rt, Image bg, Image[] chips)
            {
                Root       = root;
                Rt         = rt;
                Background = bg;
                ChipImages = chips;
            }
        }
    }
}
