using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core
{
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        private Image _fadeImage;

        private const float FadeDuration  = 0.25f;
        private const float SlideDuration = 0.25f;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeOverlay();
        }

        // Creates its own Canvas + full-screen black Image — no manual assignment needed
        private void BuildFadeOverlay()
        {
            var canvasGo = new GameObject("Canvas_Transition");
            canvasGo.transform.SetParent(transform);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99; // below UIManager Canvas_Loading (100)

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform);

            _fadeImage = imgGo.AddComponent<Image>();
            _fadeImage.color = Color.black;
            _fadeImage.raycastTarget = true;

            var rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            SetAlpha(0f);
        }

        public void FadeToScene(string sceneName, Action onMidpoint = null)
            => StartCoroutine(FadeSequence(sceneName, onMidpoint));

        // Slide transitions use fade + immediate scene swap (no slide panel needed)
        public void SlideUpToScene(string sceneName, Action onMidpoint = null)
            => StartCoroutine(FadeSequence(sceneName, onMidpoint));

        public void SlideDownToScene(string sceneName, Action onMidpoint = null)
            => StartCoroutine(FadeSequence(sceneName, onMidpoint));

        private IEnumerator FadeSequence(string sceneName, Action onMidpoint)
        {
            yield return Fade(0f, 1f, FadeDuration);
            onMidpoint?.Invoke();
            SceneManager.LoadScene(sceneName);
            yield return Fade(1f, 0f, FadeDuration);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            _fadeImage.gameObject.SetActive(true);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, UI.UIEasing.EaseInOut(Mathf.Clamp01(elapsed / duration))));
                yield return null;
            }
            SetAlpha(to);
            if (to <= 0f) _fadeImage.gameObject.SetActive(false);
        }

        private void SetAlpha(float a)
        {
            var c = _fadeImage.color;
            c.a = a;
            _fadeImage.color = c;
        }
    }
}
