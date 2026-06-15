using System;
using ProjectFill.Contracts.Ad;
using UnityEngine;

namespace Game.Services
{
    public class AdApiService : MonoBehaviour
    {
        private static AdApiService _instance;

        public static AdApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AdApiService");
                    _instance = go.AddComponent<AdApiService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RecordInterstitialShown(int stageId, Action onSuccess = null, Action<string> onError = null)
        {
            var req = new AdInterstitialShownRequest { StageId = stageId };
            var body = JsonUtility.ToJson(req);

            NetworkService.Instance.Post("/api/ad/interstitial/shown", body, (ok, result) =>
            {
                if (ok) onSuccess?.Invoke();
                else onError?.Invoke(result);
            });
        }

        // Stub: DOUBLE_REWARD_STAGE_CLEAR removed — not in Signal Sort design.
        public void ClaimDoubleReward(int stageId, string attemptId, string provider, string adToken, Action<AdDoubleRewardGrantResponse> onSuccess, Action<string> onError)
            => onError?.Invoke("DOUBLE_REWARD_NOT_SUPPORTED");
    }
}
