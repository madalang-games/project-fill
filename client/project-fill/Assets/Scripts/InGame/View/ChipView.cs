using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.InGame.View
{
    // A single signal chip visual: tinted rounded body, neon outline + glow, type glyph,
    // a circuit "back" for Blind lanes, and a pulsing overload glow. Fits its container.
    public class ChipView : MonoBehaviour
    {
        private SpriteRenderer _glow, _fill, _outline, _back, _backCirc;
        private SpriteRenderer[] _pinsL;
        private SpriteRenderer[] _pinsR;
        private SpriteRenderer _indexDot;
        private TextMeshPro _label;

        private Chip _chip;
        private bool _revealed = true;
        private bool _selected;

        private bool _built;
        private Vector2 _size;

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

            _glow = WorldUtil.CreateSprite(transform, "Glow", s.Glow, new Color(1, 1, 1, 0f), size + new Vector2(0.12f, 0.12f), sortingOrder: 7);

            // Create 3 left pins and 3 right pins to look like IC chip packaging
            _pinsL = new SpriteRenderer[3];
            _pinsR = new SpriteRenderer[3];
            
            float pinW = size.x * 0.12f;
            float pinH = size.y * 0.08f;
            Color pinCol = new Color(0.75f, 0.77f, 0.82f); // silver/metallic
            
            for (int i = 0; i < 3; i++)
            {
                float py = -size.y * 0.25f + i * (size.y * 0.25f);
                
                var pinL = WorldUtil.CreateSprite(transform, $"Pin_L_{i}", s.Chip, pinCol, new Vector2(pinW, pinH), sortingOrder: 8);
                pinL.transform.localPosition = new Vector3(-size.x * 0.5f - pinW * 0.3f, py, 0f);
                _pinsL[i] = pinL;
                
                var pinR = WorldUtil.CreateSprite(transform, $"Pin_R_{i}", s.Chip, pinCol, new Vector2(pinW, pinH), sortingOrder: 8);
                pinR.transform.localPosition = new Vector3(size.x * 0.5f + pinW * 0.3f, py, 0f);
                _pinsR[i] = pinR;
            }

            // Casual token look: hide the IC side-pins (kept in the hierarchy for legacy sprite skins).
            for (int i = 0; i < 3; i++) { _pinsL[i].gameObject.SetActive(false); _pinsR[i].gameObject.SetActive(false); }

            _fill = WorldUtil.CreateSprite(transform, "Fill", s.Chip, Color.white, size, sortingOrder: 9);
            _outline = WorldUtil.CreateSprite(transform, "Outline", s.ChipOutline, Color.white, size, sortingOrder: 10);

            _label = WorldUtil.CreateLabel(transform, "Glyph", "", 30f, size);
            _label.fontStyle = FontStyles.Bold;
            _label.sortingOrder = 14;
            _label.gameObject.SetActive(false); // chip is color-only — no type glyph

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
                var col = _chip.Type.ToColor();

                // Casual color token: body = the signal color, bold dark outline, dark glyph for
                // contrast. (When a real sprite is slotted into SpriteSet it drives the shape; these
                // colors still tint it.)
                _fill.color    = col;
                _outline.color = new Color(0.10f, 0.11f, 0.15f, 1f);
                _label.text    = _chip.Type.ToLabel();
                _label.color   = Color.Lerp(col, Color.black, 0.72f);
                _indexDot.gameObject.SetActive(true);
                _indexDot.color = Color.Lerp(col, Color.white, 0.5f);
            }
            else
            {
                _back.gameObject.SetActive(true);
                _backCirc.gameObject.SetActive(true);
                _fill.color    = new Color(0.09f, 0.11f, 0.14f);
                _outline.color = new Color(0.25f, 0.45f, 0.52f);
                _label.text    = "";
                _indexDot.gameObject.SetActive(false);
                
                Color pinCol = new Color(0.38f, 0.40f, 0.45f);
                for (int i = 0; i < 3; i++)
                {
                    _pinsL[i].color = pinCol;
                    _pinsR[i].color = pinCol;
                }
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
            _fill.color    = Color.Lerp(col, Color.white, a);
            _outline.color = Color.Lerp(new Color(0.10f, 0.11f, 0.15f, 1f), Color.white, a);
            _label.color   = Color.Lerp(Color.Lerp(col, Color.black, 0.72f), Color.white, a);
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
                var col = _revealed ? _chip.Type.ToColor() : new Color(0.4f, 0.7f, 0.8f);
                _punch = Mathf.MoveTowards(_punch, 0f, Time.deltaTime * 5f);
                _glow.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.7f, 1f, pulse));
                float s = Mathf.Lerp(1.08f, 1.14f, pulse) + _punch * 0.12f; // pop on select, settle to pulse
                transform.localScale = new Vector3(s, s, 1f);
                
                Color selectPinCol = Color.Lerp(col, Color.white, 0.4f);
                for (int i = 0; i < 3; i++)
                {
                    _pinsL[i].color = selectPinCol;
                    _pinsR[i].color = selectPinCol;
                }
            }
            else if (_revealed && _chip.Overload)
            {
                // Ch5 overload chip: red-orange pulse glow.
                _glow.color = new Color(1f, 0.35f, 0.12f, Mathf.Lerp(0.35f, 0.75f, pulse));
                
                Color overloadPinCol = new Color(1f, 0.5f, 0.15f);
                for (int i = 0; i < 3; i++)
                {
                    _pinsL[i].color = overloadPinCol;
                    _pinsR[i].color = overloadPinCol;
                }
            }
            else
            {
                // Soft constant ambient glow behind chips for electronic motherboard feel
                if (_revealed)
                {
                    var col = _chip.Type.ToColor();
                    _glow.color = new Color(col.r, col.g, col.b, 0.18f);
                }
                else
                {
                    _glow.color = new Color(0.25f, 0.45f, 0.52f, 0.08f);
                }
            }
        }
    }
}
