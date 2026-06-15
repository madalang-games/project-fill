namespace ProjectFill.Application.Common;

public sealed class GameApiException : Exception
{
    public GameApiException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
