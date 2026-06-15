using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

namespace Game.Services
{
    public class RewardsApiService : MonoBehaviour
    {
        public static RewardsApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchRewardSources(Action<RewardSourcesResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/rewards/sources", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<RewardSourcesResponseJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void ClaimReward(string sourceId, Action<RewardClaimResponse> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"sourceId\":\"{Escape(sourceId)}\"}}";
            NetworkService.Instance.Post("/api/rewards/claim", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<RewardClaimResponseJson>(result);
                var response = json.ToContract();
                if (json.currency != null && json.currency.softDelta != 0)
                    CurrencyApiService.Instance?.UpdateGold(json.currency.ToContract());
                InventoryApiService.Instance?.FetchInventory();
                onSuccess?.Invoke(response);
            });
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class RewardSourceJson
        {
            public string sourceId;
            public string sourceType;
            public int rewardGroupId;
            public bool claimable;
            public string nextAvailableAt;
            public string uiSurface;

            public RewardSourceDto ToContract() => new RewardSourceDto
            {
                SourceId = sourceId ?? string.Empty,
                SourceType = sourceType ?? string.Empty,
                RewardGroupId = rewardGroupId,
                Claimable = claimable,
                NextAvailableAt = ParseTime(nextAvailableAt),
                UiSurface = uiSurface ?? string.Empty
            };
        }

        [Serializable]
        private class RewardSourcesResponseJson
        {
            public List<RewardSourceJson> sources;
            public string serverTime;

            public RewardSourcesResponse ToContract()
            {
                var response = new RewardSourcesResponse
                {
                    ServerTime = ParseTime(serverTime) ?? DateTimeOffset.UtcNow
                };
                if (sources != null)
                {
                    foreach (var src in sources)
                    {
                        if (src != null) response.Sources.Add(src.ToContract());
                    }
                }
                return response;
            }
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
                RewardType = rewardType ?? string.Empty,
                TargetId = targetId,
                Amount = amount,
                DurationSeconds = durationSeconds
            };
        }


        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;
            public long softDelta;

            public ProjectFill.Contracts.Currency.CurrencySnapshot ToContract()
                => new ProjectFill.Contracts.Currency.CurrencySnapshot { SoftAmount = softAmount, SoftDelta = softDelta };
        }

        [Serializable]
        private class RewardClaimResponseJson
        {
            public string sourceId;
            public List<GrantedRewardJson> grantedRewards;
            public CurrencySnapshotJson currency;
            public string serverTime;

            public RewardClaimResponse ToContract()
            {
                var response = new RewardClaimResponse
                {
                    SourceId = sourceId ?? string.Empty,
                    ServerTime = ParseTime(serverTime) ?? DateTimeOffset.UtcNow,
                };
                if (grantedRewards != null)
                {
                    foreach (var gr in grantedRewards)
                    {
                        if (gr != null) response.GrantedRewards.Add(gr.ToContract());
                    }
                }
                return response;
            }
        }

        private static DateTimeOffset? ParseTime(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : (DateTimeOffset?)null;
        }
    }
}
