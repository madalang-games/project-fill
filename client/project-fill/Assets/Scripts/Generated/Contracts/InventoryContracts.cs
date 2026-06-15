#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Inventory
{
    public sealed class InventoryItemDto
    {
        public int ItemId { get; set; }
        public int Count { get; set; }
    }

    public sealed class InventorySnapshot
    {
        public List<InventoryItemDto> Items { get; set; } = new List<InventoryItemDto>();
    }

    public sealed class SpendItemRequest
    {
        public int ItemId { get; set; }
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
    public sealed class SpendItemResponse
    {
        public InventorySnapshot Inventory { get; set; } = new InventorySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class BuyItemRequest
    {
        public int ItemId { get; set; }
    }

    public sealed class BuyItemResponse
    {
        public InventorySnapshot Inventory { get; set; } = new InventorySnapshot();
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }
}
