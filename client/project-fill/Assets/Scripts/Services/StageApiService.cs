using System;
using System.Collections.Generic;
using System.Text;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Contracts.Stage;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    // Signal Sort campaign stage-clear submission. The server is authoritative for best-moves,
    // ranking, first-clear / chapter-chest rewards, and achievement progress — the client only
    // reports the result and applies the returned snapshot.
    public class StageApiService : MonoBehaviour
    {
        private const int CurrentRulesetVersion = 1; // mirrors server StageService.CurrentRulesetVersion

        private static StageApiService _instance;
        public static StageApiService Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // completedSignalTypes must equal the stage's {0..types-1} set (server-validated).
        public void ClearStage(int stageId, int movesUsed, IReadOnlyList<int> completedSignalTypes,
            Action<StageClearResponse> onSuccess = null, Action<string> onError = null)
        {
            var sb = new StringBuilder();
            sb.Append("{\"rulesetVersion\":").Append(CurrentRulesetVersion)
              .Append(",\"movesUsed\":").Append(movesUsed)
              .Append(",\"completedSignalTypes\":[");
            for (int i = 0; i < completedSignalTypes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(completedSignalTypes[i]);
            }
            sb.Append("]}");

            NetworkService.Instance.Post($"/api/stages/{stageId}/clear", sb.ToString(), (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var response = JsonUtility.FromJson<StageClearResponseJson>(result).ToContract();
                if (response?.Currency != null)
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
            public long softDelta;
            public ProjectFill.Contracts.Currency.CurrencySnapshot ToContract() =>
                new ProjectFill.Contracts.Currency.CurrencySnapshot { SoftAmount = softAmount, SoftDelta = softDelta };
        }

        [Serializable]
        private class StageClearResponseJson
        {
            public int stageId;
            public int movesUsed;
            public int bestMovesUsed;
            public int stageRank;
            public bool isNewBest;
            public bool isFirstClear;
            public bool chapterCompleted;
            public int chapterChestRewardGroupId;
            public int totalClearedStages;
            public int maxClearedStageId;
            public int weeklyClearedCount;
            public List<GrantedRewardJson> grantedRewards;
            public CurrencySnapshotJson currency;

            public StageClearResponse ToContract()
            {
                var r = new StageClearResponse
                {
                    StageId = stageId,
                    MovesUsed = movesUsed,
                    BestMovesUsed = bestMovesUsed,
                    StageRank = stageRank,
                    IsNewBest = isNewBest,
                    IsFirstClear = isFirstClear,
                    ChapterCompleted = chapterCompleted,
                    ChapterChestRewardGroupId = chapterChestRewardGroupId,
                    TotalClearedStages = totalClearedStages,
                    MaxClearedStageId = maxClearedStageId,
                    WeeklyClearedCount = weeklyClearedCount,
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
