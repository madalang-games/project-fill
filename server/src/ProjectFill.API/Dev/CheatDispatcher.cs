using System.Globalization;
using ProjectFill.Application.Cheat;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Cheat;
using ProjectFill.Domain.Enums;

namespace ProjectFill.API.Dev;

// Domain branch: maps a ParsedCheatCommand to the matching CheatService call. Per-domain arity and
// action-token → enum mapping live here; every parse/validation failure is INVALID_COMMAND (400).
public sealed class CheatDispatcher
{
    private readonly CheatService _cheat;

    public CheatDispatcher(CheatService cheat) => _cheat = cheat;

    public async Task<CheatCommandResponse> DispatchAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        var (message, data) = cmd.Domain switch
        {
            CheatDomain.Gold => await GoldAsync(userId, cmd, correlationId, ct),
            CheatDomain.Item => await ItemAsync(userId, cmd, correlationId, ct),
            CheatDomain.Stage => await StageAsync(userId, cmd, correlationId, ct),
            CheatDomain.Tutorial => await TutorialAsync(userId, cmd, correlationId, ct),
            CheatDomain.Ad => await AdAsync(userId, cmd, correlationId, ct),
            CheatDomain.Cosmetic => await CosmeticAsync(userId, cmd, correlationId, ct),
            CheatDomain.Achievement => await AchievementAsync(userId, cmd, correlationId, ct),
            CheatDomain.Attendance => await AttendanceAsync(userId, cmd, correlationId, ct),
            _ => throw new GameApiException(ErrorCodes.InvalidCommand, "Unsupported domain."),
        };

        return new CheatCommandResponse
        {
            Success = true,
            Command = cmd.Raw,
            Message = message,
            Data = data,
        };
    }

    private async Task<(string, object?)> GoldAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 2);
        var action = ParseAdjustAction(cmd.Args[0]);
        var amount = ParseLong(cmd.Args[1]);
        var balanceAfter = await _cheat.GoldAsync(userId, action, amount, cmd.Raw, correlationId, ct);
        return ($"gold {action} {amount} → {balanceAfter}", new { balanceAfter });
    }

    private async Task<(string, object?)> ItemAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 3);
        var target = ParseTarget(cmd.Args[0]);
        var action = ParseAdjustAction(cmd.Args[1]);
        var amount = (int)ParseLong(cmd.Args[2]);
        var inventoryAfter = await _cheat.ItemAsync(userId, target, action, amount, cmd.Raw, correlationId, ct);
        return ($"item {cmd.Args[0]} {action} {amount}", new { inventoryAfter });
    }

    private async Task<(string, object?)> StageAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 2);
        if (ParseAdjustAction(cmd.Args[0]) != CheatAction.Set)
            throw new GameApiException(ErrorCodes.InvalidCommand, "stage only supports 'set'.");
        var stageId = (int)ParseLong(cmd.Args[1]);
        var highestStageAfter = await _cheat.StageAsync(userId, stageId, cmd.Raw, correlationId, ct);
        return ($"stage set {stageId} → {highestStageAfter}", new { highestStageAfter });
    }

    private async Task<(string, object?)> TutorialAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 2);
        var target = ParseTarget(cmd.Args[0]);
        var seen = ParseBool(cmd.Args[1]);
        var seenTutorialIds = await _cheat.TutorialAsync(userId, target, seen, cmd.Raw, correlationId, ct);
        return ($"tutorial {cmd.Args[0]} {seen}", new { seenTutorialIds });
    }

    private async Task<(string, object?)> AdAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 1);
        var bypass = ParseBool(cmd.Args[0]);
        await _cheat.AdAsync(userId, bypass, cmd.Raw, correlationId, ct);
        return ($"ad bypass {bypass}", null);
    }

    private async Task<(string, object?)> CosmeticAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 2);
        var target = ParseStringTarget(cmd.Args[0]);
        var unlock = ParseToggle(cmd.Args[1], "unlock", "lock");
        var unlockedCosmeticIds = await _cheat.CosmeticAsync(userId, target, unlock, cmd.Raw, correlationId, ct);
        return ($"cosmetic {cmd.Args[0]} {(unlock ? "unlock" : "lock")}", new { unlockedCosmeticIds });
    }

    private async Task<(string, object?)> AchievementAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        RequireArity(cmd, 2);
        var target = ParseStringTarget(cmd.Args[0]);
        var complete = ParseToggle(cmd.Args[1], "complete", "reset");
        var achievementStateAfter = await _cheat.AchievementAsync(userId, target, complete, cmd.Raw, correlationId, ct);
        return ($"achievement {cmd.Args[0]} {(complete ? "complete" : "reset")}", new { achievementStateAfter });
    }

    private async Task<(string, object?)> AttendanceAsync(long userId, ParsedCheatCommand cmd, string correlationId, CancellationToken ct)
    {
        if (cmd.Args.Count == 0)
            throw new GameApiException(ErrorCodes.InvalidCommand, "'attendance' expects 'setday {n}' or 'reset'.");

        int? day;
        if (cmd.Args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            RequireArity(cmd, 1);
            day = null;
        }
        else if (cmd.Args[0].Equals("setday", StringComparison.OrdinalIgnoreCase))
        {
            RequireArity(cmd, 2);
            day = (int)ParseLong(cmd.Args[1]);
        }
        else
        {
            throw new GameApiException(ErrorCodes.InvalidCommand, $"Unknown attendance verb '{cmd.Args[0]}'.");
        }

        var attendanceDay = await _cheat.AttendanceAsync(userId, day, cmd.Raw, correlationId, ct);
        return ($"attendance {string.Join(' ', cmd.Args)} → {attendanceDay}", new { attendanceDay });
    }

    private static void RequireArity(ParsedCheatCommand cmd, int count)
    {
        if (cmd.Args.Count != count)
            throw new GameApiException(ErrorCodes.InvalidCommand, $"'{cmd.DomainToken}' expects {count} argument(s).");
    }

    private static CheatAction ParseAdjustAction(string token) => token.ToLowerInvariant() switch
    {
        "add" => CheatAction.Add,
        "red" => CheatAction.Reduce,
        "set" => CheatAction.Set,
        _ => throw new GameApiException(ErrorCodes.InvalidCommand, $"Unknown action '{token}'."),
    };

    // null target = "all".
    private static int? ParseTarget(string token)
    {
        if (token.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        return (int)ParseLong(token);
    }

    // null target = "all"; otherwise the raw string id (cosmetic/achievement ids are strings).
    private static string? ParseStringTarget(string token)
        => token.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : token;

    private static bool ParseToggle(string token, string onToken, string offToken)
    {
        if (token.Equals(onToken, StringComparison.OrdinalIgnoreCase)) return true;
        if (token.Equals(offToken, StringComparison.OrdinalIgnoreCase)) return false;
        throw new GameApiException(ErrorCodes.InvalidCommand, $"Expected {onToken}|{offToken}, got '{token}'.");
    }

    private static bool ParseBool(string token) => token.ToLowerInvariant() switch
    {
        "true" => true,
        "false" => false,
        _ => throw new GameApiException(ErrorCodes.InvalidCommand, $"Expected true|false, got '{token}'."),
    };

    private static long ParseLong(string token)
    {
        if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;
        throw new GameApiException(ErrorCodes.InvalidCommand, $"Expected a number, got '{token}'.");
    }
}
