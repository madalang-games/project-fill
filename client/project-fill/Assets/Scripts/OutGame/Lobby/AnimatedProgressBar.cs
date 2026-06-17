using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Sprite-free progress bar. Fill is driven by RectTransform width (anchorMax.x), so it needs
    /// no sprite or Image.fillAmount. Presentation is fully code-generated: animated fill-up,
    /// ratio-based gradient color, and a soft continuous glow (gentle brightness breathing toward white,
    /// stronger once completed). Wired by UIEditorSetup.BuildAchievementCell; driven via SetProgress.
    /// </summary>
    public class AnimatedProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Image _fillImage;

        [SerializeField] private Color _lowColor = new Color(0.95f, 0.28f, 0.40f);  // empty: warm red
        [SerializeField] private Color _midColor = new Color(0.30f, 0.79f, 0.94f);  // half: cyan
        [SerializeField] private Color _highColor = new Color(0.02f, 0.84f, 0.63f); // full: teal

        private const float FillDuration = 0.5f;

        private float _current;
        private float _target;
        private bool _completed;
        private Color _baseColor;
        private Coroutine _anim;

        public void SetProgress(float ratio, bool completed)
        {
            _target = Mathf.Clamp01(ratio);
            _completed = completed;

            if (!isActiveAndEnabled)
            {
                Apply(_target);
                return;
            }
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateFill());
        }

        private IEnumerator AnimateFill()
        {
            float start = _current;
            float t = 0f;
            while (t < FillDuration)
            {
                t += Time.deltaTime;
                Apply(Mathf.Lerp(start, _target, Mathf.SmoothStep(0f, 1f, t / FillDuration)));
                yield return null;
            }
            Apply(_target);
            _anim = null;
        }

        private void Apply(float ratio)
        {
            _current = ratio;
            if (_fill != null) _fill.anchorMax = new Vector2(ratio, 1f);
            _baseColor = Gradient(ratio);
        }

        private Color Gradient(float r) => r < 0.5f
            ? Color.Lerp(_lowColor, _midColor, r / 0.5f)
            : Color.Lerp(_midColor, _highColor, (r - 0.5f) / 0.5f);

        private void Update()
        {
            if (_fillImage == null) return;

            // Soft continuous glow: breathe the fill toward white. Stronger + faster once completed.
            float amp = _completed ? 0.24f : 0.10f;
            float speed = _completed ? 3.2f : 1.8f;
            float g = amp * (0.5f + 0.5f * Mathf.Sin(Time.time * speed));
            var c = Color.Lerp(_baseColor, Color.white, g);
            c.a = 1f;
            _fillImage.color = c;
        }
    }
}
