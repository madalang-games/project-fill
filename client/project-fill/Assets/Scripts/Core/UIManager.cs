using System;
using System.Collections.Generic;
using Game.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private Canvas _popupCanvas;
        private Canvas _overlayCanvas;
        private Canvas _toastCanvas;
        private Canvas _loadingCanvas;

        private ToastView _toast;
        private LoadingOverlayView _loadingOverlay;
        private NetworkErrorView _networkError;

        private readonly Stack<GameObject> _popupStack = new Stack<GameObject>();
        private GameObject _currentOverlay;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvases();
            CreateStaticInstances();
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame &&
                _popupStack.Count > 0)
                CloseTopPopup();
        }

        public T ShowPopup<T>(Action<T> onInit = null) where T : MonoBehaviour
        {
            string path = $"Prefabs/UI/{typeof(T).Name}";
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) { Debug.LogError($"[UIManager] Prefab not found: {path}"); return null; }
            var go = Instantiate(prefab, _popupCanvas.transform);
            var view = go.GetComponent<T>();
            onInit?.Invoke(view);
            _popupStack.Push(go);
            return view;
        }

        public T ShowOverlay<T>(Action<T> onInit = null) where T : MonoBehaviour
        {
            if (_currentOverlay != null)
            {
                Destroy(_currentOverlay);
                _currentOverlay = null;
            }
            string path = $"Prefabs/UI/{typeof(T).Name}";
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) { Debug.LogError($"[UIManager] Prefab not found: {path}"); return null; }
            var go = Instantiate(prefab, _overlayCanvas.transform);
            var view = go.GetComponent<T>();
            onInit?.Invoke(view);
            _currentOverlay = go;
            return view;
        }

        public void ShowToast(string message, ToastType type = ToastType.Warning)
        {
            if (_toast == null)
            {
                Debug.Log("[UIManager] ToastView is missing, attempting to recreate...");
                _toast = LoadStatic<ToastView>(_toastCanvas, "Prefabs/UI/ToastView");
            }

            if (_toast != null) 
            {
                _toast.gameObject.SetActive(true);
                _toast.Show(message, type);
            }
            else
            {
                Debug.LogError($"[UIManager] Failed to show toast. Prefab might be missing: Prefabs/UI/ToastView");
            }
        }

        public void ShowLoading()
        {
            if (_loadingOverlay != null) _loadingOverlay.Show();
        }

        public void HideLoading()
        {
            if (_loadingOverlay != null) _loadingOverlay.Hide();
        }

        public void ShowNetworkError(Action onRetry)
        {
            HideLoading();
            if (_networkError != null) _networkError.Show(onRetry);
        }

        public void HideNetworkError()
        {
            if (_networkError != null) _networkError.Hide();
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count == 0) return;
            Destroy(_popupStack.Pop());
        }

        public T GetCurrentOverlay<T>() where T : MonoBehaviour
            => _currentOverlay != null ? _currentOverlay.GetComponent<T>() : null;

        public void CloseAllPopups()
        {
            while (_popupStack.Count > 0)
                Destroy(_popupStack.Pop());
        }

        public void CloseOverlay()
        {
            if (_currentOverlay == null) return;
            Destroy(_currentOverlay);
            _currentOverlay = null;
        }

        private void BuildCanvases()
        {
            _popupCanvas   = CreateCanvas("Canvas_Popup",   10);
            _overlayCanvas = CreateCanvas("Canvas_Overlay", 20);
            _toastCanvas   = CreateCanvas("Canvas_Toast",   30);
            _loadingCanvas = CreateCanvas("Canvas_Loading", 100);
        }

        private Canvas CreateCanvas(string canvasName, int sortOrder)
        {
            var go = new GameObject(canvasName);
            go.transform.SetParent(transform);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void CreateStaticInstances()
        {
            _toast          = LoadStatic<ToastView>(_toastCanvas,   "Prefabs/UI/ToastView");
            _loadingOverlay = LoadStatic<LoadingOverlayView>(_loadingCanvas, "Prefabs/UI/LoadingOverlayView");
            _networkError   = LoadStatic<NetworkErrorView>(_loadingCanvas,   "Prefabs/UI/NetworkErrorView");
        }

        private T LoadStatic<T>(Canvas parent, string path) where T : MonoBehaviour
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[UIManager] Static prefab not found: {path}");
                return null;
            }
            var go = Instantiate(prefab, parent.transform);
            var view = go.GetComponent<T>();
            go.SetActive(false);
            return view;
        }
    }
}
