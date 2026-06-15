using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Inventory;
using ProjectFill.Contracts.Inventory;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBaseEx
{
    private readonly InventoryService _inventory;

    public InventoryController(InventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet]
    public Task<InventorySnapshot> Get(CancellationToken ct)
        => _inventory.GetInventoryAsync(PlayerId, ct);

    [HttpPost("spend")]
    public Task<InventorySnapshot> Spend([FromBody] SpendItemRequest request, CancellationToken ct)
        => _inventory.SpendItemAsync(PlayerId, request.ItemId, request.Amount, request.Reason, CorrelationId, ct);

    [HttpPost("buy")]
    public async Task<BuyItemResponse> Buy([FromBody] BuyItemRequest request, CancellationToken ct)
    {
        var (inventory, currency) = await _inventory.BuyItemAsync(PlayerId, request.ItemId, CorrelationId, ct);
        return new BuyItemResponse
        {
            Inventory = inventory,
            Currency = currency,
            ServerTime = System.DateTimeOffset.UtcNow
        };
    }
}
