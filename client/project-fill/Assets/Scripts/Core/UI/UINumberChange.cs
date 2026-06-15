using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class UINumberChange : MonoBehaviour
    {
        [SerializeField] private float  _punchScale    = 1.35f;
        [SerializeField] private float  _punchDuration = 0.28f;
        [SerializeField] private Color  _decreaseColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color  _increaseColor = new Color(0.38f, 1f, 0.55f);
        [SerializeField] private float  _flashDuration = 0.20f;
        [SerializeField] private string _formatString  = "{0}";

        private TMP_Text  _text;
        private Color     _baseColor;
        private Coroutine _anim;
        private int       _lastValue = int.MinValue;

        private void Awake()
        {
            _text      = GetComponent<TMP_Text>();
            _baseColor = _text.color;
        }

        // Set integer value; silent on first call, animated on subsequent changes.
        public void Set(int value)
        {
            if (value == _lastValue) return;
            bool isFirst   = _lastValue == int.MinValue;
            bool decreased = !isFirst && value < _lastValue;
            _lastValue = value;
            _text.text = string.Format(_formatString, value);
            if (!isFirst) PlayAnim(decreased);
        }

        // Set non-integer display (e.g. "∞"); resets tracking so next Set() animates.
        public void SetRaw(string text)
        {
            _lastValue = int.MinValue;
            if (_text != null) _text.text = text;
        }

        private void PlayAnim(bool decreased)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimCoroutine(decreased));
        }

        private IEnumerator AnimCoroutine(bool decreased)
        {
            var   rt         = (RectTransform)transform;
            Color flashColor = decreased ? _decreaseColor : _increaseColor;
            float half       = _punchDuration * 0.35f;
            float elapsed    = 0f;

            while (elapsed < _punchDuration)
            {
                elapsed += Time.deltaTime;

                float scale = elapsed < half
                    ? Mathf.Lerp(1f, _punchScale, UIEasing.EaseOut(Mathf.Clamp01(elapsed / half)))
                    : Mathf.Lerp(_punchScale, 1f,  UIEasing.EaseOut(Mathf.Clamp01((elapsed - half) / (_punchDuration - half))));
                rt.localScale = Vector3.one * scale;

                float colorT = Mathf.Clamp01(elapsed / _flashDuration);
                _text.color  = Color.Lerp(flashColor, _baseColor, UIEasing.EaseOut(colorT));

                yield return null;
            }

            rt.localScale = Vector3.one;
            _text.color   = _baseColor;
            _anim         = null;
        }
    }
}
