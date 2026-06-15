using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    public class PerfectClearEffectView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _perfectText;
        [SerializeField] private ParticleSystem _confetti;

        private const float TextPopDuration = 0.4f;
        private const float HoldDuration    = 2.0f;
        private const float WobblePeriod    = 1.0f;
        private const float WobbleAngle     = 3f;

        public void Play(Action onComplete = null)
            => StartCoroutine(PlaySequence(onComplete));

        private IEnumerator PlaySequence(Action onComplete)
        {
            // text pop in
            if (_perfectText != null)
            {
                _perfectText.gameObject.SetActive(true);
                var rt = _perfectText.GetComponent<RectTransform>();
                rt.localScale = Vector3.zero;

                float elapsed = 0f;
                while (elapsed < TextPopDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = UIEasing.EaseOutBack(Mathf.Clamp01(elapsed / TextPopDuration));
                    rt.localScale = Vector3.one * Mathf.Lerp(0f, 1f, t);
                    yield return null;
                }
                rt.localScale = Vector3.one;
            }

            if (_confetti != null) _confetti.Play();

            // wobble + hold
            float holdElapsed = 0f;
            while (holdElapsed < HoldDuration)
            {
                holdElapsed += Time.deltaTime;
                if (_perfectText != null)
                {
                    float angle = Mathf.Sin(holdElapsed * (Mathf.PI * 2f / WobblePeriod)) * WobbleAngle;
                    _perfectText.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
