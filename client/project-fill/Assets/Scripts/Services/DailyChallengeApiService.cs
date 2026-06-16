using System;
using System.Collections.Generic;
using ProjectFill.Contracts.DailyChallenge;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class DailyChallengeApiService : MonoBehaviour
    {
        public static DailyChallengeApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchToday(Action<DailyChallengeTodayResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/daily-challenge/today", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                onSuccess?.Invoke(JsonUtility.FromJson<DailyChallengeTodayResponseJson>(result).ToContract());
            });
        }

        public void SubmitClear(int movesUsed, int clearTimeSeconds, Action<SubmitChallengeClearResponse> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"movesUsed\":{movesUsed},\"clearTimeSeconds\":{clearTimeSeconds}}}";
            NetworkService.Instance.Post("/api/daily-challenge/today/clear", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var response = JsonUtility.FromJson<SubmitChallengeClearResponseJson>(result).ToContract();
                if (response?.Currency != null)
                    CurrencyApiService.Instance?.UpdateGold(response.Currency);
                onSuccess?.Invoke(response);
            });
        }

        public void FetchRanking(int page, int pageSize, Action<ChallengeRankingResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get($"/api/daily-challenge/today/ranking?page={page}&pageSize={pageSize}", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                onSuccess?.Invoke(JsonUtility.FromJson<ChallengeRankingResponseJson>(result).ToContract());
            });
        }

        public void FetchStreak(Action<ChallengeStreakResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/daily-challenge/streak", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                onSuccess?.Invoke(JsonUtility.FromJson<ChallengeStreakResponseJson>(result).ToContract());
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
            public ProjectFill.Contracts.Currency.CurrencySnapshot ToContract() => new ProjectFill.Contracts.Currency.CurrencySnapshot { SoftAmount = softAmount };
        }

        [Serializable]
        private class DailyChallengeTodayResponseJson
        {
            public string challengeDate;
            public string stageSeed;
            public int signalTypeCount;
            public int laneCount;
            public int gimmickId;
            public bool played;
            public bool isCleared;
            public int myMovesUsed;
            public int myRank;
            public int currentStreak;
            public int participantCount;

            public DailyChallengeTodayResponse ToContract() => new DailyChallengeTodayResponse
            {
                ChallengeDate = challengeDate,
                StageSeed = stageSeed,
                SignalTypeCount = signalTypeCount,
                LaneCount = laneCount,
                GimmickId = gimmickId,
                Played = played,
                IsCleared = isCleared,
                MyMovesUsed = myMovesUsed,
                MyRank = myRank,
                CurrentStreak = currentStreak,
                ParticipantCount = participantCount,
            };
        }

        [Serializable]
        private class SubmitChallengeClearResponseJson
        {
            public int movesUsed;
            public int rank;
            public int currentStreak;
            public int bestStreak;
            public int streakRewardGroupId;
            public List<GrantedRewardJson> grantedRewards;
            public List<string> unlockedCosmetics;
            public CurrencySnapshotJson currency;

            public SubmitChallengeClearResponse ToContract()
            {
                var r = new SubmitChallengeClearResponse
                {
                    MovesUsed = movesUsed,
                    Rank = rank,
                    CurrentStreak = currentStreak,
                    BestStreak = bestStreak,
                    StreakRewardGroupId = streakRewardGroupId,
                    Currency = currency?.ToContract() ?? new ProjectFill.Contracts.Currency.CurrencySnapshot(),
                };
                if (grantedRewards != null)
                    foreach (var g in grantedRewards)
                        if (g != null) r.GrantedRewards.Add(g.ToContract());
                if (unlockedCosmetics != null) r.UnlockedCosmetics.AddRange(unlockedCosmetics);
                return r;
            }
        }

        [Serializable]
        private class ChallengeRankingEntryJson
        {
            public int rank;
            public long userId;
            public string displayName;
            public int avatarId;
            public int movesUsed;
            public int clearTimeSeconds;
            public bool isMe;

            public ChallengeRankingEntryDto ToContract() => new ChallengeRankingEntryDto
            {
                Rank = rank, UserId = userId, DisplayName = displayName, AvatarId = avatarId,
                MovesUsed = movesUsed, ClearTimeSeconds = clearTimeSeconds, IsMe = isMe,
            };
        }

        [Serializable]
        private class ChallengeRankingResponseJson
        {
            public List<ChallengeRankingEntryJson> entries;
            public int page;
            public int pageSize;
            public int totalCount;

            public ChallengeRankingResponse ToContract()
            {
                var r = new ChallengeRankingResponse { Page = page, PageSize = pageSize, TotalCount = totalCount };
                if (entries != null)
                    foreach (var e in entries)
                        if (e != null) r.Entries.Add(e.ToContract());
                return r;
            }
        }

        [Serializable]
        private class ChallengeStreakResponseJson
        {
            public int currentStreak;
            public int bestStreak;
            public string lastClearDate;

            public ChallengeStreakResponse ToContract() => new ChallengeStreakResponse
            {
                CurrentStreak = currentStreak, BestStreak = bestStreak, LastClearDate = lastClearDate,
            };
        }
    }
}
#pragma warning restore 0649
