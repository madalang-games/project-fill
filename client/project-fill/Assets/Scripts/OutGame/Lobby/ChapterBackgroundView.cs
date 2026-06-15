using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Per-chapter animated decoration layer. Gradient is handled by HomeTabView.
    /// Animations stop automatically when scrolled off-screen (OnDisable culling).
    /// </summary>
    public class ChapterBackgroundView : MonoBehaviour
    {
        private struct Particle
        {
            public RectTransform Rt;
            public Image         Img;
            public float         Phase;
            public Color         Color;
        }

        private struct FishState
        {
            public RectTransform Rt;
            public float         Dir;
            public float         Phase;
            public float         Speed;
        }

        private struct JellyfishState
        {
            public RectTransform Root;
            public float         BaseY;
            public float         Phase;
            public float         Speed;
        }

        private const float HalfWidth = 520f;

        // ── Viewport culling bounds (set by Bind, read by HomeTabView) ──
        public float YTop { get; private set; }
        public float YBot { get; private set; }

        // ── State ──────────────────────────────────────────────────
        private ChapterBgTheme _theme;
        private float          _height;
        private bool           _animating;
        private bool           _initialized;
        private int            _bgThemeId;

        // Shared particles
        private readonly List<Particle> _particles = new();

        // ── Grassland ──────────────────────────────────────────────
        private RectTransform                _sun;
        private readonly List<RectTransform> _clouds     = new();
        private readonly List<RectTransform> _grass      = new();
        private readonly List<float>         _grassPhase = new();

        // ── Ocean ──────────────────────────────────────────────────
        private readonly List<(RectTransform rt, float phase, float speed, float baseY)> _waves         = new();
        private readonly List<FishState>                                                  _fishList      = new();
        private readonly List<JellyfishState>                                             _jellyfishList = new();
        private readonly List<(Image img, float phase, float period)>                     _rays          = new();
        private readonly List<RectTransform> _seaweed      = new();
        private readonly List<float>         _seaweedPhase = new();

        // ── Forest ─────────────────────────────────────────────────
        private readonly List<(Image img, float phase, float speed, float alphaScale)> _sunbeams = new();

        // ── Desert ─────────────────────────────────────────────────
        private RectTransform _desertSun;
        private readonly List<(Image img, float baseY, float phase, float speed)> _shimmer  = new();
        private readonly List<(Image img, float phase, float speed)>               _sparkles = new();

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

            CreateParticles();
            CreateDecorations(bgThemeId);

            if (gameObject.activeInHierarchy)
                StartCoroutine(MasterLoop());
        }

        private void OnEnable()
        {
            if (_height > 0f && !_animating)
                StartCoroutine(MasterLoop());
        }

        private void OnDisable()
        {
            _animating = false;
            StopAllCoroutines();
        }

        // ── Particle creation ──────────────────────────────────────
        private void CreateParticles()
        {
            int   count = _theme.ParticleCount;
            float minSz = _theme.ParticleSize * 0.6f;
            float maxSz = _theme.ParticleSize * 1.6f;

            for (int i = 0; i < count; i++)
            {
                float sz  = Random.Range(minSz, maxSz);
                Color col = _theme.ParticleColor;

                var go = new GameObject($"P{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(col.r, col.g, col.b, 0f);
                img.raycastTarget = false;

                var pRt = go.GetComponent<RectTransform>();
                pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 1f);
                pRt.pivot     = new Vector2(0.5f, 0.5f);
                pRt.sizeDelta = new Vector2(sz, sz);
                pRt.anchoredPosition = RandomPos();

                _particles.Add(new Particle
                {
                    Rt    = pRt,
                    Img   = img,
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Color = col,
                });
            }
        }

        // ── Theme dispatch ─────────────────────────────────────────
        private void CreateDecorations(int id)
        {
            if (id == 1) { CreateSun(); CreateClouds(3); CreateGrass(20); }
            if (id == 2) { CreateSandBeach(); CreateWaves(2); CreateSeaweed(8); CreateFish(7); CreateRays(4); CreateJellyfish(3); }
            if (id == 3) { CreateForestFog(); CreateForestEdge(); CreateSunbeams(4); CreateTreeTrunks(6); CreateLeafCanopy(8); CreateMushrooms(4); CreateGroundLitter(); }
            if (id == 4) { CreateDesertSun(); CreateHeatShimmer(6); CreateSandDunes(4); CreateSphinx(); CreateCactus(3); CreateDesertSparkle(30); }
        }

        // ════════════════════════════════════════════════════════════
        // GRASSLAND
        // ════════════════════════════════════════════════════════════
        private void CreateSun()
        {
            float sunY = -_height * 0.10f;
            float sunX = HalfWidth * 0.55f;

            for (int r = 3; r >= 1; r--)
                MakeImg(transform, new Vector2(sunX, sunY), new Vector2(72f + r * 36f, 72f + r * 36f),
                    new Color(1f, 0.85f, 0.2f, 0.06f * (4 - r)));

            var sunImg = MakeImg(transform, new Vector2(sunX, sunY), new Vector2(66f, 66f),
                new Color(1f, 0.90f, 0.25f, 0.92f));
            _sun = sunImg.rectTransform;

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                float rad   = angle * Mathf.Deg2Rad;
                var   ray   = MakeImg(transform,
                    new Vector2(sunX + Mathf.Cos(rad) * 52f, sunY + Mathf.Sin(rad) * 52f),
                    new Vector2(6f, 22f), new Color(1f, 0.88f, 0.3f, 0.65f));
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
        }

        private void CreateClouds(int count)
        {
            float[] ys     = { -_height * 0.06f, -_height * 0.14f, -_height * 0.22f };
            float[] xs     = { -HalfWidth * 0.3f, HalfWidth * 0.15f, -HalfWidth * 0.6f };
            float[] widths = { 220f, 180f, 140f };

            for (int i = 0; i < count; i++)
            {
                var root = new GameObject($"Cloud{i}", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                var cRt = root.GetComponent<RectTransform>();
                cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 1f);
                cRt.pivot     = new Vector2(0.5f, 0.5f);
                cRt.sizeDelta = Vector2.one;
                cRt.anchoredPosition = new Vector2(xs[i], ys[i]);

                float w = widths[i], h = w * 0.38f;
                Color c = new Color(1f, 1f, 1f, 0.60f);
                BuildCloudPart(root.transform, new Vector2(0, 0),                  new Vector2(w,         h * 0.65f), c);
                BuildCloudPart(root.transform, new Vector2(-w * 0.18f, h * 0.18f), new Vector2(w * 0.52f, h * 0.9f), c);
                BuildCloudPart(root.transform, new Vector2( w * 0.12f, h * 0.10f), new Vector2(w * 0.48f, h),        c);
                _clouds.Add(cRt);
            }
        }

        private void BuildCloudPart(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var img = MakeImg(parent, pos, size, color);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        private void CreateGrass(int count)
        {
            float bottomY = -_height + 20f;
            float span    = HalfWidth * 1.7f;

            for (int i = 0; i < count; i++)
            {
                float h = Random.Range(26f, 52f);
                float x = -HalfWidth * 0.85f + i * (span / count) + Random.Range(-10f, 10f);
                var blade = MakeImg(transform, new Vector2(x, bottomY),
                    new Vector2(Random.Range(4f, 8f), h),
                    new Color(0.18f, Random.Range(0.28f, 0.45f), 0.12f, 0.88f));
                blade.rectTransform.pivot = new Vector2(0.5f, 0f);
                _grass.Add(blade.rectTransform);
                _grassPhase.Add(Random.Range(0f, Mathf.PI * 2f));
            }
        }

        // ════════════════════════════════════════════════════════════
        // OCEAN
        // ════════════════════════════════════════════════════════════
        private void CreateSandBeach()
        {
            float sandH = _height * 0.18f;
            MakeImg(transform, new Vector2(0f, -sandH * 0.5f), new Vector2(HalfWidth * 2.2f, sandH),
                new Color(0.96f, 0.87f, 0.70f, 0.40f));

            for (int i = 0; i < 16; i++)
                MakeImg(transform,
                    new Vector2(Random.Range(-HalfWidth * 0.88f, HalfWidth * 0.88f),
                                -Random.Range(3f, sandH - 4f)),
                    new Vector2(Random.Range(2f, 5f), Random.Range(2f, 5f)),
                    new Color(0.76f, 0.62f, 0.42f, 0.38f));
        }

        private void CreateWaves(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float baseY = -_height * (0.18f + i * 0.06f);
                var go = new GameObject($"Wave{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.55f, 0.88f, 0.98f, 0.30f - i * 0.06f);
                img.raycastTarget = false;
                var wRt = go.GetComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0f, 1f);
                wRt.anchorMax = new Vector2(1f, 1f);
                wRt.pivot     = new Vector2(0.5f, 0.5f);
                wRt.sizeDelta = new Vector2(0f, 10f + i * 6f);
                wRt.anchoredPosition = new Vector2(0f, baseY);
                _waves.Add((wRt, i * 1.2f, 1.4f + i * 0.5f, baseY));
            }
        }

        private void CreateSeaweed(int count)
        {
            float topLimit    = -_height * 0.28f;
            float bottomLimit = -_height * 0.92f;
            float span        = HalfWidth * 1.6f;

            for (int i = 0; i < count; i++)
            {
                float x     = -HalfWidth * 0.80f + i * (span / count) + Random.Range(-25f, 25f);
                float tNorm = count > 1 ? i / (float)(count - 1) : 0f;
                float yBase = Mathf.Max(Mathf.Lerp(topLimit, bottomLimit, tNorm) + Random.Range(-40f, 40f), bottomLimit);

                var go = new GameObject($"Seaweed{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.05f, Random.Range(0.45f, 0.65f), 0.30f, 0.80f);
                img.raycastTarget = false;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(Random.Range(6f, 11f), Random.Range(60f, 110f));
                rt.anchoredPosition = new Vector2(x, yBase);
                _seaweed.Add(rt);
                _seaweedPhase.Add(Random.Range(0f, Mathf.PI * 2f));
            }
        }

        private void CreateFish(int count)
        {
            float[] ys     = { -_height * 0.35f, -_height * 0.50f, -_height * 0.65f,
                               -_height * 0.42f, -_height * 0.58f, -_height * 0.72f, -_height * 0.45f };
            float[] speeds = { 38f, 26f, 34f, 42f, 28f, 36f, 30f };
            Color[] fishColors = {
                new Color(0.25f, 0.55f, 0.82f, 0.72f),  // ocean blue
                new Color(0.82f, 0.38f, 0.15f, 0.72f),  // orange
                new Color(0.20f, 0.72f, 0.60f, 0.70f),  // teal
                new Color(0.85f, 0.70f, 0.15f, 0.70f),  // yellow
                new Color(0.68f, 0.25f, 0.65f, 0.68f),  // purple
                new Color(0.25f, 0.55f, 0.82f, 0.65f),  // blue variant
                new Color(0.30f, 0.75f, 0.55f, 0.70f),  // green
            };

            for (int i = 0; i < count; i++)
            {
                var root = new GameObject($"Fish{i}", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                var fRt = root.GetComponent<RectTransform>();
                fRt.anchorMin = fRt.anchorMax = new Vector2(0.5f, 1f);
                fRt.pivot     = new Vector2(0.5f, 0.5f);
                fRt.sizeDelta = Vector2.one;
                fRt.anchoredPosition = new Vector2(Random.Range(-HalfWidth * 0.6f, HalfWidth * 0.6f), ys[i % ys.Length]);

                float s  = Random.Range(0.75f, 1.25f);
                Color fc = fishColors[i % fishColors.Length];
                // Body
                MakeImgLocal(root.transform, new Vector2(3f, 0),   new Vector2(30f * s, 13f * s), fc);
                // Tail fin
                MakeImgLocal(root.transform, new Vector2(-16f * s, 0), new Vector2(11f * s, 16f * s), new Color(fc.r, fc.g, fc.b, fc.a * 0.8f));
                // Eye
                MakeImgLocal(root.transform, new Vector2(11f * s, 2f), new Vector2(4f * s, 4f * s), new Color(0.9f, 0.95f, 1f, 0.9f));
                // Dorsal fin
                MakeImgLocal(root.transform, new Vector2(2f * s, 8f * s), new Vector2(14f * s, 6f * s), new Color(fc.r * 0.85f, fc.g * 0.85f, fc.b * 0.85f, fc.a * 0.75f));

                _fishList.Add(new FishState { Rt = fRt, Dir = i % 2 == 0 ? 1f : -1f, Phase = Random.Range(0f, Mathf.PI * 2f), Speed = speeds[i % speeds.Length] });
            }
        }

        private void CreateRays(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var ray = MakeImg(transform,
                    new Vector2(Random.Range(-HalfWidth * 0.55f, HalfWidth * 0.55f),
                                -Random.Range(_height * 0.22f, _height * 0.68f)),
                    new Vector2(Random.Range(10f, 22f), Random.Range(140f, 280f)),
                    new Color(0.72f, 0.95f, 1f, 0f));
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-22f, 22f));
                _rays.Add((ray, Random.Range(0f, Mathf.PI * 2f), Random.Range(3f, 7f)));
            }
        }

        private void CreateJellyfish(int count)
        {
            Color[] jfColors = {
                new Color(0.78f, 0.45f, 0.92f, 0.58f), // purple
                new Color(0.45f, 0.78f, 0.92f, 0.55f), // blue
                new Color(0.92f, 0.55f, 0.72f, 0.55f), // pink
            };
            float[] ys = { -_height * 0.28f, -_height * 0.45f, -_height * 0.62f,
                           -_height * 0.38f, -_height * 0.55f };

            for (int i = 0; i < count; i++)
            {
                Color jc = jfColors[i % jfColors.Length];
                float x  = Random.Range(-HalfWidth * 0.55f, HalfWidth * 0.55f);
                float y  = ys[i % ys.Length];
                float s  = Random.Range(0.75f, 1.25f);

                var root = new GameObject($"Jellyfish{i}", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                var jRt = root.GetComponent<RectTransform>();
                jRt.anchorMin = jRt.anchorMax = new Vector2(0.5f, 1f);
                jRt.pivot     = new Vector2(0.5f, 0.5f);
                jRt.sizeDelta = Vector2.one;
                jRt.anchoredPosition = new Vector2(x, y);

                // Bell dome (4 rows: wide base → narrow top)
                float[] widths  = { 42f, 35f, 24f, 14f };
                float[] heights = { 10f, 10f, 10f,  9f };
                float[] offY    = { 0f,   9f, 18f, 27f };
                for (int r = 0; r < 4; r++)
                    MakeImgLocal(root.transform, new Vector2(0f, offY[r] * s),
                        new Vector2(widths[r] * s, heights[r] * s),
                        new Color(jc.r, jc.g, jc.b, r < 2 ? jc.a : jc.a * 1.25f));

                // Tentacles
                int tentCount = Random.Range(4, 7);
                float tentSpan = 34f * s;
                for (int t = 0; t < tentCount; t++)
                {
                    float tx  = tentCount > 1 ? -tentSpan * 0.5f + t * (tentSpan / (tentCount - 1)) : 0f;
                    float len = Random.Range(22f, 42f) * s;
                    MakeImgLocal(root.transform, new Vector2(tx, -len * 0.5f),
                        new Vector2(2f * s, len), new Color(jc.r, jc.g, jc.b, jc.a * 0.52f));
                }

                _jellyfishList.Add(new JellyfishState
                {
                    Root  = jRt,
                    BaseY = y,
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Speed = Random.Range(1.2f, 2.4f),
                });
            }
        }

        // ════════════════════════════════════════════════════════════
        // FOREST
        // ════════════════════════════════════════════════════════════
        private void CreateForestFog()
        {
            // Layered dark mist in the upper portion — canopy depth
            float[] alphas  = { 0.32f, 0.20f, 0.12f };
            float[] heights = { 100f,  150f,  200f  };
            float[] ys      = { -15f,  -70f,  -140f };
            Color fog = new Color(0.04f, 0.14f, 0.03f, 1f);

            for (int i = 0; i < 3; i++)
                MakeImg(transform, new Vector2(0f, ys[i]),
                    new Vector2(HalfWidth * 2.2f, heights[i]),
                    new Color(fog.r, fog.g, fog.b, alphas[i]));

            // Mid-depth mist bands
            for (int i = 0; i < 3; i++)
            {
                float y = -_height * (0.30f + i * 0.22f);
                MakeImg(transform, new Vector2(Random.Range(-60f, 60f), y),
                    new Vector2(HalfWidth * 2.2f, Random.Range(50f, 90f)),
                    new Color(0.05f, 0.18f, 0.04f, 0.12f));
            }
        }

        private void CreateSunbeams(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x     = Random.Range(-HalfWidth * 0.60f, HalfWidth * 0.60f);
                float y     = -Random.Range(_height * 0.02f, _height * 0.52f);
                float tilt  = Random.Range(-28f, 28f);
                float coreW = Random.Range(52f, 88f);
                float h     = Random.Range(340f, 540f);
                float phase = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(5f, 11f);

                var outer = MakeImg(transform, new Vector2(x, y), new Vector2(coreW * 2.8f, h),
                    new Color(0.72f, 0.95f, 0.52f, 0f));
                outer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                _sunbeams.Add((outer, phase, speed, 0.18f));

                var inner = MakeImg(transform, new Vector2(x, y), new Vector2(coreW * 1.65f, h),
                    new Color(0.82f, 0.98f, 0.62f, 0f));
                inner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                _sunbeams.Add((inner, phase, speed, 0.44f));

                var core = MakeImg(transform, new Vector2(x, y), new Vector2(coreW, h),
                    new Color(0.95f, 1.00f, 0.78f, 0f));
                core.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                _sunbeams.Add((core, phase, speed, 1.0f));
            }
        }

        private void CreateTreeTrunks(int groups)
        {
            int   pairs = Mathf.Max(1, groups / 2);
            float step  = _height / (pairs + 1);

            for (int p = 0; p < pairs; p++)
            {
                float cy  = -(step * (p + 1));
                float h   = Random.Range(_height * 0.30f, _height * 0.42f);
                float cy2 = cy - h * 0.08f;

                float wL = Random.Range(38f, 60f);
                float xL = -HalfWidth + wL * 0.5f + Random.Range(6f, 30f);
                MakeImg(transform, new Vector2(xL - wL * 0.36f, cy2), new Vector2(wL * 0.30f, h),
                    new Color(0.05f, 0.03f, 0.01f, 0.88f));
                MakeImg(transform, new Vector2(xL, cy2), new Vector2(wL, h),
                    new Color(0.22f, 0.12f, 0.04f, 0.90f));
                MakeImg(transform, new Vector2(xL + wL * 0.32f, cy2), new Vector2(wL * 0.20f, h),
                    new Color(0.38f, 0.24f, 0.10f, 0.52f));
                for (int bl = 0; bl < 5; bl++)
                {
                    float by = cy2 + h * 0.40f - bl * (h * 0.18f);
                    MakeImg(transform, new Vector2(xL, by), new Vector2(wL * 0.80f, 2f),
                        new Color(0.07f, 0.04f, 0.01f, 0.42f));
                }

                float wR = Random.Range(38f, 60f);
                float xR = HalfWidth - wR * 0.5f - Random.Range(6f, 30f);
                MakeImg(transform, new Vector2(xR + wR * 0.36f, cy2), new Vector2(wR * 0.30f, h),
                    new Color(0.05f, 0.03f, 0.01f, 0.88f));
                MakeImg(transform, new Vector2(xR, cy2), new Vector2(wR, h),
                    new Color(0.22f, 0.12f, 0.04f, 0.90f));
                MakeImg(transform, new Vector2(xR - wR * 0.32f, cy2), new Vector2(wR * 0.20f, h),
                    new Color(0.38f, 0.24f, 0.10f, 0.52f));
                for (int bl = 0; bl < 5; bl++)
                {
                    float by = cy2 + h * 0.40f - bl * (h * 0.18f);
                    MakeImg(transform, new Vector2(xR, by), new Vector2(wR * 0.80f, 2f),
                        new Color(0.07f, 0.04f, 0.01f, 0.42f));
                }
            }

            for (int m = 0; m < 3; m++)
            {
                float cy = -(_height * (0.22f + m * 0.24f));
                float h  = Random.Range(_height * 0.20f, _height * 0.30f);
                float x  = (m % 2 == 0 ? -1f : 1f) * Random.Range(HalfWidth * 0.18f, HalfWidth * 0.48f);
                float w  = Random.Range(22f, 36f);
                MakeImg(transform, new Vector2(x, cy), new Vector2(w, h),
                    new Color(0.18f, 0.10f, 0.03f, 0.44f));
                MakeImg(transform, new Vector2(x + w * 0.32f, cy), new Vector2(w * 0.22f, h),
                    new Color(0.30f, 0.18f, 0.08f, 0.20f));
            }
        }

        private void CreateLeafCanopy(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float cx = Random.Range(-HalfWidth * 0.68f, HalfWidth * 0.68f);
                float cy = -(_height * i / count) - Random.Range(20f, 80f);

                int   leafN = Random.Range(8, 14);
                Color cc    = new Color(
                    Random.Range(0.05f, 0.18f),
                    Random.Range(0.36f, 0.60f),
                    Random.Range(0.03f, 0.12f),
                    Random.Range(0.60f, 0.88f));

                for (int l = 0; l < leafN; l++)
                {
                    var leaf = MakeImg(transform,
                        new Vector2(cx + Random.Range(-70f, 70f), cy + Random.Range(-42f, 42f)),
                        new Vector2(Random.Range(22f, 52f), Random.Range(16f, 34f)), cc);
                    leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
                }
            }
        }

        private void CreateMushrooms(int count)
        {
            float span = HalfWidth * 1.4f;
            Color[] caps = {
                new Color(0.78f, 0.15f, 0.10f, 0.92f),
                new Color(0.55f, 0.32f, 0.10f, 0.90f),
                new Color(0.82f, 0.50f, 0.15f, 0.90f),
                new Color(0.62f, 0.20f, 0.08f, 0.88f),
            };
            Color stem = new Color(0.88f, 0.82f, 0.72f, 0.85f);

            for (int i = 0; i < count; i++)
            {
                float x  = -HalfWidth * 0.70f + i * (span / count) + Random.Range(-18f, 18f);
                float y  = -_height * Random.Range(0.70f, 0.90f);
                float s  = Random.Range(0.75f, 1.30f);
                Color cc = caps[i % caps.Length];

                MakeImg(transform, new Vector2(x, y + 18f * s), new Vector2(38f * s, 16f * s), cc);
                MakeImg(transform, new Vector2(x, y + 27f * s), new Vector2(26f * s, 12f * s), cc);
                MakeImg(transform, new Vector2(x - 6f * s, y + 22f * s), new Vector2(5f * s, 5f * s), new Color(1f, 1f, 1f, 0.72f));
                MakeImg(transform, new Vector2(x + 7f * s, y + 18f * s), new Vector2(4f * s, 4f * s), new Color(1f, 1f, 1f, 0.65f));
                MakeImg(transform, new Vector2(x, y), new Vector2(11f * s, 22f * s), stem);
            }
        }

        private void CreateGroundLitter()
        {
            float baseY = -_height * 0.96f;
            MakeImg(transform, new Vector2(0f, baseY), new Vector2(HalfWidth * 2.2f, 20f),
                new Color(0.05f, 0.14f, 0.02f, 0.70f));

            for (int i = 0; i < 12; i++)
            {
                float x = Random.Range(-HalfWidth * 0.90f, HalfWidth * 0.90f);
                float w = Random.Range(20f, 55f);
                float h = Random.Range(4f, 10f);
                float rot = Random.Range(-25f, 25f);
                var root = MakeImg(transform, new Vector2(x, baseY + 5f), new Vector2(w, h),
                    new Color(0.14f, 0.08f, 0.03f, Random.Range(0.40f, 0.65f)));
                root.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
            }
        }

        private void CreateForestEdge()
        {
            float edgeW  = HalfWidth * 0.40f;
            float panelH = _height * 0.70f;
            float topY   = -_height * 0.15f;
            float cy     = topY - panelH * 0.5f;

            MakeImg(transform, new Vector2(-HalfWidth + edgeW * 0.50f, cy), new Vector2(edgeW,         panelH), new Color(0.02f, 0.10f, 0.01f, 0.35f));
            MakeImg(transform, new Vector2(-HalfWidth + edgeW * 0.26f, cy), new Vector2(edgeW * 0.50f, panelH), new Color(0.02f, 0.08f, 0.01f, 0.20f));
            MakeImg(transform, new Vector2( HalfWidth - edgeW * 0.50f, cy), new Vector2(edgeW,         panelH), new Color(0.02f, 0.10f, 0.01f, 0.35f));
            MakeImg(transform, new Vector2( HalfWidth - edgeW * 0.26f, cy), new Vector2(edgeW * 0.50f, panelH), new Color(0.02f, 0.08f, 0.01f, 0.20f));
        }

        // ════════════════════════════════════════════════════════════
        // DESERT
        // ════════════════════════════════════════════════════════════
        private void CreateDesertSun()
        {
            float sunY = -_height * 0.08f;
            float sunX =  HalfWidth * 0.60f;

            // 7 glow rings — intense halo
            float[] ringAlphas = { 0.025f, 0.04f, 0.06f, 0.08f, 0.12f, 0.16f, 0.22f };
            for (int r = 7; r >= 1; r--)
            {
                float rr = r / 7f;
                MakeImg(transform, new Vector2(sunX, sunY), new Vector2(80f + r * 52f, 80f + r * 52f),
                    new Color(1f, 0.65f + rr * 0.30f, 0.05f + rr * 0.22f, ringAlphas[r - 1]));
            }

            // Sun body
            var sunImg = MakeImg(transform, new Vector2(sunX, sunY), new Vector2(92f, 92f),
                new Color(1f, 0.90f, 0.28f, 1f));
            _desertSun = sunImg.rectTransform;

            // White-hot core layers
            MakeImg(transform, new Vector2(sunX, sunY), new Vector2(64f, 64f),
                new Color(1f, 0.98f, 0.88f, 0.90f));
            MakeImg(transform, new Vector2(sunX, sunY), new Vector2(32f, 32f),
                new Color(1f, 1f, 1f, 0.72f));

            // 12 rays (alternating long/short)
            for (int i = 0; i < 12; i++)
            {
                float angle  = i * 30f;
                float rad    = angle * Mathf.Deg2Rad;
                float rayLen = i % 2 == 0 ? 36f : 24f;
                var   ray    = MakeImg(transform,
                    new Vector2(sunX + Mathf.Cos(rad) * 74f, sunY + Mathf.Sin(rad) * 74f),
                    new Vector2(8f, rayLen), new Color(1f, 0.85f, 0.28f, 0.60f));
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }

            // Vertical light shaft pointing down
            MakeImg(transform, new Vector2(sunX, sunY - _height * 0.28f),
                new Vector2(24f, _height * 0.52f), new Color(1f, 0.94f, 0.62f, 0.05f));
            MakeImg(transform, new Vector2(sunX, sunY - _height * 0.28f),
                new Vector2(9f,  _height * 0.52f), new Color(1f, 0.99f, 0.85f, 0.07f));
        }

        private void CreateHeatShimmer(int count)
        {
            for (int i = 0; i < count * 2; i++)
            {
                float x     = Random.Range(-HalfWidth * 0.62f, HalfWidth * 0.62f);
                float baseY = -_height * (0.20f + (i % count) * 0.058f + Random.Range(-0.018f, 0.018f));
                var go = new GameObject($"Shimmer{i}", typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 0.90f, 0.65f, 0f);
                img.raycastTarget = false;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Random.Range(55f, 160f), Random.Range(2f, 4f));
                rt.anchoredPosition = new Vector2(x, baseY);
                _shimmer.Add((img, baseY, Random.Range(0f, Mathf.PI * 2f), Random.Range(2.2f, 4.8f)));
            }
        }

        private void CreateSandDunes(int count)
        {
            float startDepth = -_height * 0.48f;
            float endDepth   = -_height * 0.90f;
            float step       = (endDepth - startDepth) / Mathf.Max(1, count - 1);

            for (int d = 0; d < count; d++)
            {
                float baseY  = startDepth + d * step;
                float cx     = Random.Range(-HalfWidth * 0.25f, HalfWidth * 0.25f);
                float alpha  = 0.40f + d * 0.08f;
                float baseW  = Random.Range(HalfWidth * 1.0f, HalfWidth * 1.6f);

                Color dc = new Color(0.88f - d * 0.04f, 0.60f - d * 0.04f, 0.26f - d * 0.02f, alpha);

                // 14-row arch: base widest, top narrowest — quadratic ease for natural dune silhouette
                const int rows  = 14;
                const float rowH = 15f;
                for (int row = 0; row < rows; row++)
                {
                    float t2          = (float)row / (rows - 1);
                    float widthFactor = 1f - t2 * t2;
                    MakeImg(transform, new Vector2(cx, baseY + row * rowH * 0.82f),
                        new Vector2(baseW * widthFactor, rowH), dc);
                }

                // Shadow lip at dune base
                MakeImg(transform, new Vector2(cx - baseW * 0.08f, baseY + 6f),
                    new Vector2(baseW * 0.85f, 10f),
                    new Color(dc.r * 0.55f, dc.g * 0.50f, dc.b * 0.40f, alpha * 0.60f));
            }
        }

        private void CreateSphinx()
        {
            float baseY = -_height * 0.74f;
            float s     = 1.15f;
            float cx    = HalfWidth * 0.05f;

            Color body   = new Color(0.64f, 0.42f, 0.16f, 0.90f);
            Color dark   = new Color(0.38f, 0.22f, 0.06f, 0.90f);
            Color head   = new Color(0.68f, 0.46f, 0.18f, 0.92f);
            Color nemes  = new Color(0.74f, 0.56f, 0.22f, 0.88f);
            Color shadow = new Color(0.25f, 0.12f, 0.02f, 0.42f);

            // Ground shadow
            MakeImg(transform, new Vector2(cx + 12f * s, baseY - 5f),
                new Vector2(220f * s, 12f * s), shadow);

            // Rear haunch (back, higher)
            MakeImg(transform, new Vector2(cx + 74f * s, baseY + 40f * s),
                new Vector2(54f * s, 40f * s), body);
            // Tail
            MakeImg(transform, new Vector2(cx + 98f * s, baseY + 18f * s),
                new Vector2(9f * s, 28f * s), dark);

            // Main body (horizontal lion torso)
            MakeImg(transform, new Vector2(cx + 18f * s, baseY + 24f * s),
                new Vector2(140f * s, 46f * s), body);
            // Underside shadow
            MakeImg(transform, new Vector2(cx + 18f * s, baseY + 7f * s),
                new Vector2(130f * s, 14f * s), dark);

            // Front paws (extended forward)
            MakeImg(transform, new Vector2(cx - 48f * s, baseY + 12f * s),
                new Vector2(38f * s, 18f * s), body);
            MakeImg(transform, new Vector2(cx - 8f * s, baseY + 12f * s),
                new Vector2(36f * s, 18f * s), body);
            // Paw toe hint
            MakeImg(transform, new Vector2(cx - 50f * s, baseY + 3f * s),
                new Vector2(32f * s, 8f * s), dark);
            MakeImg(transform, new Vector2(cx - 10f * s, baseY + 3f * s),
                new Vector2(30f * s, 8f * s), dark);

            // Neck
            MakeImg(transform, new Vector2(cx - 52f * s, baseY + 54f * s),
                new Vector2(28f * s, 44f * s), body);
            MakeImg(transform, new Vector2(cx - 63f * s, baseY + 54f * s),
                new Vector2(11f * s, 44f * s), dark);

            // Head
            MakeImg(transform, new Vector2(cx - 52f * s, baseY + 95f * s),
                new Vector2(42f * s, 36f * s), head);
            // Face shadow (left side)
            MakeImg(transform, new Vector2(cx - 65f * s, baseY + 95f * s),
                new Vector2(14f * s, 36f * s), dark);
            // Nose
            MakeImg(transform, new Vector2(cx - 67f * s, baseY + 88f * s),
                new Vector2(10f * s, 5f * s), dark);

            // Nemes headdress top plate
            MakeImg(transform, new Vector2(cx - 52f * s, baseY + 120f * s),
                new Vector2(52f * s, 15f * s), nemes);
            // Headdress side flaps
            MakeImg(transform, new Vector2(cx - 71f * s, baseY + 90f * s),
                new Vector2(17f * s, 44f * s), nemes);
            MakeImg(transform, new Vector2(cx - 31f * s, baseY + 90f * s),
                new Vector2(17f * s, 44f * s), nemes);
            // Nemes left shadow
            MakeImg(transform, new Vector2(cx - 76f * s, baseY + 90f * s),
                new Vector2(8f * s, 44f * s), dark);
            // Headdress crown stripe
            MakeImg(transform, new Vector2(cx - 52f * s, baseY + 128f * s),
                new Vector2(52f * s, 6f * s), new Color(0.86f, 0.72f, 0.28f, 0.88f));
        }

        private void CreateCactus(int count)
        {
            float span  = HalfWidth * 1.2f;
            Color body  = new Color(0.18f, 0.40f, 0.12f, 0.93f);
            Color shade = new Color(0.11f, 0.26f, 0.07f, 0.93f);

            for (int i = 0; i < count; i++)
            {
                float x = -HalfWidth * 0.60f + i * (span / count) + Random.Range(-22f, 22f);
                float y = -_height * Random.Range(0.82f, 0.90f);
                float s = Random.Range(0.80f, 1.30f);

                MakeImg(transform, new Vector2(x, y - 30f * s), new Vector2(14f * s, 62f * s), body);
                MakeImg(transform, new Vector2(x + 3.5f * s, y - 30f * s), new Vector2(4f * s, 62f * s), shade);

                float armY = y - 22f * s;
                MakeImg(transform, new Vector2(x - 11f * s, armY),           new Vector2(12f * s, 6f * s),  body);
                MakeImg(transform, new Vector2(x - 18f * s, armY + 10f * s), new Vector2(6f * s, 20f * s),  body);
                MakeImg(transform, new Vector2(x - 18f * s, armY + 10f * s), new Vector2(2f * s, 20f * s),  shade);

                MakeImg(transform, new Vector2(x + 11f * s, armY),           new Vector2(12f * s, 6f * s),  body);
                MakeImg(transform, new Vector2(x + 18f * s, armY + 8f * s),  new Vector2(6f * s, 16f * s),  body);
                MakeImg(transform, new Vector2(x + 18f * s, armY + 8f * s),  new Vector2(2f * s, 16f * s),  shade);
            }
        }

        private void CreateDesertSparkle(int count)
        {
            // Tiny stationary sparkles in the sand zone — rapid alpha flash
            for (int i = 0; i < count; i++)
            {
                float x  = Random.Range(-HalfWidth * 0.88f, HalfWidth * 0.88f);
                float y  = -_height * (0.32f + Random.Range(0f, 0.58f));
                float sz = Random.Range(2.5f, 5.5f);
                var img = MakeImg(transform, new Vector2(x, y), new Vector2(sz, sz),
                    new Color(1f, 0.95f, 0.70f, 0f));
                _sparkles.Add((img, Random.Range(0f, Mathf.PI * 2f), Random.Range(3.5f, 9f)));
            }
        }

        // ════════════════════════════════════════════════════════════
        // ANIMATION LOOP
        // ════════════════════════════════════════════════════════════
        private IEnumerator MasterLoop()
        {
            _animating = true;
            if (!_initialized)
            {
                ScatterParticles();
                _initialized = true;
            }

            while (_animating)
            {
                float dt = Time.deltaTime;
                float t  = Time.time;
                UpdateParticles(dt, t);
                if (_bgThemeId == 1) UpdateGrassland(dt, t);
                if (_bgThemeId == 2) UpdateOcean(dt, t);
                if (_bgThemeId == 3) UpdateForest(dt, t);
                if (_bgThemeId == 4) UpdateDesert(dt, t);
                yield return null;
            }
        }

        private void ScatterParticles()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                var p   = _particles[i];
                var pos = p.Rt.anchoredPosition;
                if (_theme.ParticleDir == ParticleDir.Horizontal)
                    pos.x = Random.Range(-HalfWidth * 0.55f, HalfWidth * 0.55f);
                pos.y = -Random.Range(0f, _height);
                p.Rt.anchoredPosition = pos;
            }
        }

        private void UpdateParticles(float dt, float t)
        {
            bool isFirefly = _bgThemeId == 3;

            for (int i = 0; i < _particles.Count; i++)
            {
                var   p   = _particles[i];
                var   pos = p.Rt.anchoredPosition;
                Color col = p.Color;
                float alpha;

                switch (_theme.ParticleDir)
                {
                    case ParticleDir.Upward:
                        pos.y += _theme.ParticleSpeed * dt;
                        pos.x += Mathf.Sin(t * 0.65f + p.Phase) * 18f * dt;
                        pos.x  = Mathf.Clamp(pos.x, -HalfWidth * 0.44f, HalfWidth * 0.44f);
                        p.Rt.anchoredPosition = pos;
                        if (pos.y > 0f)
                        {
                            p.Rt.anchoredPosition = new Vector2(Random.Range(-HalfWidth * 0.4f, HalfWidth * 0.4f), -_height);
                            p.Img.color = new Color(col.r, col.g, col.b, 0f);
                            continue;
                        }
                        if (isFirefly)
                        {
                            // Sharp blink — mostly dark with brief bright flashes
                            float blink = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 4.5f + p.Phase * 3.7f)), 2.5f);
                            alpha = col.a * blink;
                        }
                        else
                        {
                            float norm = Mathf.Clamp01(-pos.y / _height);
                            alpha = col.a * Mathf.Sin(norm * Mathf.PI);
                        }
                        break;

                    case ParticleDir.Downward:
                        pos.y -= _theme.ParticleSpeed * dt;
                        pos.x += Mathf.Sin(t * 0.8f + p.Phase) * 12f * dt;
                        pos.x  = Mathf.Clamp(pos.x, -HalfWidth * 0.44f, HalfWidth * 0.44f);
                        p.Rt.Rotate(0f, 0f, (p.Phase - Mathf.PI) * 22f * dt);
                        p.Rt.anchoredPosition = pos;
                        if (pos.y < -_height)
                        {
                            p.Rt.anchoredPosition = new Vector2(Random.Range(-HalfWidth * 0.44f, HalfWidth * 0.44f), 0f);
                            p.Rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                            p.Img.color = new Color(col.r, col.g, col.b, 0f);
                            continue;
                        }
                        {
                            float norm = Mathf.Clamp01(-pos.y / _height);
                            alpha = col.a * Mathf.Sin(norm * Mathf.PI);
                        }
                        break;

                    case ParticleDir.Horizontal:
                        pos.x += _theme.ParticleSpeed * dt;
                        pos.y += Mathf.Sin(t * 0.35f + p.Phase) * 1.5f * dt;
                        if (pos.x > HalfWidth * 0.55f)
                        {
                            pos.x = -HalfWidth * 0.55f;
                            pos.y = -Random.Range(0f, _height);
                        }
                        p.Rt.anchoredPosition = pos;
                        p.Img.color = new Color(col.r, col.g, col.b, col.a * (0.55f + 0.45f * Mathf.Sin(p.Phase + t * 1.1f)));
                        continue;

                    default:
                        alpha = 0f;
                        break;
                }

                p.Img.color = new Color(col.r, col.g, col.b, alpha);
            }
        }

        private void UpdateGrassland(float dt, float t)
        {
            if (_sun != null)
            {
                float s = 1f + 0.04f * Mathf.Sin(t * 1.1f);
                _sun.localScale = new Vector3(s, s, 1f);
            }

            float[] cloudSpeeds = { 14f, 9f, 18f };
            for (int i = 0; i < _clouds.Count; i++)
            {
                var pos = _clouds[i].anchoredPosition;
                pos.x += cloudSpeeds[i % cloudSpeeds.Length] * dt;
                if (pos.x > HalfWidth + 160f) pos.x = -HalfWidth - 160f;
                _clouds[i].anchoredPosition = pos;
            }

            for (int i = 0; i < _grass.Count; i++)
                _grass[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.1f + _grassPhase[i]) * 11f);
        }

        private void UpdateOcean(float dt, float t)
        {
            for (int i = 0; i < _waves.Count; i++)
            {
                var (rt, phase, speed, baseY) = _waves[i];
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, baseY + Mathf.Sin(t * speed + phase) * 14f);
            }

            for (int i = 0; i < _fishList.Count; i++)
            {
                var f   = _fishList[i];
                var pos = f.Rt.anchoredPosition;
                pos.x += f.Speed * f.Dir * dt;
                pos.y += Mathf.Sin(t * 1.8f + f.Phase) * 0.4f * dt;

                if (pos.x > HalfWidth * 0.85f)
                {
                    _fishList[i] = new FishState { Rt = f.Rt, Dir = -1f, Phase = f.Phase, Speed = f.Speed };
                    f.Rt.localScale = new Vector3(-1f, 1f, 1f);
                }
                else if (pos.x < -HalfWidth * 0.85f)
                {
                    _fishList[i] = new FishState { Rt = f.Rt, Dir = 1f, Phase = f.Phase, Speed = f.Speed };
                    f.Rt.localScale = Vector3.one;
                }
                f.Rt.anchoredPosition = pos;
            }

            for (int i = 0; i < _seaweed.Count; i++)
                _seaweed[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f + _seaweedPhase[i]) * 16f);

            for (int i = 0; i < _rays.Count; i++)
            {
                var (img, phase, period) = _rays[i];
                float alpha = (0.5f + 0.5f * Mathf.Sin(t * (Mathf.PI * 2f / period) + phase)) * 0.13f;
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            }

            // Jellyfish bob
            for (int i = 0; i < _jellyfishList.Count; i++)
            {
                var j = _jellyfishList[i];
                j.Root.anchoredPosition = new Vector2(
                    j.Root.anchoredPosition.x,
                    j.BaseY + Mathf.Sin(t * j.Speed + j.Phase) * 22f);
            }
        }

        private void UpdateForest(float dt, float t)
        {
            for (int i = 0; i < _sunbeams.Count; i++)
            {
                var (img, phase, speed, alphaScale) = _sunbeams[i];
                float alpha = Mathf.Max(0f, (0.08f + 0.22f * Mathf.Sin(t * (Mathf.PI * 2f / speed) + phase)) * alphaScale);
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            }
        }

        private void UpdateDesert(float dt, float t)
        {
            if (_desertSun != null)
            {
                float s = 1f + 0.035f * Mathf.Sin(t * 1.3f);
                _desertSun.localScale = new Vector3(s, s, 1f);
            }

            for (int i = 0; i < _shimmer.Count; i++)
            {
                var (img, baseY, phase, speed) = _shimmer[i];
                float jitter = Mathf.Sin(t * speed * 2.2f + phase) * 2.2f
                             + Mathf.Sin(t * speed * 0.7f + phase + 1.3f) * 1.0f;
                float alpha  = 0.04f + 0.08f * (0.5f + 0.5f * Mathf.Sin(t * speed + phase));
                var pos = img.rectTransform.anchoredPosition;
                img.rectTransform.anchoredPosition = new Vector2(pos.x, baseY + jitter);
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            }

            // Sand sparkles: sharp brief flashes — sunlight glinting off sand grains
            for (int i = 0; i < _sparkles.Count; i++)
            {
                var (img, phase, speed) = _sparkles[i];
                float blink = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * speed + phase)), 3f);
                img.color = new Color(img.color.r, img.color.g, img.color.b, blink * 0.88f);
            }
        }

        // ── Helpers ────────────────────────────────────────────────
        private static Image MakeImg(Transform parent, Vector2 pos, Vector2 size, Color color)
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

        private Vector2 RandomPos() => new(
            Random.Range(-HalfWidth * 0.4f, HalfWidth * 0.4f),
            -Random.Range(0f, _height));
    }
}
