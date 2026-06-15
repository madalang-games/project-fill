#nullable enable
namespace ProjectFill.Contracts.Currency
{
    public sealed class CurrencySnapshot
    {
        public long SoftAmount { get; set; }
        public long SoftDelta { get; set; }
    }
}
