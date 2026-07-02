using System.Collections;
using UnityEngine;

namespace Game.InGame.View
{
    // A single signal chip visual: tinted rounded body, neon outline + glow, type glyph,
    // a circuit "back" for Blind lanes, and a pulsing overload glow. Fits its container.
    public class ChipView : MonoBehaviour
    {
        private SpriteRenderer _glow, _fill, _outline, _sheen, _finish, _back, _backCirc;
        private SpriteRenderer _overloadBadge, _badgeBack, _sweep; // Ch5 overload markers
        private SpriteRenderer _indexDot;

        private Chip _chip;
        private bool _revealed = true;
        private bool _selected;

        private bool _built;
        private Vector2 _size;
        private Vector2 _builtSize;

        // Size the visuals were constructed at — flight-chip pooling reuses an instance only when the
        // requested size matches, so a board resize can't leave a recycled flyer at the wrong scale.
        public Vector2 BuiltSize => _builtSize;

        // Skin tokens (from SpriteSet/BoardTheme); defaults reproduce the original look.
        private Color _outlineColor = new(0.10f, 0.11f, 0.15f, 1f);
        private float _fillAlpha    = 1f;
        private float _glowAlpha    = 0.18f;
        private bool  _neonEdge;
        private bool  _outlinePulse;  // premium: rim brightness breathes (animated in Update)
        private bool  _ghost;         // chip_ghost: holographic flicker (body wavers + glitch blinks)
        private bool  _spectrum;      // top-tier: rim cycles through the colour spectrum (red→…→blue)
        private Color _accent       = new(0.21f, 0.84f, 0.95f);

        public Transform Rt => transform;

        // Builds the chip visuals once. Call after instantiating the ChipView prefab.
        // (The prefab is a thin GO + this component; the body is procedural so art can be
        //  swapped via BoardSkin/SpriteSet rather than re-authoring the prefab hierarchy.)
        public ChipView Initialize(SpriteSet sprites, Vector2 size)
        {
            _size = size;
            if (!_built) Construct(sprites, size);
            return this;
        }

        private void Construct(SpriteSet s, Vector2 size)
        {
            _built = true;
            _builtSize = size;

            _outlineColor = s.ChipOutlineColor;
            _fillAlpha    = s.ChipFillAlpha;
            _glowAlpha    = s.ChipGlowAlpha;
            _neonEdge     = s.ChipNeonEdge;
            _outlinePulse = s.ChipOutlinePulse;
            _ghost        = s.ChipGhost;
            _spectrum     = s.ChipSpectrum;
            _accent       = s.Accent;

            _glow = WorldUtil.CreateSprite(transform, "Glow", s.Glow, new Color(1, 1, 1, 0f), size + new Vector2(0.12f, 0.12f), sortingOrder: 7);

            // Fill is inset slightly inside the outline so the solid rim fully frames the signal colour —
            // the body never pokes past the rounded corners (outline drawn at full `size`, sortingOrder 11).
            _fill = WorldUtil.CreateSprite(transform, "Fill", s.Chip, Color.white, size * 0.94f, sortingOrder: 9);

            // Top gloss sheen: a soft bright band across the upper third so the tinted square reads as a
            // luminous data cell (emissive register) rather than a flat sticker. Hidden on blind cells.
            _sheen = WorldUtil.CreateSprite(transform, "Sheen", s.Chip, new Color(1f, 1f, 1f, 0.14f), new Vector2(size.x * 0.78f, size.y * 0.34f), sortingOrder: 10);
            _sheen.transform.localPosition = new Vector3(0f, size.y * 0.24f, 0f);

            // Cosmetic surface finish (dither/scanline/bevel/gloss) masked to the chip shape, over the
            // fill, under the outline. The look is baked into the sprite → draw with white. null = Flat.
            _finish = WorldUtil.CreateSprite(transform, "Finish", s.ChipFinishSprite, Color.white, size * 0.94f, sliced: false, sortingOrder: 10);
            if (s.ChipFinishSprite == null) _finish.gameObject.SetActive(false);

            _outline = WorldUtil.CreateSprite(transform, "Outline", s.ChipOutline, Color.white, size, sortingOrder: 11);

            // Create index dot at top-left
            float dotSize = size.x * 0.08f;
            _indexDot = WorldUtil.CreateSprite(transform, "IndexDot", s.Disc, new Color(1f, 1f, 1f, 0.45f), new Vector2(dotSize, dotSize), sliced: false, sortingOrder: 11);
            _indexDot.transform.localPosition = new Vector3(-size.x * 0.35f, size.y * 0.35f, 0f);

            // Blind "back": rounded chip body in dark circuit-blue with an inset circuit pattern.
            // Circ sits under the chip ROOT (sibling of Back), NOT as Back's child — a non-9-sliced
            // chip skin gives Back a <1 localScale that a child Circ would inherit and shrink by.
            _back = WorldUtil.CreateSprite(transform, "Back", s.Chip, new Color(0.10f, 0.14f, 0.20f), size, sortingOrder: 12);
            _backCirc = WorldUtil.CreateSprite(transform, "Circ", s.Circuit, new Color(0.32f, 0.62f, 0.68f, 0.9f), size, sliced: false, sortingOrder: 13);
            _back.gameObject.SetActive(false);
            _backCirc.gameObject.SetActive(false);

            // Ch5 overload markers: a corner lightning badge (static identifier, on a dark disc so it
            // reads against any signal colour) + an in-bounds bright sweep bar (periodic discharge,
            // animated in Update). Gameplay-fixed, not a cosmetic axis → drawn straight from TextureFactory.
            float badge = size.x * 0.40f;
            var badgePos = new Vector3(size.x * 0.32f, size.y * 0.32f, -0.02f);
            _badgeBack = WorldUtil.CreateSprite(transform, "OverloadBack", s.Disc, new Color(0.08f, 0.05f, 0.02f, 0.85f), new Vector2(badge, badge), sliced: false, sortingOrder: 14);
            _overloadBadge = WorldUtil.CreateSprite(transform, "OverloadBolt", TextureFactory.Bolt(), new Color(1f, 0.62f, 0.16f), new Vector2(badge * 0.82f, badge * 0.82f), sliced: false, sortingOrder: 15);
            _badgeBack.transform.localPosition = badgePos;
            _overloadBadge.transform.localPosition = badgePos;
            _sweep = WorldUtil.CreateSprite(transform, "OverloadSweep", s.Chip, new Color(1f, 0.85f, 0.5f, 0f), new Vector2(size.x * 0.94f, size.y * 0.22f), sortingOrder: 12);
            _badgeBack.gameObject.SetActive(false);
            _overloadBadge.gameObject.SetActive(false);
            _sweep.gameObject.SetActive(false);
        }

