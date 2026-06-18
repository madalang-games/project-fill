using UnityEngine;

namespace Game.InGame.View
{
    // Visual energy tier of a board skin. Cheap skins are Static (color-only restyle);
    // premium skins are Dynamic (animated neon edges / breathing borders / accent gradients).
    public enum BoardFxTier { Static, Dynamic }

    // Resolved board-piece skin: the color/finish tokens the board pieces (chips, lanes, nodes)
    // render with, instead of hardcoded constants. A board cosmetic is the master container —
    // it themes all three piece types + sets the Fx tier. Chip / Lane cosmetics, when active,
    // layer their own overrides on top (the player can mix a chip skin onto any board).
    //
    // Defaults below MUST reproduce the original procedural look so default-board/chip/lane
    // players see no regression. Chip BODY hue always stays the signal color (gameplay identity);
    // skins only restyle the outline, fill alpha, ambient glow, and the lane/node frame colors.
    public sealed class BoardTheme
    {
        // ── Chip ────────────────────────────────────────────────
        public Color ChipOutline   = new(0.10f, 0.11f, 0.15f, 1f);
        public float ChipFillAlpha = 1f;
        public float ChipGlowAlpha = 0.18f;   // ambient halo behind a resting chip
        public bool  ChipNeonEdge  = false;    // Dynamic: glow animates signal↔accent gradient
        public bool  ChipOutlinePulse = false; // premium: rim brightness breathes toward white/accent
        public bool  ChipGhost     = false;    // chip_ghost: holographic flicker (not just low alpha)
        public bool  ChipSpectrum  = false;    // top-tier: rim cycles through the colour spectrum
        public ChipFinish ChipFinish = ChipFinish.Flat; // cosmetic surface material (stipple/scanline/bevel/gloss)

        // ── Lane ────────────────────────────────────────────────
        // Border is the INTERACTION-STATE channel (select/valid/pending) — skins must NOT rely on it
        // for identity since it is overwritten while the player plays. Lane skin identity lives in the
        // always-visible furniture: Rail (side guide-walls) + Backlight (inner ambient glow).
        public Color LaneBorder    = new(0.35f, 0.40f, 0.55f, 0.60f);
        public Color LaneBody      = new(0.08f, 0.10f, 0.15f, 0.45f);
        public Color LaneSocket    = new(0.06f, 0.07f, 0.10f, 0.60f);
        public Color LaneContacts  = new(0.92f, 0.69f, 0.20f, 1f);
        public Color LaneRail      = new(0.22f, 0.25f, 0.34f, 1f);    // side guide-rail metal (skin identity)
        public Color LaneBacklight = new(0.21f, 0.84f, 0.95f, 0.18f); // inner ambient behind sockets (skin accent)
        // Premium lane identity (replaces the old vertical LaneFlow sweep, which collided with the blind
        // scan). 0 = static. >0 = backlight + a tight inner-frame accent line breathe, BOTH kept inside the
        // lane bounds so a premium lane never bleeds onto its neighbours. Never the state border.
        public float LaneEdgePulse = 0f;

        // ── Board surface (the visible inset panel the pieces sit on) ──
        public Color Surface       = new(0.05f, 0.07f, 0.12f, 0.62f);
        public Color SurfaceBorder = new(0.21f, 0.84f, 0.95f, 0.45f);
        // Premium board FX, scaled by cosmetic price (cheap boards = 0 = no regression). EdgePulse =
        // surface-frame alpha/accent breathe amplitude; MoteCount = drifting ambient glow motes on the
        // surface (fixed pool, animated — no per-frame spawn churn), rendered BEHIND the pieces.
        public float SurfaceEdgePulse = 0f;
        public int   BoardMoteCount   = 0;
        public int   SurfaceEdgeSparkCount = 0; // top-tier: sparks orbiting the surface PERIMETER (edge FX)
        public bool  SpectrumCycle    = false;  // top-tier: edge/motes/sparks cycle hue (red→…→blue)

        // ── Shared ──────────────────────────────────────────────
        public Color       Accent = new(0.21f, 0.84f, 0.95f); // neon tint for dynamic FX + node ring
        public BoardFxTier Fx     = BoardFxTier.Static;

