using System;
using UnityEngine;

namespace Game.Core
{
    public class GoogleSignInBridge : MonoBehaviour
    {
        public static GoogleSignInBridge Instance { get; private set; }

        Action<string, string> _pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
#if UNITY_ANDROID
            if (Instance != null) return;
            new GameObject("GoogleSignInBridge").AddComponent<GoogleSignInBridge>();
#endif
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.name = "GoogleSignInBridge";
        }

#if UNITY_ANDROID
        public void SignIn(string webClientId, Action<string, string> onComplete)
        {
            _pending = onComplete;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var plugin = new AndroidJavaClass("com.madalang.pixelpop.GoogleSignInPlugin"))
            {
                plugin.CallStatic("signIn", activity, webClientId);
            }
        }
#endif

        public void SignOut(string webClientId)
        {
#if UNITY_ANDROID
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var plugin = new AndroidJavaClass("com.madalang.pixelpop.GoogleSignInPlugin"))
            {
                plugin.CallStatic("signOut", activity, webClientId);
            }
#endif
        }

        // Called by UnitySendMessage from Java
        void OnSignInSuccess(string idToken)
        {
            var cb = _pending;
            _pending = null;
            cb?.Invoke(idToken, null);
        }

        // Called by UnitySendMessage from Java
        void OnSignInFailed(string errorCode)
        {
            var cb = _pending;
            _pending = null;
            cb?.Invoke(null, errorCode);
        }
    }
}