        public void SetChip(Chip chip, bool revealed)
        {
            _chip     = chip;
            _revealed = revealed;
            gameObject.SetActive(true);
            ApplyVisual();
        }

        public Chip Chip     => _chip;
        public bool Revealed => _revealed;

        private void ApplyVisual()
        {
            if (_revealed)
            {
                _back.gameObject.SetActive(false);
                _backCirc.gameObject.SetActive(false);
                _sheen.gameObject.SetActive(true);
                if (_finish != null && _finish.sprite != null) _finish.gameObject.SetActive(true);
                var col = _chip.Type.ToColor();

                // Casual color token: body = the signal color, bold dark outline, dark glyph for
                // contrast. (When a real sprite is slotted into SpriteSet it drives the shape; these
                // colors still tint it.)
                _fill.color    = new Color(col.r, col.g, col.b, _fillAlpha);
                _outline.color = _outlineColor;
                _indexDot.gameObject.SetActive(true);
                _indexDot.color = Color.Lerp(col, Color.white, 0.5f);

                _badgeBack.gameObject.SetActive(_chip.Overload);
                _overloadBadge.gameObject.SetActive(_chip.Overload);
            }
            else
            {
                _back.gameObject.SetActive(true);
                _backCirc.gameObject.SetActive(true);
                _sheen.gameObject.SetActive(false);
                if (_finish != null) _finish.gameObject.SetActive(false);
                _fill.color    = new Color(0.09f, 0.11f, 0.14f);
                _outline.color = new Color(0.25f, 0.45f, 0.52f);
                _indexDot.gameObject.SetActive(false);
                _badgeBack.gameObject.SetActive(false);
                _overloadBadge.gameObject.SetActive(false);
                _sweep.gameObject.SetActive(false);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        private bool _flashing;

        // White-flash overlay for the move teleport FX (vanish at source, materialize at dest).
        // a: 0 = normal color, 1 = full white. Flight chips are always revealed.
        public void SetFlash(float a)
        {
            if (!_built || !_revealed) return;
            a = Mathf.Clamp01(a);
            _flashing = a > 0f;
            var col = _chip.Type.ToColor();
            var fc = Color.Lerp(col, Color.white, a); fc.a = _fillAlpha;
            _fill.color    = fc;
            _outline.color = Color.Lerp(_outlineColor, Color.white, a);
            if (_glow != null) _glow.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.18f, 0.95f, a));
        }

        private Vector3 _targetLocalPos = Vector3.zero;
        private float _punch; // one-shot select-pop overshoot, decays in Update

        public void SetSelected(bool on, float hoverYOffset = 0f)
        {
            _selected = on;
            _targetLocalPos = on ? new Vector3(0f, hoverYOffset, 0f) : Vector3.zero;
            if (on) { _punch = 1f; }
            else { transform.localScale = Vector3.one; }
        }

        public void AnimateReveal()
        {
            StopAllCoroutines();
            StartCoroutine(FlipRoutine());
        }

        private IEnumerator FlipRoutine()
        {
            float dur = 0.15f, t = 0f;
            float baseScale = _selected ? 1.08f : 1f;
            while (t < dur)
            {
                float p  = t / dur;
                float sx = Mathf.Abs(Mathf.Cos(p * Mathf.PI)); // 1 → 0 → 1 flip on X
                transform.localScale = new Vector3(sx * baseScale, baseScale, 1f);
                t += Time.deltaTime;
                yield return null;
            }
            transform.localScale = Vector3.one * baseScale;
        }

