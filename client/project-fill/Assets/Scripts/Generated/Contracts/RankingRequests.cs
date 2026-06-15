#nullable enable
namespace ProjectFill.Contracts.Ranking
{
    public sealed class RankingPageRequest
    {
        public int Offset { get; set; }
        public int Limit { get; set; }
    }
}
