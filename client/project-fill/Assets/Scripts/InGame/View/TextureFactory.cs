using System.Collections.Generic;
using UnityEngine;

namespace Game.InGame.View
{
    // Procedural sprite generator. Everything the board needs to look like a circuit/semiconductor
    // board is generated here in code so the prototype is self-contained (no imported art).
    // Sprites are white/neutral and tinted at runtime via Image.color; results are cached.
    public static class TextureFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        private static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null) Cache[key] = s = make();
            return s;
        }

        private static Texture2D NewTex(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode   = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            return t;
        }

        private static Sprite Make9(Texture2D tex, int border)
        {
            var rect = new Rect(0, 0, tex.width, tex.height);
            var b    = new Vector4(border, border, border, border);
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        // Signed distance to a rounded box centred in a size×size field.
        private static float RoundedBoxSdf(float px, float py, int size, float radius)
        {
            float half = size * 0.5f;
            float qx = Mathf.Abs(px - half) - (half - radius);
            float qy = Mathf.Abs(py - half) - (half - radius);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        // Solid rounded rectangle (9-sliced — corners stay crisp at any size). Tint via Image.color.
        public static Sprite RoundedRect(int size = 64, float cornerFrac = 0.26f)
        {
            return Get($"rr_{size}_{cornerFrac}", () =>
            {
                var tex = NewTex(size);
                float r = size * cornerFrac;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxSdf(x + 0.5f, y + 0.5f, size, r);
                    float a = Mathf.Clamp01(0.5f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                return Make9(tex, Mathf.CeilToInt(r) + 1);
            });
        }

        // Rounded-rect outline ring just inside the boundary.
        public static Sprite RoundedOutline(int size = 64, float cornerFrac = 0.26f, float thicknessFrac = 0.10f)
        {
            return Get($"ro_{size}_{cornerFrac}_{thicknessFrac}", () =>
            {
                var tex = NewTex(size);
                float r = size * cornerFrac;
                float th = size * thicknessFrac;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxSdf(x + 0.5f, y + 0.5f, size, r);
                    // band centred at d = -th/2 (just inside the edge)
                    float ring = Mathf.Abs(d + th * 0.5f) - th * 0.5f;
                    float a = Mathf.Clamp01(0.5f - ring);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                return Make9(tex, Mathf.CeilToInt(r) + 1);
            });
        }

        // Soft outer glow (rounded, alpha falls off toward the edge). Tint via Image.color.
        public static Sprite Glow(int size = 96, float cornerFrac = 0.30f)
        {
            return Get($"gl_{size}_{cornerFrac}", () =>
            {
                var tex = NewTex(size);
                float r = size * cornerFrac;
                float feather = size * 0.22f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxSdf(x + 0.5f, y + 0.5f, size, r);
                    // d<=0 inside → full; fade out over `feather` px outside.
                    float a = Mathf.Clamp01(1f - Mathf.Max(d, 0f) / feather);
                    a = a * a; // softer
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                return Make9(tex, Mathf.CeilToInt(r) + Mathf.CeilToInt(feather) + 1);
            });
        }

        // Filled circle / disc.
        public static Sprite Disc(int size = 64)
        {
            return Get($"disc_{size}", () =>
            {
                var tex = NewTex(size);
                float c = size * 0.5f, rad = size * 0.5f - 1f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) - rad;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        // Ring (for panel nodes / lock shackle).
        public static Sprite Ring(int size = 64, float thicknessFrac = 0.14f)
        {
            return Get($"ring_{size}_{thicknessFrac}", () =>
            {
                var tex = NewTex(size);
                float c = size * 0.5f, rad = size * 0.5f - 1f, th = size * thicknessFrac;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float ring = Mathf.Abs(dist - (rad - th * 0.5f)) - th * 0.5f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - ring)));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        // Circuit-board texture: dark base, faint grid, traces and solder nodes. Used for the
        // scene background and for Blind-lane chip backs. Colours baked in.
        public static Sprite Circuit(int size = 256, Color? baseColor = null, Color? lineColor = null)
        {
            var bc = baseColor ?? new Color(0.07f, 0.08f, 0.12f);
            var lc = lineColor ?? new Color(0.16f, 0.55f, 0.62f);
            string key = $"circ_{size}_{bc}_{lc}";
            return Get(key, () =>
            {
                var tex = NewTex(size);
                var rng = new System.Random(12345);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, bc);

                int grid = Mathf.Max(16, size / 8);
                var faint = Color.Lerp(bc, lc, 0.18f);
                for (int x = 0; x < size; x += grid)
                    for (int y = 0; y < size; y++) tex.SetPixel(x, y, faint);
                for (int y = 0; y < size; y += grid)
                    for (int x = 0; x < size; x++) tex.SetPixel(x, y, faint);

                // a few brighter traces + nodes
                for (int i = 0; i < size / 24; i++)
                {
                    int gx = rng.Next(size / grid) * grid;
                    int gy = rng.Next(size / grid) * grid;
                    int len = (rng.Next(2, 5)) * grid;
                    bool horiz = rng.Next(2) == 0;
                    var trace = Color.Lerp(bc, lc, 0.6f);
                    for (int t = 0; t < len; t++)
                    {
                        int px = horiz ? Mathf.Min(gx + t, size - 1) : gx;
                        int py = horiz ? gy : Mathf.Min(gy + t, size - 1);
                        tex.SetPixel(px, py, trace);
                        if (horiz && py + 1 < size) tex.SetPixel(px, py + 1, trace);
                        if (!horiz && px + 1 < size) tex.SetPixel(px + 1, py, trace);
                    }
                    DrawNode(tex, gx, gy, 3, lc);
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        private static void DrawNode(Texture2D tex, int cx, int cy, int rad, Color col)
        {
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                if (dx * dx + dy * dy > rad * rad) continue;
                int px = cx + dx, py = cy + dy;
                if (px < 0 || py < 0 || px >= tex.width || py >= tex.height) continue;
                tex.SetPixel(px, py, col);
            }
        }
    }
}
