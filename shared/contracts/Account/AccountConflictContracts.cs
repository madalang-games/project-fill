#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Account
{
    public sealed class SaveSnapshotDto
    {
        public int MaxStageId { get; set; }
        public long Gold { get; set; }
        public int TotalStars { get; set; }
        public int TotalItems { get; set; }
    }

    public sealed class LinkAccountResponse
    {
        public bool Success { get; set; }
        public bool Conflict { get; set; }
        public SaveSnapshotDto? LocalSave { get; set; }
        public SaveSnapshotDto? CloudSave { get; set; }
        public string? ConflictToken { get; set; }
        // No-conflict path: server returns auth tokens directly instead of conflict data
        public AuthResponse? Auth { get; set; }
    }

    public sealed class ResolveConflictRequest
    {
        public string ConflictToken { get; set; } = string.Empty;
        public string Selection { get; set; } = string.Empty; // "local" | "cloud"
    }

    public sealed class ResolveConflictResponse
    {
        public bool Success { get; set; }
        public AuthResponse? Auth { get; set; }
    }
}
