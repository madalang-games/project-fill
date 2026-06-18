using Game.Core;
using Game.Core.UI;
using Game.Services;
using UnityEngine;
using static Game.Services.BootstrapService;

namespace Game.OutGame.Boot
{
    public class BootSceneEntry : MonoBehaviour
    {
        [SerializeField] private BootView            _bootView;
        [SerializeField] private SceneBackgroundView _sceneBackground;

        private const string LobbyScene = "Lobby";
        private const string BootScene  = "Boot";

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            _sceneBackground?.Bind(0, BackgroundMode.Default);
        }

        private void Start()
        {
            Debug.Log("[Boot] Start");

            if (UIManager.Instance == null)
                Debug.LogError("[Boot] UIManager.Instance is null — add UIManager GameObject to Boot scene");

            if (SceneTransition.Instance == null)
                Debug.LogWarning("[Boot] SceneTransition.Instance is null — add SceneTransition GameObject to Boot scene");

            if (AuthService.Instance == null)
            {
                Debug.LogError("[Boot] AuthService.Instance is null — add AuthService GameObject to Boot scene");
                return;
            }

            UIManager.Instance?.ShowLoading();

            if (BootstrapService.Instance != null)
                BootstrapService.Instance.Initialize(OnBootstrapResult);
            else
                AuthService.Instance.Initialize(OnAuthResult);
        }

        private void OnBootstrapResult(BootstrapResult result)
        {
            switch (result)
            {
                case BootstrapResult.ForceUpdate:
                    UIManager.Instance?.HideLoading();
                    UIManager.Instance?.ShowPopup<ForceUpdateView>(v => v?.Init());
                    break;
                case BootstrapResult.PatchFailed:
                    UIManager.Instance?.HideLoading();
                    UIManager.Instance?.ShowNetworkError(RetryFromBoot);
                    break;
                case BootstrapResult.OK:
                    AuthService.Instance.Initialize(OnAuthResult);
                    break;
            }
        }

        private void OnAuthResult(AuthResult result)
        {
            Debug.Log($"[Boot] AuthResult = {result}");

            switch (result)
            {
                case AuthResult.Authenticated:
                case AuthResult.Guest:
                    FetchProgressThenGoToLobby(showNewGuestToast: false);
                    break;

                case AuthResult.NewGuestCreated:
                    FetchProgressThenGoToLobby(showNewGuestToast: true);
                    break;

                case AuthResult.ReLoginRequired:
                    UIManager.Instance?.HideLoading();
                    ShowReLoginScreen();
                    break;

                case AuthResult.NetworkError:
                    UIManager.Instance?.HideLoading();
                    UIManager.Instance?.ShowNetworkError(RetryFromBoot);
                    break;
            }
        }

        private void FetchProgressThenGoToLobby(bool showNewGuestToast)
        {
            Services.Tutorial.TutorialManager.Instance?.LoadProgress();

            PlayerApiService.Instance.FetchProgress((ok, response) =>
            {
                if (ok && response != null)
                    PlayerProgressService.Instance.LoadFromServer(response);

                UIManager.Instance?.HideLoading();

                if (showNewGuestToast)
                    UIManager.Instance?.ShowToast(
                        LocalizationService.Instance.Get("boot.new_guest_session"),
                        Core.UI.ToastType.Warning);

                GoToLobby();
            });
        }

        private void GoToLobby()
        {
            Debug.Log($"[Boot] GoToLobby → scene='{LobbyScene}'");

#if UNITY_EDITOR
            // Validate scene is in Build Settings
            bool inBuild = false;
            foreach (var s in UnityEditor.EditorBuildSettings.scenes)
                if (s.path.EndsWith($"/{LobbyScene}.unity")) { inBuild = true; break; }
            if (!inBuild)
                Debug.LogError($"[Boot] '{LobbyScene}' not found in Build Settings → File > Build Settings > Add Open Scenes");
#endif

            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeToScene(LobbyScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(LobbyScene);
        }

        private void RetryFromBoot()
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeToScene(BootScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(BootScene);
        }

        private void ShowReLoginScreen()
        {
            UIManager.Instance?.ShowPopup<ReLoginView>(v => v.Init(
                onReLogin: () =>
                {
                    UIManager.Instance?.CloseTopPopup();
                    UIManager.Instance?.ShowLoading();
                    AuthService.Instance.Initialize(OnAuthResult);
                },
                onContinueAsGuest: OnContinueAsGuest));
        }

        private void OnContinueAsGuest()
        {
            UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                title:        LocalizationService.Instance.Get("popup.boot.continue_as_guest_title"),
                body:         LocalizationService.Instance.Get("popup.boot.continue_as_guest_body"),
                confirmLabel: LocalizationService.Instance.Get("common.btn_confirm"),
                onConfirm:    OnContinueAsGuestConfirmed,
                cancelLabel:  LocalizationService.Instance.Get("common.btn_cancel"),
                danger:       false));
        }

        private void OnContinueAsGuestConfirmed()
        {
            UIManager.Instance?.CloseAllPopups();
            UIManager.Instance?.ShowLoading();
            AuthService.Instance.ContinueAsGuest(OnAuthResult);
        }
    }
}
