#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Account
{
    public sealed class AccountMeResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Pid { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsGuest { get; set; }
        public List<string> LinkedProviders { get; set; } = new List<string>();
        public int AvatarId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string ExpiresAt { get; set; } = string.Empty;
        public AccountMeResponse Profile { get; set; } = new AccountMeResponse();
    }

    public sealed class RenameResponse
    {
        public string NewName { get; set; } = string.Empty;
    }
}
