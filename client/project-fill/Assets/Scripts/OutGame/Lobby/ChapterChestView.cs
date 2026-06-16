using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class ChapterChestView : MonoBehaviour
    {
        public enum ChestState
        {
            Inactive,
            Active,
            Claimed
        }

        [SerializeField] private Image _chestImage;
        [SerializeField] private Button _button;

        [Header("Visual States")]
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _claimedSprite;

        [Header("Effects")]
        [SerializeField] private GameObject _glowEffect;
        [SerializeField] private ParticleSystem _sparkleParticles;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Cleared Count")]
        [SerializeField] private TMP_Text _clearedCountLabel;

        private ChestState _state;
        public ChestState State => _state;

        public void SetState(ChestState state)
        {
            _state = state;

            if (_chestImage != null)
            {
                _chestImage.sprite = state switch
                {
                    ChestState.Inactive => _inactiveSprite,
                    ChestState.Active => _activeSprite,
                    ChestState.Claimed => _claimedSprite,
                    _ => _inactiveSprite
                };
            }

            if (_button != null)
                _button.interactable = true;

            if (_glowEffect != null)
                _glowEffect.SetActive(state == ChestState.Active);

            if (_sparkleParticles != null)
            {
                if (state == ChestState.Active)
                    _sparkleParticles.Play();
                else
                {
                    _sparkleParticles.Stop();
                    _sparkleParticles.Clear();
                }
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1.0f;
                // interactable stays true to prevent Button's disabled-color visual tint
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = (state != ChestState.Claimed);
            }
        }

        // current/max = cleared stages / total stages in the chapter (milestone progress).
        public void SetClearedInfo(int current, int max)
        {
            if (_clearedCountLabel != null)
                _clearedCountLabel.text = $"{current}/{max}";
        }
    }
}
