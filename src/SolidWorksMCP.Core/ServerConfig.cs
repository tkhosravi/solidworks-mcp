namespace SolidWorksMCP.Core;

/// <summary>
/// Server configuration read from environment variables, so users can tune
/// the server from the MCP client config (Claude Desktop's "env" block)
/// without touching the source or rebuild settings.
/// </summary>
public sealed record ServerConfig(
    int ReleaseYear,
    string ExePath,
    bool AutoStart,
    int StartTimeoutSeconds)
{
    public const string VersionVar = "SOLIDWORKS_VERSION";
    public const string ExePathVar = "SOLIDWORKS_EXE_PATH";
    public const string AutoStartVar = "SOLIDWORKS_AUTO_START";
    public const string StartTimeoutVar = "SOLIDWORKS_START_TIMEOUT";

    public const int DefaultReleaseYear = 2025;
    public const string DefaultExePath = @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe";
    public const int DefaultStartTimeoutSeconds = 90;
    public const int MinStartTimeoutSeconds = 5;

    public static ServerConfig Default { get; } = new(
        DefaultReleaseYear, DefaultExePath, AutoStart: false, DefaultStartTimeoutSeconds);

    /// <summary>
    /// Builds the configuration from an environment lookup
    /// (pass <c>Environment.GetEnvironmentVariable</c> in production).
    /// Invalid values throw ArgumentException with the variable name, so a
    /// typo fails loudly at startup instead of silently using a default.
    /// </summary>
    public static ServerConfig FromEnvironment(Func<string, string?> getEnv)
    {
        var releaseYear = ParseReleaseYear(getEnv(VersionVar));
        var exePath = ParseExePath(getEnv(ExePathVar));
        var autoStart = ParseBool(getEnv(AutoStartVar));
        var timeout = ParseTimeout(getEnv(StartTimeoutVar));
        return new ServerConfig(releaseYear, exePath, autoStart, timeout);
    }

    private static int ParseReleaseYear(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultReleaseYear;
        if (!int.TryParse(raw.Trim(), out var year) || year < 1995 || year > 2099)
            throw new ArgumentException(
                $"{VersionVar}='{raw}' is not a valid SolidWorks release year (e.g. 2025).");
        return year;
    }

    private static string ParseExePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultExePath;
        var path = raw.Trim().Trim('"');
        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"{ExePathVar}='{raw}' must point to SLDWORKS.exe.");
        return path;
    }

    private static bool ParseBool(string? raw) =>
        raw?.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";

    private static int ParseTimeout(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultStartTimeoutSeconds;
        if (!int.TryParse(raw.Trim(), out var seconds) || seconds < MinStartTimeoutSeconds)
            throw new ArgumentException(
                $"{StartTimeoutVar}='{raw}' must be an integer ≥ {MinStartTimeoutSeconds} (seconds).");
        return seconds;
    }
}
