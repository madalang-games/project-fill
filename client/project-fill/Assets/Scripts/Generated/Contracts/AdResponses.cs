#nullable enable
using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.Rewards;

namespace ProjectFill.Contracts.Ad
{
    public sealed class AdPlacementStatus
    {
        public string PlacementId { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public int CooldownRemainingSeconds { get; set; }
    }

    public sealed class AdEligibilityResponse
    {
        public List<AdPlacementStatus> Placements { get; set; } = new List<AdPlacementStatus>();
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class AdDoubleRewardGrantResponse
    {
        public bool Granted { get; set; }
        public bool Duplicate { get; set; }
        public bool InterstitialSuppressed { get; set; }
        public List<GrantedRewardDto> Rewards { get; set; } = new List<GrantedRewardDto>();
        public CurrencySnapshot? Currency { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }

    public sealed class AdInterstitialShownResponse
    {
        public DateTimeOffset ServerTime { get; set; }
    }
}
