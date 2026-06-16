#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.DailyChallenge
{
    public sealed class DailyChallengeTodayResponse
    {
        public string ChallengeDate { get; set; } = string.Empty;
        public string StageSeed { get; set; } = string.Empty;
        public int SignalTypeCount { get; set; }
        public int LaneCount { get; set; }
        public int GimmickId { get; set; } = -1;
        public bool Played { get; set; }
        public bool IsCleared { get; set; }
        public int MyMovesUsed { get; set; }
        public int MyRank { get; set; }
        public int CurrentStreak { get; set; }
        public int ParticipantCount { get; set; }
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class SubmitChallengeClearRequest
    {
        public int MovesUsed { get; set; }
        public int ClearTimeSeconds { get; set; }
    }

    public sealed class SubmitChallengeClearResponse
    {
        public int MovesUsed { get; set; }
        public int Rank { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int StreakRewardGroupId { get; set; }
        public List<ProjectFill.Contracts.Rewards.GrantedRewardDto> GrantedRewards { get; set; } = new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>();
        public List<string> UnlockedCosmetics { get; set; } = new List<string>();
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class ChallengeRankingEntryDto
    {
        public int Rank { get; set; }
        public long UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int AvatarId { get; set; }
        public int MovesUsed { get; set; }
        public int ClearTimeSeconds { get; set; }
        public bool IsMe { get; set; }
    }

    public sealed class ChallengeRankingResponse
    {
        public List<ChallengeRankingEntryDto> Entries { get; set; } = new List<ChallengeRankingEntryDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class ChallengeStreakResponse
    {
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public string LastClearDate { get; set; } = string.Empty;
        public System.DateTimeOffset ServerTime { get; set; }
    }
}
