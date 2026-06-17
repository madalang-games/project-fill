using UnityEngine;

namespace Game.Core.UI
{
    public enum BackgroundMode { Default, Lobby, Night }

    public readonly struct SceneBgPalette
    {
        public readonly Color SkyTop;
        public readonly Color SkyBottom;
        public readonly Color AccentA;
        public readonly Color AccentB;
        public readonly Color ParticleColor;
        public readonly float ParticleSpeed;
        public readonly int   ParticleCount;
        public readonly bool  IsNight;

        private SceneBgPalette(Color skyTop, Color skyBottom, Color accentA, Color accentB,
            Color particleColor, float particleSpeed, int particleCount, bool isNight = false)
        {
            SkyTop        = skyTop;
            SkyBottom     = skyBottom;
            AccentA       = accentA;
            AccentB       = accentB;
            ParticleColor = particleColor;
            ParticleSpeed = particleSpeed;
            ParticleCount = particleCount;
            IsNight       = isNight;
        }

        public static SceneBgPalette Get(int bgThemeId, BackgroundMode mode)
        {
            if (mode == BackgroundMode.Default)
                return Boot();
            return LobbyTheme(bgThemeId, mode == BackgroundMode.Night);
        }

        // ── Palettes (circuit / neon dark, matches ChapterBgTheme hue sweep) ──

        // Boot: circuit dusk — indigo→deep blue with cyan signal glow.
        private static SceneBgPalette Boot() => new(
            skyTop:        new Color(0.10f, 0.07f, 0.22f, 1f),
            skyBottom:     new Color(0.04f, 0.10f, 0.22f, 1f),
            accentA:       new Color(0.30f, 0.85f, 1.00f, 0.90f),
            accentB:       new Color(0.40f, 0.70f, 1.00f, 0.06f),
            particleColor: new Color(0.60f, 0.90f, 1.00f, 0.70f),
            particleSpeed: 18f, particleCount: 10);

        // Lobby/Night: dark bg tinted by the chapter's neon accent.
        private static SceneBgPalette LobbyTheme(int themeId, bool night)
        {
            Color a       = ThemeNeon(themeId);
            Color skyTop  = night ? new Color(0.03f, 0.03f, 0.07f, 1f)
                                  : new Color(0.05f, 0.06f, 0.12f, 1f);
            float bMul    = night ? 0.10f : 0.18f;
            Color skyBot  = new Color(a.r * bMul, a.g * bMul, a.b * bMul + 0.05f, 1f);

            return new SceneBgPalette(
                skyTop:        skyTop,
                skyBottom:     skyBot,
                accentA:       a,
                accentB:       new Color(a.r, a.g, a.b, 0.10f),
                particleColor: new Color(a.r, a.g, a.b, night ? 0.70f : 0.60f),
                particleSpeed: night ? 22f : 30f,
                particleCount: night ? 16 : 12,
                isNight:       night);
        }

        // Neon accent per chapter — cyan → blue → violet → magenta.
        private static Color ThemeNeon(int themeId) => themeId switch
        {
            1 => new Color(0.21f, 0.84f, 0.95f, 1f),
            2 => new Color(0.40f, 0.55f, 1.00f, 1f),
            3 => new Color(0.72f, 0.40f, 1.00f, 1f),
            4 => new Color(1.00f, 0.40f, 0.80f, 1f),
            _ => new Color(0.21f, 0.84f, 0.95f, 1f),
        };
    }
}
