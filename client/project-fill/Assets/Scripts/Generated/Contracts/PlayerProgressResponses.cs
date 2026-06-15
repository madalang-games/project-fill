#nullable enable
using System.Collections.Generic;

namespace ProjectFill.Contracts.Player
{
    public sealed class PlayerProgressResponse
    {
        public int MaxClearedStageId { get; set; }
        public List<StageProgressEntry> Stages { get; set; } = new();
        public List<int> UnlockedAvatarIds { get; set; } = new();
        public int EquippedBoardThemeId { get; set; } = 1;
        public List<int> UnlockedBoardThemeIds { get; set; } = new();
        public bool IsNoAds { get; set; }
    }

    public sealed class StageProgressEntry
    {
        public int StageId  { get; set; }
        public int BestStar { get; set; }
    }
}
