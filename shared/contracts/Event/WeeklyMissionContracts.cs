#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Event
{
    public sealed class WeeklyMissionResponse
    {
        public string WeekStartDate { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public int TotalEp { get; set; }
        public List<WeeklyMissionDto> Missions { get; set; } = new List<WeeklyMissionDto>();
        public List<WeeklyMissionMilestoneDto> Track { get; set; } = new List<WeeklyMissionMilestoneDto>();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class WeeklyMissionDto
    {
        public string MissionId { get; set; } = string.Empty;
        public int ConditionType { get; set; }
        public string NameKey { get; set; } = string.Empty;
        public string DescKey { get; set; } = string.Empty;
        public int TargetValue { get; set; }
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public int EpReward { get; set; }
    }

    public sealed class WeeklyMissionMilestoneDto
    {
        public int EpThreshold { get; set; }
        public int RewardGroupId { get; set; }
        public bool IsReached { get; set; }
        public bool IsClaimed { get; set; }
    }

    public sealed class ClaimWeeklyMissionResponse
    {
        public int EpThreshold { get; set; }
        public List<ProjectFill.Contracts.Rewards.GrantedRewardDto> GrantedRewards { get; set; } = new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>();
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }
}
