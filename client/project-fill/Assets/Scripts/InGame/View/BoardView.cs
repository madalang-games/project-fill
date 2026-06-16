using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Binds the Signal Sort board to its authored scene canvas (built by UIEditorSetup) and drives
    // animation. Chrome (HUD, booster bar, containers) is authored; lanes/chips/panel nodes are
    // instantiated at runtime from thin prefabs; stuck/clear popups route through UIManager so the
    // scrim follows the standard popup-backdrop convention.
    public class BoardView : MonoBehaviour
    {
        [Header("Containers (authored)")]
        [SerializeField] private Image     _background;
        [SerializeField] private Transform _lanesContainer;
        [SerializeField] private Transform _panelContainer;
        [SerializeField] private Transform _flightLayer;
        [SerializeField] private Image     _dim;

        [Header("HUD (authored)")]
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private TMP_Text _bestText;

        // Personal best for the active stage, pushed by the controller (0 = no record).
        private int _bestMoves;

        [Header("Boosters (authored)")]
        [SerializeField] private Button   _undoBtn;
        [SerializeField] private Button   _shuffleBtn;
        [SerializeField] private Button   _addLaneBtn;
        [SerializeField] private TMP_Text _addLaneLabel;

        [Header("Controls (authored)")]
        [SerializeField] private Button _pauseBtn;

        [Header("Runtime prefabs")]
        [SerializeField] private GameObject _lanePrefab;
        [SerializeField] private GameObject _chipPrefab;
        [SerializeField] private GameObject _nodePrefab;

        [Header("Art override (code fallback)")]
        [SerializeField] private BoardSkin _skin = new();

        [Header("World Settings")]
        [SerializeField] private Camera _worldCamera;

        private SpriteSet _sprites;
        private Board _board;
        private StageDefinition _def;
        private bool _inputBlocked;
        private bool _softStuck;
        private bool _wired;

        private SignalPanelView _panel;
        private RectTransform   _shuffleBtnRt;
        private readonly List<LaneView> _laneViews = new();

        private float _worldWidth;
        private float _panelWorldHeight;
        private float _lanesWorldHeight;

        public bool IsInputBlocked => _inputBlocked;

        public event Action<int>         OnLaneTapped;
        public event Action<BoosterType> OnBoosterTapped;
        public event Action OnPauseTapped;

        private void Awake()
        {
            _sprites = SpriteSet.Resolve(_skin);
            if (_worldCamera == null) _worldCamera = Camera.main;
        }

        public void SetWorldDimensions(float width, float panelHeight, float lanesHeight)
        {
            _worldWidth = width;
            _panelWorldHeight = panelHeight;
            _lanesWorldHeight = lanesHeight;
        }

        private void WireButtons()
        {
            if (_wired) return;
            _wired = true;

            BindBooster(_undoBtn,    BoosterType.Undo);
            BindBooster(_shuffleBtn, BoosterType.Shuffle);
            BindBooster(_addLaneBtn, BoosterType.AddLane);

            Bind(_pauseBtn,   () => OnPauseTapped?.Invoke());
        }

        private void BindBooster(Button btn, BoosterType type)
            => Bind(btn, () => OnBoosterTapped?.Invoke(type));

        private void Bind(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.AddListener(() => { if (!_inputBlocked) action(); });
        }

        // ── Build ────────────────────────────────────────────────────────────

        public void Init(Board board, StageDefinition def)
        {
            _board = board;
            _def   = def;
            _inputBlocked = false;
            _softStuck    = false;

            EnsureChrome();   // build a responsive fallback if the authored canvas isn't wired
            WireButtons();    // once (guarded)
            if (_shuffleBtn != null) _shuffleBtnRt = (RectTransform)_shuffleBtn.transform;

            // Trigger resizer to ensure we have valid world dimensions before initializing components
            var resizer = GetComponent<BoardWorldResizer>();
            if (resizer != null)
            {
                resizer.ResizeBoard();
            }

            if (_panel == null)
                _panel = _panelContainer.GetComponent<SignalPanelView>()
                      ?? _panelContainer.gameObject.AddComponent<SignalPanelView>();
            _panel.Initialize(_sprites, _nodePrefab, _def.Types, _def.RelayOrder, _worldWidth, _panelWorldHeight);

            BuildLanes(_worldWidth, _lanesWorldHeight);
            RefreshAll();
        }

        // When the authored InGame canvas (UIEditorSetup) is present and wired, use it. Otherwise
        // build a self-contained, anchor-based (responsive) board so the scene works with zero baking.
        // All prefab spawns are null-safe, so the fallback renders fully procedurally.
        private void EnsureChrome()
        {
            if ((_lanesContainer && _panelContainer && _flightLayer) || TryResolveAuthored())
            {
                if (_background && _background.sprite == null) _background.sprite = _sprites.Circuit;
                return;
            }
            Debug.LogError("[BoardView] Authored InGameCanvas elements not found! Please build UI using Tools/UI Setup/1 - Create All Prefabs.");

            // Fallback generation to prevent runtime crashes
            if (!_panelContainer)
            {
                var go = new GameObject("Fallback_SignalPanel");
                go.transform.SetParent(transform, false);
                _panelContainer = go.transform;
            }
            if (!_lanesContainer)
            {
                var go = new GameObject("Fallback_LanesContainer");
                go.transform.SetParent(transform, false);
                _lanesContainer = go.transform;
            }
            if (!_flightLayer)
            {
                var go = new GameObject("Fallback_FlightLayer");
                go.transform.SetParent(transform, false);
                _flightLayer = go.transform;
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindDeep(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        // Bind to existing authored children (names match UIEditorSetup.SetupInGame) when the BoardView
        // serialized refs are empty. Returns false if the core containers aren't present.
        private bool TryResolveAuthored()
        {
            Transform searchRoot = transform;
            var canvasGo = GameObject.Find("InGameCanvas_Base") ?? GameObject.Find("InGameCanvas_Base(Clone)");
            if (canvasGo != null)
            {
                searchRoot = canvasGo.transform;
            }
            else
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null) searchRoot = canvas.transform;
            }

            var worldRoot = GameObject.Find("BoardWorldRoot");
            Transform panel = null;
            Transform lanes = null;
            Transform flight = null;

            if (worldRoot != null)
            {
                panel = FindDeep(worldRoot.transform, "SignalPanel");
                lanes = FindDeep(worldRoot.transform, "LanesContainer");
                flight = FindDeep(worldRoot.transform, "FlightLayer");
            }
            else
            {
                panel = FindDeep(searchRoot, "SignalPanel");
                lanes = FindDeep(searchRoot, "LanesContainer");
                flight = FindDeep(searchRoot, "FlightLayer");
            }

            if (panel == null || lanes == null || flight == null) return false;

            _panelContainer = panel;
            _lanesContainer = lanes;
            _flightLayer    = flight;

            var dim = FindDeep(searchRoot, "Dim"); if (dim) _dim = dim.GetComponent<Image>();
            var bg  = FindDeep(searchRoot, "Background"); if (bg) _background = bg.GetComponent<Image>();

            var hud = FindDeep(searchRoot, "HUD");
            if (hud)
            {
                if (!_stageText)   _stageText   = FindTMP(hud, "StageText");
                if (!_movesText)   _movesText   = FindTMP(hud, "MovesText");
                if (!_bestText)    _bestText    = FindTMP(hud, "BestText");
                if (!_pauseBtn)    _pauseBtn    = FindBtn(hud, "PauseButton");
            }
            var bar = FindDeep(searchRoot, "BoosterBar");
            if (bar)
            {
                if (!_undoBtn)    _undoBtn    = FindBtn(bar, "UndoButton");
                if (!_shuffleBtn) _shuffleBtn = FindBtn(bar, "ShuffleButton");
                if (!_addLaneBtn) _addLaneBtn = FindBtn(bar, "AddLaneButton");
                
                var lbl = FindDeep(bar, "Label") ?? FindDeep(bar, "Text");
                if (lbl && !_addLaneLabel) _addLaneLabel = lbl.GetComponent<TMP_Text>();
            }
            return true;
        }

        private static TMP_Text FindTMP(Transform parent, string name)
        {
            var t = FindDeep(parent, name);
            return t ? t.GetComponent<TMP_Text>() : null;
        }

        private static Button FindBtn(Transform parent, string name)
        {
            var t = FindDeep(parent, name);
            return t ? t.GetComponent<Button>() : null;
        }

        private void BuildLanes(float containerWidth, float containerHeight)
        {
            foreach (var lv in _laneViews) if (lv) Destroy(lv.gameObject);
            _laneViews.Clear();

            int n = _board.Lanes.Count;
            float gap = containerWidth * 0.02f;
            float maxLaneW = containerHeight * 0.30f;  // square-chip cap when lanes are few (#6)
            float minLaneW = containerHeight * 0.16f;  // below this, wrap lanes into a grid row

            // Columns that keep lane width >= minLaneW; overflow wraps into additional rows so a
            // high lane count lays out as a balanced grid instead of one ultra-thin strip.
            int maxCols = Mathf.Max(1, Mathf.FloorToInt((containerWidth - gap) / (minLaneW + gap)));
            int cols = Mathf.Min(n, maxCols);
            int rows = Mathf.CeilToInt((float)n / cols);
            cols = Mathf.CeilToInt((float)n / rows);   // balance rows (e.g. 12 → 6×2, not 6+6 vs 7+5)

            float laneW = Mathf.Min(maxLaneW, (containerWidth  - gap * (cols + 1)) / cols);
            float laneH = (containerHeight - gap * (rows + 1)) / rows;

            float gridH = laneH * rows + gap * (rows - 1);
            float topY  = gridH * 0.5f - laneH * 0.5f; // center Y of the top row

            for (int i = 0; i < n; i++)
            {
                int row   = i / cols;
                int col   = i % cols;
                int inRow = Mathf.Min(cols, n - row * cols);          // lanes present on this row
                float rowW   = laneW * inRow + gap * (inRow - 1);
                float startX = -rowW * 0.5f + laneW * 0.5f;          // center each row by its own count

                float x = startX + col * (laneW + gap);
                float y = topY - row * (laneH + gap);
                Vector2 laneSize = new Vector2(laneW, laneH);

                var lv = SpawnLane(i, _board.Lanes[i], laneSize);
                lv.transform.localPosition = new Vector3(x, y, 0f);
                lv.OnTapped += tapped => { if (!_inputBlocked) OnLaneTapped?.Invoke(tapped); };
                _laneViews.Add(lv);
            }
        }

        private LaneView SpawnLane(int index, SlotLane lane, Vector2 size)
        {
            GameObject go = _lanePrefab != null
                ? Instantiate(_lanePrefab, _lanesContainer)
                : new GameObject($"Lane_{index}");
            if (_lanePrefab == null) go.transform.SetParent(_lanesContainer, false);
            go.name = $"Lane_{index}";
            var lv = go.GetComponent<LaneView>() ?? go.AddComponent<LaneView>();
            return lv.Initialize(index, _sprites, lane, _chipPrefab, size);
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
            if (_laneViews.Count != _board.Lanes.Count) BuildLanes(_worldWidth, _lanesWorldHeight);
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

        // Pushes the personal best for the active stage; HUD then redraws via UpdateHud.
        public void SetBestMoves(int best)
        {
            _bestMoves = best;
            UpdateHud();
        }

        public void UpdateHud()
        {
            if (_stageText)   _stageText.text   = _def.Name;
            if (_movesText)   _movesText.text   = _board.MoveCount.ToString();
            if (_bestText)    _bestText.text    = _bestMoves > 0 ? _bestMoves.ToString() : "-";
        }

        public void UpdateBoosters()
        {
            if (_undoBtn)    _undoBtn.interactable    = _board.CanUndo;
            if (_addLaneBtn) _addLaneBtn.interactable = !_board.AddLaneUsed;

            // Boosters are gold-priced (spec §4): show the cost from item.csv. Undo is free/unlimited.
            var undoTxt = FindDeep(_undoBtn.transform, "Label")?.GetComponent<TMP_Text>()
                       ?? FindDeep(_undoBtn.transform, "Text")?.GetComponent<TMP_Text>();
            if (undoTxt) undoTxt.text = ""; // free/unlimited — icon alone conveys it (no locale text)

            var shuffleTxt = FindDeep(_shuffleBtn.transform, "Label")?.GetComponent<TMP_Text>()
                          ?? FindDeep(_shuffleBtn.transform, "Text")?.GetComponent<TMP_Text>();
            if (shuffleTxt) shuffleTxt.text = BoosterPrice(BoosterType.Shuffle).ToString();

            if (_addLaneLabel) _addLaneLabel.text = BoosterPrice(BoosterType.AddLane).ToString();
        }

        private static int BoosterPrice(BoosterType type)
        {
            var items = Game.Utils.CsvLoader.Load<ProjectFill.Data.Generated.Item>(
                ProjectFill.Data.Generated.Item.ResourcePath);
            int id = type.ItemId();
            if (items != null)
                foreach (var it in items)
                    if (it.id == id) return it.price;
            return 0;
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

        public void AnimateMove(int from, int to, Chip chip, int count, int destBase,
            List<(int lane, SignalType type)> absorbed, Action onComplete)
        {
            StartCoroutine(MoveRoutine(from, to, chip, count, destBase, absorbed, onComplete));
        }

        // Pours the moved same-type run onto the destination one chip at a time (the top chip peels
        // first and lands lowest), then resolves completion sweeps. Landing slots are read live so a
        // mid-flight board resize can't leave a chip hovering beside its slot (no snap-correction).
        private IEnumerator MoveRoutine(int from, int to, Chip chip, int count, int destBase,
            List<(int lane, SignalType type)> absorbed, Action onComplete)
        {
            ClearHighlights();
            var fromLv = _laneViews[from];
            var toLv   = _laneViews[to];
            Vector2 size = toLv.ChipPixelSize();
            if (count < 1) count = 1;
            float dropH = size.y * 3f;

            // Source teleport-out: the moved run shrinks toward its own center with a white flash, then
            // is gone. Stand-in flyers play the FX at the vacated slots (the model already dropped them).
            int remaining = _board.Lanes[from].Count;
            var srcPos = new Vector3[count];
            var vanishers = new ChipView[count];
            for (int j = 0; j < count; j++)
            {
                srcPos[j] = fromLv.SlotWorldPos(remaining + j);
                vanishers[j] = SpawnFlightChip(chip, size, srcPos[j]);
            }
            RefreshLane(from);  // real source drops the run immediately
            if (_board.Lanes[from].Kind == LaneKind.Blind) fromLv.AnimateTopReveal();
            yield return VanishAll(vanishers, srcPos, 0.16f);
            for (int j = 0; j < count; j++) Destroy(vanishers[j].gameObject);

            // Dest teleport-in: each chip flashes in white above its slot, restores its real color,
            // then gravity-drops into place.
            var dstPos = new Vector3[count];
            var flyers = new ChipView[count];
            for (int j = 0; j < count; j++)
            {
                dstPos[j] = toLv.SlotWorldPos(destBase + j);
                flyers[j] = SpawnFlightChip(chip, size, dstPos[j] + Vector3.up * dropH);
            }
            yield return MaterializeAll(flyers, 0.16f);
            yield return DropAll(flyers, dstPos, size, 0.18f);
            for (int j = 0; j < count; j++)
                flyers[j].Rt.position = toLv.SlotWorldPos(destBase + j); // settle on the live slot pos

            bool completes = absorbed != null && absorbed.Count > 0;
            if (!completes)
            {
                for (int j = 0; j < count; j++) Destroy(flyers[j].gameObject);
                RefreshLane(to);
            }
            else
            {
                bool toAbsorbed = false;
                foreach (var a in absorbed) if (a.lane == to) { toAbsorbed = true; break; }

                for (int j = 0; j < count; j++) Destroy(flyers[j].gameObject);

                // If the player's chips landed in a lane that is NOT itself being absorbed, show
                // them immediately so another lane's sweep doesn't visually swallow them (#4).
                if (!toAbsorbed) RefreshLane(to);

                foreach (var (laneIdx, type) in absorbed)
                    yield return CompleteSweep(laneIdx, type, size);
                RefreshLanes();              // sync pending/lock state after cascade
                _panel.UpdateState(_board);  // light nodes after chips arrive
            }

            onComplete?.Invoke();
        }

        // Source teleport-out: each chip shrinks toward its own center while flashing white, then is gone.
        private IEnumerator VanishAll(ChipView[] flyers, Vector3[] at, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                float p = t / dur;
                for (int k = 0; k < flyers.Length; k++)
                {
                    if (flyers[k] == null) continue;
                    flyers[k].Rt.position   = at[k];   // pin (Update would drift localPos before flash starts)
                    flyers[k].SetFlash(p);
                    flyers[k].Rt.localScale = Vector3.one * (1f - p);
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        // Dest teleport-in: each chip pops in as a white flash at full scale, then lerps back to its
        // real color (in place, above the slot). The drop is handled separately by DropAll.
        private IEnumerator MaterializeAll(ChipView[] flyers, float dur)
        {
            for (int k = 0; k < flyers.Length; k++)
            {
                if (flyers[k] == null) continue;
                flyers[k].Rt.localScale = Vector3.one;
                flyers[k].SetFlash(1f);
            }
            float t = 0f;
            while (t < dur)
            {
                float p = t / dur;
                for (int k = 0; k < flyers.Length; k++)
                    if (flyers[k] != null) flyers[k].SetFlash(1f - p);  // white → real color
                t += Time.deltaTime;
                yield return null;
            }
            for (int k = 0; k < flyers.Length; k++)
                if (flyers[k] != null) flyers[k].SetFlash(0f);
        }

        // Gravity drop: each restored chip falls from above its destination slot with downward
        // acceleration (ease-in) into place. A small per-chip stagger makes a multi-chip run cascade
        // bottom-first. No source→target traversal — the chip already teleported above the target.
        private IEnumerator DropAll(ChipView[] flyers, Vector3[] to, Vector2 size, float dur)
        {
            int nF = flyers.Length;
            float dropH = size.y * 3f;
            const float stagger = 0.04f;

            for (int k = 0; k < nF; k++)
            {
                if (flyers[k] == null) continue;
                flyers[k].Rt.position   = to[k] + Vector3.up * dropH;
                flyers[k].Rt.localScale = Vector3.one;
            }

            float total = dur + stagger * (nF - 1);
            float t = 0f;
            while (t < total)
            {
                for (int k = 0; k < nF; k++)
                {
                    if (flyers[k] == null) continue;
                    float p    = Mathf.Clamp01((t - k * stagger) / dur);
                    float fall = 1f - p * p;                          // ease-in: accelerate downward
                    flyers[k].Rt.position   = to[k] + Vector3.up * (dropH * fall);
                }
                t += Time.deltaTime;
                yield return null;
            }
            for (int k = 0; k < nF; k++)
            {
                if (flyers[k] == null) continue;
                flyers[k].Rt.position   = to[k];
                flyers[k].Rt.localScale = Vector3.one;
            }
        }

        private IEnumerator CompleteSweep(int laneIndex, SignalType type, Vector2 size)
        {
            var lv = _laneViews[laneIndex];
            Vector3 nodePos = _panel.NodeWorldPos(type);

            var flyers = new List<ChipView>();
            var starts = new List<Vector3>();
            for (int s = 0; s < SlotLane.Capacity; s++)
            {
                Vector3 p = lv.SlotWorldPos(s);
                flyers.Add(SpawnFlightChip(new Chip(type), size, p));
                starts.Add(p);
            }
            RefreshLane(laneIndex);  // empty the real lane now that the flyers represent its chips

            float dur = 0.4f, t = 0f;
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

        private ChipView SpawnFlightChip(Chip chip, Vector2 size, Vector3 worldPos)
        {
            GameObject go = _chipPrefab != null
                ? Instantiate(_chipPrefab, _flightLayer)
                : new GameObject("FlyChip");
            if (_chipPrefab == null) go.transform.SetParent(_flightLayer, false);
            var fly = (go.GetComponent<ChipView>() ?? go.AddComponent<ChipView>()).Initialize(_sprites, size);
            var rt = fly.transform;
            rt.position  = worldPos;
            fly.SetChip(chip, true);
            return fly;
        }

        private void SpawnBurst(Vector3 worldPos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float rSize = UnityEngine.Random.Range(0.08f, 0.20f);
                var dot = WorldUtil.CreateSprite(_flightLayer, "P", _sprites.Disc, color, new Vector2(rSize, rSize), sliced: false);
                var rt = dot.transform;
                rt.position  = worldPos;
                float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * UnityEngine.Random.Range(0.6f, 2.0f);
                StartCoroutine(BurstParticle(dot, dir, UnityEngine.Random.Range(0.4f, 0.7f)));
            }
        }

        private IEnumerator BurstParticle(SpriteRenderer sr, Vector3 dir, float life)
        {
            Vector3 start = sr.transform.position;
            float t = 0f;
            while (t < life)
            {
                float p = t / life;
                sr.transform.position   = start + dir * p + Vector3.down * (1.2f * p * p);
                sr.transform.localScale = Vector3.one * (1f - p);
                if (sr) { var c = sr.color; c.a = 1f - p; sr.color = c; }
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        // ── Soft / Hard stuck ────────────────────────────────────────────────

        public void SetSoftStuck(bool on) => _softStuck = on;

        private void Update()
        {
            // physics raycasting tap logic
            if (!_inputBlocked && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();

                if (UnityEngine.EventSystems.EventSystem.current == null ||
                    !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    bool isOverUI = false;
                    if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
                    {
                        var touch = Touchscreen.current.touches[0];
                        int touchId = touch.touchId.ReadValue();
                        if (UnityEngine.EventSystems.EventSystem.current != null &&
                            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId))
                        {
                            isOverUI = true;
                        }
                    }

                    if (!isOverUI)
                    {
                        Camera cam = _worldCamera != null ? _worldCamera : Camera.main;
                        if (cam != null)
                        {
                            Vector3 worldPos = cam.ScreenToWorldPoint(pointerPos);
                            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                            if (hit.collider != null)
                            {
                                var lane = hit.collider.GetComponent<LaneView>();
                                if (lane != null)
                                {
                                    lane.Tap();
                                }
                            }
                        }
                    }
                }
            }

            float target = _softStuck ? 0.15f : 0f;
            if (_dim)
            {
                var c = _dim.color;
                c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime * 0.6f);
                _dim.color = c;
            }
            if (_shuffleBtnRt)
            {
                float s = _softStuck ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f : 1f;
                _shuffleBtnRt.localScale = Vector3.one * s;
            }
        }

        public void BlockInput(bool block) => _inputBlocked = block;

        // ── Overlays (route through UIManager — scrim = popup backdrop convention) ────

        public void ShowStuckPanel(bool addLaneAvailable, Action onAddLane, Action onShuffle, Action onGiveUp)
        {
            Action add  = () => { BlockInput(false); onAddLane?.Invoke(); };
            Action shuf = () => { BlockInput(false); onShuffle?.Invoke(); };

            var v = UIManager.Instance != null
                ? UIManager.Instance.ShowPopup<StuckPopupView>(p => p.Configure(addLaneAvailable, add, shuf, onGiveUp))
                : null;
            if (v != null) return;

            var card = BuildRuntimeOverlay("SIGNAL BLOCKED", "더 이상 이동 가능한 신호 칩이 없습니다",
                new Color(0.10f, 0.10f, 0.17f, 0.98f), new Color(1f, 0.72f, 0.2f), out var scrim);
            float y = 0.50f;
            if (addLaneAvailable)
            {
                AddOverlayButton(card, "＋  ADD LANE  (Ad)", new Color(0.12f, 0.42f, 0.40f), y, () => { Destroy(scrim); add(); });
                y -= 0.18f;
            }
            AddOverlayButton(card, "⟳  SHUFFLE  100", new Color(0.18f, 0.20f, 0.38f), y, () => { Destroy(scrim); shuf(); });
            AddOverlayButton(card, "Forfeit Stage", new Color(0.28f, 0.12f, 0.12f), y - 0.18f, () => { Destroy(scrim); onGiveUp?.Invoke(); });
        }

        public void ShowClearPanel(Action onNext, Action onLobby)
        {
            var v = UIManager.Instance != null
                ? UIManager.Instance.ShowPopup<ClearPopupView>(p => p.Configure(_board.MoveCount, onNext, onLobby))
                : null;
            if (v != null) return;

            var card = BuildRuntimeOverlay("STAGE CLEAR", $"MOVES  {_board.MoveCount}",
                new Color(0.07f, 0.16f, 0.11f, 0.98f), new Color(0.35f, 1f, 0.6f), out var scrim);
            AddOverlayButton(card, "Next Stage  ›", new Color(0.10f, 0.38f, 0.20f), 0.40f, () => { Destroy(scrim); onNext?.Invoke(); });
            AddOverlayButton(card, "Lobby", new Color(0.16f, 0.17f, 0.27f), 0.22f, () => { Destroy(scrim); onLobby?.Invoke(); });
        }

        private Transform BuildRuntimeOverlay(string title, string body, Color bg, Color titleColor, out GameObject scrim)
        {
            var s = UiUtil.Image(transform, "Scrim", null, new Color(0, 0, 0, 0.55f));
            UiUtil.Stretch(s.rectTransform);
            s.raycastTarget = true;
            scrim = s.gameObject;

            var card = UiUtil.Image(s.transform, "Card", _sprites.LaneSlot, bg);
            UiUtil.Anchors(card.rectTransform, 0.08f, 0.30f, 0.92f, 0.70f);

            var t = UiUtil.Label(card.transform, "Title", title, 50f);
            UiUtil.Anchors(t.rectTransform, 0.05f, 0.72f, 0.95f, 0.94f);
            t.color = titleColor; t.fontStyle = FontStyles.Bold;
            var b = UiUtil.Label(card.transform, "Body", body, 26f);
            UiUtil.Anchors(b.rectTransform, 0.05f, 0.58f, 0.95f, 0.72f);
            b.color = new Color(0.65f, 0.7f, 0.82f);
            return card.transform;
        }

        private void AddOverlayButton(Transform card, string label, Color color, float y, Action onClick)
        {
            var img = UiUtil.Image(card, "Btn", _sprites.LaneSlot, color);
            img.raycastTarget = true;
            UiUtil.Anchors(img.rectTransform, 0.08f, y, 0.92f, y + 0.15f);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = UiUtil.Label(img.transform, "L", label, 28f);
            UiUtil.Stretch(lbl.rectTransform);
        }

        public static void GoToScene(string scene)
        {
            if (SceneTransition.Instance != null) SceneTransition.Instance.FadeToScene(scene);
            else SceneManager.LoadScene(scene);
        }

        // Legacy tutorial hook — Signal Sort has no grid cells, so there is nothing to return.
        public CellView GetCellView(int row, int col) => null;
    }
}
