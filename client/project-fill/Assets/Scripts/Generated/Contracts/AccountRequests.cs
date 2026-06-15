#nullable enable

namespace ProjectFill.Contracts.Account
{
    public sealed class GuestLoginRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }

    public sealed class SocialLoginRequest
    {
        public string Provider { get; set; } = string.Empty; // "google" | "apple"
        public string IdToken { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string? Nonce { get; set; }
        public string? GuestRefreshToken { get; set; }
    }

    public sealed class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public sealed class LinkAccountRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string? GuestRefreshToken { get; set; }
    }

    public sealed class RenameRequest
    {
        public string NewName { get; set; } = string.Empty;
    }
}
