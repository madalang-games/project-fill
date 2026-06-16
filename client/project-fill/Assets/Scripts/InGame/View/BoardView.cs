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
    // Procedurally builds and animates the Signal Sort board UI at runtime. No prefab dependency.
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private BoardBackground _background;   // legacy scene link (kept)
        [SerializeField] private BoardSkin       _skin = new(); // optional art override; code fallback

        private Canvas _canvas;
        private SpriteSet _sprites;
        private Board _board;
        private StageDefinition _def;
        private bool _inputBlocked;
        private bool _softStuck;

        private RectTransform _lanesContainer, _flightLayer;
        private SignalPanelView _panel;
        private Image _dimOverlay;
        private TextMeshProUGUI _movesText, _stageText, _gimmickText;
        private RectTransform _shuffleBtnRt;
        private Button _undoBtn, _shuffleBtn, _addLaneBtn;
        private TextMeshProUGUI _addLaneLabel;
        private readonly List<LaneView> _laneViews = new();

        private GameObject _stuckPanel, _clearPanel;

        public bool IsInputBlocked => _inputBlocked;

        public event Action<int>         OnLaneTapped;
        public event Action<BoosterType> OnBoosterTapped;
        public event Action OnChapterCycle, OnRestart, OnBack;

        private void Awake()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            _sprites = SpriteSet.Resolve(_skin);
        }

        // ── Build ────────────────────────────────────────────────────────────

        public void Init(Board board, StageDefinition def)
        {
            _board = board;
            _def   = def;
            _inputBlocked = false;
            _softStuck    = false;

            foreach (Transform t in transform) Destroy(t.gameObject);
            _laneViews.Clear();
            _stuckPanel = _clearPanel = null;

            var root = UiUtil.Rect(transform, "Root");
            UiUtil.Stretch(root);
            UiUtil.Image(root, "BG", _sprites.Circuit, new Color(0.06f, 0.07f, 0.11f), sliced: false)
                .rectTransform.SetAsFirstSibling();
            UiUtil.Stretch((RectTransform)root.Find("BG"));

            BuildHud(root);
            BuildPanel(root);
            BuildLanesContainer(root);
            BuildBoosters(root);

            _flightLayer = UiUtil.Rect(transform, "FlightLayer");
            UiUtil.Stretch(_flightLayer);

            RefreshAll();
        }

        private void BuildHud(RectTransform root)
        {
            var hud = UiUtil.Image(root, "HUD", _sprites.LaneSlot, new Color(0.10f, 0.11f, 0.17f));
            UiUtil.Anchors(hud.rectTransform, 0f, 0.90f, 1f, 1f);

            MakeButton(hud.transform, "Back", "‹", 52f, new Vector4(0.01f, 0.18f, 0.11f, 0.82f),
                new Color(0.16f, 0.17f, 0.26f), () => OnBack?.Invoke());

            _stageText = UiUtil.Label(hud.transform, "Stage", "", 30f, TextAlignmentOptions.Left);
            UiUtil.Anchors(_stageText.rectTransform, 0.13f, 0.45f, 0.62f, 0.92f);
            _gimmickText = UiUtil.Label(hud.transform, "Gimmick", "", 20f, TextAlignmentOptions.Left);
            UiUtil.Anchors(_gimmickText.rectTransform, 0.13f, 0.10f, 0.62f, 0.45f);
            _gimmickText.color = new Color(0.4f, 0.85f, 0.95f);

            _movesText = UiUtil.Label(hud.transform, "Moves", "0", 46f, TextAlignmentOptions.Right);
            UiUtil.Anchors(_movesText.rectTransform, 0.60f, 0.40f, 0.84f, 0.92f);
            _movesText.color = new Color(0.85f, 0.92f, 1f);
            var ml = UiUtil.Label(hud.transform, "MovesLbl", "MOVES", 16f, TextAlignmentOptions.Right);
            UiUtil.Anchors(ml.rectTransform, 0.60f, 0.12f, 0.84f, 0.40f);
            ml.color = new Color(0.5f, 0.55f, 0.7f);

            // dev: cycle chapter sample stages to verify every gimmick. Vector4 = (xMin,xMax,yMin,yMax)
            MakeButton(hud.transform, "Chapter", "CH▶", 26f, new Vector4(0.85f, 0.99f, 0.52f, 0.90f),
                new Color(0.20f, 0.16f, 0.30f), () => OnChapterCycle?.Invoke());
            MakeButton(hud.transform, "Restart", "↻", 34f, new Vector4(0.85f, 0.99f, 0.10f, 0.48f),
                new Color(0.18f, 0.18f, 0.28f), () => OnRestart?.Invoke());
        }

        private void BuildPanel(RectTransform root)
        {
            var panel = UiUtil.Image(root, "SignalPanelBg", _sprites.LaneSlot, new Color(0.09f, 0.10f, 0.16f));
            UiUtil.Anchors(panel.rectTransform, 0.02f, 0.805f, 0.98f, 0.895f);
            _panel = SignalPanelView.Build(panel.transform, _sprites, _def.Types, _def.RelayOrder);
        }

        private void BuildLanesContainer(RectTransform root)
        {
            _lanesContainer = UiUtil.Rect(root, "Lanes");
            UiUtil.Anchors(_lanesContainer, 0f, 0.135f, 1f, 0.79f);

            _dimOverlay = UiUtil.Image(root, "Dim", null, new Color(0.02f, 0.02f, 0.04f, 0f));
            UiUtil.Anchors(_dimOverlay.rectTransform, 0f, 0.135f, 1f, 0.79f);

            BuildLanes();
        }

        private void BuildLanes()
        {
            foreach (var lv in _laneViews) if (lv) Destroy(lv.gameObject);
            _laneViews.Clear();

            int n = _board.Lanes.Count;
            float gap = Mathf.Max(0.006f, 0.05f / n);
            float total = 1f - 0.04f - gap * (n - 1);
            float lw = total / n;

            for (int i = 0; i < n; i++)
            {
                float x0 = 0.02f + i * (lw + gap);
                var lv = LaneView.Build(_lanesContainer, i, _sprites, _board.Lanes[i]);
                UiUtil.Anchors((RectTransform)lv.transform, x0, 0.02f, x0 + lw, 0.98f);
                lv.OnTapped += idx => { if (!_inputBlocked) OnLaneTapped?.Invoke(idx); };
                _laneViews.Add(lv);
            }
        }

        private void BuildBoosters(RectTransform root)
        {
            var bar = UiUtil.Image(root, "BoosterBar", _sprites.LaneSlot, new Color(0.10f, 0.11f, 0.17f));
            UiUtil.Anchors(bar.rectTransform, 0f, 0f, 1f, 0.125f);

            _undoBtn = MakeBooster(bar.transform, "↶", "UNDO", 0, BoosterType.Undo, out _);
            _shuffleBtn = MakeBooster(bar.transform, "⟳", "SHUFFLE  100", 1, BoosterType.Shuffle, out _shuffleBtnRt);
            _addLaneBtn = MakeBooster(bar.transform, "＋", "ADD LANE  500", 2, BoosterType.AddLane, out _, out _addLaneLabel);
        }

        private Button MakeBooster(Transform parent, string icon, string label, int slot, BoosterType type,
            out RectTransform rt, out TextMeshProUGUI subLabel)
        {
            float x0 = 0.04f + slot * 0.32f;
            var holder = UiUtil.Image(parent, $"B_{type}", _sprites.LaneSlot, new Color(0.16f, 0.17f, 0.27f));
            holder.raycastTarget = true;
            rt = holder.rectTransform;
            UiUtil.Anchors(rt, x0, 0.14f, x0 + 0.28f, 0.86f);
            var btn = holder.gameObject.AddComponent<Button>();
            btn.targetGraphic = holder;
            btn.onClick.AddListener(() => { if (!_inputBlocked) OnBoosterTapped?.Invoke(type); });

            var ic = UiUtil.Label(holder.transform, "Icon", icon, 44f);
            UiUtil.Anchors(ic.rectTransform, 0f, 0.30f, 1f, 1f);
            ic.color = new Color(0.7f, 0.9f, 1f);
            subLabel = UiUtil.Label(holder.transform, "Sub", label, 16f);
            UiUtil.Anchors(subLabel.rectTransform, 0f, 0.04f, 1f, 0.30f);
            subLabel.color = new Color(0.55f, 0.6f, 0.75f);
            return btn;
        }

        private Button MakeBooster(Transform parent, string icon, string label, int slot, BoosterType type, out RectTransform rt)
            => MakeBooster(parent, icon, label, slot, type, out rt, out _);

        private void MakeButton(Transform parent, string name, string label, float size, Vector4 anchors,
            Color color, Action onClick)
        {
            var img = UiUtil.Image(parent, name, _sprites.LaneSlot, color);
            img.raycastTarget = true;
            UiUtil.Anchors(img.rectTransform, anchors.x, anchors.z, anchors.y, anchors.w);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = UiUtil.Label(img.transform, "L", label, size);
            UiUtil.Stretch(lbl.rectTransform);
            lbl.color = new Color(0.85f, 0.92f, 1f);
        }

        // ── Refresh ──────────────────────────────────────────────────────────

        public void RefreshAll()
        {
            RefreshLanes();
            _panel.UpdateState(_board);
        }

        // Lanes + HUD + boosters, but not the Signal Panel (so node lighting can be timed to FX).
        private void RefreshLanes()
        {
            if (_laneViews.Count != _board.Lanes.Count) BuildLanes();
            for (int i = 0; i < _laneViews.Count; i++) RefreshLane(i);
            UpdateHud();
            UpdateBoosters();
        }

        public void RefreshLane(int index)
        {
            if (index < 0 || index >= _laneViews.Count) return;
            _laneViews[index].Refresh(_board.Lanes[index], PendingNumber(index));
        }

        // Relay queue position label for a pending lane.
        private int PendingNumber(int laneIndex)
        {
            if (!_board.HasRelay || !_board.Lanes[laneIndex].Pending) return 0;
            var type = _board.Lanes[laneIndex].Chips[0].Type;
            int idx = -1;
            for (int i = 0; i < _board.RelayOrder.Count; i++)
                if (_board.RelayOrder[i] == type) { idx = i; break; }
            if (idx < 0) return 1;
            return Mathf.Max(1, idx - _board.RelayProgress + 1);
        }

        public void UpdateHud()
        {
            _stageText.text   = _def.Name;
            _gimmickText.text = _def.GimmickLabel;
            _movesText.text   = _board.MoveCount.ToString();
        }

        public void UpdateBoosters()
        {
            _undoBtn.interactable = _board.CanUndo;
            _addLaneBtn.interactable = !_board.AddLaneUsed;
            _addLaneLabel.text = _board.AddLaneUsed ? "ADD LANE  USED" : "ADD LANE  500";
        }

        // ── Selection / highlight ────────────────────────────────────────────

        public void SetSelection(int index)
        {
            ClearHighlights();
            if (index < 0 || index >= _laneViews.Count) return;
            _laneViews[index].SetSelected(true);

            var top = _board.Lanes[index].TopChip;
            if (top == null) return;
            for (int i = 0; i < _laneViews.Count; i++)
                if (i != index && _board.Lanes[i].CanAccept(top.Value))
                    _laneViews[i].SetValidTarget(true);
        }

        public void ClearHighlights()
        {
            foreach (var lv in _laneViews) lv.ClearHighlight();
        }

        public void PlayInvalid(int laneIndex)
        {
            if (laneIndex >= 0 && laneIndex < _laneViews.Count) _laneViews[laneIndex].PlayInvalid();
        }

        // ── Move animation ───────────────────────────────────────────────────

        public void AnimateMove(int from, int to, Chip chip, int srcSlot,
            List<(int lane, SignalType type)> absorbed, Action onComplete)
        {
            StartCoroutine(MoveRoutine(from, to, chip, srcSlot, absorbed, onComplete));
        }

        private IEnumerator MoveRoutine(int from, int to, Chip chip, int srcSlot,
            List<(int lane, SignalType type)> absorbed, Action onComplete)
        {
            ClearHighlights();
            var fromLv = _laneViews[from];
            var toLv   = _laneViews[to];
            Vector3 srcPos = fromLv.SlotWorldPos(srcSlot);
            Vector2 size   = toLv.ChipPixelSize();

            RefreshLane(from);
            if (_board.Lanes[from].Kind == LaneKind.Blind) _laneViews[from].AnimateTopReveal();

            bool completes = absorbed != null && absorbed.Count > 0;
            // destination landing slot = current top (post-move) when not absorbed, else top socket.
            int destSlot = Mathf.Max(0, _board.Lanes[to].Count - 1);
            Vector3 destPos = toLv.SlotWorldPos(completes ? SlotLane.Capacity - 1 : destSlot);

            // dest lane still shows pre-move chips (not yet refreshed) — flight fills the gap.
            yield return FlightTween(chip, size, srcPos, destPos, 0.22f);

            if (!completes)
            {
                RefreshLane(to);
            }
            else
            {
                RefreshLanes();                       // clear absorbed lanes before the sweep
                foreach (var (laneIdx, type) in absorbed)
                    yield return CompleteSweep(laneIdx, type, size);
                _panel.UpdateState(_board);           // light nodes after chips arrive
            }

            onComplete?.Invoke();
        }

        private IEnumerator FlightTween(Chip chip, Vector2 size, Vector3 from, Vector3 to, float dur)
        {
            var fly = ChipView.Build(_flightLayer, _sprites);
            var rt  = fly.Rt;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.position  = from;
            fly.SetChip(chip, true);

            float t = 0f, arc = Vector3.Distance(from, to) * 0.18f;
            while (t < dur)
            {
                float p = t / dur;
                float e = 1f - (1f - p) * (1f - p); // ease-out
                var pos = Vector3.Lerp(from, to, e);
                pos.y += Mathf.Sin(p * Mathf.PI) * arc;
                rt.position = pos;
                rt.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, e);
                t += Time.deltaTime;
                yield return null;
            }
            rt.position = to;
            Destroy(fly.gameObject);
        }

        // Four chips sweep from a completed lane up to its Signal Panel node + particle burst.
        private IEnumerator CompleteSweep(int laneIndex, SignalType type, Vector2 size)
        {
            var lv = _laneViews[laneIndex];
            Vector3 nodePos = _panel.NodeWorldPos(type);

            var flyers = new List<ChipView>();
            for (int s = 0; s < SlotLane.Capacity; s++)
            {
                var fly = ChipView.Build(_flightLayer, _sprites);
                var rt = fly.Rt;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                rt.position  = lv.SlotWorldPos(s);
                fly.SetChip(new Chip(type), true);
                flyers.Add(fly);
            }

            float dur = 0.4f, t = 0f;
            var starts = flyers.ConvertAll(f => f.Rt.position);
            while (t < dur)
            {
                for (int i = 0; i < flyers.Count; i++)
                {
                    float p = Mathf.Clamp01((t - i * 0.04f) / (dur - 0.12f));
                    float e = p * p;
                    flyers[i].Rt.position   = Vector3.Lerp(starts[i], nodePos, e);
                    flyers[i].Rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, e);
                }
                t += Time.deltaTime;
                yield return null;
            }
            foreach (var f in flyers) Destroy(f.gameObject);

            _panel.PlayRegister(type);
            SpawnBurst(nodePos, type.ToColor(), 14);

            yield return new WaitForSeconds(0.12f);
        }

        private void SpawnBurst(Vector3 worldPos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var dot = UiUtil.Image(_flightLayer, "P", _sprites.Disc, color, sliced: false);
                var rt = dot.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = Vector2.one * UnityEngine.Random.Range(8f, 20f);
                rt.position  = worldPos;
                float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * UnityEngine.Random.Range(60f, 200f);
                StartCoroutine(BurstParticle(rt, dir, UnityEngine.Random.Range(0.4f, 0.7f)));
            }
        }

        private IEnumerator BurstParticle(RectTransform rt, Vector3 dir, float life)
        {
            Vector3 start = rt.position;
            var img = rt.GetComponent<Image>();
            float t = 0f;
            while (t < life)
            {
                float p = t / life;
                rt.position   = start + dir * p + Vector3.down * (120f * p * p);
                rt.localScale = Vector3.one * (1f - p);
                if (img) { var c = img.color; c.a = 1f - p; img.color = c; }
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        // ── Soft / Hard stuck ────────────────────────────────────────────────

        public void SetSoftStuck(bool on) => _softStuck = on;

        private void Update()
        {
            // soft stuck: dim board + pulse shuffle button (non-intrusive nudge)
            float target = _softStuck ? 0.15f : 0f;
            if (_dimOverlay)
            {
                var c = _dimOverlay.color;
                c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime * 0.6f);
                _dimOverlay.color = c;
            }
            if (_shuffleBtnRt)
            {
                float s = _softStuck ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f : 1f;
                _shuffleBtnRt.localScale = Vector3.one * s;
            }
        }

        public void BlockInput(bool block) => _inputBlocked = block;

        // ── Overlays ─────────────────────────────────────────────────────────

        public void ShowStuckPanel(bool addLaneAvailable, Action onAddLane, Action onShuffle, Action onGiveUp)
        {
            Dismiss(ref _stuckPanel);
            _stuckPanel = BuildOverlay("SIGNAL BLOCKED", "더 이상 이동 가능한 신호 칩이 없습니다",
                new Color(0.10f, 0.10f, 0.17f, 0.98f), new Color(1f, 0.72f, 0.2f));
            var rt = (RectTransform)_stuckPanel.transform;

            float y = 0.50f;
            if (addLaneAvailable)
            {
                AddOverlayButton(rt, "＋  ADD LANE  (광고 무료)", new Color(0.12f, 0.42f, 0.40f), y, () =>
                { Dismiss(ref _stuckPanel); _inputBlocked = false; onAddLane?.Invoke(); });
                y -= 0.18f;
            }
            AddOverlayButton(rt, "⟳  SHUFFLE  (100G)", new Color(0.18f, 0.20f, 0.38f), y, () =>
            { Dismiss(ref _stuckPanel); _inputBlocked = false; onShuffle?.Invoke(); });
            y -= 0.18f;
            AddOverlayButton(rt, "스테이지 포기", new Color(0.28f, 0.12f, 0.12f), y, () =>
            { Dismiss(ref _stuckPanel); onGiveUp?.Invoke(); });
        }

        public void ShowClearPanel(Action onNext, Action onLobby)
        {
            Dismiss(ref _clearPanel);
            _clearPanel = BuildOverlay("STAGE CLEAR", $"이동 횟수  {_board.MoveCount}",
                new Color(0.07f, 0.16f, 0.11f, 0.98f), new Color(0.35f, 1f, 0.6f));
            var rt = (RectTransform)_clearPanel.transform;
            AddOverlayButton(rt, "다음 스테이지  ›", new Color(0.10f, 0.38f, 0.20f), 0.40f, () =>
            { Dismiss(ref _clearPanel); onNext?.Invoke(); });
            AddOverlayButton(rt, "로비로", new Color(0.16f, 0.17f, 0.27f), 0.22f, () =>
            { Dismiss(ref _clearPanel); onLobby?.Invoke(); });
        }

        private GameObject BuildOverlay(string title, string body, Color bg, Color titleColor)
        {
            var scrim = UiUtil.Image(transform, "Scrim", null, new Color(0, 0, 0, 0.55f));
            UiUtil.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;

            var card = UiUtil.Image(scrim.transform, "Card", _sprites.LaneSlot, bg);
            UiUtil.Anchors(card.rectTransform, 0.08f, 0.30f, 0.92f, 0.70f);

            var t = UiUtil.Label(card.transform, "Title", title, 50f);
            UiUtil.Anchors(t.rectTransform, 0.05f, 0.72f, 0.95f, 0.94f);
            t.color = titleColor; t.fontStyle = FontStyles.Bold;
            var b = UiUtil.Label(card.transform, "Body", body, 26f);
            UiUtil.Anchors(b.rectTransform, 0.05f, 0.58f, 0.95f, 0.72f);
            b.color = new Color(0.65f, 0.7f, 0.82f);
            return scrim.gameObject;
        }

        private void AddOverlayButton(RectTransform parentCard, string label, Color color, float y, Action onClick)
        {
            // parentCard is the scrim; place buttons inside the card
            var card = parentCard.Find("Card");
            var img = UiUtil.Image(card, "Btn", _sprites.LaneSlot, color);
            img.raycastTarget = true;
            UiUtil.Anchors(img.rectTransform, 0.08f, y, 0.92f, y + 0.15f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = UiUtil.Label(img.transform, "L", label, 28f);
            UiUtil.Stretch(lbl.rectTransform);
        }

        private void Dismiss(ref GameObject go) { if (go) { Destroy(go); go = null; } }

        public static void GoToScene(string scene)
        {
            if (SceneTransition.Instance != null) SceneTransition.Instance.FadeToScene(scene);
            else SceneManager.LoadScene(scene);
        }

        // Legacy tutorial hook — Signal Sort has no grid cells, so there is nothing to return.
        public CellView GetCellView(int row, int col) => null;
    }
}
