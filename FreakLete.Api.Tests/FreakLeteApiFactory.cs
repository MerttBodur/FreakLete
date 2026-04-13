using FreakLete.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FreakLete.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that:
/// 1. Points to a dedicated test PostgreSQL database (freaklete_test)
/// 2. Provides a dummy Gemini API key so startup doesn't throw
/// 3. Applies migrations automatically
/// 4. Wipes all user data between test classes via ResetDatabaseAsync
/// </summary>
public class FreakLeteApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=freaklete_test;Username=postgres;Password=4qxabc2p5ower+-";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.UseSetting("Jwt:Key", "TestJwtKey-AtLeast32Characters-Long-Enough-For-HS256!");
        builder.UseSetting("Jwt:Issuer", "FreakLete.Api");
        builder.UseSetting("Jwt:Audience", "FreakLete.App");
        builder.UseSetting("Gemini:ApiKey", "fake-gemini-key-for-tests");
        builder.UseSetting("Gemini:Model", "gemini-2.5-flash-lite");

        // TEST FIX: Disable EventLog provider to avoid permission errors in test/CI environment.
        builder.ConfigureLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole();
        });
    }

    public async Task InitializeAsync()
    {
        // Drop any stale test DB and recreate from scratch via migrations
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        // Drop the test database on teardown to keep things clean
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Truncates all user-related data. Call between test methods for isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Truncate cascade from Users wipes everything
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "Users" CASCADE""");
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "AuthLoginAttempts" """);
    }
}
