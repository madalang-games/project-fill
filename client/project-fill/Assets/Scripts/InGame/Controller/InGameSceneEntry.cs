using Game.InGame.View;
using UnityEngine;

namespace Game.InGame.Controller
{
    public class InGameSceneEntry : MonoBehaviour
    {
        [SerializeField] private InGameController          _controller;
        [SerializeField] private InGameSceneBackgroundView _sceneBg; // kept for scene compatibility
        [SerializeField] private int                       _startStageIndex; // index into StageLibrary.Samples

        private void Start()
        {
            Screen.orientation          = ScreenOrientation.Portrait;
            Application.targetFrameRate  = 60;

            if (_controller == null)
            {
                Debug.LogError("[InGame] InGameController reference missing on SceneEntry");
                return;
            }
            _controller.Begin(_startStageIndex);
        }
    }
}
