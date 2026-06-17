using UnityEngine;

namespace Game.OutGame.Lobby
{
    public enum ParticleDir { Upward, Downward, Horizontal }

    public readonly struct ChapterBgTheme
    {
        public readonly Color       TopColor;
        public readonly Color       BottomColor;
        public readonly Color       ParticleColor;
        public readonly float       ParticleSpeed;
        public readonly int         ParticleCount;
        public readonly float       ParticleSize;
        public readonly string      PathResourceKey;
        public readonly float       PathScrollSpeed;
        public readonly Color       PathColor;
        public readonly float       PathWidth;
        public readonly ParticleDir ParticleDir;

        public ChapterBgTheme(Color top, Color bottom, Color particle, float speed, int count, float size,
            string pathResourceKey, float pathScrollSpeed, Color pathColor, float pathWidth,
            ParticleDir particleDir = ParticleDir.Upward)
        {
            TopColor        = top;
            BottomColor     = bottom;
            ParticleColor   = particle;
            ParticleSpeed   = speed;
            ParticleCount   = count;
            ParticleSize    = size;
            PathResourceKey = pathResourceKey;
            PathScrollSpeed = pathScrollSpeed;
            PathColor       = pathColor;
            PathWidth       = pathWidth;
            ParticleDir     = particleDir;
        }

        public static ChapterBgTheme Get(int themeId) => themeId switch
        {
            // Chapters share one continuous bottom→top gradient (circuit/aurora hue
            // sweep). Each chapter's TopColor equals the next chapter's BottomColor
            // so the multi-stop gradient has no seam jump (avoids banding/noise).
            1 => new ChapterBgTheme(                               // Cyan-blue
                top:      new Color(0.171f, 0.194f, 0.450f, 1f),  // = P1 (ch2 bottom)
                bottom:   new Color(0.171f, 0.357f, 0.450f, 1f),  // P0
                particle: new Color(0.55f, 0.92f, 1.00f, 0.70f),  // neon cyan glow
                speed: 35f, count: 14, size: 7f,
                pathResourceKey: "path_chapter_1",
                pathScrollSpeed: 0.15f,
                pathColor: new Color(0.21f, 0.84f, 0.95f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Upward),

            2 => new ChapterBgTheme(                               // Deep blue
                top:      new Color(0.310f, 0.171f, 0.450f, 1f),  // = P2 (ch3 bottom)
                bottom:   new Color(0.171f, 0.194f, 0.450f, 1f),  // P1 (= ch1 top)
                particle: new Color(0.62f, 0.74f, 1.00f, 0.65f),  // neon indigo glow
                speed: 38f, count: 16, size: 7f,
                pathResourceKey: "path_chapter_2",
                pathScrollSpeed: 0.3f,
                pathColor: new Color(0.40f, 0.55f, 1.00f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Upward),

            3 => new ChapterBgTheme(                               // Violet
                top:      new Color(0.450f, 0.171f, 0.427f, 1f),  // = P3 (ch4 bottom)
                bottom:   new Color(0.310f, 0.171f, 0.450f, 1f),  // P2 (= ch2 top)
                particle: new Color(0.82f, 0.62f, 1.00f, 0.70f),  // neon violet glow
                speed: 16f, count: 14, size: 4f,
                pathResourceKey: "path_chapter_3",
                pathScrollSpeed: 0.10f,
                pathColor: new Color(0.72f, 0.40f, 1.00f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Upward),

            4 => new ChapterBgTheme(                               // Magenta → pink
                top:      new Color(0.450f, 0.171f, 0.264f, 1f),  // P4
                bottom:   new Color(0.450f, 0.171f, 0.427f, 1f),  // P3 (= ch3 top)
                particle: new Color(1.00f, 0.62f, 0.90f, 0.60f),  // neon magenta glow
                speed: 52f, count: 20, size: 4f,
                pathResourceKey: "path_chapter_4",
                pathScrollSpeed: 0.20f,
                pathColor: new Color(1.00f, 0.40f, 0.80f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Horizontal),

            _ => new ChapterBgTheme(
                top:      Color.grey,
                bottom:   Color.black,
                particle: Color.white,
                speed: 50f, count: 10, size: 8f,
                pathResourceKey: "",
                pathScrollSpeed: 0.2f,
                pathColor: Color.white,
                pathWidth: 48f)
        };
    }
}
