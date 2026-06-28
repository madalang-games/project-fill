using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Attendance;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class AttendanceApiService : MonoBehaviour
    {
        public static AttendanceApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchStatus(Action<AttendanceStatusResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/attendance/status", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<AttendanceStatusResponseJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void Claim(Action<AttendanceClaimResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Post("/api/attendance/claim", "{}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<AttendanceClaimResponseJson>(result);
                var response = json.ToContract();
                if (json?.currency != null) // raw field is null when server omits currency; central guard skips empty 0/0
                    CurrencyApiService.Instance?.UpdateGold(response.Currency);
                onSuccess?.Invoke(response);
            });
        }

        [Serializable]
        private class AttendanceDayJson
        {
            public int day;
            public int rewardGroupId;
            public bool isClaimed;
            public bool isToday;

            public AttendanceDayDto ToContract() => new AttendanceDayDto
            {
                Day = day,
                RewardGroupId = rewardGroupId,
                IsClaimed = isClaimed,
                IsToday = isToday,
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
        private class AttendanceStatusResponseJson
        {
            public int currentDay;
            public int currentCycle;
            public int currentStreak;
            public int bestStreak;
            public int totalAttendedDays;
            public bool claimedToday;
            public List<AttendanceDayJson> days;

            public AttendanceStatusResponse ToContract()
            {
                var response = new AttendanceStatusResponse
                {
                    CurrentDay = currentDay,
                    CurrentCycle = currentCycle,
                    CurrentStreak = currentStreak,
                    BestStreak = bestStreak,
                    TotalAttendedDays = totalAttendedDays,
                    ClaimedToday = claimedToday,
                };
                if (days != null)
                    foreach (var d in days)
                        if (d != null) response.Days.Add(d.ToContract());
                return response;
            }
        }

        [Serializable]
        private class AttendanceClaimResponseJson
        {
            public int day;
            public int cycle;
            public int streak;
            public int totalAttendedDays;
            public List<GrantedRewardJson> grantedRewards;
            public int milestoneRewardGroupId;
            public List<GrantedRewardJson> milestoneRewards;
            public List<string> unlockedCosmetics;
            public CurrencySnapshotJson currency;

            public AttendanceClaimResponse ToContract()
            {
                var response = new AttendanceClaimResponse
                {
                    Day = day,
                    Cycle = cycle,
                    Streak = streak,
                    TotalAttendedDays = totalAttendedDays,
                    MilestoneRewardGroupId = milestoneRewardGroupId,
                    Currency = currency?.ToContract() ?? new ProjectFill.Contracts.Currency.CurrencySnapshot(),
                };
                if (grantedRewards != null)
                    foreach (var r in grantedRewards)
                        if (r != null) response.GrantedRewards.Add(r.ToContract());
                if (milestoneRewards != null)
                    foreach (var r in milestoneRewards)
                        if (r != null) response.MilestoneRewards.Add(r.ToContract());
                if (unlockedCosmetics != null)
                    response.UnlockedCosmetics.AddRange(unlockedCosmetics);
                return response;
            }
        }
    }
}
#pragma warning restore 0649
