#nullable enable
using System.Collections.Generic;
using ProjectFill.Contracts.Rewards;

namespace ProjectFill.Contracts.Iap
{
    public sealed class VerifyIapRequest
    {
        public int InfoId { get; set; }
        public string StoreProductId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string PurchaseToken { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "google", "apple", "mock"
        public string RawReceipt { get; set; } = string.Empty;
    }

    public sealed class VerifyIapResponse
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public bool IsNoAds { get; set; }
        public long CurrentGold { get; set; }
        /// <summary>-1 = unlimited, 0+ = remaining purchase count</summary>
        public int RemainingPurchases { get; set; } = -1;
        public List<GrantedRewardDto> GrantedRewards { get; set; } = new List<GrantedRewardDto>();
    }

    public sealed class IapProductStatusDto
    {
        public int InfoId { get; set; }
        public string StoreProductId { get; set; } = string.Empty;
        /// <summary>-1 = unlimited, 0+ = remaining purchase count</summary>
        public int RemainingPurchases { get; set; } = -1;
    }

    public sealed class GetIapProductsResponse
    {
        public List<IapProductStatusDto> Products { get; set; } = new List<IapProductStatusDto>();
    }
}