        private void Update()
        {
            if (_glow == null) return;
            if (_flashing) return; // SetFlash owns the visuals during the teleport FX
            float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.5f + 0.5f;

            // Interpolate local position for slide up / slide down
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, _targetLocalPos, Time.deltaTime * 14f);

            if (_selected)
            {
                if (_sweep != null && _sweep.gameObject.activeSelf) _sweep.gameObject.SetActive(false);
                var col = _revealed ? _chip.Type.ToColor() : new Color(0.4f, 0.7f, 0.8f);
                _punch = Mathf.MoveTowards(_punch, 0f, Time.deltaTime * 5f);
                _glow.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.7f, 1f, pulse));
                float s = Mathf.Lerp(1.08f, 1.14f, pulse) + _punch * 0.12f; // pop on select, settle to pulse
                transform.localScale = new Vector3(s, s, 1f);
            }
            else if (_revealed && _chip.Overload)
            {
                // Ch5 overload chip: a periodic charge→discharge GlowSweep so the "can't sit alone"
                // chips read at a glance. Charge ramps the orange halo up; discharge flashes it, throbs
                // the body brighter, and runs a bright bar up through the cell (kept in-bounds — alpha
                // fades to 0 at both travel ends so it never clips past the chip).
                const float period = 1.8f;
                float cyc       = (Time.unscaledTime % period) / period;
                bool  discharge = cyc >= 0.7f;
                float chargeT   = Mathf.InverseLerp(0f, 0.7f, cyc);
                float dischT    = discharge ? Mathf.InverseLerp(0.7f, 1f, cyc) : 0f;

                float glowA = discharge ? Mathf.Lerp(1f, 0.3f, dischT) : Mathf.Lerp(0.3f, 0.8f, chargeT);
                _glow.color = new Color(1f, 0.4f, 0.12f, glowA);

                var   col  = _chip.Type.ToColor();
                float thr  = discharge ? Mathf.Lerp(0.4f, 0f, dischT) : 0f; // brief whiten on discharge
                _fill.color = Color.Lerp(new Color(col.r, col.g, col.b, _fillAlpha), new Color(1f, 0.72f, 0.4f, _fillAlpha), thr);

                if (discharge)
                {
                    _sweep.gameObject.SetActive(true);
                    float py = Mathf.Lerp(-_size.y * 0.42f, _size.y * 0.42f, dischT);
                    _sweep.transform.localPosition = new Vector3(0f, py, 0.01f);
                    _sweep.color = new Color(1f, 0.85f, 0.5f, 0.55f * Mathf.Sin(dischT * Mathf.PI));
                }
                else
                {
                    _sweep.gameObject.SetActive(false);
                }
            }
            else
            {
                // Soft constant ambient glow behind chips for electronic motherboard feel.
                // Neon-edge skins (dynamic boards/chips) animate a signal↔accent gradient halo.
                if (_revealed)
                {
                    var col = _chip.Type.ToColor();
                    if (_neonEdge)
                    {
                        float g = Mathf.Sin(Time.unscaledTime * 3f) * 0.5f + 0.5f;
                        var a   = Color.Lerp(col, _accent, g);
                        _glow.color = new Color(a.r, a.g, a.b, Mathf.Lerp(_glowAlpha, _glowAlpha * 2.2f, g));
                    }
                    else
                    {
                        _glow.color = new Color(col.r, col.g, col.b, _glowAlpha);
                    }
                }
                else
                {
                    _glow.color = new Color(0.25f, 0.45f, 0.52f, 0.08f);
                }
            }

            // Premium chip identity (only while resting & revealed — selected/overload own the visuals).
            if (_revealed && !_selected && !(_chip.Overload))
            {
                // Spectrum (top-tier): rim cycles continuously through the colour wheel.
                if (_spectrum)
                {
                    float h = (Time.unscaledTime * 0.16f) % 1f;
                    _outline.color = Color.HSVToRGB(h, 0.85f, 1f);
                }
                // Holographic ghost: body alpha wavers + brief glitch dropouts, rim shimmers to accent.
                else if (_ghost)
                {
                    float sh     = Mathf.Sin(Time.unscaledTime * 3.3f) * 0.5f + 0.5f;
                    float glitch = Mathf.PerlinNoise(Time.unscaledTime * 8f, 0.37f) > 0.86f ? 0.45f : 1f;
                    var   col    = _chip.Type.ToColor();
                    _fill.color    = new Color(col.r, col.g, col.b, _fillAlpha * Mathf.Lerp(0.55f, 1f, sh) * glitch);
                    _outline.color = Color.Lerp(_outlineColor, _accent, 0.35f + 0.45f * sh);
                }
                // Pulsing rim: outline breathes toward white so a costly chip reads as "lit".
                else if (_outlinePulse)
                {
                    float p = Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f;
                    _outline.color = Color.Lerp(_outlineColor, Color.Lerp(_outlineColor, Color.white, 0.6f), p);
                }
            }
        }
    }
}
