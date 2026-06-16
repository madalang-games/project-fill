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

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_worldCamera == null) _worldCamera = Camera.main;
            if (_boardView == null) _boardView = GetComponent<BoardView>();
            if (_gameArea == null)
            {
                var safeArea = transform.Find("SafeAreaRoot");
                if (safeArea != null) _gameArea = safeArea.Find("GameArea") as RectTransform;
            }
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

            if (_gameArea == null || _worldCamera == null) return;

            Vector3[] canvasCorners = new Vector3[4];
            _gameArea.GetWorldCorners(canvasCorners);

            Canvas canvas = _gameArea.GetComponentInParent<Canvas>();
            Camera eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(eventCamera, canvasCorners[0]);
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(eventCamera, canvasCorners[2]);

            float dist = Mathf.Abs(_worldCamera.transform.position.z);
            Vector3 worldBL = _worldCamera.ScreenToWorldPoint(new Vector3(screenBL.x, screenBL.y, dist));
            Vector3 worldTR = _worldCamera.ScreenToWorldPoint(new Vector3(screenTR.x, screenTR.y, dist));

            Vector3 worldCenter = (worldBL + worldTR) * 0.5f;
            worldCenter.z = 0f;

            float worldWidth = worldTR.x - worldBL.x;
            float worldHeight = worldTR.y - worldBL.y;

            if (_boardWorldRoot != null)
            {
                _boardWorldRoot.position = worldCenter;
            }

            float screenHeight = screenTR.y - screenBL.y;
            if (screenHeight <= 0f) return;

            float panelHeightFrac = 200f / screenHeight;
            float panelWorldHeight = worldHeight * panelHeightFrac;
            float lanesWorldHeight = worldHeight * (1f - panelHeightFrac);

            if (_panelContainer != null)
            {
                float panelY = worldHeight * 0.5f - panelWorldHeight * 0.5f;
                _panelContainer.position = new Vector3(worldCenter.x, worldCenter.y + panelY, 0f);
            }

            if (_lanesContainer != null)
            {
                float lanesY = -worldHeight * 0.5f + lanesWorldHeight * 0.5f;
                _lanesContainer.position = new Vector3(worldCenter.x, worldCenter.y + lanesY, 0f);
            }

            if (_flightLayer != null)
            {
                _flightLayer.position = worldCenter;
            }

            if (_boardView != null)
            {
                _boardView.SetWorldDimensions(worldWidth, panelWorldHeight, lanesWorldHeight);
            }
        }
    }
}
