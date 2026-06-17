using System.Collections;
using System.Collections.Generic;
using Game.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Per-chapter background decoration layer, tinted with the chapter theme color
    /// (the flat gradient itself is drawn by HomeTabView). Three ambient effects:
    ///   • blinking star LEDs (random period twinkle)
    ///   • drifting glow motes (slow upward depth particles)
    ///   • a scanline sweep band (periodic vertical light pass)
    /// Sprites are injected via DynamicResourceService; each falls back to a code
    /// shape when its sprite is absent. Animation stops when scrolled off-screen.
    /// </summary>
    public class ChapterBackgroundView : MonoBehaviour
    {
        private struct Star
        {
            public Image Img;
            public float Phase;
            public float Period;
            public Color Color;
        }

        private struct Mote
        {
            public RectTransform Rt;
            public Image         Img;
            public float         Speed;
            public float         Phase;
            public Color         Color;
        }

        private const float  HalfWidth   = 520f;
        private const string StarResKey  = "led_star";      // dynamic_resource.csv keys
        private const string MoteResKey  = "deco_mote";
        private const string ScanResKey  = "deco_scanline";

        // ── Viewport culling bounds (set by Bind, read by HomeTabView) ──
        public float YTop { get; private set; }
        public float YBot { get; private set; }

        // ── State ──────────────────────────────────────────────────
        private ChapterBgTheme    _theme;
        private float             _height;
        private bool              _animating;
        private int               _bgThemeId;

        private readonly List<Star> _stars = new();
        private readonly List<Mote> _motes = new();

        private RectTransform _scanRt;
        private Image         _scanImg;
        private float         _scanPeriod;
        private Color         _scanColor;

        // ── Public API ─────────────────────────────────────────────
        public void Bind(int chapterId, int bgThemeId, float yAnchoredTop, float height)
        {
            YTop       = yAnchoredTop;
            YBot       = yAnchoredTop - height;
            _bgThemeId = bgThemeId;
            _theme     = ChapterBgTheme.Get(bgThemeId);
            _height    = height;

            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMax = new Vector2(0f, yAnchoredTop);
            rt.offsetMin = new Vector2(0f, yAnchoredTop - height);

            CreateStars();
            CreateMotes();
            CreateScanline();

            if (gameObject.activeInHierarchy)
                StartCoroutine(AnimLoop());
        }

        private void OnEnable()
        {
            if (_height > 0f && !_animating)
                StartCoroutine(AnimLoop());
        }

        private void OnDisable()
        {
            _animating = false;
            StopAllCoroutines();
        }

        // ── Star field ─────────────────────────────────────────────
        private void CreateStars()
        {
            Sprite sprite = DynamicResourceService.Instance?.GetSprite(StarResKey);
            int    count  = Mathf.Clamp(Mathf.RoundToInt(_height / 110f), 16, 70);

            // Tint toward white at the core so the LED reads as a bright twinkle.
            Color baseCol = Color.Lerp(_theme.ParticleColor, Color.white, 0.35f);
            baseCol.a = _theme.ParticleColor.a;

            for (int i = 0; i < count; i++)
            {
                float sz  = Random.Range(9f, 20f);
                var   img = MakeImg(sprite, sz, baseCol);
                img.rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-HalfWidth * 0.9f, HalfWidth * 0.9f),
                    -Random.Range(0f, _height));
                if (sprite == null)
                    img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f); // diamond fallback

                _stars.Add(new Star
                {
                    Img    = img,
                    Phase  = Random.Range(0f, Mathf.PI * 2f),
                    Period = Random.Range(1.2f, 3.6f),
                    Color  = baseCol,
                });
            }
        }

        // ── Drift glow motes ───────────────────────────────────────
        private void CreateMotes()
        {
            Sprite sprite = DynamicResourceService.Instance?.GetSprite(MoteResKey);
            int    count  = Mathf.Clamp(Mathf.RoundToInt(_height / 200f), 6, 24);

            Color col = _theme.ParticleColor;
            col.a *= 0.55f; // softer than stars

            for (int i = 0; i < count; i++)
            {
                float sz  = Random.Range(5f, 11f);
                var   img = MakeImg(sprite, sz, col);
                img.rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-HalfWidth * 0.85f, HalfWidth * 0.85f),
                    -Random.Range(0f, _height));

                _motes.Add(new Mote
                {
                    Rt    = img.rectTransform,
                    Img   = img,
                    Speed = Random.Range(10f, 26f),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Color = col,
                });
            }
        }

        // ── Scanline sweep ─────────────────────────────────────────
        private void CreateScanline()
        {
            Sprite sprite = DynamicResourceService.Instance?.GetSprite(ScanResKey);

            _scanColor = _theme.PathColor;
            _scanColor.a = 0.10f;
            _scanPeriod  = Random.Range(7f, 11f);

            var go = new GameObject("Scanline", typeof(Image));
            go.transform.SetParent(transform, false);
            _scanImg = go.GetComponent<Image>();
            _scanImg.raycastTarget = false;
            _scanImg.sprite        = sprite;
            _scanImg.color         = new Color(_scanColor.r, _scanColor.g, _scanColor.b, 0f);

            _scanRt = go.GetComponent<RectTransform>();
            _scanRt.anchorMin = new Vector2(0f, 1f);
            _scanRt.anchorMax = new Vector2(1f, 1f);
            _scanRt.pivot     = new Vector2(0.5f, 0.5f);
            _scanRt.sizeDelta = new Vector2(0f, 70f); // full width, fixed band height
            _scanRt.anchoredPosition = Vector2.zero;
        }

        // ── Animation ──────────────────────────────────────────────
        private IEnumerator AnimLoop()
        {
            _animating = true;
            while (_animating)
            {
                float dt = Time.deltaTime;
                float t  = Time.time;
                UpdateStars(t);
                UpdateMotes(dt, t);
                UpdateScanline(t);
                yield return null;
            }
        }

        private void UpdateStars(float t)
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                var   s     = _stars[i];
                float wave  = Mathf.Sin(t * (Mathf.PI * 2f / s.Period) + s.Phase);
                // Sharp pulse: mostly dim with brief bright flashes (LED feel).
                float blink = Mathf.Pow(Mathf.Max(0f, wave), 2.2f);
                s.Img.color = new Color(s.Color.r, s.Color.g, s.Color.b, s.Color.a * blink);
            }
        }

        private void UpdateMotes(float dt, float t)
        {
            for (int i = 0; i < _motes.Count; i++)
            {
                var m   = _motes[i];
                var pos = m.Rt.anchoredPosition;
                pos.y += m.Speed * dt;                              // drift up
                pos.x += Mathf.Sin(t * 0.5f + m.Phase) * 12f * dt;  // gentle sway
                pos.x  = Mathf.Clamp(pos.x, -HalfWidth * 0.9f, HalfWidth * 0.9f);
                if (pos.y > 0f) pos.y = -_height;                   // wrap to bottom
                m.Rt.anchoredPosition = pos;

                float alpha = m.Color.a * (0.5f + 0.5f * Mathf.Sin(t * 0.9f + m.Phase));
                m.Img.color = new Color(m.Color.r, m.Color.g, m.Color.b, alpha);
            }
        }

        private void UpdateScanline(float t)
        {
            if (_scanRt == null) return;
            float p = Mathf.Repeat(t / _scanPeriod, 1f); // 0→1 over period
            _scanRt.anchoredPosition = new Vector2(0f, -p * _height);
            // Fade in/out at the ends of the sweep so it never pops.
            float alpha = _scanColor.a * Mathf.Sin(p * Mathf.PI);
            _scanImg.color = new Color(_scanColor.r, _scanColor.g, _scanColor.b, alpha);
        }

        // ── Helpers ────────────────────────────────────────────────
        private Image MakeImg(Sprite sprite, float size, Color color)
        {
            var go  = new GameObject("_", typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite        = sprite;
            img.color         = new Color(color.r, color.g, color.b, 0f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }
    }
}
