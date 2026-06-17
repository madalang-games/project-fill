using System.Collections;
using System.Collections.Generic;
using Game.Core.UI;
using Game.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    /// <summary>
    /// InGame board background. Renders the active Board Skin cosmetic
    /// (<see cref="BoardBgTheme"/>) as a gradient + ambient deco template
    /// (grid / motes / scanline / twinkle). Sprites are injected via
    /// DynamicResourceService with a code fallback for each.
    /// Place on a full-stretch UI object on a Canvas that renders BEHIND the
    /// World-space board (low sortingOrder / Screen Space-Camera far plane).
    /// </summary>
    public class InGameSceneBackgroundView : MonoBehaviour
    {
        private const float  ContentH    = 1400f; // tall enough for any portrait height
        private const float  HalfW       = 380f;
        private const string MoteResKey  = "deco_mote";
        private const string ScanResKey  = "deco_scanline";
        private const string StarResKey  = "led_star";

        private struct Mote { public RectTransform Rt; public Image Img; public float Speed; public float Phase; public Color Color; }
        private struct Scan { public RectTransform Rt; public Image Img; public float Period; public Color Color; }
        private struct Star { public Image Img; public float Phase; public float Period; public Color Color; }

        private RectTransform _content;
        private bool          _animating;
        private bool          _applied;

        private readonly List<Mote> _motes = new();
        private readonly List<Scan> _scans = new();
        private readonly List<Star> _stars = new();

        // ── Lifecycle ──────────────────────────────────────────────
        private void Start()
        {
            // Use the cached active skin; if cosmetics were never fetched this session
            // (e.g. direct InGame entry), show the default then refresh once fetched.
            Apply(CosmeticState.ResolveBoardSkin());

            if (!CosmeticState.HasData && CosmeticApiService.Instance != null)
            {
                CosmeticApiService.Instance.FetchCosmetics(
                    _ => Apply(CosmeticState.ResolveBoardSkin()),
                    _ => { /* keep default background on failure */ });
            }
        }

        private void OnEnable()  { if (_applied && !_animating) StartCoroutine(AnimLoop()); }
        private void OnDisable() { _animating = false; StopAllCoroutines(); }

        // ── Public API ─────────────────────────────────────────────
        public void Apply(string boardSkinId)
        {
            Clear();

            var theme = BoardBgTheme.Get(boardSkinId);
            BuildContent();
            CreateGradient(theme.Top, theme.Bottom);
            CreateDeco(theme);

            _applied = true;
            if (gameObject.activeInHierarchy)
                StartCoroutine(AnimLoop());
        }

        // ── Build ──────────────────────────────────────────────────
        private void BuildContent()
        {
            var go = new GameObject("_content", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _content = go.GetComponent<RectTransform>();
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = Vector2.one;
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
        }

        private void CreateGradient(Color top, Color bottom)
        {
            var go = new GameObject("_gradient", typeof(RectTransform));
            go.transform.SetParent(_content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var grad = go.AddComponent<UIVerticalGradient>();
            grad.raycastTarget = false;
            grad.SetColors(top, bottom);
        }

        private void CreateDeco(BoardBgTheme theme)
        {
            Color a = theme.Accent;
            switch (theme.Kind)
            {
                case BoardDecoKind.Minimal:
                    CreateGrid(5, 8, new Color(a.r, a.g, a.b, 0.06f));
                    break;
                case BoardDecoKind.Quantum:
                    CreateMotes(18, new Color(a.r, a.g, a.b, 0.50f));
                    CreateStars(22, Color.Lerp(a, Color.white, 0.4f));
                    CreateScanlines(1, new Color(a.r, a.g, a.b, 0.10f));
                    break;
                case BoardDecoKind.Retro:
                    CreateGrid(9, 14, new Color(a.r, a.g, a.b, 0.16f));
                    CreateScanlines(3, new Color(a.r, a.g, a.b, 0.12f));
                    break;
                default: // NeonGrid
                    CreateGrid(7, 11, new Color(a.r, a.g, a.b, 0.12f));
                    CreateMotes(12, new Color(a.r, a.g, a.b, 0.50f));
                    CreateScanlines(1, new Color(a.r, a.g, a.b, 0.10f));
                    break;
            }
        }

        // ── Deco creators ──────────────────────────────────────────
        private void CreateGrid(int cols, int rows, Color color)
        {
            for (int i = 1; i < cols; i++)
            {
                float x = i / (float)cols;
                var line = MakeLine(new Vector2(x, 0f), new Vector2(x, 1f), color);
                line.rectTransform.sizeDelta = new Vector2(2f, 0f); // vertical: fixed width, stretch height
            }
            for (int j = 1; j < rows; j++)
            {
                float y = j / (float)rows;
                var line = MakeLine(new Vector2(0f, y), new Vector2(1f, y), color);
                line.rectTransform.sizeDelta = new Vector2(0f, 2f); // horizontal: fixed height, stretch width
            }
        }

        private void CreateMotes(int count, Color color)
        {
            Sprite sprite = DynamicResourceService.Instance?.GetSprite(MoteResKey);
            for (int i = 0; i < count; i++)
            {
                float sz  = Random.Range(5f, 11f);
                var   img = MakeSprite(sprite, sz, color);
                img.rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-HalfW, HalfW), -Random.Range(0f, ContentH));
                _motes.Add(new Mote
                {
                    Rt = img.rectTransform, Img = img,
                    Speed = Random.Range(10f, 26f),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Color = color,
                });
            }
        }

        private void CreateStars(int count, Color color)
        {
            Sprite sprite = DynamicResourceService.Instance?.GetSprite(StarResKey);
            for (int i = 0; i < count; i++)
            {
                float sz  = Random.Range(6f, 14f);
                var   img = MakeSprite(sprite, sz, color);
                img.rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-HalfW, HalfW), -Random.Range(0f, ContentH));
                if (sprite == null) img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                _stars.Add(new Star
                {
                    Img = img,
                    Phase  = Random.Range(0f, Mathf.PI * 2f),
                    Period = Random.Range(1.2f, 3.6f),
                    Color  = color,
                });
            }
        }

        private void CreateScanlines(int count, Color color)
        {
            Sprite sprite = DynamicResourceService.Instance?.GetSprite(ScanResKey);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Scan{i}", typeof(Image));
                go.transform.SetParent(_content, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.sprite        = sprite;
                img.color         = new Color(color.r, color.g, color.b, 0f);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(0f, 60f);
                _scans.Add(new Scan
                {
                    Rt = rt, Img = img,
                    Period = Random.Range(6f, 11f) + i * 1.7f, // stagger multiple bands
                    Color  = color,
                });
            }
        }

        // ── Animation ──────────────────────────────────────────────
        private IEnumerator AnimLoop()
        {
            _animating = true;
            while (_animating)
            {
                float dt = Time.deltaTime;
                float t  = Time.time;
                UpdateMotes(dt, t);
                UpdateStars(t);
                UpdateScanlines(t);
                yield return null;
            }
        }

        private void UpdateMotes(float dt, float t)
        {
            for (int i = 0; i < _motes.Count; i++)
            {
                var m   = _motes[i];
                var pos = m.Rt.anchoredPosition;
                pos.y += m.Speed * dt;
                pos.x += Mathf.Sin(t * 0.5f + m.Phase) * 12f * dt;
                pos.x  = Mathf.Clamp(pos.x, -HalfW, HalfW);
                if (pos.y > 0f) pos.y = -ContentH;
                m.Rt.anchoredPosition = pos;
                float alpha = m.Color.a * (0.5f + 0.5f * Mathf.Sin(t * 0.9f + m.Phase));
                m.Img.color = new Color(m.Color.r, m.Color.g, m.Color.b, alpha);
            }
        }

        private void UpdateStars(float t)
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                var   s     = _stars[i];
                float blink = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * (Mathf.PI * 2f / s.Period) + s.Phase)), 2.2f);
                s.Img.color = new Color(s.Color.r, s.Color.g, s.Color.b, s.Color.a * blink);
            }
        }

        private void UpdateScanlines(float t)
        {
            for (int i = 0; i < _scans.Count; i++)
            {
                var   s = _scans[i];
                float p = Mathf.Repeat(t / s.Period, 1f);
                s.Rt.anchoredPosition = new Vector2(0f, -p * ContentH);
                float alpha = s.Color.a * Mathf.Sin(p * Mathf.PI);
                s.Img.color = new Color(s.Color.r, s.Color.g, s.Color.b, alpha);
            }
        }

        // ── Cleanup ────────────────────────────────────────────────
        private void Clear()
        {
            _animating = false;
            StopAllCoroutines();
            _motes.Clear();
            _scans.Clear();
            _stars.Clear();
            if (_content == null) return;
            if (Application.isPlaying) Destroy(_content.gameObject);
            else DestroyImmediate(_content.gameObject);
            _content = null;
        }

        // ── Helpers ────────────────────────────────────────────────
        private Image MakeSprite(Sprite sprite, float size, Color color)
        {
            var go  = new GameObject("_", typeof(Image));
            go.transform.SetParent(_content, false);
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

        private Image MakeLine(Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go  = new GameObject("_line", typeof(Image));
            go.transform.SetParent(_content, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color         = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }
    }
}
