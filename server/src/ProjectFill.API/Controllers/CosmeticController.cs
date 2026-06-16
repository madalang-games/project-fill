using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Contracts.Cosmetic;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/cosmetics")]
public sealed class CosmeticController : ControllerBaseEx
{
    private readonly CosmeticService _cosmetics;

    public CosmeticController(CosmeticService cosmetics)
    {
        _cosmetics = cosmetics;
    }

    [HttpGet]
    public Task<CosmeticListResponse> Get(CancellationToken ct)
        => _cosmetics.GetListAsync(PlayerId, ct);

    [HttpPost("{id}/unlock")]
    public async Task<UnlockCosmeticResponse> Unlock(string id, CancellationToken ct)
    {
        var (cosmeticId, currency) = await _cosmetics.UnlockWithGoldAsync(PlayerId, id, CorrelationId, ct);
        return new UnlockCosmeticResponse
        {
            CosmeticId = cosmeticId,
            Currency = currency,
            ServerTime = System.DateTimeOffset.UtcNow,
        };
    }

    [HttpPost("active")]
    public async Task<SetActiveCosmeticResponse> SetActive([FromBody] SetActiveCosmeticRequest request, CancellationToken ct)
    {
        var active = await _cosmetics.SetActiveAsync(PlayerId, request, CorrelationId, ct);
        return new SetActiveCosmeticResponse
        {
            Active = active,
            ServerTime = System.DateTimeOffset.UtcNow,
        };
    }
}
