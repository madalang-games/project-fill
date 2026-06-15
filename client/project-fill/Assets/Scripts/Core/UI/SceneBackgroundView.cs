using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    // UI Canvas background for Boot and Lobby scenes.
    // Place on a Canvas with sortOrder = -1. RectTransform must be full-stretch.
    public class SceneBackgroundView : MonoBehaviour
    {
        // Pan offsets per LobbyTab index: Home(0)=right, Shop(1)=center, Ranking(2)=left
        private static readonly float[] TabPanOffsets = { 80f, 0f, -80f };

        // Tall constant covers all portrait screen heights without needing layout callbacks
        private const float ContentHeight = 1200f;

        private RectTransform _content;
        private SceneBgPalette _palette;
        private BackgroundMode _mode;
        private bool _initialized;
        private bool _animating;
        private Coroutine _panCoroutine;

        private struct Particle { public RectTransform Rt; public Image Img; public float Phase; }
        private readonly List<Particle> _particles = new();

        private RectTransform _sunOrMoon;
        private readonly List<(RectTransform rt, float speed)>  _clouds   = new();
        private readonly List<(Image img, float phase, float period)> _twinkles = new();
        private readonly List<(Image img, float phase, float period)> _rays     = new();

        // ── Public API ───────────────────────────────────────────────

        public void Bind(int bgThemeId, BackgroundMode mode)
        {
            Clear();
            _mode    = mode;
            _palette = SceneBgPalette.Get(bgThemeId, mode);
            BuildContent();
            CreateGradient();
            CreateDecorations(bgThemeId);
            _initialized = true;
            if (gameObject.activeInHierarchy)
                StartCoroutine(AnimLoop());
        }

        public void PanTo(int tabIndex, float duration = 0.65f)
        {
            if (!_initialized || _content == null) return;
            float target = tabIndex >= 0 && tabIndex < TabPanOffsets.Length
                ? TabPanOffsets[tabIndex] : 0f;
            if (_panCoroutine != null) StopCoroutine(_panCoroutine);
            _panCoroutine = StartCoroutine(PanCoroutine(target, duration));
        }

        // ── Lifecycle ────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_initialized && !_animating)
                StartCoroutine(AnimLoop());
        }

        private void OnDisable()
        {
            _animating = false;
            StopAllCoroutines();
        }

        // ── Build ────────────────────────────────────────────────────

        private void BuildContent()
        {
            var go = new GameObject("_content");
            go.transform.SetParent(transform, false);
            _content = go.AddComponent<RectTransform>();
            // Full stretch + 240px wider for ±80px pan without edge artifacts
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.pivot     = new Vector2(0.5f, 0.5f);
            _content.offsetMin = new Vector2(-120f, 0f);
            _content.offsetMax = new Vector2( 120f, 0f);
        }

        private void CreateGradient()
        {
            var go = new GameObject("_gradient");
            go.transform.SetParent(_content, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var grad = go.AddComponent<UIVerticalGradient>();
            grad.raycastTarget = false;
            grad.SetColors(_palette.SkyTop, _palette.SkyBottom);
        }

        private void CreateDecorations(int bgThemeId)
        {
            bool night = _mode == BackgroundMode.Night;
            CreateParticles();

            if (_mode == BackgroundMode.Default)
            {
                CreateSunOrMoon(isMoon: false);
                CreateClouds(2, new Color(1f, 0.85f, 0.70f, 0.45f));
            }
            else if (bgThemeId == 1 && !night)
            {
                CreateSunOrMoon(isMoon: false);
                CreateClouds(3, new Color(1f, 1f, 1f, 0.55f));
            }
            else if (bgThemeId == 1 && night)
            {
                CreateSunOrMoon(isMoon: true);
                CreateTwinkleStars(40);
            }
            else if (bgThemeId == 2 && !night)
            {
                CreateSunOrMoon(isMoon: false);
                CreateRays(5);
            }
            else if (bgThemeId == 2 && night)
            {
                CreateRays(8);
                CreateTwinkleStars(12);
            }
        }

        // ── Decoration creators ──────────────────────────────────────

        private void CreateSunOrMoon(bool isMoon)
        {
            float bodySize = isMoon ? 52f : 66f;
            float x = 80f, y = -50f;

            for (int r = 3; r >= 1; r--)
            {
                float sz = bodySize + r * 28f;
                Color gc = _palette.AccentB;
                MakeImg(_content, new Vector2(x, y), new Vector2(sz, sz),
                    new Color(gc.r, gc.g, gc.b, gc.a * (4 - r)));
            }

            var body = MakeImg(_content, new Vector2(x, y),
                new Vector2(bodySize, bodySize), _palette.AccentA);
            _sunOrMoon = body.GetComponent<RectTransform>();

            if (!isMoon)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    float rad   = angle * Mathf.Deg2Rad;
                    float dist  = bodySize * 0.82f;
                    var rPos    = new Vector2(x + Mathf.Cos(rad) * dist, y + Mathf.Sin(rad) * dist);
                    Color ac    = _palette.AccentA;
                    var ray     = MakeImg(_content, rPos, new Vector2(6f, 20f),
                        new Color(ac.r, ac.g, ac.b, 0.60f));
                    ray.GetComponent<RectTransform>().localRotation =
                        Quaternion.Euler(0f, 0f, angle + 90f);
                }
            }
        }

        private void CreateClouds(int count, Color color)
        {
            float[] xs     = { -80f, 20f, -20f };
            float[] ys     = { -30f, -60f, -95f };
            float[] ws     = { 200f, 160f, 130f };
            float[] speeds = {  14f,   9f,  18f };
            int     n      = Mathf.Min(count, xs.Length);

            for (int i = 0; i < n; i++)
            {
                var root = new GameObject($"Cloud{i}");
                root.transform.SetParent(_content, false);
                var cRt = root.AddComponent<RectTransform>();
                cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 1f);
                cRt.pivot     = new Vector2(0.5f, 0.5f);
                cRt.sizeDelta = Vector2.one;
                cRt.anchoredPosition = new Vector2(xs[i], ys[i]);

                float w = ws[i], h = w * 0.38f;
                MakeImgLocal(root.transform, Vector2.zero,                          new Vector2(w,        h * 0.65f), color);
                MakeImgLocal(root.transform, new Vector2(-w * 0.18f,  h * 0.18f),  new Vector2(w * 0.52f, h * 0.9f), color);
                MakeImgLocal(root.transform, new Vector2( w * 0.12f,  h * 0.10f),  new Vector2(w * 0.48f, h),        color);
                _clouds.Add((cRt, speeds[i]));
            }
        }

        private void CreateTwinkleStars(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x  = Random.Range(-110f, 110f);
                float y  = -Random.Range(10f, ContentHeight * 0.7f);
                float sz = Random.Range(2f, 7f);
                var img  = MakeImg(_content, new Vector2(x, y), new Vector2(sz, sz),
                    new Color(1f, 1f, 1f, Random.Range(0.2f, 0.85f)));
                _twinkles.Add((img, Random.Range(0f, Mathf.PI * 2f), Random.Range(1.5f, 5f)));
            }
        }

        private void CreateRays(int count)
        {
            Color ac = _palette.AccentA;
            for (int i = 0; i < count; i++)
            {
                float x  = Random.Range(-100f, 100f);
                float y  = -Random.Range(50f, ContentHeight * 0.5f);
                var ray  = MakeImg(_content, new Vector2(x, y),
                    new Vector2(Random.Range(12f, 28f), Random.Range(160f, 320f)),
                    new Color(ac.r, ac.g, ac.b, 0f));
                ray.GetComponent<RectTransform>().localRotation =
                    Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
                _rays.Add((ray, Random.Range(0f, Mathf.PI * 2f), Random.Range(3f, 7f)));
            }
        }

        private void CreateParticles()
        {
            for (int i = 0; i < _palette.ParticleCount; i++)
            {
                var go  = new GameObject($"P{i}", typeof(Image));
                go.transform.SetParent(_content, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = new Color(_palette.ParticleColor.r, _palette.ParticleColor.g,
                    _palette.ParticleColor.b, 0f);
                float sz = Random.Range(4f, 10f);
                var pRt  = go.GetComponent<RectTransform>();
                pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 1f);
                pRt.pivot     = new Vector2(0.5f, 0.5f);
                pRt.sizeDelta = new Vector2(sz, sz);
                pRt.anchoredPosition = new Vector2(
                    Random.Range(-110f, 110f), -Random.Range(0f, ContentHeight));
                _particles.Add(new Particle { Rt = pRt, Img = img,
                    Phase = Random.Range(0f, Mathf.PI * 2f) });
            }
        }

        // ── Animation ────────────────────────────────────────────────

        private IEnumerator AnimLoop()
        {
            _animating = true;
            while (_animating)
            {
                float dt = Time.deltaTime;
                float t  = Time.time;
                UpdateParticles(dt, t);
                UpdateSunOrMoon(t);
                UpdateClouds(dt);
                UpdateTwinkles(t);
                UpdateRays(t);
                yield return null;
            }
        }

        private void UpdateParticles(float dt, float t)
        {
            Color pc = _palette.ParticleColor;
            for (int i = 0; i < _particles.Count; i++)
            {
                var p   = _particles[i];
                var pos = p.Rt.anchoredPosition;
                pos.y += _palette.ParticleSpeed * dt;
                pos.x += Mathf.Sin(t * 0.65f + p.Phase) * 18f * dt;
                pos.x  = Mathf.Clamp(pos.x, -110f, 110f);
                p.Rt.anchoredPosition = pos;

                if (pos.y > 0f)
                {
                    p.Rt.anchoredPosition = new Vector2(Random.Range(-100f, 100f), -ContentHeight);
                    p.Img.color = new Color(pc.r, pc.g, pc.b, 0f);
                    continue;
                }
                float alpha = pc.a * Mathf.Sin(Mathf.Clamp01(-pos.y / ContentHeight) * Mathf.PI);
                p.Img.color = new Color(pc.r, pc.g, pc.b, alpha);
            }
        }

        private void UpdateSunOrMoon(float t)
        {
            if (_sunOrMoon == null) return;
            float s = 1f + 0.03f * Mathf.Sin(t * (_mode == BackgroundMode.Night ? 0.6f : 1.1f));
            _sunOrMoon.localScale = new Vector3(s, s, 1f);
        }

        private void UpdateClouds(float dt)
        {
            for (int i = 0; i < _clouds.Count; i++)
            {
                var (rt, speed) = _clouds[i];
                var pos = rt.anchoredPosition;
                pos.x += speed * dt;
                if (pos.x > 150f) pos.x = -150f;
                rt.anchoredPosition = pos;
            }
        }

        private void UpdateTwinkles(float t)
        {
            for (int i = 0; i < _twinkles.Count; i++)
            {
                var (img, phase, period) = _twinkles[i];
                float alpha = 0.2f + 0.65f * (0.5f + 0.5f * Mathf.Sin(
                    t * (Mathf.PI * 2f / period) + phase));
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        private void UpdateRays(float t)
        {
            float maxA = _mode == BackgroundMode.Night ? 0.25f : 0.13f;
            for (int i = 0; i < _rays.Count; i++)
            {
                var (img, phase, period) = _rays[i];
                float alpha = (0.5f + 0.5f * Mathf.Sin(t * (Mathf.PI * 2f / period) + phase)) * maxA;
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        private IEnumerator PanCoroutine(float targetX, float duration)
        {
            float startX  = _content.anchoredPosition.x;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = Mathf.Lerp(startX, targetX,
                    UIEasing.EaseInOut(Mathf.Min(elapsed / duration, 1f)));
                _content.anchoredPosition = new Vector2(x, _content.anchoredPosition.y);
                yield return null;
            }
            _content.anchoredPosition = new Vector2(targetX, _content.anchoredPosition.y);
        }

        // ── Cleanup ──────────────────────────────────────────────────

        private void Clear()
        {
            _particles.Clear();
            _clouds.Clear();
            _twinkles.Clear();
            _rays.Clear();
            _sunOrMoon   = null;
            _initialized = false;
            _animating   = false;
            StopAllCoroutines();
            if (_content == null) return;
            if (Application.isPlaying) Destroy(_content.gameObject);
            else DestroyImmediate(_content.gameObject);
            _content = null;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private static Image MakeImg(RectTransform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go  = new GameObject("_", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color; img.raycastTarget = false;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return img;
        }

        private static Image MakeImgLocal(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go  = new GameObject("_", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color; img.raycastTarget = false;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return img;
        }
    }
}
