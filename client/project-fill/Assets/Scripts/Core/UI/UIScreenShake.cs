using System.Collections;
using UnityEngine;

namespace Game.Core.UI
{
    public class UIScreenShake : MonoBehaviour
    {
        private RectTransform _rt;
        private Vector2 _origin;

        public enum ShakeLevel { Medium, Heavy }

        private void Awake()
        {
            _rt     = GetComponent<RectTransform>();
            _origin = _rt.anchoredPosition;
        }

        public void Shake(ShakeLevel level)
        {
            StopAllCoroutines();
            switch (level)
            {
                case ShakeLevel.Medium: StartCoroutine(ShakeSequence(6f,  0.2f, 3)); break;
                case ShakeLevel.Heavy:  StartCoroutine(ShakeSequence(10f, 0.35f, 4)); break;
            }
        }

        private IEnumerator ShakeSequence(float amplitude, float duration, int oscillations)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t   = elapsed / duration;
                float env = 1f - t;
                float x   = Mathf.Sin(t * oscillations * Mathf.PI * 2f) * amplitude * env;
                float y   = Mathf.Sin(t * oscillations * Mathf.PI * 2f + 1f) * amplitude * 0.5f * env;
                _rt.anchoredPosition = _origin + new Vector2(x, y);
                yield return null;
            }
            _rt.anchoredPosition = _origin;
        }
    }
}
