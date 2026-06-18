#nullable enable
using System.Collections.Generic;

namespace ProjectFill.Contracts.Stage
{
    public sealed class StageClearRequest
    {
        public int RulesetVersion { get; set; }
        public int MovesUsed { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public List<int> CompletedSignalTypes { get; set; } = new List<int>();
        public bool BoostersUsed { get; set; }
    }
}
