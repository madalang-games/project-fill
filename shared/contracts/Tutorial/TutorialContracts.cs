#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Tutorial
{
    public sealed class TutorialProgressResponse
    {
        public List<int> CompletedTutorialIds { get; set; } = new List<int>();
    }

    public sealed class TutorialProgressUpdateResponse
    {
        public bool Success { get; set; }
        public List<int> CompletedTutorialIds { get; set; } = new List<int>();
    }
}
