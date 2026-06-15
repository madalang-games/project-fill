using System;
using System.Collections;
using UnityEngine;

namespace Game.Core.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelAppear : MonoBehaviour
    {
        private RectTransform _rt;
        private CanvasGroup   _cg;

        private const float AppearDuration    = 0.2f;
        private const float DisappearDuration = 0.15f;
        private const float AppearStartScale  = 0.85f;
        private const float DisappearEndScale = 0.9f;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _cg = GetComponent<CanvasGroup>();
        }

        private void OnEnable() => StartCoroutine(AppearSequence());

        public void Disappear(Action onComplete = null)
            => StartCoroutine(DisappearSequence(onComplete));

        private IEnumerator AppearSequence()
        {
            float elapsed = 0f;
            while (elapsed < AppearDuration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseOut(Mathf.Clamp01(elapsed / AppearDuration));
                _rt.localScale = Vector3.one * Mathf.Lerp(AppearStartScale, 1f, t);
                _cg.alpha      = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            _rt.localScale = Vector3.one;
            _cg.alpha      = 1f;
        }

        private IEnumerator DisappearSequence(Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < DisappearDuration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseIn(Mathf.Clamp01(elapsed / DisappearDuration));
                _rt.localScale = Vector3.one * Mathf.Lerp(1f, DisappearEndScale, t);
                _cg.alpha      = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            onComplete?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
