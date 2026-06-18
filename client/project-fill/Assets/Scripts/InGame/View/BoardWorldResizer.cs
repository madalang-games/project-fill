using UnityEngine;

namespace Game.InGame.View
{
    [ExecuteAlways]
    public class BoardWorldResizer : MonoBehaviour
    {
        [SerializeField] private RectTransform _gameArea;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private BoardView _boardView;

        [Header("World Space Root Containers")]
        [SerializeField] private Transform _boardWorldRoot;
        [SerializeField] private Transform _lanesContainer;
        [SerializeField] private Transform _panelContainer;
        [SerializeField] private Transform _flightLayer;

        [Header("Chrome to clear (board fits between these)")]
        [SerializeField] private RectTransform _topBar;    // HUD strip (top)
        [SerializeField] private RectTransform _bottomBar; // BoosterBar (bottom)

        [Header("Board Layout")]
        [Range(0f, 0.45f)] [SerializeField] private float _marginX         = 0.04f;  // board inset from left/right (fraction of viewport width)
        [Range(0f, 0.45f)] [SerializeField] private float _barGap          = 0.02f;  // gap between board and HUD/BoosterBar (fraction of usable band)
        [Range(0f, 0.45f)] [SerializeField] private float _innerPad        = 0.015f; // pieces inset from the board surface edge (small = easy tap)
        [Range(0.05f, 0.6f)] [SerializeField] private float _panelHeightFrac = 0.16f; // Signal Panel share of inner height (lanes take the rest)

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_worldCamera == null) _worldCamera = Camera.main;
            if (_boardView == null) _boardView = GetComponent<BoardView>();

            var safeArea = transform.Find("SafeAreaRoot");
            if (_gameArea == null && safeArea != null) _gameArea = safeArea.Find("GameArea") as RectTransform;
            if (_topBar == null && safeArea != null) _topBar = safeArea.Find("HUD") as RectTransform;
            if (_bottomBar == null && safeArea != null) _bottomBar = safeArea.Find("BoosterBar") as RectTransform;

            if (_boardWorldRoot == null)
            {
                var go = GameObject.Find("BoardWorldRoot");
                if (go != null) _boardWorldRoot = go.transform;
            }
            if (_boardWorldRoot != null)
            {
                if (_lanesContainer == null) _lanesContainer = _boardWorldRoot.Find("LanesContainer");
                if (_panelContainer == null) _panelContainer = _boardWorldRoot.Find("SignalPanel");
                if (_flightLayer == null) _flightLayer = _boardWorldRoot.Find("FlightLayer");
            }
        }

        private void LateUpdate()
        {
            ResizeBoard();
        }

        public void ResizeBoard()
        {
            ResolveReferences();

            if (_worldCamera == null || !_worldCamera.orthographic) return;

            // Clamp tunables to sane ranges so live inspector scrubbing can't drive a negative board
            // size (which flips the container scale → the "screen rotates 180°" artifact).
            float marginX   = Mathf.Clamp(_marginX, 0f, 0.45f);
            float barGap    = Mathf.Clamp(_barGap, 0f, 0.45f);
            float innerPad  = Mathf.Clamp(_innerPad, 0f, 0.45f);
            float panelFrac = Mathf.Clamp(_panelHeightFrac, 0.05f, 0.60f);

            // The canvas render camera (for mapping HUD/BoosterBar UI corners into world space).
            RectTransform canvasRef = _gameArea != null ? _gameArea : (_topBar != null ? _topBar : _bottomBar);
            Camera eventCamera = null;
            if (canvasRef != null)
            {
                Canvas canvas = canvasRef.GetComponentInParent<Canvas>();
                eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            }

            // Base rect = the orthographic camera's VISIBLE world viewport. Sizing off this (not GameArea,
            // which can extend past the screen) guarantees the board always fits left↔right on screen.
            float halfH = _worldCamera.orthographicSize;
            float halfW = halfH * _worldCamera.aspect;
            Vector3 camPos = _worldCamera.transform.position;
            float centerX  = camPos.x;
            float viewTop  = camPos.y + halfH;
            float viewBot  = camPos.y - halfH;
            float viewW    = 2f * halfW;

            if (_boardWorldRoot != null) _boardWorldRoot.position = new Vector3(camPos.x, camPos.y, 0f);

            // Usable vertical band = viewport minus the HUD (top) and BoosterBar (bottom) so the board
            // never hides behind them. Bars fall back to the viewport edges if not found.
            float usableTop = viewTop;
            float usableBot = viewBot;
            if (TryWorldRect(_topBar, eventCamera, out _, out _, out _, out _, out float hudBot))
                usableTop = Mathf.Min(usableTop, hudBot);
            if (TryWorldRect(_bottomBar, eventCamera, out _, out _, out _, out float barTop, out _))
                usableBot = Mathf.Max(usableBot, barTop);

            float bandH = usableTop - usableBot;
            if (bandH <= 0f || viewW <= 0f) return;

            // Board surface: viewport width minus left/right margin; height = band minus a small gap.
            float gap          = bandH * barGap;
            float boardW       = viewW * (1f - 2f * marginX);
            float boardH       = bandH - 2f * gap;
            float boardCenterY = (usableTop + usableBot) * 0.5f;
            Vector3 boardCenter = new Vector3(centerX, boardCenterY, 0f);

            if (_boardView != null)
                _boardView.SetBoardSurface(_boardWorldRoot, boardCenter, new Vector2(boardW, boardH));

            // Pieces sit INSIDE the surface with a small padding (small → larger lanes/chips, easier taps).
            float piecesW = boardW * (1f - 2f * innerPad);
            float piecesH = boardH * (1f - 2f * innerPad);

            float panelWorldHeight = piecesH * panelFrac;
            float lanesWorldHeight = piecesH - panelWorldHeight;

            if (_panelContainer != null)
            {
                float panelY = boardCenterY + piecesH * 0.5f - panelWorldHeight * 0.5f;
                _panelContainer.position = new Vector3(centerX, panelY, 0f);
            }

            if (_lanesContainer != null)
            {
                float lanesY = boardCenterY - piecesH * 0.5f + lanesWorldHeight * 0.5f;
                _lanesContainer.position = new Vector3(centerX, lanesY, 0f);
            }

            if (_flightLayer != null)
            {
                _flightLayer.position = boardCenter;
            }

            if (_boardView != null)
            {
                _boardView.SetWorldDimensions(piecesW, panelWorldHeight, lanesWorldHeight);
            }
        }

        // Converts a RectTransform's corners into the world camera's coordinate space (the board lives
        // in World Space under the orthographic camera, the UI lives on the canvas).
        private bool TryWorldRect(RectTransform rt, Camera eventCamera,
            out Vector3 center, out float width, out float height, out float top, out float bottom)
        {
            center = default; width = height = top = bottom = 0f;
            if (rt == null) return false;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

            float dist = Mathf.Abs(_worldCamera.transform.position.z);
            Vector3 worldBL = _worldCamera.ScreenToWorldPoint(new Vector3(screenBL.x, screenBL.y, dist));
            Vector3 worldTR = _worldCamera.ScreenToWorldPoint(new Vector3(screenTR.x, screenTR.y, dist));

            center = (worldBL + worldTR) * 0.5f; center.z = 0f;
            width  = worldTR.x - worldBL.x;
            height = worldTR.y - worldBL.y;
            top    = worldTR.y;
            bottom = worldBL.y;
            return true;
        }
    }
}