        // Composes a theme: board master tokens, then chip + lane skin overrides on top.
        public static BoardTheme Resolve(string boardSkinId, string chipSkinId, string laneSkinId)
        {
            var t = Board(boardSkinId);
            // Inner backlight tracks the board accent by default (a lane skin may override below).
            t.LaneBacklight = new Color(t.Accent.r, t.Accent.g, t.Accent.b, 0.18f);
            ApplyChip(t, chipSkinId);
            ApplyLane(t, laneSkinId);

            // Surface follows the BOARD palette (accent), independent of chip/lane overrides.
            // Must read clearly against the near-black camera clear (~0.06) — keep it a distinctly
            // lighter panel + a bold accent frame so the board outline is obviously visible.
            var sf = Color.Lerp(new Color(0.11f, 0.13f, 0.20f, 1f), t.Accent, 0.18f);
            sf.a = 0.95f;
            t.Surface       = sf;
            t.SurfaceBorder = new Color(t.Accent.r, t.Accent.g, t.Accent.b, 0.90f);
            return t;
        }

        // ── Board master ────────────────────────────────────────
        private static BoardTheme Board(string id) => id switch
        {
            // Void — minimal black, cool blue-grey accent. Static, color-only.
            "board_void" => new BoardTheme
            {
                Accent       = new Color(0.30f, 0.45f, 0.60f, 1f),
                ChipOutline  = new Color(0.08f, 0.10f, 0.14f, 1f),
                ChipGlowAlpha = 0.10f,
                LaneBorder   = new Color(0.28f, 0.34f, 0.44f, 0.55f),
                LaneBody     = new Color(0.04f, 0.05f, 0.08f, 0.50f),
                LaneContacts = new Color(0.40f, 0.46f, 0.56f, 1f),
                Fx           = BoardFxTier.Static,
            },

            // Retro DOS — green terminal. Static.
            "board_retro_dos" => new BoardTheme
            {
                Accent       = new Color(0.30f, 1.00f, 0.40f, 1f),
                ChipOutline  = new Color(0.04f, 0.14f, 0.04f, 1f),
                ChipGlowAlpha = 0.16f,
                LaneBorder   = new Color(0.24f, 0.55f, 0.28f, 0.60f),
                LaneBody     = new Color(0.01f, 0.07f, 0.02f, 0.55f),
                LaneSocket   = new Color(0.02f, 0.10f, 0.03f, 0.60f),
                LaneContacts = new Color(0.36f, 0.85f, 0.40f, 1f),
                Fx           = BoardFxTier.Static,
            },

            // Vintage Terminal — amber CRT. Static.
            "board_vintage" => new BoardTheme
            {
                Accent       = new Color(1.00f, 0.72f, 0.25f, 1f),
                ChipOutline  = new Color(0.16f, 0.09f, 0.01f, 1f),
                ChipGlowAlpha = 0.18f,
                LaneBorder   = new Color(0.60f, 0.42f, 0.16f, 0.60f),
                LaneBody     = new Color(0.10f, 0.06f, 0.01f, 0.50f),
                LaneContacts = new Color(0.95f, 0.70f, 0.24f, 1f),
                Fx           = BoardFxTier.Static,
            },

            // Circuit Board — teal traces. Dynamic neon.
            "board_circuit" => new BoardTheme
            {
                Accent       = new Color(0.30f, 1.00f, 0.75f, 1f),
                ChipOutline  = new Color(0.06f, 0.30f, 0.24f, 1f),
                ChipGlowAlpha = 0.24f,
                ChipNeonEdge = true,
                LaneBorder   = new Color(0.20f, 0.70f, 0.55f, 0.65f),
                LaneBody     = new Color(0.02f, 0.12f, 0.09f, 0.50f),
                LaneContacts = new Color(0.30f, 0.95f, 0.72f, 1f),
                Fx           = BoardFxTier.Dynamic,
                SurfaceEdgePulse = 0.18f, BoardMoteCount = 4,
            },

            // Collector's Edition — gold grid. Dynamic neon.
            "board_collector" => new BoardTheme
            {
                Accent       = new Color(1.00f, 0.84f, 0.35f, 1f),
                ChipOutline  = new Color(0.28f, 0.21f, 0.04f, 1f),
                ChipGlowAlpha = 0.26f,
                ChipNeonEdge = true,
                LaneBorder   = new Color(0.85f, 0.68f, 0.28f, 0.70f),
                LaneBody     = new Color(0.12f, 0.09f, 0.02f, 0.50f),
                LaneContacts = new Color(1.00f, 0.82f, 0.34f, 1f),
                Fx           = BoardFxTier.Dynamic,
                SurfaceEdgePulse = 0.28f, BoardMoteCount = 6, SurfaceEdgeSparkCount = 4,
            },

            // Quantum Field — violet. Dynamic, fullest effects.
            "board_quantum" => new BoardTheme
            {
                Accent       = new Color(0.72f, 0.40f, 1.00f, 1f),
                ChipOutline  = new Color(0.18f, 0.08f, 0.30f, 1f),
                ChipGlowAlpha = 0.28f,
                ChipNeonEdge = true,
                LaneBorder   = new Color(0.62f, 0.40f, 0.95f, 0.70f),
                LaneBody     = new Color(0.10f, 0.04f, 0.18f, 0.50f),
                LaneContacts = new Color(0.78f, 0.46f, 1.00f, 1f),
                Fx           = BoardFxTier.Dynamic,
                SurfaceEdgePulse = 0.38f, BoardMoteCount = 8, SurfaceEdgeSparkCount = 6,
            },

            // Signal Champion — magenta. Dynamic neon.
            "board_challenge" => new BoardTheme
            {
                Accent       = new Color(1.00f, 0.45f, 0.80f, 1f),
                ChipOutline  = new Color(0.28f, 0.05f, 0.18f, 1f),
                ChipGlowAlpha = 0.26f,
                ChipNeonEdge = true,
                LaneBorder   = new Color(0.90f, 0.40f, 0.72f, 0.70f),
                LaneBody     = new Color(0.16f, 0.03f, 0.11f, 0.50f),
                LaneContacts = new Color(1.00f, 0.48f, 0.82f, 1f),
                Fx           = BoardFxTier.Dynamic,
                SurfaceEdgePulse = 0.30f, BoardMoteCount = 6, SurfaceEdgeSparkCount = 5,
            },

            // Spectrum Flux — top-tier showcase: full hue-cycling edge + motes + edge sparks.
            "board_spectrum" => new BoardTheme
            {
                Accent       = new Color(0.95f, 0.95f, 1.00f, 1f),
                ChipOutline  = new Color(0.14f, 0.14f, 0.20f, 1f),
                ChipGlowAlpha = 0.30f,
                ChipNeonEdge = true,
                LaneBorder   = new Color(0.70f, 0.70f, 0.85f, 0.70f),
                LaneBody     = new Color(0.06f, 0.06f, 0.12f, 0.50f),
                LaneContacts = new Color(0.90f, 0.90f, 1.00f, 1f),
                Fx           = BoardFxTier.Dynamic,
                SurfaceEdgePulse = 0.45f, BoardMoteCount = 9, SurfaceEdgeSparkCount = 8,
                SpectrumCycle = true,
            },

            // board_default (Neo-Semi) and any unknown id → original look.
            _ => new BoardTheme(),
        };

