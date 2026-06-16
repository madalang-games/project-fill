using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Attendance;
using ProjectFill.Contracts.Attendance;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController : ControllerBaseEx
{
    private readonly AttendanceService _attendance;

    public AttendanceController(AttendanceService attendance)
    {
        _attendance = attendance;
    }

    [HttpGet("status")]
    public Task<AttendanceStatusResponse> Status(CancellationToken ct)
        => _attendance.GetStatusAsync(PlayerId, ct);

    [HttpPost("claim")]
    public Task<AttendanceClaimResponse> Claim(CancellationToken ct)
        => _attendance.ClaimAsync(PlayerId, CorrelationId, ct);
}
