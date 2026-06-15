using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class StageNodeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text       _stageLabel;
        [SerializeField] private GameObject[]   _starFills;    // 3 star fill images
        [SerializeField] private Button         _button;
        [SerializeField] private Image          _border;
        [SerializeField] private Image          _difficultyOutline; // neon border (Normal=blue, Hard=red)
        [SerializeField] private GameObject     _skullIcon;         // Hard only badge
        [SerializeField] private GameObject     _lockOverlay;

        private static readonly Dictionary<int, Sprite> _chapterSpriteCache = new();
        private static readonly Color _lockedTint = new Color(0.75f, 0.75f, 0.75f, 1f);

        public event Action<int> OnTapped;

        private int       _stageId;
        private int       _difficulty;
        private Color     _baseColor;
        private Coroutine _outlinePulse;

        private void Awake()
        {
            _button.onClick.AddListener(() => OnTapped?.Invoke(_stageId));
        }

        public void Bind(int stageId, int stars, bool unlocked, bool isCurrent, int chapterId = 0, int difficulty = 0)
        {
            _stageId    = stageId;
            _difficulty = difficulty;

            if (_stageLabel != null) _stageLabel.text = stageId.ToString();
            if (_lockOverlay != null) _lockOverlay.SetActive(!unlocked);

            for (int i = 0; i < _starFills.Length; i++)
                if (_starFills[i] != null) _starFills[i].SetActive(i < stars);

            if (_border != null && chapterId > 0)
            {
                if (!_chapterSpriteCache.TryGetValue(chapterId, out var spr))
                {
                    spr = Resources.Load<Sprite>($"Sprites/StageNodes/stage_node_ch_{chapterId}");
                    _chapterSpriteCache[chapterId] = spr;
                }
                if (spr != null) _border.sprite = spr;
                _border.color = unlocked ? Color.white : _lockedTint;
            }

            if (_outlinePulse != null) { StopCoroutine(_outlinePulse); _outlinePulse = null; }
            if (_difficultyOutline != null)
            {
                bool show = difficulty > 0;
                _difficultyOutline.gameObject.SetActive(show);
                if (show)
                {
                    _baseColor = DifficultyStyle.Get(difficulty);
                    _difficultyOutline.color = _baseColor;
                    if (gameObject.activeInHierarchy)
                        _outlinePulse = StartCoroutine(PulseOutline(_baseColor));
                }
            }
            if (_skullIcon != null) _skullIcon.SetActive(difficulty == 2);
        }

        private void OnEnable()
        {
            if (_difficulty > 0 && _outlinePulse == null)
                _outlinePulse = StartCoroutine(PulseOutline(_baseColor));
        }

        private void OnDisable()
        {
            if (_outlinePulse != null) { StopCoroutine(_outlinePulse); _outlinePulse = null; }
        }

        private IEnumerator PulseOutline(Color baseColor)
        {
            var brightColor = Color.Lerp(baseColor, Color.white, 0.4f);
            while (true)
            {
                float t = (Mathf.Sin(Time.time * 1.8f) + 1f) * 0.5f;
                _difficultyOutline.color = Color.Lerp(baseColor, brightColor, t);
                yield return null;
            }
        }
    }
}
