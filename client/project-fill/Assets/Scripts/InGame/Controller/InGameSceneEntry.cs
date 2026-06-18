using Game.Core;
using Game.Core.UI;
using Game.InGame.View;
using Game.OutGame.Lobby;
using Game.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.InGame.Controller
{
    public class InGameSceneEntry : MonoBehaviour
    {
        [SerializeField] private InGameController          _controller;
        [SerializeField] private InGameSceneBackgroundView _sceneBg; // kept for scene compatibility
        [SerializeField] private int                       _startStageIndex; // index into StageLibrary.Samples

        private const string LobbyScene = "Lobby";

        private void Start()
        {
            Screen.orientation          = ScreenOrientation.Portrait;
            Application.targetFrameRate  = 60;

            if (_controller == null)
            {
                Debug.LogError("[InGame] InGameController reference missing on SceneEntry");
                return;
            }

            // Campaign stage selected in the lobby (StageInfoPopupView PLAY → EnterStage) lands here via
            // ScrollStateCache; index = stageId - 1. Falls back to _startStageIndex for direct in-editor play.
            int stageId = ScrollStateCache.LastPlayedStageId;
            int index   = stageId > 0 ? stageId - 1 : _startStageIndex;

            var api = StageApiService.Instance;
            if (api == null)
            {
                // Offline/dev play (no Boot scene → no StageApiService) → start locally.
                _controller.Begin(index);
                return;
            }

            // Server-authoritative gate: only build the board once the server confirms the stage is reachable.
            api.StartStage(index + 1,
                onSuccess: res =>
                {
                    PlayerProgressService.Instance?.ApplyMaxClearedStage(res.MaxClearedStageId);
                    _controller.Begin(index, res.SessionId);
                },
                onError: OnStartFailed);
        }

        // Stage not reachable (STAGE_LOCKED) or server error: surface a toast and bounce back to the lobby.
        private static void OnStartFailed(string errorResponse)
        {
            var loc = LocalizationService.Instance;
            UIManager.Instance?.ShowToast(loc != null ? loc.GetErrorFromResponse(errorResponse) : errorResponse, ToastType.Warning);

            var transition = SceneTransition.Instance;
            if (transition != null) transition.SlideDownToScene(LobbyScene);
            else SceneManager.LoadScene(LobbyScene);
        }
    }
}
