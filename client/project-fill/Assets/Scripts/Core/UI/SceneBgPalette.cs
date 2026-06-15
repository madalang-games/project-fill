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
            return (bgThemeId, mode) switch
            {
                (1, BackgroundMode.Lobby) => GrasslandLobby(),
                (1, BackgroundMode.Night) => GrasslandNight(),
                (2, BackgroundMode.Lobby) => OceanLobby(),
                (2, BackgroundMode.Night) => OceanNight(),
                _                         => Boot()
            };
        }

        // ── Palettes ─────────────────────────────────────────────────

        private static SceneBgPalette Boot() => new(
            skyTop:        new Color(0.82f, 0.45f, 0.25f, 1f),
            skyBottom:     new Color(0.35f, 0.12f, 0.30f, 1f),
            accentA:       new Color(1.00f, 0.80f, 0.30f, 0.90f),
            accentB:       new Color(1.00f, 0.55f, 0.20f, 0.06f),
            particleColor: new Color(1.00f, 0.88f, 0.50f, 0.70f),
            particleSpeed: 18f, particleCount: 10);

        private static SceneBgPalette GrasslandLobby() => new(
            skyTop:        new Color(0.42f, 0.71f, 0.83f, 1f),
            skyBottom:     new Color(0.24f, 0.48f, 0.21f, 1f),
            accentA:       new Color(1.00f, 0.90f, 0.25f, 0.92f),
            accentB:       new Color(1.00f, 0.85f, 0.20f, 0.06f),
            particleColor: new Color(0.98f, 0.94f, 0.40f, 0.85f),
            particleSpeed: 30f, particleCount: 12);

        private static SceneBgPalette GrasslandNight() => new(
            skyTop:        new Color(0.05f, 0.06f, 0.18f, 1f),
            skyBottom:     new Color(0.07f, 0.16f, 0.09f, 1f),
            accentA:       new Color(0.90f, 0.92f, 1.00f, 0.90f),
            accentB:       new Color(0.60f, 0.65f, 0.95f, 0.15f),
            particleColor: new Color(0.70f, 0.90f, 0.50f, 0.75f),
            particleSpeed: 20f, particleCount: 14, isNight: true);

        private static SceneBgPalette OceanLobby() => new(
            skyTop:        new Color(0.10f, 0.22f, 0.37f, 1f),
            skyBottom:     new Color(0.18f, 0.55f, 0.48f, 1f),
            accentA:       new Color(1.00f, 0.90f, 0.25f, 0.85f),
            accentB:       new Color(0.55f, 0.88f, 0.98f, 0.06f),
            particleColor: new Color(0.72f, 0.94f, 1.00f, 0.65f),
            particleSpeed: 40f, particleCount: 14);

        private static SceneBgPalette OceanNight() => new(
            skyTop:        new Color(0.02f, 0.04f, 0.15f, 1f),
            skyBottom:     new Color(0.03f, 0.12f, 0.20f, 1f),
            accentA:       new Color(0.00f, 0.80f, 0.70f, 0.60f),
            accentB:       new Color(0.20f, 0.40f, 0.80f, 0.20f),
            particleColor: new Color(0.40f, 0.95f, 0.85f, 0.70f),
            particleSpeed: 22f, particleCount: 18, isNight: true);
    }
}
