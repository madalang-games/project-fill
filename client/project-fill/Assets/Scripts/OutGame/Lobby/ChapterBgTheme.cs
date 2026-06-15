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
            1 => new ChapterBgTheme(                               // Grassland
                top:      new Color(0.42f, 0.71f, 0.83f, 1f),     // sky blue
                bottom:   new Color(0.24f, 0.48f, 0.21f, 1f),     // grass green
                particle: new Color(0.98f, 0.94f, 0.40f, 0.85f),
                speed: 35f, count: 14, size: 7f,
                pathResourceKey: "path_chapter_1",
                pathScrollSpeed: 0.15f,
                pathColor: new Color(0.65f, 0.95f, 0.55f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Upward),

            2 => new ChapterBgTheme(                               // Ocean
                top:      new Color(0.28f, 0.74f, 0.90f, 1f),     // bright coastal blue / shallow water
                bottom:   new Color(0.04f, 0.18f, 0.42f, 1f),     // deep ocean
                particle: new Color(0.72f, 0.94f, 1.00f, 0.65f),  // light blue bubbles
                speed: 38f, count: 16, size: 7f,
                pathResourceKey: "path_chapter_2",
                pathScrollSpeed: 0.3f,
                pathColor: new Color(0.20f, 0.85f, 0.98f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Upward),

            3 => new ChapterBgTheme(                               // Forest
                top:      new Color(0.06f, 0.20f, 0.04f, 1f),     // dark canopy
                bottom:   new Color(0.14f, 0.30f, 0.06f, 1f),     // forest floor
                particle: new Color(0.80f, 1.00f, 0.35f, 0.92f),  // firefly yellow-green
                speed: 16f, count: 14, size: 4f,
                pathResourceKey: "path_chapter_3",
                pathScrollSpeed: 0.10f,
                pathColor: new Color(0.28f, 0.65f, 0.12f, 1f),
                pathWidth: 140f,
                particleDir: ParticleDir.Upward),

            4 => new ChapterBgTheme(                               // Desert
                top:      new Color(0.96f, 0.82f, 0.44f, 1f),     // bleached amber sky
                bottom:   new Color(0.76f, 0.50f, 0.20f, 1f),     // terracotta sand
                particle: new Color(0.90f, 0.76f, 0.52f, 0.50f),  // sandy dust
                speed: 52f, count: 20, size: 4f,
                pathResourceKey: "path_chapter_4",
                pathScrollSpeed: 0.20f,
                pathColor: new Color(0.95f, 0.80f, 0.35f, 1f),
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
