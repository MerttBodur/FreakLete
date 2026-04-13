using Microsoft.Extensions.Configuration;

namespace FreakLete.Api;

/// <summary>
/// Resolves startup migration behavior from configuration.
/// Extracted for unit-testability without spinning up the full host.
/// </summary>
public static class DatabaseStartupConfig
{
    /// <summary>
    /// Returns true if auto-migrate should run on startup.
    ///
    /// Resolution order:
    ///   1. Database:AutoMigrate config key (env var: Database__AutoMigrate) — explicit wins
    ///   2. Development environment default → true
    ///   3. All other environments (Production, Staging, …) → false
    ///
    /// Production Railway deployments must set Database__AutoMigrate=true explicitly
    /// or apply migrations manually before deploy.
    /// </summary>
    public static bool ShouldAutoMigrate(IConfiguration configuration, bool isDevelopment)
    {
        var raw = configuration["Database:AutoMigrate"];
        if (raw is not null && bool.TryParse(raw, out var parsed))
            return parsed;
        return isDevelopment;
    }
}
