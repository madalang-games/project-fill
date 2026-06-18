using System;
using UnityEngine;

namespace Game.InGame.View
{
    // Optional art override. Leave fields empty and TextureFactory provides a procedural fallback,
    // so designers can later drop in real sprites without touching code (asset slot-in).
    [Serializable]
    public class BoardSkin
    {
        public Sprite chip;
        public Sprite chipOutline;
        public Sprite laneOutline; // lane frame / sockets (separate from chip outline)
        public Sprite glow;
        public Sprite laneSlot;
        public Sprite circuit;
        public Sprite panelNode;
        public Sprite disc;
        public Sprite ring;
        public Sprite lockSeal;
    }

    // Resolved render bundle used at runtime: sprite refs (skin override or procedural fallback)
    // PLUS the active cosmetic's color/finish tokens (from BoardTheme). Every board view already
    // receives a SpriteSet, so carrying the theme here skins chips/lanes/nodes with no extra plumbing.
    public class SpriteSet
    {
        public Sprite Chip, ChipOutline, LaneOutline, Glow, LaneSlot, Circuit, PanelNode, Disc, Ring, LockSeal;

        // Theme tokens (defaults = original procedural look; populated from BoardTheme).
        public Color       ChipOutlineColor = new(0.10f, 0.11f, 0.15f, 1f);
        public float       ChipFillAlpha    = 1f;
        public float       ChipGlowAlpha    = 0.18f;
        public bool        ChipNeonEdge;
        public bool        ChipOutlinePulse;
        public bool        ChipGhost;
        public bool        ChipSpectrum;       // top-tier: chip rim cycles the colour spectrum
        public ChipFinish  ChipFinish       = ChipFinish.Flat;
        public Sprite      ChipFinishSprite;   // procedural surface-finish overlay (null = Flat)
        public float       LaneEdgePulse;      // premium lane backlight + inner-frame breathe (0 = static)
        public Color       LaneBorder       = new(0.35f, 0.40f, 0.55f, 0.60f);
        public Color       LaneBody         = new(0.08f, 0.10f, 0.15f, 0.45f);
        public Color       LaneSocket       = new(0.06f, 0.07f, 0.10f, 0.60f);
        public Color       LaneContacts     = new(0.92f, 0.69f, 0.20f, 1f);
        public Color       LaneRail         = new(0.22f, 0.25f, 0.34f, 1f);
        public Color       LaneBacklight    = new(0.21f, 0.84f, 0.95f, 0.18f);
        public Color       Surface          = new(0.05f, 0.07f, 0.12f, 0.62f);
        public Color       SurfaceBorder    = new(0.21f, 0.84f, 0.95f, 0.45f);
        public float       SurfaceEdgePulse;   // premium board frame breathe amplitude (0 = static)
        public int         BoardMoteCount;     // premium board ambient drifting motes (0 = none)
        public int         SurfaceEdgeSparkCount; // top-tier perimeter-orbiting edge sparks (0 = none)
        public bool        SpectrumCycle;      // top-tier: board edge/motes/sparks cycle hue
        public Color       Accent           = new(0.21f, 0.84f, 0.95f);
        public BoardFxTier Fx               = BoardFxTier.Static;

        public static SpriteSet Resolve(BoardSkin s, BoardTheme theme = null)
        {
            s ??= new BoardSkin();
            theme ??= new BoardTheme();
            return new SpriteSet
            {
                Chip        = s.chip        ? s.chip        : TextureFactory.RoundedRect(64, 0.28f),
                // Solid occluding rim (not a thin centred band) so a bright/neon outline never lets the
                // signal-colour fill peek through at the rounded corners — clean, finished chip edges.
                ChipOutline = s.chipOutline ? s.chipOutline : TextureFactory.RoundedRimSolid(64, 0.28f, 0.09f),
                LaneOutline = s.laneOutline ? s.laneOutline : TextureFactory.RoundedOutline(64, 0.28f, 0.09f),
                Glow        = s.glow        ? s.glow        : TextureFactory.Glow(96, 0.14f),
                LaneSlot    = s.laneSlot    ? s.laneSlot    : TextureFactory.RoundedRect(48, 0.32f),
                Circuit     = s.circuit     ? s.circuit     : TextureFactory.Circuit(256),
                PanelNode   = s.panelNode   ? s.panelNode   : TextureFactory.RoundedRect(48, 0.36f),
                Disc        = s.disc        ? s.disc        : TextureFactory.Disc(64),
                Ring        = s.ring        ? s.ring        : TextureFactory.Ring(64, 0.16f),
                LockSeal    = s.lockSeal    ? s.lockSeal    : TextureFactory.Padlock(64),

                ChipOutlineColor = theme.ChipOutline,
                ChipFillAlpha    = theme.ChipFillAlpha,
                ChipGlowAlpha    = theme.ChipGlowAlpha,
                ChipNeonEdge     = theme.ChipNeonEdge,
                ChipOutlinePulse = theme.ChipOutlinePulse,
                ChipGhost        = theme.ChipGhost,
                ChipSpectrum     = theme.ChipSpectrum,
                ChipFinish       = theme.ChipFinish,
                ChipFinishSprite = TextureFactory.ChipFinishOverlay(theme.ChipFinish),
                LaneEdgePulse    = theme.LaneEdgePulse,
                LaneBorder       = theme.LaneBorder,
                LaneBody         = theme.LaneBody,
                LaneSocket       = theme.LaneSocket,
                LaneContacts     = theme.LaneContacts,
                LaneRail         = theme.LaneRail,
                LaneBacklight    = theme.LaneBacklight,
                Surface          = theme.Surface,
                SurfaceBorder    = theme.SurfaceBorder,
                SurfaceEdgePulse = theme.SurfaceEdgePulse,
                BoardMoteCount   = theme.BoardMoteCount,
                SurfaceEdgeSparkCount = theme.SurfaceEdgeSparkCount,
                SpectrumCycle    = theme.SpectrumCycle,
                Accent           = theme.Accent,
                Fx               = theme.Fx,
            };
        }
    }
}
