using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public class UIPulseGlowEffect : MonoBehaviour
    {
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _minScale = 0.95f;
        [SerializeField] private float _maxScale = 1.15f;
        [SerializeField] private float _rotationSpeed = 30f;

        private RectTransform _rectTransform;
        private Image _image;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
        }

        private void Update()
        {
            // Pulse Scale
            float scale = _minScale + (_maxScale - _minScale) * (0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed));
            if (_rectTransform != null)
            {
                _rectTransform.localScale = new Vector3(scale, scale, 1f);
                
                // Slow rotation
                _rectTransform.Rotate(Vector3.forward * (_rotationSpeed * Time.deltaTime));
            }

            // Pulse Opacity/Emission
            if (_image != null)
            {
                Color c = _image.color;
                c.a = 0.6f + 0.4f * Mathf.Sin(Time.time * _pulseSpeed * 1.5f);
                _image.color = c;
            }
        }
    }
}
