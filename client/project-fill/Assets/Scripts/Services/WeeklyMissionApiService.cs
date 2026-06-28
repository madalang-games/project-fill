using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Event;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    // Weekly Mission Event client: GET status (missions + progress + cumulative EP + track) and
    // POST milestone claim. Progress is aggregated server-side in the stage-clear flow — no submit here.
    public class WeeklyMissionApiService : MonoBehaviour
    {
        public static WeeklyMissionApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchStatus(Action<WeeklyMissionResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/events/weekly-mission", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                onSuccess?.Invoke(JsonUtility.FromJson<WeeklyMissionResponseJson>(result).ToContract());
            });
        }

        public void Claim(int threshold, Action<ClaimWeeklyMissionResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Post($"/api/events/weekly-mission/claim/{threshold}", "{}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<ClaimWeeklyMissionResponseJson>(result);
                var response = json.ToContract();
                if (json?.currency != null) // raw field is null when server omits currency; central guard skips empty 0/0
                    CurrencyApiService.Instance?.UpdateGold(response.Currency);
                onSuccess?.Invoke(response);
            });
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
                RewardType = rewardType, TargetId = targetId, Amount = amount, DurationSeconds = durationSeconds,
            };
        }

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;
            public ProjectFill.Contracts.Currency.CurrencySnapshot ToContract() =>
                new ProjectFill.Contracts.Currency.CurrencySnapshot { SoftAmount = softAmount };
        }

        [Serializable]
        private class WeeklyMissionJson
        {
            public string missionId;
            public int conditionType;
            public string nameKey;
            public string descKey;
            public int targetValue;
            public int progress;
            public bool isCompleted;
            public int epReward;

            public WeeklyMissionDto ToContract() => new WeeklyMissionDto
            {
                MissionId = missionId, ConditionType = conditionType, NameKey = nameKey, DescKey = descKey,
                TargetValue = targetValue, Progress = progress, IsCompleted = isCompleted, EpReward = epReward,
            };
        }

        [Serializable]
        private class WeeklyMissionMilestoneJson
        {
            public int epThreshold;
            public int rewardGroupId;
            public bool isReached;
            public bool isClaimed;

            public WeeklyMissionMilestoneDto ToContract() => new WeeklyMissionMilestoneDto
            {
                EpThreshold = epThreshold, RewardGroupId = rewardGroupId, IsReached = isReached, IsClaimed = isClaimed,
            };
        }

        [Serializable]
        private class WeeklyMissionResponseJson
        {
            public string weekStartDate;
            public int daysRemaining;
            public int totalEp;
            public List<WeeklyMissionJson> missions;
            public List<WeeklyMissionMilestoneJson> track;

            public WeeklyMissionResponse ToContract()
            {
                var r = new WeeklyMissionResponse
                {
                    WeekStartDate = weekStartDate, DaysRemaining = daysRemaining, TotalEp = totalEp,
                };
                if (missions != null)
                    foreach (var m in missions)
                        if (m != null) r.Missions.Add(m.ToContract());
                if (track != null)
                    foreach (var t in track)
                        if (t != null) r.Track.Add(t.ToContract());
                return r;
            }
        }

        [Serializable]
        private class ClaimWeeklyMissionResponseJson
        {
            public int epThreshold;
            public List<GrantedRewardJson> grantedRewards;
            public CurrencySnapshotJson currency;

            public ClaimWeeklyMissionResponse ToContract()
            {
                var r = new ClaimWeeklyMissionResponse
                {
                    EpThreshold = epThreshold,
                    Currency = currency?.ToContract() ?? new ProjectFill.Contracts.Currency.CurrencySnapshot(),
                };
                if (grantedRewards != null)
                    foreach (var g in grantedRewards)
                        if (g != null) r.GrantedRewards.Add(g.ToContract());
                return r;
            }
        }
    }
}
#pragma warning restore 0649
