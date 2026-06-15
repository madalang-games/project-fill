using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    /// <summary>
    /// Lightweight, C#-based icon idle animator.
    /// Modifies Transform properties (scale, position, rotation) purely in code,
    /// avoiding Unity Animator/Sprite locks and rendering conflicts on target devices.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIIconIdleAnimator : MonoBehaviour
    {
        public enum AnimationType
        {
            Pulse,
            Float,
            Rotate,
            /// <summary>Slow brightness glow + periodic flash sweep. Ideal for HUD/header icons.</summary>
            GlowSweep,
            /// <summary>Two quick scale beats then long rest. Ideal for life/stamina icons.</summary>
            HeartbeatPulse
        }

        [SerializeField] private AnimationType _animationType = AnimationType.Float;
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _amount = 10f; // Scale percent for Pulse/HeartbeatPulse, pixels for Float, degrees for Rotate, glow intensity for GlowSweep

        private RectTransform _rt;
        private Graphic _graphic;
        private Vector3 _initialScale;
        private Vector2 _initialPosition;
        private bool _positionCaptured;
        private Color _initialColor;
        private float _timeOffset;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _graphic = GetComponent<Graphic>();
            _initialScale = _rt.localScale;
            _initialColor = _graphic != null ? _graphic.color : Color.white;

            // Randomize phase offset to ensure multiple instances on screen breathe asynchronously
            _timeOffset = Random.Range(0f, 100f);
        }

        private void OnEnable()
        {
            if (_rt != null)
            {
                _rt.localScale = _initialScale;
                if (_positionCaptured)
                    _rt.anchoredPosition = _initialPosition;
            }
            if (_graphic != null)
                _graphic.color = _initialColor;
        }

        private void Update()
        {
            if (_rt == null) return;

            // Defer position capture to first Update so layout systems (HLG etc.) have resolved first
            if (!_positionCaptured)
            {
                _initialPosition = _rt.anchoredPosition;
                _positionCaptured = true;
            }

            float t = (Time.time + _timeOffset) * _speed;

            switch (_animationType)
            {
                case AnimationType.Float:
                    float newY = _initialPosition.y + Mathf.Sin(t) * _amount;
                    _rt.anchoredPosition = new Vector2(_initialPosition.x, newY);
                    break;

                case AnimationType.Pulse:
                    float s = 1f + Mathf.Sin(t) * (_amount * 0.01f);
                    _rt.localScale = _initialScale * s;
                    break;

                case AnimationType.Rotate:
                    float r = Mathf.Sin(t) * _amount;
                    _rt.localRotation = Quaternion.Euler(0f, 0f, r);
                    break;

                case AnimationType.GlowSweep:
                    UpdateGlowSweep(t);
                    break;

                case AnimationType.HeartbeatPulse:
                    UpdateHeartbeatPulse();
                    break;
            }
        }

        private void UpdateGlowSweep(float t)
        {
            if (_graphic == null) return;

            // Slow continuous brightness oscillation
            float glow = 1f + Mathf.Sin(t) * (_amount * 0.005f);

            // Periodic flash sweep: quick brightness spike every 5 seconds over 0.4s
            const float sweepInterval = 5f;
            const float sweepDuration = 0.4f;
            float sweepPhase = (Time.time + _timeOffset) % sweepInterval;
            float sweep = sweepPhase < sweepDuration
                ? Mathf.Sin(sweepPhase / sweepDuration * Mathf.PI) * 0.35f
                : 0f;

            float b = Mathf.Clamp(glow + sweep, 0f, 2f);
            _graphic.color = new Color(
                _initialColor.r * b,
                _initialColor.g * b,
                _initialColor.b * b,
                _initialColor.a);
        }

        private void UpdateHeartbeatPulse()
        {
            // Pattern: beat1_up → beat1_down → beat2_up(weaker) → beat2_down → rest
            const float beatDuration = 0.12f;
            const float beat2Scale = 0.7f;
            float restDuration = Mathf.Max(0.1f, (1f / _speed) - beatDuration * 4f);
            float cycleDuration = beatDuration * 4f + restDuration;
            float cycleT = (Time.time + _timeOffset) % cycleDuration;
            float scaleAdd = _amount * 0.01f;

            float s;
            if (cycleT < beatDuration)
                s = 1f + (cycleT / beatDuration) * scaleAdd;
            else if (cycleT < beatDuration * 2f)
                s = 1f + (1f - (cycleT - beatDuration) / beatDuration) * scaleAdd;
            else if (cycleT < beatDuration * 3f)
                s = 1f + ((cycleT - beatDuration * 2f) / beatDuration) * scaleAdd * beat2Scale;
            else if (cycleT < beatDuration * 4f)
                s = 1f + (1f - (cycleT - beatDuration * 3f) / beatDuration) * scaleAdd * beat2Scale;
            else
                s = 1f;

            _rt.localScale = _initialScale * s;
        }

        // Helper setters for configuring animation programmatically if needed
        public void Configure(AnimationType type, float speed, float amount)
        {
            _animationType = type;
            _speed = speed;
            _amount = amount;
        }
    }
}
