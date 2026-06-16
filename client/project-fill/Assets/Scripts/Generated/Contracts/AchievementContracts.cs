#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Achievement
{
    public sealed class AchievementDto
    {
        public string AchievementId { get; set; } = string.Empty;
        public int Category { get; set; }
        public string NameKey { get; set; } = string.Empty;
        public string DescKey { get; set; } = string.Empty;
        public int Tier { get; set; }
        public int ConditionType { get; set; }
        public int ConditionValue { get; set; }
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
        public bool RewardClaimed { get; set; }
    }

    public sealed class AchievementListResponse
    {
        public List<AchievementDto> Achievements { get; set; } = new List<AchievementDto>();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class ClaimAchievementResponse
    {
        public string AchievementId { get; set; } = string.Empty;
        public List<ProjectFill.Contracts.Rewards.GrantedRewardDto> GrantedRewards { get; set; } = new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>();
        public List<string> UnlockedCosmetics { get; set; } = new List<string>();
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }
}
