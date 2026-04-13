using System.Reflection;
using FreakLete.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FreakLete.Api.Tests;

/// <summary>
/// Verifies that all recent migrations have [Migration] and [DbContext] attributes
/// so EF Core can discover and apply them automatically.
/// These are pure reflection tests — no database required.
/// </summary>
public class MigrationDiscoveryTests
{
    private static readonly Assembly MigrationsAssembly =
        typeof(AppDbContext).Assembly;

    [Theory]
    [InlineData("20260412000000_BillingRawPayloadMaxLength")]
    [InlineData("20260413000000_AddUserTokenVersion")]
    [InlineData("20260413000001_AddAuthLoginAttempts")]
    [InlineData("20260413000002_AddGooglePlayRtdnEvents")]
    public void Migration_HasMigrationAttribute(string migrationId)
    {
        var type = MigrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<MigrationAttribute>()?.Id == migrationId);

        Assert.NotNull(type);
    }

    [Theory]
    [InlineData("20260412000000_BillingRawPayloadMaxLength")]
    [InlineData("20260413000000_AddUserTokenVersion")]
    [InlineData("20260413000001_AddAuthLoginAttempts")]
    [InlineData("20260413000002_AddGooglePlayRtdnEvents")]
    public void Migration_HasDbContextAttribute_PointingToAppDbContext(string migrationId)
    {
        var type = MigrationsAssembly.GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<MigrationAttribute>()?.Id == migrationId);

        Assert.NotNull(type);

        var dbCtxAttr = type!.GetCustomAttribute<DbContextAttribute>();
        Assert.NotNull(dbCtxAttr);
        Assert.Equal(typeof(AppDbContext), dbCtxAttr.ContextType);
    }
}
