using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public class ChapterUnlockOverlayView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup    _dimGroup;
        [SerializeField] private RectTransform  _chapterCard;
        [SerializeField] private TMP_Text       _chapterLabel;
        [SerializeField] private ParticleSystem _fanfare;
        [SerializeField] private GraphicRaycaster _raycaster;

        // 2.7s total timeline per spec
        private const float DimDuration      = 0.2f;
        private const float FlyInDuration    = 0.4f;
        private const float TextPopDuration  = 0.3f;
        private const float FanfareDuration  = 1.0f;
        private const float HoldDuration     = 0.5f;
        private const float FadeOutDuration  = 0.3f;
        private const float FlyInStartOffset = -400f;

        private void Awake()
        {
            if (_raycaster != null) _raycaster.enabled = false;
        }

        public void Play(int chapterNumber, Action onComplete)
        {
            if (_chapterLabel != null) _chapterLabel.text = $"Chapter {chapterNumber} Unlocked!";
            StartCoroutine(PlaySequence(onComplete));
        }

        private IEnumerator PlaySequence(Action onComplete)
        {
            // block interaction
            if (_raycaster != null) _raycaster.enabled = false;

            // 0.0s dim fade-in
            yield return FadeDim(0f, 0.7f, DimDuration);

            // 0.2s card fly-in from bottom
            if (_chapterCard != null)
            {
                var endPos = _chapterCard.anchoredPosition;
                _chapterCard.anchoredPosition = new Vector2(endPos.x, endPos.y + FlyInStartOffset);
                yield return SlideCard(_chapterCard, endPos, FlyInDuration);
            }

            // 0.6s text pop
            if (_chapterLabel != null)
            {
                var rt = _chapterLabel.GetComponent<RectTransform>();
                rt.localScale = Vector3.zero;
                float e = 0f;
                while (e < TextPopDuration)
                {
                    e += Time.deltaTime;
                    float t = UIEasing.EaseOutBack(Mathf.Clamp01(e / TextPopDuration));
                    rt.localScale = Vector3.one * Mathf.Lerp(0f, 1f, t);
                    yield return null;
                }
                rt.localScale = Vector3.one;
            }

            // 0.9s fanfare particles
            if (_fanfare != null) _fanfare.Play();
            yield return new WaitForSeconds(FanfareDuration);

            // 1.9s hold
            yield return new WaitForSeconds(HoldDuration);

            // 2.4s fade out
            yield return FadeDim(0.7f, 0f, FadeOutDuration);

            // restore interaction
            if (_raycaster != null) _raycaster.enabled = true;

            onComplete?.Invoke();
            Game.Core.UIManager.Instance?.CloseOverlay();
        }

        private IEnumerator FadeDim(float from, float to, float duration)
        {
            if (_dimGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _dimGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _dimGroup.alpha = to;
        }

        private IEnumerator SlideCard(RectTransform rt, Vector2 target, float duration)
        {
            var start   = rt.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseOut(Mathf.Clamp01(elapsed / duration));
                rt.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }
            rt.anchoredPosition = target;
        }
    }
}
