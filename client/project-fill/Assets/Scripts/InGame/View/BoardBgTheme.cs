using UnityEngine;

namespace Game.InGame.View
{
    // Visual archetype that decides which ambient effects the board background renders.
    public enum BoardDecoKind { NeonGrid, Minimal, Quantum, Retro }

    // Per board-skin cosmetic palette + decoration template. Keyed by the cosmetic_id
    // from shared/datas/cosmetic/cosmetic_item.csv (Board category). Pure config —
    // InGameSceneBackgroundView turns it into a gradient + ambient deco.
    public readonly struct BoardBgTheme
    {
        public readonly Color         Top;
        public readonly Color         Bottom;
        public readonly Color         Accent; // drives grid / motes / scanline tint
        public readonly BoardDecoKind Kind;

        private BoardBgTheme(Color top, Color bottom, Color accent, BoardDecoKind kind)
        {
            Top    = top;
            Bottom = bottom;
            Accent = accent;
            Kind   = kind;
        }

        public static BoardBgTheme Get(string boardSkinId) => boardSkinId switch
        {
            "board_void" => new BoardBgTheme(            // Void — minimal black grid
                top:    new Color(0.010f, 0.010f, 0.020f, 1f),
                bottom: new Color(0.030f, 0.030f, 0.050f, 1f),
                accent: new Color(0.30f, 0.45f, 0.60f, 1f),
                kind:   BoardDecoKind.Minimal),

            "board_quantum" => new BoardBgTheme(         // Quantum Field — violet wave + particles
                top:    new Color(0.10f, 0.04f, 0.18f, 1f),
                bottom: new Color(0.20f, 0.05f, 0.22f, 1f),
                accent: new Color(0.72f, 0.40f, 1.00f, 1f),
                kind:   BoardDecoKind.Quantum),

            "board_retro_dos" => new BoardBgTheme(       // Retro DOS — green terminal
                top:    new Color(0.00f, 0.03f, 0.00f, 1f),
                bottom: new Color(0.00f, 0.08f, 0.02f, 1f),
                accent: new Color(0.30f, 1.00f, 0.40f, 1f),
                kind:   BoardDecoKind.Retro),

            "board_circuit" => new BoardBgTheme(         // Circuit Board — teal traces
                top:    new Color(0.02f, 0.10f, 0.08f, 1f),
                bottom: new Color(0.03f, 0.16f, 0.12f, 1f),
                accent: new Color(0.30f, 1.00f, 0.75f, 1f),
                kind:   BoardDecoKind.NeonGrid),

            "board_vintage" => new BoardBgTheme(         // Vintage Terminal — amber CRT
                top:    new Color(0.06f, 0.03f, 0.00f, 1f),
                bottom: new Color(0.12f, 0.07f, 0.01f, 1f),
                accent: new Color(1.00f, 0.72f, 0.25f, 1f),
                kind:   BoardDecoKind.Retro),

            "board_collector" => new BoardBgTheme(       // Collector's Edition — gold grid
                top:    new Color(0.10f, 0.08f, 0.02f, 1f),
                bottom: new Color(0.16f, 0.12f, 0.03f, 1f),
                accent: new Color(1.00f, 0.84f, 0.35f, 1f),
                kind:   BoardDecoKind.NeonGrid),

            "board_challenge" => new BoardBgTheme(       // Signal Champion — magenta field
                top:    new Color(0.12f, 0.03f, 0.10f, 1f),
                bottom: new Color(0.20f, 0.05f, 0.14f, 1f),
                accent: new Color(1.00f, 0.45f, 0.80f, 1f),
                kind:   BoardDecoKind.Quantum),

            // board_default (Neo-Semi) and any unknown id
            _ => new BoardBgTheme(
                top:    new Color(0.06f, 0.07f, 0.14f, 1f),
                bottom: new Color(0.04f, 0.12f, 0.20f, 1f),
                accent: new Color(0.21f, 0.84f, 0.95f, 1f),
                kind:   BoardDecoKind.NeonGrid),
        };
    }
}
