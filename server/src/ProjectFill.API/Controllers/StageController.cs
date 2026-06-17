using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Stage;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/stages")]
public sealed class StageController : ControllerBaseEx
{
    private readonly StageService _stage;

    public StageController(StageService stage)
    {
        _stage = stage;
    }

    [HttpPost("{stageId:int}/start")]
    public Task<StageStartResponse> Start(int stageId, CancellationToken ct)
        => _stage.StartStageAsync(PlayerId, stageId, ct);

    [HttpPost("{stageId:int}/clear")]
    public Task<StageClearResponse> Clear(int stageId, [FromBody] StageClearRequest request, CancellationToken ct)
        => _stage.ClearStageAsync(PlayerId, stageId, request, CorrelationId, ct);
}
