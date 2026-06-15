using UnityEngine;

namespace Game.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafeArea;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void OnEnable() => ApplySafeArea();

        private void OnRectTransformDimensionsChange() => ApplySafeArea();

        private void ApplySafeArea()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            var safe = Screen.safeArea;
            if (safe == _lastSafeArea) return;
            _lastSafeArea = safe;

            var screenSize = new Vector2(Screen.width, Screen.height);
            _rt.anchorMin = safe.position / screenSize;
            _rt.anchorMax = (safe.position + safe.size) / screenSize;
        }
    }
}
