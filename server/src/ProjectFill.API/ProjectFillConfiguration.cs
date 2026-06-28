using System.Globalization;

namespace ProjectFill.API;

public sealed class ProjectFillConfiguration
{
    public required string GameEnvironment { get; init; }
    public required string LogLevel { get; init; }
    public required DatabaseOptions Database { get; init; }
    public required RedisOptions Redis { get; init; }
    public required AuthOptions Auth { get; init; }
    public required AdRewardOptions AdReward { get; init; }
    public required GooglePlayOptions GooglePlay { get; init; }
    public required AppOptions App { get; init; }
    public required RateLimitOptions RateLimit { get; init; }
    public required DevOptions Dev { get; init; }

    public static ProjectFillConfiguration Load(IConfiguration configuration)
    {
        var gameEnvironment = EnvRequired("GAME_ENV");
        var loaded = new ProjectFillConfiguration
        {
            GameEnvironment = gameEnvironment,
            LogLevel = EnvOptional("LOG_LEVEL") ?? "Information",
            Database = new DatabaseOptions
            {
                Host = EnvRequired("DB_HOST"),
                Port = EnvIntRequired("DB_PORT"),
                Name = EnvRequired("DB_NAME"),
                User = EnvRequired("DB_USER"),
                Password = EnvRequired("DB_PASSWORD")
            },
            Redis = new RedisOptions
            {
                Host = EnvRequired("REDIS_HOST"),
                Port = EnvIntRequired("REDIS_PORT")
            },
            Auth = new AuthOptions
            {
                JwtAuthority = EnvAbsoluteUriRequired("JWT_AUTHORITY"),
                JwtIssuer    = EnvAbsoluteUriOptional("JWT_ISSUER") ?? EnvAbsoluteUriRequired("JWT_AUTHORITY"),
                JwtAudience  = EnvRequired("JWT_AUDIENCE")
            },
            AdReward = new AdRewardOptions
            {
                VerifyMode = EnvOneOfRequired("AD_REWARD_VERIFY_MODE", "ssv", "mock")
            },
            GooglePlay = new GooglePlayOptions
            {
                PackageName       = EnvOptional("GOOGLE_PLAY_PACKAGE_NAME") ?? string.Empty,
                ServiceAccountJson = EnvOptional("GOOGLE_PLAY_SERVICE_ACCOUNT_JSON") ?? string.Empty
            },
            App = new AppOptions
            {
                ClientId = EnvRequired("APP_CLIENT_ID"),
                AllowedClientVersion = EnvRequired("APP_ALLOWED_CLIENT_VERSION"),
                RequiredClientVersion = EnvRequired("APP_REQUIRED_CLIENT_VERSION"),
                AllowedProtocolVersion = EnvRequired("APP_ALLOWED_PROTOCOL_VERSION")
            },
            RateLimit = new RateLimitOptions
            {
                StageStartPerHour      = ConfigIntOptional(configuration, "RateLimit:StageStartPerHour")      ?? 720,
                TransactionalPerMinute = ConfigIntOptional(configuration, "RateLimit:TransactionalPerMinute") ?? 30
            },
            Dev = new DevOptions
            {
                Enabled = string.Equals(gameEnvironment, "dev", StringComparison.OrdinalIgnoreCase),
                CheatWhitelist = EnvCsvOptional("DEV_CHEAT_WHITELIST")
            }
        };

        loaded.ApplyTo(configuration);
        return loaded;
    }

    public void ApplyTo(IConfiguration configuration)
    {
        configuration["ConnectionStrings:MySQL"] = Database.ConnectionString;
        configuration["Redis:Connection"] = Redis.ConnectionString;
        configuration["Jwt:Authority"] = Auth.JwtAuthority;
        configuration["Jwt:Issuer"] = Auth.JwtIssuer;
        configuration["Jwt:Audience"] = Auth.JwtAudience;
        configuration["AdReward:VerifyMode"] = AdReward.VerifyMode;
        configuration["Game:Environment"] = GameEnvironment;
        configuration["GooglePlay:PackageName"] = GooglePlay.PackageName;
        configuration["GooglePlay:ServiceAccountJson"] = GooglePlay.ServiceAccountJson;
        configuration["App:ClientId"] = App.ClientId;
        configuration["App:AllowedClientVersion"] = App.AllowedClientVersion;
        configuration["App:RequiredClientVersion"] = App.RequiredClientVersion;
        configuration["App:AllowedProtocolVersion"] = App.AllowedProtocolVersion;
    }

    public sealed class DatabaseOptions
    {
        public required string Host { get; init; }
        public required int Port { get; init; }
        public required string Name { get; init; }
        public required string User { get; init; }
        public required string Password { get; init; }

        public string ConnectionString => $"Server={Host};Port={Port};Database={Name};User={User};Password={Password}";
    }

    public sealed class RedisOptions
    {
        public required string Host { get; init; }
        public required int Port { get; init; }

        public string ConnectionString => $"{Host}:{Port},abortConnect=false,resolveDns=true";
    }

    public sealed class AuthOptions
    {
        public required string JwtAuthority { get; init; }
        public required string JwtIssuer { get; init; }
        public required string JwtAudience { get; init; }
    }

    public sealed class AdRewardOptions
    {
        public required string VerifyMode { get; init; }
    }

    public sealed class GooglePlayOptions
    {
        // Optional: empty until Google Play IAP verification is configured.
        // GooglePlayVerifier rejects real (non-mock) receipts with IAP_VERIFICATION_FAILED if unset.
        public required string PackageName { get; init; }
        public required string ServiceAccountJson { get; init; }
    }

    public sealed class AppOptions
    {
        public required string ClientId { get; init; }
        public required string AllowedClientVersion { get; init; }
        public required string RequiredClientVersion { get; init; }
        public required string AllowedProtocolVersion { get; init; }
    }

    public sealed class RateLimitOptions
    {
        public required int StageStartPerHour { get; init; }
        public required int TransactionalPerMinute { get; init; }
    }

    public sealed class DevOptions
    {
        // True only when GAME_ENV == dev. Gates the cheat endpoints (404 otherwise).
        public required bool Enabled { get; init; }

        // Platform PIDs allowed to use cheats. Empty => allow all (local dev convenience).
        public required IReadOnlyList<string> CheatWhitelist { get; init; }
    }

    private static string EnvRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: missing required environment variable.");
    }

    private static string? EnvOptional(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

    private static IReadOnlyList<string> EnvCsvOptional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static int EnvIntRequired(string key)
    {
        var value = EnvRequired(key);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not a valid integer.");
    }

    private static string EnvAbsoluteUriRequired(string key)
    {
        var value = EnvRequired(key);
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not a valid absolute URI.");
    }

    private static string? EnvAbsoluteUriOptional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out _)) return value;
        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not a valid absolute URI.");
    }

    private static string EnvOneOfRequired(string key, params string[] allowed)
    {
        var value = EnvRequired(key).Trim().ToLowerInvariant();
        if (allowed.Contains(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not one of: {string.Join(", ", allowed)}.");
    }

    private static int? ConfigIntOptional(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Configuration error at configuration:{key}: `{value}` is not a valid integer.");
    }
}
