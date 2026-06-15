using UnityEngine;

namespace Game.Core.UI
{
    public class UIFloatAnimation : MonoBehaviour
    {
        [SerializeField] private float _amplitude = 4f;
        [SerializeField] private float _period    = 3f;
        [SerializeField] private bool  _randomOffset = true;

        private RectTransform _rt;
        private Vector2 _basePosition;
        private float   _phase;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _basePosition = _rt.anchoredPosition;
            _phase = _randomOffset ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        }

        private void OnEnable()  => _basePosition = _rt.anchoredPosition;
        private void OnDisable() => _rt.anchoredPosition = _basePosition;

        private void Update()
        {
            float y = Mathf.Sin(Time.time * (Mathf.PI * 2f / _period) + _phase) * _amplitude;
            _rt.anchoredPosition = _basePosition + new Vector2(0f, y);
        }
    }
}
