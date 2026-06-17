#nullable enable
using System;
using System.Collections.Generic;

namespace ProjectFill.Contracts.Stage
{
    public sealed class StageStartResponse
    {
        public int StageId { get; set; }
        public int MaxClearedStageId { get; set; }
        public int RulesetVersion { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class StageClearResponse
    {
        public int StageId { get; set; }
        public int MovesUsed { get; set; }
        public int BestMovesUsed { get; set; }
        public int StageRank { get; set; }
        public bool IsNewBest { get; set; }
        public bool IsFirstClear { get; set; }
        public bool ChapterCompleted { get; set; }
        public int ChapterChestRewardGroupId { get; set; }
        public int TotalClearedStages { get; set; }
        public int MaxClearedStageId { get; set; }
        public int WeeklyClearedCount { get; set; }
        public List<ProjectFill.Contracts.Rewards.GrantedRewardDto> GrantedRewards { get; set; } = new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>();
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public DateTimeOffset ServerTime { get; set; }
    }
}
