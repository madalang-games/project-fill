using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Achievement;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class AchievementApiService : MonoBehaviour
    {
        private static AchievementApiService _instance;

        public static AchievementApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AchievementApiService");
                    _instance = go.AddComponent<AchievementApiService>();
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

        public void FetchList(Action<AchievementListResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/achievements", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<AchievementListResponseJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void Claim(string achievementId, Action<ClaimAchievementResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Post($"/api/achievements/{achievementId}/claim", "{}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<ClaimAchievementResponseJson>(result);
                var response = json.ToContract();
                if (json?.currency != null) // raw field is null when server omits currency; central guard skips empty 0/0
                    CurrencyApiService.Instance?.UpdateGold(response.Currency);
                onSuccess?.Invoke(response);
            });
        }

        [Serializable]
        private class AchievementJson
        {
            public string achievementId;
            public int category;
            public string nameKey;
            public string descKey;
            public int tier;
            public int conditionType;
            public int conditionValue;
            public int progress;
            public bool isCompleted;
            public bool rewardClaimed;

            public AchievementDto ToContract() => new AchievementDto
            {
                AchievementId = achievementId,
                Category = category,
                NameKey = nameKey,
                DescKey = descKey,
                Tier = tier,
                ConditionType = conditionType,
                ConditionValue = conditionValue,
                Progress = progress,
                IsCompleted = isCompleted,
                RewardClaimed = rewardClaimed,
            };
        }

        [Serializable]
        private class GrantedRewardJson
        {
            public string rewardType;
            public int targetId;
            public int amount;
            public int durationSeconds;

            public GrantedRewardDto ToContract() => new GrantedRewardDto
            {
                RewardType = rewardType,
                TargetId = targetId,
                Amount = amount,
                DurationSeconds = durationSeconds,
            };
        }

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;

            public ProjectFill.Contracts.Currency.CurrencySnapshot ToContract() => new ProjectFill.Contracts.Currency.CurrencySnapshot
            {
                SoftAmount = softAmount
            };
        }

        [Serializable]
        private class AchievementListResponseJson
        {
            public List<AchievementJson> achievements;

            public AchievementListResponse ToContract()
            {
                var response = new AchievementListResponse();
                if (achievements != null)
                    foreach (var a in achievements)
                        if (a != null) response.Achievements.Add(a.ToContract());
                return response;
            }
        }

        [Serializable]
        private class ClaimAchievementResponseJson
        {
            public string achievementId;
            public List<GrantedRewardJson> grantedRewards;
            public List<string> unlockedCosmetics;
            public CurrencySnapshotJson currency;

            public ClaimAchievementResponse ToContract()
            {
                var response = new ClaimAchievementResponse
                {
                    AchievementId = achievementId,
                    Currency = currency?.ToContract() ?? new ProjectFill.Contracts.Currency.CurrencySnapshot(),
                };
                if (grantedRewards != null)
                    foreach (var r in grantedRewards)
                        if (r != null) response.GrantedRewards.Add(r.ToContract());
                if (unlockedCosmetics != null)
                    response.UnlockedCosmetics.AddRange(unlockedCosmetics);
                return response;
            }
        }
    }
}
#pragma warning restore 0649
