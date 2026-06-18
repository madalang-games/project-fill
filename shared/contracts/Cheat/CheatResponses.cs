#nullable enable
namespace ProjectFill.Contracts.Cheat
{
    public sealed class CheatCommandResponse
    {
        public bool Success { get; set; }
        public string Command { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
