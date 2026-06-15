#nullable enable
using ProjectFill.Contracts.Currency;

namespace ProjectFill.Contracts.Player
{
    public sealed class UserProfileUpdateRequest
    {
        public string? DisplayName { get; set; }
        public int? AvatarId { get; set; }
    }

    public sealed class UserProfileUpdateResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public int AvatarId { get; set; }
        public int BoardThemeId { get; set; }
        public CurrencySnapshot Currency { get; set; } = new CurrencySnapshot();
    }
}