        // ── Chip skin override (body hue stays the signal color) ──
        private static void ApplyChip(BoardTheme t, string id)
        {
            switch (id)
            {
                case "chip_hex": // crisp dark edge + retro pixel stipple
                    t.ChipOutline = new Color(0.06f, 0.09f, 0.16f, 1f);
                    t.ChipFinish  = ChipFinish.Dither;
                    break;
                case "chip_retro": // green terminal edge + CRT scanlines
                    t.ChipOutline = new Color(0.05f, 0.16f, 0.06f, 1f);
                    t.ChipFinish  = ChipFinish.Scanline;
                    break;
                case "chip_crystal": // bright crystalline rim + glassy gloss + pulsing rim
                    t.ChipOutline      = new Color(0.75f, 0.88f, 0.98f, 1f);
                    t.ChipGlowAlpha    = 0.22f;
                    t.ChipFinish       = ChipFinish.Gloss;
                    t.ChipOutlinePulse = true;
                    break;
                case "chip_platinum": // metallic silver rim + embossed bevel + pulsing rim
                    t.ChipOutline      = new Color(0.85f, 0.88f, 0.92f, 1f);
                    t.ChipGlowAlpha    = 0.24f;
                    t.ChipFinish       = ChipFinish.Bevel;
                    t.ChipOutlinePulse = true;
                    break;
                case "chip_ghost": // holographic ghost token: translucent body + flicker/scan (not just alpha)
                    t.ChipOutline   = new Color(0.55f, 0.70f, 0.78f, 0.85f);
                    t.ChipFillAlpha = 0.55f;
                    t.ChipGlowAlpha = 0.14f;
                    t.ChipGhost     = true;
                    break;
                case "chip_neon": // animated neon edge + glossy sheen + pulsing rim
                    t.ChipOutline      = new Color(0.30f, 0.95f, 1.00f, 1f);
                    t.ChipGlowAlpha    = 0.30f;
                    t.ChipNeonEdge     = true;
                    t.ChipFinish       = ChipFinish.Gloss;
                    t.ChipOutlinePulse = true;
                    break;
                case "chip_daily": // dynamic festive neon + stipple + pulsing rim
                    t.ChipOutline      = new Color(1.00f, 0.55f, 0.85f, 1f);
                    t.ChipGlowAlpha    = 0.30f;
                    t.ChipNeonEdge     = true;
                    t.ChipFinish       = ChipFinish.Dither;
                    t.ChipOutlinePulse = true;
                    break;
                case "chip_prism": // top-tier: rim cycles the colour spectrum + glossy sheen
                    t.ChipOutline   = new Color(0.95f, 0.95f, 1.00f, 1f);
                    t.ChipGlowAlpha = 0.30f;
                    t.ChipNeonEdge  = true;
                    t.ChipFinish    = ChipFinish.Gloss;
                    t.ChipSpectrum  = true;
                    break;
                // chip_default / empty / unknown → keep board-provided chip tokens
            }
        }

