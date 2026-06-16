#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Cosmetic
{
    public sealed class CosmeticItemDto
    {
        public string CosmeticId { get; set; } = string.Empty;
        public int Category { get; set; }
        public string NameKey { get; set; } = string.Empty;
        public string DescKey { get; set; } = string.Empty;
        public int UnlockType { get; set; }
        public int UnlockCost { get; set; }
        public string UnlockConditionId { get; set; } = string.Empty;
        public string PreviewRes { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool Unlocked { get; set; }
    }

    public sealed class ActiveCosmeticsDto
    {
        public string ChipSkin { get; set; } = string.Empty;
        public string LaneSkin { get; set; } = string.Empty;
        public string BoardSkin { get; set; } = string.Empty;
        public bool UseCustomBoardSkin { get; set; }
    }

    public sealed class CosmeticListResponse
    {
        public List<CosmeticItemDto> Items { get; set; } = new List<CosmeticItemDto>();
        public ActiveCosmeticsDto Active { get; set; } = new ActiveCosmeticsDto();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class UnlockCosmeticResponse
    {
        public string CosmeticId { get; set; } = string.Empty;
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class SetActiveCosmeticRequest
    {
        public string ChipSkin { get; set; } = string.Empty;
        public string LaneSkin { get; set; } = string.Empty;
        public string BoardSkin { get; set; } = string.Empty;
        public bool UseCustomBoardSkin { get; set; }
    }

    public sealed class SetActiveCosmeticResponse
    {
        public ActiveCosmeticsDto Active { get; set; } = new ActiveCosmeticsDto();
        public System.DateTimeOffset ServerTime { get; set; }
    }
}
