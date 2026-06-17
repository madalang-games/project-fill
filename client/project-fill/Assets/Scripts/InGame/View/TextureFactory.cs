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

        // Soft radial glow: small bright core fading smoothly to fully transparent at the texture
        // edge. Plain (NOT 9-sliced) so the radial falloff isn't stretched — WorldUtil scales the
        // transform to fit. `coreFrac` = solid-core radius as a fraction of size; keep it small so the
        // glow reads as a compact halo, not a hard block. Tint via SpriteRenderer.color.
        public static Sprite Glow(int size = 96, float coreFrac = 0.14f)
        {
            return Get($"gl_{size}_{coreFrac}", () =>
            {
                var tex = NewTex(size);
                float c = size * 0.5f;
                float edge = size * 0.5f - 1f; // alpha reaches 0 at the texture edge
                float core = size * coreFrac;  // fully-bright inner radius
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float a = dist <= core ? 1f : Mathf.Clamp01(1f - (dist - core) / (edge - core));
                    a = a * a; // soft quadratic falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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

        // Padlock icon: rounded body + top shackle arc + keyhole punch. White/neutral so it can be
        // tinted (the lane lock tints it to the UnlockType color → color = which signal opens it).
        public static Sprite Padlock(int size = 64)
        {
            return Get($"lock_{size}", () =>
            {
                var tex = NewTex(size);
                const float bx0 = 0.24f, bx1 = 0.76f, by0 = 0.06f, by1 = 0.52f, br = 0.08f; // body
                const float scx = 0.5f, scy = 0.58f, sR = 0.17f, sT = 0.075f;               // shackle
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;

                    // Body (rounded-rect SDF, normalized units).
                    float bhx = (bx1 - bx0) * 0.5f - br, bhy = (by1 - by0) * 0.5f - br;
                    float qx = Mathf.Abs(u - (bx0 + bx1) * 0.5f) - bhx;
                    float qy = Mathf.Abs(v - (by0 + by1) * 0.5f) - bhy;
                    float bd = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                               + Mathf.Min(Mathf.Max(qx, qy), 0f) - br;
                    float a = Mathf.Clamp01(0.5f - bd * size);

                    // Shackle: top arc of an annulus + two legs reaching down to the body.
                    float dd  = Mathf.Sqrt((u - scx) * (u - scx) + (v - scy) * (v - scy));
                    float arc = (v >= scy) ? Mathf.Clamp01(0.5f - (Mathf.Abs(dd - sR) - sT * 0.5f) * size) : 0f;
                    float leg = 0f;
                    if (v <= scy && v >= by1 - 0.02f)
                    {
                        float l = Mathf.Min(Mathf.Abs(u - (scx - sR)), Mathf.Abs(u - (scx + sR))) - sT * 0.5f;
                        leg = Mathf.Clamp01(0.5f - l * size);
                    }
                    a = Mathf.Max(a, Mathf.Max(arc, leg));

                    // Keyhole: punch a small hole in the body face.
                    if (Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.30f) * (v - 0.30f)) < 0.055f) a = 0f;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        // Circuit-board texture: dark base, faint grid, traces and solder nodes. Used for the
        // scene background and for Blind-lane chip backs. Colours baked in.
        public static Sprite Circuit(int size = 256, Color? baseColor = null, Color? lineColor = null)
        {
            var bc = baseColor ?? new Color(0.07f, 0.08f, 0.12f); // Dark PCB base
            var lc = lineColor ?? new Color(0.16f, 0.55f, 0.62f); // Traces / neon lines
            string key = $"circ_{size}_{bc}_{lc}";
            return Get(key, () =>
            {
                var tex = NewTex(size);
                var rng = new System.Random(12345);
                
                // 1. Fill base
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, bc);

                // 2. Faint grid
                int grid = Mathf.Max(16, size / 8);
                var faint = Color.Lerp(bc, lc, 0.12f);
                for (int x = 0; x < size; x += grid)
                    for (int y = 0; y < size; y++) tex.SetPixel(x, y, faint);
                for (int y = 0; y < size; y += grid)
                    for (int x = 0; x < size; x++) tex.SetPixel(x, y, faint);

                // 3. Thick power/ground buses on the sides
                var railColor = Color.Lerp(bc, lc, 0.25f);
                for (int y = 0; y < size; y++)
                {
                    tex.SetPixel(8, y, railColor);
                    tex.SetPixel(9, y, railColor);
                    tex.SetPixel(size - 9, y, railColor);
                    tex.SetPixel(size - 10, y, railColor);
                }

                // 4. SMD component footprints (resistors/capacitors)
                var bodyCol = new Color(0.25f, 0.22f, 0.2f);
                var termCol = new Color(0.7f, 0.7f, 0.72f);
                for (int i = 0; i < 8; i++)
                {
                    int cx = rng.Next(20, size - 20);
                    int cy = rng.Next(20, size - 20);
                    bool horiz = rng.Next(2) == 0;
                    
                    // Draw footprint rectangle: body 6x4, terminals 2x4 at ends
                    int w = horiz ? 10 : 6;
                    int h = horiz ? 6 : 10;
                    for (int dy = -h/2; dy <= h/2; dy++)
                    for (int dx = -w/2; dx <= w/2; dx++)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < size && py >= 0 && py < size)
                        {
                            bool isTerminal = horiz ? (System.Math.Abs(dx) >= w/2 - 1) : (System.Math.Abs(dy) >= h/2 - 1);
                            tex.SetPixel(px, py, isTerminal ? termCol : bodyCol);
                        }
                    }
                }

                // 5. Richer circuit traces with 45-degree segments and dual parallel lines
                var trace = Color.Lerp(bc, lc, 0.55f);
                for (int i = 0; i < size / 20; i++)
                {
                    int x = rng.Next(2, size / grid - 2) * grid;
                    int y = rng.Next(2, size / grid - 2) * grid;
                    int len = rng.Next(2, 4) * grid;
                    int dir = rng.Next(4); // 0: Right, 1: Up, 2: 45deg Right-Up, 3: 45deg Right-Down
                    
                    // Draw traces (sometimes parallel for dual signal paths)
                    bool parallel = rng.Next(3) == 0;
                    int offset = parallel ? 4 : 0;
                    
                    for (int t = 0; t < len; t++)
                    {
                        int px1 = x, py1 = y;
                        int px2 = x + offset, py2 = y + offset;
                        
                        if (dir == 0) { px1 += t; px2 += t; }
                        else if (dir == 1) { py1 += t; py2 += t; }
                        else if (dir == 2) { px1 += t; py1 += t; px2 += t; py2 += t; }
                        else if (dir == 3) { px1 += t; py1 -= t; px2 += t; py2 -= t; }
                        
                        DrawPixelThick(tex, px1, py1, trace, size);
                        if (parallel) DrawPixelThick(tex, px2, py2, trace, size);
                    }
                    
                    // Draw solder pads at ends
                    DrawSolderPad(tex, x, y, lc, size);
                    int ex = x, ey = y;
                    if (dir == 0) ex += len - 1;
                    else if (dir == 1) ey += len - 1;
                    else if (dir == 2) { ex += len - 1; ey += len - 1; }
                    else if (dir == 3) { ex += len - 1; ey -= len - 1; }
                    DrawSolderPad(tex, ex, ey, lc, size);
                }
                
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            });
        }

        private static void DrawPixelThick(Texture2D tex, int x, int y, Color col, int size)
        {
            for (int dy = 0; dy <= 1; dy++)
            for (int dx = 0; dx <= 1; dx++)
            {
                int px = x + dx, py = y + dy;
                if (px >= 0 && py >= 0 && px < size && py < size)
                    tex.SetPixel(px, py, col);
            }
        }

        private static void DrawSolderPad(Texture2D tex, int cx, int cy, Color col, int size)
        {
            // Draw a circular ring with a center hole (solder pad)
            int rad = 4;
            for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                float d = dx * dx + dy * dy;
                if (d > rad * rad || d < 1.5f) continue; // circle outline, leave center empty (hole)
                int px = cx + dx, py = cy + dy;
                if (px >= 0 && py >= 0 && px < size && py < size)
                    tex.SetPixel(px, py, col);
            }
        }
    }
}
