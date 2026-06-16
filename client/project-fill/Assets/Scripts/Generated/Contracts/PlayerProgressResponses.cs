#nullable enable
using System.Collections.Generic;

namespace ProjectFill.Contracts.Player
{
    public sealed class PlayerProgressResponse
    {
        public List<int> UnlockedAvatarIds { get; set; } = new List<int>();
        public bool IsNoAds { get; set; }
    }
}
