using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // A single signal chip visual: tinted rounded body, neon outline + glow, type glyph,
    // a circuit "back" for Blind lanes, and a pulsing overload glow. Stretches to fill its slot.
    public class ChipView : MonoBehaviour
    {
        private RectTransform _rt;
        private Image _glow, _fill, _outline, _back;
        private TextMeshProUGUI _label;

        private Chip _chip;
        private bool _revealed = true;
        private bool _selected;

        public RectTransform Rt => _rt;

        public static ChipView Build(Transform parent, SpriteSet sprites)
        {
            var rt = UiUtil.Rect(parent, "Chip");
            var cv = rt.gameObject.AddComponent<ChipView>();
            cv.Construct(sprites);
            return cv;
        }

        private void Construct(SpriteSet s)
        {
            _rt = (RectTransform)transform;
            UiUtil.Stretch(_rt);

            _glow = UiUtil.Image(transform, "Glow", s.Glow, new Color(1, 1, 1, 0f));
            UiUtil.Stretch(_glow.rectTransform);
            _glow.rectTransform.offsetMin = new Vector2(-14, -14);
            _glow.rectTransform.offsetMax = new Vector2(14, 14);

            _fill = UiUtil.Image(transform, "Fill", s.Chip, Color.white);
            UiUtil.Stretch(_fill.rectTransform, 2f);

            _outline = UiUtil.Image(transform, "Outline", s.ChipOutline, Color.white);
            UiUtil.Stretch(_outline.rectTransform, 2f);

            _label = UiUtil.Label(transform, "Glyph", "", 30f);
            UiUtil.Stretch(_label.rectTransform);
            _label.fontStyle = FontStyles.Bold;

            // Blind "back": rounded chip body in dark circuit-blue with an inset circuit pattern.
            _back = UiUtil.Image(transform, "Back", s.Chip, new Color(0.10f, 0.14f, 0.20f));
            UiUtil.Stretch(_back.rectTransform, 2f);
            var circ = UiUtil.Image(_back.transform, "Circ", s.Circuit, new Color(0.32f, 0.62f, 0.68f, 0.9f), sliced: false);
            UiUtil.Stretch(circ.rectTransform, 6f);
            _back.gameObject.SetActive(false);
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
                var col = _chip.Type.ToColor();
                _fill.color    = col;
                _outline.color = Color.Lerp(col, Color.white, 0.55f);
                _label.text    = _chip.Type.ToLabel();
                _label.color   = new Color(0f, 0f, 0f, 0.62f);
            }
            else
            {
                _back.gameObject.SetActive(true);
                _fill.color    = new Color(0.14f, 0.18f, 0.24f);
                _outline.color = new Color(0.30f, 0.55f, 0.62f);
                _label.text    = "";
            }
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetSelected(bool on)
        {
            _selected = on;
            if (!on) { transform.localScale = Vector3.one; }
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
            float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.5f + 0.5f;

            if (_selected)
            {
                var col = _revealed ? _chip.Type.ToColor() : new Color(0.4f, 0.7f, 0.8f);
                _glow.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.45f, 0.9f, pulse));
                float s = Mathf.Lerp(1.06f, 1.11f, pulse);
                transform.localScale = new Vector3(s, s, 1f);
            }
            else if (_revealed && _chip.Overload)
            {
                // Ch5 overload chip: red-orange pulse glow.
                _glow.color = new Color(1f, 0.35f, 0.12f, Mathf.Lerp(0.25f, 0.7f, pulse));
            }
            else
            {
                _glow.color = new Color(_glow.color.r, _glow.color.g, _glow.color.b, 0f);
            }
        }
    }
}
