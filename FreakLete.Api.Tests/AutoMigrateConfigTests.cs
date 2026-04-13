using FreakLete.Api;
using Microsoft.Extensions.Configuration;

namespace FreakLete.Api.Tests;

/// <summary>
/// Unit tests for DatabaseStartupConfig.ShouldAutoMigrate — no database required.
/// Verifies that the Production-safe default (false) and explicit overrides behave correctly.
/// </summary>
public class AutoMigrateConfigTests
{
    [Theory]
    [InlineData(null, true, true)]     // No config + Development → auto-migrate (safe default)
    [InlineData(null, false, false)]   // No config + Production  → no auto-migrate (safe default)
    [InlineData("true", false, true)]  // Explicitly true even in Production → auto-migrate
    [InlineData("false", true, false)] // Explicitly false even in Development → no auto-migrate
    [InlineData("True", false, true)]  // Case insensitive
    [InlineData("False", true, false)] // Case insensitive
    public void ShouldAutoMigrate_ResolvedCorrectly(
        string? configuredValue, bool isDevelopment, bool expected)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        if (configuredValue is not null)
            pairs.Add(new("Database:AutoMigrate", configuredValue));

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(pairs)
            .Build();

        var result = DatabaseStartupConfig.ShouldAutoMigrate(config, isDevelopment);

        Assert.Equal(expected, result);
    }
}
