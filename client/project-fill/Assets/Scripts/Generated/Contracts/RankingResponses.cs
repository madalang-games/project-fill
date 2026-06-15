#nullable enable
using System;
using System.Collections.Generic;

namespace ProjectFill.Contracts.Ranking
{
    public sealed class RankingEntryDto
    {
        public long UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int AvatarId { get; set; }
        public int Rank { get; set; }
        public int Score { get; set; }
    }

    public sealed class RankingPageResponse
    {
        public string RankingType { get; set; } = string.Empty;
        public int Offset { get; set; }
        public int Limit { get; set; }
        public List<RankingEntryDto> Entries { get; set; } = new List<RankingEntryDto>();
    }

    public sealed class MyRankingResponse
    {
        public string RankingType { get; set; } = string.Empty;
        public RankingEntryDto? Entry { get; set; }
    }

    public sealed class StageRankResponse
    {
        public int StageId { get; set; }
        public int? Rank { get; set; }
        public int? BestMovesUsed { get; set; }
    }

    public sealed class RankingRebuildResponse
    {
        public bool Rebuilt { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }
}