        // ── Lane skin override ───────────────────────────────────
        private static void ApplyLane(BoardTheme t, string id)
        {
            switch (id)
            {
                case "lane_bronze":
                    t.LaneBorder    = new Color(0.72f, 0.48f, 0.24f, 0.65f);
                    t.LaneContacts  = new Color(0.85f, 0.55f, 0.25f, 1f);
                    t.LaneRail      = new Color(0.46f, 0.31f, 0.16f, 1f);
                    t.LaneBacklight = new Color(0.85f, 0.55f, 0.25f, 0.16f);
                    // Cheapest lane (bronze, 900g) = border/colour restyle only, no pulse.
                    t.LaneEdgePulse = 0f;
                    break;
                case "lane_holo":
                    t.LaneBorder    = new Color(0.40f, 0.85f, 1.00f, 0.70f);
                    t.LaneBody      = new Color(0.06f, 0.12f, 0.18f, 0.45f);
                    t.LaneContacts  = new Color(0.45f, 0.90f, 1.00f, 1f);
                    t.LaneRail      = new Color(0.24f, 0.45f, 0.58f, 1f);
                    t.LaneBacklight = new Color(0.45f, 0.90f, 1.00f, 0.22f);
                    t.LaneEdgePulse = 0.7f;
                    break;
                case "lane_terminal":
                    t.LaneBorder    = new Color(0.30f, 0.80f, 0.36f, 0.65f);
                    t.LaneBody      = new Color(0.02f, 0.10f, 0.03f, 0.50f);
                    t.LaneContacts  = new Color(0.36f, 0.88f, 0.42f, 1f);
                    t.LaneRail      = new Color(0.16f, 0.34f, 0.18f, 1f);
                    t.LaneBacklight = new Color(0.36f, 0.88f, 0.42f, 0.18f);
                    t.LaneEdgePulse = 0.8f;
                    break;
                case "lane_crystal":
                    t.LaneBorder    = new Color(0.78f, 0.90f, 1.00f, 0.75f);
                    t.LaneBody      = new Color(0.08f, 0.12f, 0.20f, 0.45f);
                    t.LaneContacts  = new Color(0.80f, 0.92f, 1.00f, 1f);
                    t.LaneRail      = new Color(0.55f, 0.66f, 0.80f, 1f);
                    t.LaneBacklight = new Color(0.80f, 0.92f, 1.00f, 0.20f);
                    t.LaneEdgePulse = 1.0f;
                    break;
                case "lane_ghost":
                    t.LaneBorder    = new Color(0.55f, 0.70f, 0.78f, 0.40f);
                    t.LaneBody      = new Color(0.06f, 0.08f, 0.12f, 0.25f);
                    t.LaneContacts  = new Color(0.55f, 0.66f, 0.74f, 0.70f);
                    t.LaneRail      = new Color(0.40f, 0.48f, 0.54f, 0.55f);
                    t.LaneBacklight = new Color(0.55f, 0.66f, 0.74f, 0.10f);
                    t.LaneEdgePulse = 0.3f;
                    break;
                // lane_default / empty / unknown → keep board-provided lane tokens
            }
        }
    }
}
