#nullable enable
using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Currency;

namespace ProjectFill.Contracts.Rewards
{
    public sealed class RewardSourceDto
    {
        public string SourceId { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public int RewardGroupId { get; set; }
        public bool Claimable { get; set; }
        public DateTimeOffset? NextAvailableAt { get; set; }
        public string UiSurface { get; set; } = string.Empty;
    }

    public sealed class RewardSourcesResponse
    {
        public List<RewardSourceDto> Sources { get; set; } = new List<RewardSourceDto>();
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class GrantedRewardDto
    {
        public string RewardType { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public int Amount { get; set; }
        public int DurationSeconds { get; set; }
    }

    public sealed class RewardClaimResponse
    {
        public string SourceId { get; set; } = string.Empty;
        public List<GrantedRewardDto> GrantedRewards { get; set; } = new List<GrantedRewardDto>();
        public CurrencySnapshot? Currency { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class AdRewardClaimResponse
    {
        public bool Granted { get; set; }
        public bool Duplicate { get; set; }
        public string PlacementId { get; set; } = string.Empty;
        public List<GrantedRewardDto> GrantedRewards { get; set; } = new List<GrantedRewardDto>();
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class AdRewardStatusResponse
    {
        public string Status { get; set; } = string.Empty; // PENDING, GRANTED
        public string PlacementId { get; set; } = string.Empty;
        public List<GrantedRewardDto> GrantedRewards { get; set; } = new List<GrantedRewardDto>();
        public ProjectFill.Contracts.Currency.CurrencySnapshot? Currency { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }
}
