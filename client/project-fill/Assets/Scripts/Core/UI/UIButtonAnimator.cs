using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Core.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private bool _isCTA;
        [SerializeField] private SfxId _clickSfxId = SfxId.ConfirmClick;

        private RectTransform _rt;
        private Coroutine _idleCoroutine;
        private bool _pressed;

        private const float PressDuration   = 0.08f;
        private const float ReleaseDuration = 0.08f;
        private const float PressScale      = 0.92f;
        private const float OvershootScale  = 1.05f;
        private const float CTAPeriod       = 2.5f;
        private const float CTAMaxScale     = 1.04f;

        private void Awake() => _rt = GetComponent<RectTransform>();

        private void OnEnable()
        {
            _rt.localScale = Vector3.one;
            if (_isCTA) _idleCoroutine = StartCoroutine(CTAIdle());
        }

        private void OnDisable()
        {
            if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
            _rt.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (_idleCoroutine != null) { StopCoroutine(_idleCoroutine); _idleCoroutine = null; }
            _pressed = true;
            SfxEventBus.Play(_clickSfxId);
            StartCoroutine(ScaleTo(PressScale, PressDuration, UIEasing.EaseIn));
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (!_pressed) return;
            _pressed = false;
            StartCoroutine(ReleaseSequence());
        }

        private IEnumerator ReleaseSequence()
        {
            yield return ScaleTo(OvershootScale, ReleaseDuration, UIEasing.EaseOut);
            yield return ScaleTo(1f,             ReleaseDuration, UIEasing.EaseInOut);
            if (_isCTA) _idleCoroutine = StartCoroutine(CTAIdle());
        }

        private IEnumerator CTAIdle()
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime / CTAPeriod;
                float s = Mathf.Lerp(1f, CTAMaxScale, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);
                _rt.localScale = Vector3.one * s;
                yield return null;
            }
        }

        private IEnumerator ScaleTo(float target, float duration, System.Func<float, float> ease)
        {
            float start   = _rt.localScale.x;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = ease(Mathf.Clamp01(elapsed / duration));
                _rt.localScale = Vector3.one * Mathf.Lerp(start, target, t);
                yield return null;
            }
            _rt.localScale = Vector3.one * target;
        }

        public void SetInteractable(bool interactable)
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.interactable = interactable;

            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = interactable ? 1f : 0.4f;

            if (!interactable && _idleCoroutine != null)
            {
                StopCoroutine(_idleCoroutine);
                _idleCoroutine = null;
                _rt.localScale = Vector3.one;
            }
        }
    }
}
