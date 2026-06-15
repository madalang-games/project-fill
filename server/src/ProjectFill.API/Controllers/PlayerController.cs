using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Player;
using ProjectFill.Contracts.Player;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/player")]
public sealed class PlayerController : ControllerBaseEx
{
    private readonly PlayerService _player;

    public PlayerController(PlayerService player)
    {
        _player = player;
    }

    [HttpGet("progress")]
    public Task<PlayerProgressResponse> GetProgress(CancellationToken ct)
        => _player.GetProgressAsync(PlayerId, ct);

    [HttpPost("profile")]
    public Task<UserProfileUpdateResponse> UpdateProfile([FromBody] UserProfileUpdateRequest request, CancellationToken ct)
        => _player.UpdateProfileAsync(PlayerId, request, CorrelationId, ct);
}
