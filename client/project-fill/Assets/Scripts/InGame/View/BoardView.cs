using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.Rewards;
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

        [Header("Lane Layout (fixed size)")]
        [SerializeField] private float _laneWidth  = 0.95f;   // fixed lane (and chip) width — never stretched
        [SerializeField] private float _laneHeight = 3.2f;    // fits 4 stacked square chips
        [SerializeField] private float _laneGap    = 0.18f;   // fixed world gap between lanes
        [SerializeField] private int   _maxColumns = 6;       // wrap into rows past this; grid stays balanced

        private SpriteSet _sprites;
        private Board _board;
        private StageDefinition _def;
        private bool _inputBlocked;
        private bool _softStuck;
        private bool _wired;

        private SignalPanelView _panel;
        private RectTransform   _shuffleBtnRt;
        private readonly List<LaneView> _laneViews = new();

        // Visible board surface (themed inset panel the lanes/panel sit inside); sized by the resizer.
        private SpriteRenderer _surfaceFill;
        private SpriteRenderer _surfaceEdge;

        // Premium board ambient FX (drifting glow motes on the surface, behind the pieces). Fixed pool
        // animated in Update — no per-frame spawn churn. Live position tracks the resizer-driven surface.
        private Vector3 _surfaceCenter;
        private Vector2 _surfaceSize;
        private SpriteRenderer[] _motes;
        private float[] _moteOffX, _moteOffY, _motePhase, _moteSpeed;
        // Top-tier edge sparks: glow points orbiting the surface PERIMETER (distinct from internal motes).
        private SpriteRenderer[] _sparks;
        private float[] _sparkU, _sparkSpeed;

        private float _worldWidth;
        private float _panelWorldHeight;
        private float _lanesWorldHeight;
        private float _lanesScale = 1f;   // uniform fit-shrink applied to _lanesContainer (flight FX must match)
        private float _gridW, _gridH;     // unscaled lane-grid size (cached by BuildLanes for live re-fit)

        public bool IsInputBlocked => _inputBlocked;

        public event Action<int>         OnLaneTapped;
        public event Action<BoosterType> OnBoosterTapped;
        public event Action OnPauseTapped;

        private void Awake()
        {
            // Skin the board pieces from the player's active cosmetics (board master + chip/lane
            // overrides). Falls back to the default theme if cosmetics weren't fetched this session.
            var theme = BoardTheme.Resolve(
                CosmeticState.ResolveBoardSkin(),
                CosmeticState.ActiveChipSkin,
                CosmeticState.ActiveLaneSkin);
            _sprites = SpriteSet.Resolve(_skin, theme);
            if (_worldCamera == null) _worldCamera = Camera.main;
        }

        // Re-skins the board live: re-resolves the theme from CosmeticState and rebuilds the pieces
        // with the new SpriteSet. Normal flow skins once in Awake; this is the runtime re-skin path
        // (used by the dev skin switcher). Safe to call anytime after Init.
        public void RebuildSkins()
        {
            var theme = BoardTheme.Resolve(
                CosmeticState.ResolveBoardSkin(),
                CosmeticState.ActiveChipSkin,
                CosmeticState.ActiveLaneSkin);
            _sprites = SpriteSet.Resolve(_skin, theme);
            if (_surfaceFill != null)
            {
                _surfaceFill.sprite = _sprites.LaneSlot;    _surfaceFill.color = _sprites.Surface;
                _surfaceEdge.sprite = _sprites.LaneOutline; _surfaceEdge.color = _sprites.SurfaceBorder;
            }
            // Mote pool was built for the OLD skin's count — drop it so the next SetBoardSurface rebuilds
            // it for the new skin (dev skin switcher may change BoardMoteCount).
            if (_motes != null)
            {
                foreach (var m in _motes) if (m != null) Destroy(m.gameObject);
                _motes = null;
            }
            if (_sparks != null)
            {
                foreach (var s in _sparks) if (s != null) Destroy(s.gameObject);
                _sparks = null;
            }
            if (_board != null && _def != null) Init(_board, _def); // rebuilds panel + lanes with _sprites
        }

        public void SetWorldDimensions(float width, float panelHeight, float lanesHeight)
        {
            _worldWidth = width;
            _panelWorldHeight = panelHeight;
            _lanesWorldHeight = lanesHeight;
            ApplyLanesFit(); // re-fit live so margin/padding changes resize the lanes (not only at build)
        }

        // Recomputes the uniform lane-grid shrink for the current pieces area and applies it to the
        // container. Cheap (no rebuild) — the grid is authored in local space, so scaling the container
        // resizes every lane + chip + tap collider together.
        private void ApplyLanesFit()
        {
            if (_lanesContainer == null || _gridW <= 0f || _gridH <= 0f) return;
            // Clamp to a positive scale — a negative fit (from a bad/oversized margin) would mirror the
            // container (the "180° flip" artifact). Never enlarge past 1.
            _lanesScale = Mathf.Clamp(Mathf.Min(1f, _worldWidth / _gridW, _lanesWorldHeight / _gridH), 0.05f, 1f);
            _lanesContainer.localScale = Vector3.one * _lanesScale;
        }

        // Positions/sizes the visible themed board surface (called by BoardWorldResizer each frame).
        // The surface sits behind the lanes/nodes (negative sortingOrder); pieces lay out inside it.
        // worldRoot = BoardWorldRoot (the SAME world-space parent as LanesContainer/SignalPanel). The
        // resizer passes it so the surface lives in world space with the pieces — NEVER under the
        // screen-space canvas (which would mangle the world position and float it off on its own).
        public void SetBoardSurface(Transform worldRoot, Vector3 worldCenter, Vector2 worldSize)
        {
            if (_sprites == null) return; // edit-mode / pre-Awake guard
            EnsureBoardSurface(worldRoot);
            if (_surfaceFill == null) return;
            // Slight +z keeps the surface BEHIND the pieces (z=0); sortingOrder 0 (pieces ≥1) also does.
            _surfaceFill.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0.05f);
            _surfaceFill.size               = worldSize;
            _surfaceEdge.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0.04f);
            _surfaceEdge.size               = worldSize;

            _surfaceCenter = worldCenter;
            _surfaceSize   = worldSize;
            EnsureBoardMotes(worldRoot, worldSize);
            EnsureBoardSparks(worldRoot, worldSize);
        }

        // Builds the premium board mote pool once (sized to the surface). Behind the pieces, scattered
        // at random rest offsets; Update drifts + twinkles them. No-op when the skin has no motes.
        private void EnsureBoardMotes(Transform root, Vector2 worldSize)
        {
            if (_motes != null || _sprites == null || _sprites.BoardMoteCount <= 0) return;
            int n = _sprites.BoardMoteCount;
            _motes     = new SpriteRenderer[n];
            _moteOffX  = new float[n]; _moteOffY = new float[n];
            _motePhase = new float[n]; _moteSpeed = new float[n];
            var rng  = new System.Random(20260618);
            float md = worldSize.x * 0.09f; // mote diameter relative to board width
            var ac   = _sprites.Accent;
            for (int i = 0; i < n; i++)
            {
                var m = WorldUtil.CreateSprite(root, $"BoardMote_{i}", _sprites.Glow, new Color(ac.r, ac.g, ac.b, 0f), Vector2.one * md, sliced: false, sortingOrder: 0);
                _motes[i]     = m;
                _moteOffX[i]  = (float)(rng.NextDouble() - 0.5) * 0.78f; // rest offset within ±0.39 of size
                _moteOffY[i]  = (float)(rng.NextDouble() - 0.5) * 0.78f;
                _motePhase[i] = (float)rng.NextDouble() * 6.283f;
                _moteSpeed[i] = 0.25f + (float)rng.NextDouble() * 0.5f;
            }
        }

        // Builds the top-tier edge-spark pool once: glow points that orbit the surface PERIMETER (an edge
        // effect distinct from the internal motes). No-op when the skin has no edge sparks.
        private void EnsureBoardSparks(Transform root, Vector2 worldSize)
        {
            if (_sparks != null || _sprites == null || _sprites.SurfaceEdgeSparkCount <= 0) return;
            int n = _sprites.SurfaceEdgeSparkCount;
            _sparks     = new SpriteRenderer[n];
            _sparkU     = new float[n];
            _sparkSpeed = new float[n];
            var rng = new System.Random(31415926);
            float sd = worldSize.x * 0.06f;
            var ac  = _sprites.Accent;
            for (int i = 0; i < n; i++)
            {
                _sparks[i]     = WorldUtil.CreateSprite(root, $"BoardEdgeSpark_{i}", _sprites.Glow, new Color(ac.r, ac.g, ac.b, 0f), Vector2.one * sd, sliced: false, sortingOrder: 0);
                _sparkU[i]     = (float)rng.NextDouble();
                _sparkSpeed[i] = (0.05f + (float)rng.NextDouble() * 0.05f) * (rng.Next(2) == 0 ? 1f : -1f);
            }
        }

        // A point on the perimeter of a centred rectangle, parameterised by u∈[0,1) clockwise from the
        // bottom-left corner. Used to drive edge sparks around the surface frame.
        private static Vector3 EdgePoint(Vector3 c, Vector2 s, float u)
        {
            float w = s.x, h = s.y, per = 2f * (w + h), d = ((u % 1f) + 1f) % 1f * per;
            float x, y;
            if (d < w)              { x = -w * 0.5f + d;             y = -h * 0.5f; }
            else if (d < w + h)     { x =  w * 0.5f;                 y = -h * 0.5f + (d - w); }
            else if (d < 2f * w + h){ x =  w * 0.5f - (d - w - h);   y =  h * 0.5f; }
            else                    { x = -w * 0.5f;                 y =  h * 0.5f - (d - 2f * w - h); }
            return new Vector3(c.x + x, c.y + y, 0.02f);
        }

        // The accent colour driving premium FX — cycles through the spectrum on SpectrumCycle skins,
        // otherwise the skin's fixed accent.
        private Color FxAccent()
            => _sprites.SpectrumCycle
                ? Color.HSVToRGB((Time.unscaledTime * 0.08f) % 1f, 0.75f, 1f)
                : _sprites.Accent;

        // Per-frame premium board FX: surface-frame colour-shift breathe + drifting motes + orbiting edge
        // sparks. All gated by the skin tokens (cheap boards = 0 = nothing runs).
        private void BoardSurfaceFx()
        {
            if (_sprites == null) return;
            float pulse = Mathf.Sin(Time.unscaledTime * 2.2f) * 0.5f + 0.5f;
            Color acc   = FxAccent();

            if (_surfaceEdge != null && _sprites.SurfaceEdgePulse > 0f)
            {
                float amp     = _sprites.SurfaceEdgePulse;
                var   b       = _sprites.SurfaceBorder;
                Color baseCol = _sprites.SpectrumCycle ? acc : b;                  // spectrum overrides hue
                Color shifted = Color.Lerp(baseCol, Color.white, 0.35f * amp * pulse); // colour shift, not just alpha
                float a       = Mathf.Clamp01((_sprites.SpectrumCycle ? 0.9f : b.a) * Mathf.Lerp(1f - 0.4f * amp, 1f + 0.3f * amp, pulse));
                _surfaceEdge.color = new Color(shifted.r, shifted.g, shifted.b, a);
            }

            if (_motes != null)
            {
                for (int i = 0; i < _motes.Length; i++)
                {
                    float t  = Time.unscaledTime * _moteSpeed[i] + _motePhase[i];
                    float dx = Mathf.Sin(t) * 0.06f;
                    float dy = Mathf.Cos(t * 0.8f) * 0.06f;
                    _motes[i].transform.position = new Vector3(
                        _surfaceCenter.x + (_moteOffX[i] + dx) * _surfaceSize.x,
                        _surfaceCenter.y + (_moteOffY[i] + dy) * _surfaceSize.y,
                        0.03f); // in front of the surface (z 0.05), behind the pieces (sortingOrder ≥1)
                    float tw = Mathf.Sin(t * 1.7f) * 0.5f + 0.5f;
                    _motes[i].color = new Color(acc.r, acc.g, acc.b, Mathf.Lerp(0.05f, 0.22f, tw));
                }
            }

            if (_sparks != null)
            {
                for (int i = 0; i < _sparks.Length; i++)
                {
                    float u = _sparkU[i] + Time.unscaledTime * _sparkSpeed[i];
                    _sparks[i].transform.position = EdgePoint(_surfaceCenter, _surfaceSize, u);
                    float tw = Mathf.Sin(u * Mathf.PI * 4f + Time.unscaledTime * 3f) * 0.5f + 0.5f;
                    _sparks[i].color = new Color(acc.r, acc.g, acc.b, Mathf.Lerp(0.12f, 0.55f, tw));
                }
            }
        }

        private void EnsureBoardSurface(Transform worldRoot)
        {
            // Resolve the world root robustly: explicit arg → lanes' parent → scene lookup → self.
            Transform root = worldRoot != null ? worldRoot
                : (_lanesContainer != null && _lanesContainer.parent != null) ? _lanesContainer.parent
                : (GameObject.Find("BoardWorldRoot")?.transform ?? transform);

            if (_surfaceFill == null)
            {
                // sortingOrder 0: above the camera clear / background, below every lane piece (lowest = 1).
                _surfaceFill = WorldUtil.CreateSprite(root, "BoardSurface",     _sprites.LaneSlot,    _sprites.Surface,       Vector2.one, sortingOrder: 0);
                _surfaceEdge = WorldUtil.CreateSprite(root, "BoardSurfaceEdge", _sprites.LaneOutline, _sprites.SurfaceBorder, Vector2.one, sortingOrder: 0);
            }
            else if (root != null && _surfaceFill.transform.parent != root)
            {
                // Was created before the world root resolved (parented under the canvas) — relocate it.
                _surfaceFill.transform.SetParent(root, worldPositionStays: false);
                _surfaceEdge.transform.SetParent(root, worldPositionStays: false);
            }
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
            float laneW = _laneWidth;
            float laneH = _laneHeight;
            float gap   = _laneGap;

            // Balanced grid by a FIXED column cap (not width-derived) so each lane keeps a constant size.
            int cols = Mathf.Min(n, Mathf.Max(1, _maxColumns));
            int rows = Mathf.CeilToInt((float)n / cols);
            cols = Mathf.CeilToInt((float)n / rows);   // balance rows (e.g. 12 → 6×2, not 6+6 vs 7+5)

            // Lanes never stretch: the fixed-size grid is uniformly shrunk (never enlarged) to fit the
            // container. Chips + tap colliders are children, so they scale with the container.
            float gridW = laneW * cols + gap * (cols - 1);
            float gridH = laneH * rows + gap * (rows - 1);
            _gridW = gridW;
            _gridH = gridH;
            _lanesScale = Mathf.Min(1f, containerWidth / gridW, containerHeight / gridH);
            _lanesContainer.localScale = Vector3.one * _lanesScale;

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
                lv.OnTapped += tapped => OnLaneTapped?.Invoke(tapped); // controller queues taps during move anim
                _laneViews.Add(lv);

                // Tag the lane by id (slot_lane_1…) and, if it carries a gimmick, by gimmick id
                // (gimmick_locked/blind/overload) so GimmickAppear tutorials can highlight the real lane.
                var laneIds = new List<string> { $"slot_lane_{i + 1}" };
                var laneModel = _board.Lanes[i];
                if (laneModel.Kind == LaneKind.Locked)     laneIds.Add("gimmick_locked");
                else if (laneModel.Kind == LaneKind.Blind) laneIds.Add("gimmick_blind");
                foreach (var c in laneModel.Chips) if (c.Overload) { laneIds.Add("gimmick_overload"); break; }
                var laneTt = lv.gameObject.GetComponent<TutorialTarget>() ?? lv.gameObject.AddComponent<TutorialTarget>();
                laneTt.SetIds(laneIds.ToArray());
            }

            // Whole-lane-area target (tut.free_sort step).
            if (_lanesContainer != null)
            {
                var areaTt = _lanesContainer.GetComponent<TutorialTarget>() ?? _lanesContainer.gameObject.AddComponent<TutorialTarget>();
                areaTt.SetIds("slot_lane_area");
            }

            // Signal Panel target (relay/panel explanation steps).
            if (_panelContainer != null)
            {
                var panelTt = _panelContainer.GetComponent<TutorialTarget>() ?? _panelContainer.gameObject.AddComponent<TutorialTarget>();
                panelTt.SetIds("signal_panel");
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

            // Shuffle / AddLane are OWNED items: show the held quantity (× N), not a gold price.
            // Undo is free, single-step — icon alone conveys it (no count, no locale text); button greys out via CanUndo.
            var undoTxt = FindDeep(_undoBtn.transform, "Label")?.GetComponent<TMP_Text>()
                       ?? FindDeep(_undoBtn.transform, "Text")?.GetComponent<TMP_Text>();
            if (undoTxt) undoTxt.text = "";

            var shuffleTxt = FindDeep(_shuffleBtn.transform, "Label")?.GetComponent<TMP_Text>()
                          ?? FindDeep(_shuffleBtn.transform, "Text")?.GetComponent<TMP_Text>();
            if (shuffleTxt) shuffleTxt.text = OwnedLabel(BoosterType.Shuffle);

            if (_addLaneLabel) _addLaneLabel.text = OwnedLabel(BoosterType.AddLane);
        }

        // Booster-bar label: held quantity (× N). Out of stock shows × 0 (tapping still instant-buys one
        // with gold via InGameController.BuyBoosterThenUse — the price is not surfaced on the bar).
        private static string OwnedLabel(BoosterType type)
        {
            var progress = Game.Services.PlayerProgressService.Instance;
            int owned = progress != null ? progress.GetItemCount(type.ItemId()) : 0;
            return $"× {owned}";
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
            Vector2 size = toLv.ChipPixelSize() * _lanesScale;  // flight layer is unscaled — match the shrunk board
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
                UpdateHud();        // move count
                UpdateBoosters();   // re-enable Undo (CanUndo) after a plain move
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
            var landed = new bool[nF];
            float t = 0f;
            while (t < total)
            {
                for (int k = 0; k < nF; k++)
                {
                    if (flyers[k] == null) continue;
                    float p    = Mathf.Clamp01((t - k * stagger) / dur);
                    float fall = 1f - p * p;                          // ease-in: accelerate downward
                    flyers[k].Rt.position   = to[k] + Vector3.up * (dropH * fall);
                    if (p >= 1f && !landed[k])                        // impact accent the instant it touches the slot
                    {
                        landed[k] = true;
                        var col = flyers[k].Chip.Type.ToColor();
                        // Impact: a flattened shockwave ring hugging the slot floor + a few motes kicked up.
                        SpawnRing(to[k], col, size.x * 0.6f, size.x * 1.8f, 0.24f, flattenY: 0.35f);
                        SpawnMotes(to[k], col, 3);
                    }
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

            // Register = "signal locked": double shockwave + pixel data-bit shards + a light pulse that
            // propagates along the connector to the next node in the chain.
            _panel.PlayRegister(type);
            var col = type.ToColor();
            SpawnRing(nodePos, col, size.x * 0.7f, size.x * 2.0f, 0.30f);   // inner crisp pulse
            SpawnRing(nodePos, col, size.x * 1.1f, size.x * 2.9f, 0.44f);   // outer wide wave
            SpawnShards(nodePos, col, 12);
            if (_panel.TryGetTraceTarget(type, out var traceFrom, out var traceTo))
                SpawnTrace(traceFrom, traceTo, col);

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

        // Upward-kicked motes: a small cone of dust flicked up off the slot on landing (BurstParticle's
        // built-in downward gravity arcs them back down for weight).
        private void SpawnMotes(Vector3 worldPos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float rSize = UnityEngine.Random.Range(0.05f, 0.10f);
                var dot = WorldUtil.CreateSprite(_flightLayer, "Mote", _sprites.Disc, color, new Vector2(rSize, rSize), sliced: false, sortingOrder: 31);
                dot.transform.position = worldPos;
                float ang = Mathf.PI * 0.5f + UnityEngine.Random.Range(-0.5f, 0.5f); // up ± spread
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * UnityEngine.Random.Range(0.8f, 1.6f);
                StartCoroutine(BurstParticle(dot, dir, UnityEngine.Random.Range(0.35f, 0.55f)));
            }
        }

        // Pixel data-bit shards: small rounded squares that burst radially, spin, and fall — reads as
        // bits of signal scattering when a set registers (used on node register only).
        private void SpawnShards(Vector3 worldPos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float rSize = UnityEngine.Random.Range(0.10f, 0.18f);
                var sh = WorldUtil.CreateSprite(_flightLayer, "Shard", _sprites.Chip, color, new Vector2(rSize, rSize), sliced: false, sortingOrder: 31);
                sh.transform.position = worldPos;
                float ang  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var dir    = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * UnityEngine.Random.Range(1.2f, 2.6f);
                float spin = UnityEngine.Random.Range(-360f, 360f);
                StartCoroutine(ShardParticle(sh, dir, spin, UnityEngine.Random.Range(0.45f, 0.7f)));
            }
        }

        // Light pulse that runs along the connector from a registered node to the next node in the chain.
        private void SpawnTrace(Vector3 from, Vector3 to, Color color)
        {
            float headSize = 0.30f;
            var head = WorldUtil.CreateSprite(_flightLayer, "Trace", _sprites.Glow, color, new Vector2(headSize, headSize), sliced: false, sortingOrder: 32);
            head.transform.position = from;
            StartCoroutine(TraceRoutine(head, from, to, color));
        }

        // Expanding, fading ripple ring used as an impact accent (chip landing, node register).
        // flattenY < 1 squashes it into a ground-hugging shockwave (used on landing).
        private void SpawnRing(Vector3 worldPos, Color color, float startSize, float endSize, float life, float flattenY = 1f)
        {
            var ring = WorldUtil.CreateSprite(_flightLayer, "Ripple", _sprites.Ring, color, new Vector2(startSize, startSize), sliced: false, sortingOrder: 30);
            ring.transform.position = worldPos;
            StartCoroutine(RingRoutine(ring, startSize, endSize, life, flattenY));
        }

        private IEnumerator RingRoutine(SpriteRenderer sr, float startSize, float endSize, float life, float flattenY)
        {
            Vector3 baseScale = sr.transform.localScale; // WorldUtil already fit this to startSize
            Color baseCol = sr.color;
            float t = 0f;
            while (t < life)
            {
                float p = t / life;
                float f = Mathf.Lerp(startSize, endSize, p) / startSize;
                sr.transform.localScale = new Vector3(baseScale.x * f, baseScale.y * f * flattenY, baseScale.z);
                var c = baseCol; c.a = baseCol.a * (1f - p); sr.color = c;
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        private IEnumerator ShardParticle(SpriteRenderer sr, Vector3 dir, float spin, float life)
        {
            Vector3 start = sr.transform.position;
            Vector3 baseScale = sr.transform.localScale;
            float t = 0f;
            while (t < life)
            {
                float p = t / life;
                sr.transform.position      = start + dir * p + Vector3.down * (2.0f * p * p);
                sr.transform.localScale    = baseScale * (1f - p);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, spin * p);
                if (sr) { var c = sr.color; c.a = 1f - p; sr.color = c; }
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(sr.gameObject);
        }

        private IEnumerator TraceRoutine(SpriteRenderer head, Vector3 from, Vector3 to, Color color)
        {
            float life = 0.28f, t = 0f;
            while (t < life)
            {
                float p = t / life;
                head.transform.position = Vector3.Lerp(from, to, p);
                var c = color; c.a = Mathf.Sin(p * Mathf.PI); head.color = c; // 0→1→0 streak
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(head.gameObject);
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

            BoardSurfaceFx();
        }

        public void BlockInput(bool block) => _inputBlocked = block;

        // ── Overlays (route through UIManager — scrim = popup backdrop convention) ────

        public void ShowStuckPanel(bool addLaneAvailable, Action onAddLane, Action onRetry, Action onGiveUp)
        {
            Action add   = () => { BlockInput(false); onAddLane?.Invoke(); };
            Action retry = () => { BlockInput(false); onRetry?.Invoke(); };

            var v = UIManager.Instance != null
                ? UIManager.Instance.ShowPopup<FailOverlayView>(p => p.Configure(addLaneAvailable, add, retry, onGiveUp))
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
            AddOverlayButton(card, "Retry Stage", new Color(0.12f, 0.30f, 0.34f), y, () => { Destroy(scrim); retry(); });
            AddOverlayButton(card, "Forfeit Stage", new Color(0.28f, 0.12f, 0.12f), y - 0.18f, () => { Destroy(scrim); onGiveUp?.Invoke(); });
        }

        public void ShowClearPanel(int stageId, string attemptId,
            IReadOnlyList<GrantedRewardDto> rewards, bool canDouble, ClearSummary? summary, Action onNext, Action onLobby)
        {
            var v = UIManager.Instance != null
                ? UIManager.Instance.ShowPopup<ResultOverlayView>(p => p.Configure(stageId, attemptId, rewards, canDouble, summary, onNext, onLobby))
                : null;
            if (v != null) return;

            string body = summary.HasValue
                ? $"MOVES  {summary.Value.Moves}   BEST  {summary.Value.Best}"
                : $"MOVES  {_board.MoveCount}";
            var card = BuildRuntimeOverlay("STAGE CLEAR", body,
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
