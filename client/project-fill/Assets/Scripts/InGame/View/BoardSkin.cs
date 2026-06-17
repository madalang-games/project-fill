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

    // Resolved sprite references actually used at runtime (skin override or procedural fallback).
    public class SpriteSet
    {
        public Sprite Chip, ChipOutline, LaneOutline, Glow, LaneSlot, Circuit, PanelNode, Disc, Ring, LockSeal;

        public static SpriteSet Resolve(BoardSkin s)
        {
            s ??= new BoardSkin();
            return new SpriteSet
            {
                Chip        = s.chip        ? s.chip        : TextureFactory.RoundedRect(64, 0.28f),
                ChipOutline = s.chipOutline ? s.chipOutline : TextureFactory.RoundedOutline(64, 0.28f, 0.09f),
                LaneOutline = s.laneOutline ? s.laneOutline : TextureFactory.RoundedOutline(64, 0.28f, 0.09f),
                Glow        = s.glow        ? s.glow        : TextureFactory.Glow(96, 0.14f),
                LaneSlot    = s.laneSlot    ? s.laneSlot    : TextureFactory.RoundedRect(48, 0.32f),
                Circuit     = s.circuit     ? s.circuit     : TextureFactory.Circuit(256),
                PanelNode   = s.panelNode   ? s.panelNode   : TextureFactory.RoundedRect(48, 0.36f),
                Disc        = s.disc        ? s.disc        : TextureFactory.Disc(64),
                Ring        = s.ring        ? s.ring        : TextureFactory.Ring(64, 0.16f),
                LockSeal    = s.lockSeal    ? s.lockSeal    : TextureFactory.Padlock(64),
            };
        }
    }
}
