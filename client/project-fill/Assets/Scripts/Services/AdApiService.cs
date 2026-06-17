using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Ad;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

#pragma warning disable 0649
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

        // Result-screen 2x reward: submits the watched rewarded-ad token for the stage's clear reward
        // to be re-granted server-side (verified + idempotent). Syncs gold on success.
        public void ClaimDoubleReward(int stageId, string attemptId, string provider, string adToken,
            Action<AdDoubleRewardGrantResponse> onSuccess, Action<string> onError)
        {
            string body =
                "{\"provider\":\"" + Escape(provider) + "\"," +
                "\"adToken\":\"" + Escape(adToken) + "\"," +
                "\"stageId\":" + stageId + "," +
                "\"attemptId\":\"" + Escape(attemptId) + "\"}";

            NetworkService.Instance.Post("/api/ad/double-reward", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var response = JsonUtility.FromJson<AdDoubleRewardGrantJson>(result).ToContract();
                if (response?.Currency != null)
                    CurrencyApiService.Instance?.UpdateGold(response.Currency);
                onSuccess?.Invoke(response);
            });
        }

        private static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class GrantedRewardJson
        {
            public string rewardType;
            public int targetId;
            public int amount;
            public int durationSeconds;

            public GrantedRewardDto ToContract() => new GrantedRewardDto
            {
                RewardType = rewardType, TargetId = targetId, Amount = amount, DurationSeconds = durationSeconds,
            };
        }

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;
            public long softDelta;
            public CurrencySnapshot ToContract() => new CurrencySnapshot { SoftAmount = softAmount, SoftDelta = softDelta };
        }

        [Serializable]
        private class AdDoubleRewardGrantJson
        {
            public bool granted;
            public bool duplicate;
            public List<GrantedRewardJson> rewards;
            public CurrencySnapshotJson currency;

            public AdDoubleRewardGrantResponse ToContract()
            {
                var r = new AdDoubleRewardGrantResponse { Granted = granted, Duplicate = duplicate };
                if (rewards != null)
                    foreach (var g in rewards)
                        if (g != null) r.Rewards.Add(g.ToContract());
                r.Currency = currency?.ToContract();
                return r;
            }
        }
    }
}
#pragma warning restore 0649
