using UnityEngine;

namespace Game.Core.UI
{
    public class UIScalePulse : MonoBehaviour
    {
        [SerializeField] private float _minScale = 1f;
        [SerializeField] private float _maxScale = 1.12f;
        [SerializeField] private float _period   = 1.2f;

        private RectTransform _rt;

        private void Awake() => _rt = GetComponent<RectTransform>();
        private void OnDisable() => _rt.localScale = Vector3.one;

        private void Update()
        {
            float t = (Mathf.Sin(Time.time * (Mathf.PI * 2f / _period)) + 1f) * 0.5f;
            float s = Mathf.Lerp(_minScale, _maxScale, t);
            _rt.localScale = Vector3.one * s;
        }
    }
}
