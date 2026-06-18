using ProjectFill.Application.Common;
using ProjectFill.Domain.Enums;

namespace ProjectFill.API.Dev;

public sealed record ParsedCheatCommand(CheatDomain Domain, string DomainToken, IReadOnlyList<string> Args, string Raw);

public static class CheatCommandParser
{
    // Grammar: /{domain} [target] [action] [value]. Domain is validated against the catalog;
    // per-domain arity is enforced by CheatDispatcher. Malformed input → 400 INVALID_COMMAND.
    public static ParsedCheatCommand Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new GameApiException(ErrorCodes.InvalidCommand, "Empty command.");

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith('/'))
            throw new GameApiException(ErrorCodes.InvalidCommand, "Command must start with '/'.");

        var tokens = trimmed[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            throw new GameApiException(ErrorCodes.InvalidCommand, "Missing domain.");

        var domainToken = tokens[0].ToLowerInvariant();
        if (!CheatCommandCatalog.TryGet(domainToken, out var spec))
            throw new GameApiException(ErrorCodes.InvalidCommand, $"Unknown domain '{domainToken}'.");

        return new ParsedCheatCommand(spec!.Domain, domainToken, tokens.Skip(1).ToArray(), trimmed);
    }
}
