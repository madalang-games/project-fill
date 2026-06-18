using System;
using System.Collections;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class StageNodeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _stageLabel;
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _node; // ui_stage_node sprite; chapter-tinted, or static power-off when locked
        [SerializeField] private Image    _glow; // difficulty aura behind node (Normal/Hard only); gently color-pulsed

        // Circuit "power cut" look for locked stages: desaturated cold slate.
        private static readonly Color _lockedColor = new Color(0.20f, 0.23f, 0.30f, 1f);

        private const float GlowPulseAlpha  = 0.60f; // unlocked: steady alpha, color shifts
        private const float GlowStaticAlpha = 0.30f; // locked: steady, no animation
        private const float PulseBrighten   = 0.55f; // base → white blend at pulse peak
        private const float PulseSpeed      = 2.0f;
        private const float NodeTintWeight  = 0.55f; // white → chapter PathColor

        public event Action<int> OnTapped;

        private int       _stageId;
        private bool      _unlocked;
        private Color     _glowColor;
        private Coroutine _glowPulse;

        private void Awake()
        {
            _button.onClick.AddListener(() => OnTapped?.Invoke(_stageId));
        }

        public void Bind(int stageId, bool unlocked, int chapterId, int difficulty)
        {
            _stageId  = stageId;
            _unlocked = unlocked;
            if (_stageLabel != null) _stageLabel.text = stageId.ToString();

            StopPulse();

            // Node tint: unlocked → chapter-themed (bright, pops on the dark gradient); locked → power-off slate.
            if (_node != null)
                _node.color = unlocked
                    ? Color.Lerp(Color.white, ChapterBgTheme.Get(chapterId).PathColor, NodeTintWeight)
                    : _lockedColor;

            // Difficulty glow (Easy=0 → none) shown in BOTH states; unlocked breathes (dynamic),
            // locked is steady (static).
            bool hasGlow = difficulty > 0;
            if (_glow != null)
            {
                _glow.gameObject.SetActive(hasGlow);
                if (hasGlow)
                {
                    _glowColor = DifficultyStyle.Get(difficulty); // 1=teal, 2=crimson
                    if (unlocked)
                    {
                        _glow.color = WithAlpha(_glowColor, GlowPulseAlpha);
                        if (gameObject.activeInHierarchy)
                            _glowPulse = StartCoroutine(PulseGlow());
                    }
                    else
                    {
                        _glow.color = WithAlpha(_glowColor, GlowStaticAlpha);
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (_unlocked && _glow != null && _glow.gameObject.activeSelf && _glowPulse == null)
                _glowPulse = StartCoroutine(PulseGlow());
        }

        private void OnDisable() => StopPulse();

        private void StopPulse()
        {
            if (_glowPulse != null) { StopCoroutine(_glowPulse); _glowPulse = null; }
        }

        // Color pulse — alpha stays fixed; the hue shifts base → brightened and back for a clearly
        // dynamic feel (alpha breathing alone read as static).
        private IEnumerator PulseGlow()
        {
            var bright = Color.Lerp(_glowColor, Color.white, PulseBrighten);
            while (true)
            {
                float t = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
                var c = Color.Lerp(_glowColor, bright, t);
                _glow.color = WithAlpha(c, GlowPulseAlpha);
                yield return null;
            }
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
