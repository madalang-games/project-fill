using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Boot
{
    public class ForceUpdateView : MonoBehaviour
    {
        [SerializeField] private Button _updateButton;

        public void Init()
        {
            if (_updateButton == null) { Debug.LogError("[ForceUpdateView] _updateButton is not assigned — wire it in the prefab"); return; }
            _updateButton.onClick.AddListener(OpenStore);
        }

        private static void OpenStore()
        {
#if UNITY_ANDROID
            Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IOS
            Application.OpenURL(AppConfig.AppStoreUrl);
#else
            Application.OpenURL(AppConfig.GooglePlayStoreUrl);
#endif
        }
    }
}
