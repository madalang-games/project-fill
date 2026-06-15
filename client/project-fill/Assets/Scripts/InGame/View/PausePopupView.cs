using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class PausePopupView : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _stageSelectButton;
    }
}
