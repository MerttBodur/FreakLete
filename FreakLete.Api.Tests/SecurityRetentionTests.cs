using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

/// <summary>
/// Integration tests for security retention cleanup behavior.
/// Tests the EF Core ExecuteDeleteAsync predicate that SecurityRetentionCleanupService uses —
/// verifies old records are deleted and recent records are preserved.
/// Requires a live PostgreSQL test database (freaklete_test).
/// </summary>
[Collection("Api")]
public class SecurityRetentionTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;

    public SecurityRetentionTests(FreakLeteApiFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── AuthLoginAttempts ───────────────────────────────────────────

    [Fact]
    public async Task RetentionCleanup_DeletesOldAuthLoginAttempts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuthLoginAttempts.AddRange(
            MakeLoginAttempt("OLD@X.COM", daysAgo: 31),
            MakeLoginAttempt("RECENT@X.COM", daysAgo: 1));
        await db.SaveChangesAsync();

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await db.AuthLoginAttempts
            .Where(a => a.OccurredAtUtc < cutoff)
            .ExecuteDeleteAsync();

        Assert.Equal(1, deleted);
        Assert.Equal(1, await db.AuthLoginAttempts.CountAsync());
    }

    [Fact]
    public async Task RetentionCleanup_KeepsRecentAuthLoginAttempts()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AuthLoginAttempts.Add(MakeLoginAttempt("RECENT@X.COM", daysAgo: 1));
        await db.SaveChangesAsync();

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await db.AuthLoginAttempts
            .Where(a => a.OccurredAtUtc < cutoff)
            .ExecuteDeleteAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(1, await db.AuthLoginAttempts.CountAsync());
    }

    // ── GooglePlayRtdnEvents ────────────────────────────────────────

    [Fact]
    public async Task RetentionCleanup_DeletesOldGooglePlayRtdnEvents()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.GooglePlayRtdnEvents.AddRange(
            MakeRtdnEvent("msg-old", daysAgo: 91),
            MakeRtdnEvent("msg-recent", daysAgo: 1));
        await db.SaveChangesAsync();

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var deleted = await db.GooglePlayRtdnEvents
            .Where(e => e.ReceivedAtUtc < cutoff)
            .ExecuteDeleteAsync();

        Assert.Equal(1, deleted);
        Assert.Equal(1, await db.GooglePlayRtdnEvents.CountAsync());
    }

    [Fact]
    public async Task RetentionCleanup_KeepsRecentGooglePlayRtdnEvents()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.GooglePlayRtdnEvents.Add(MakeRtdnEvent("msg-recent", daysAgo: 1));
        await db.SaveChangesAsync();

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var deleted = await db.GooglePlayRtdnEvents
            .Where(e => e.ReceivedAtUtc < cutoff)
            .ExecuteDeleteAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(1, await db.GooglePlayRtdnEvents.CountAsync());
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static AuthLoginAttempt MakeLoginAttempt(string normalizedEmail, int daysAgo) => new()
    {
        NormalizedEmail = normalizedEmail,
        IpAddress = "1.2.3.4",
        OccurredAtUtc = DateTime.UtcNow.AddDays(-daysAgo),
        WasSuccessful = false
    };

    private static GooglePlayRtdnEvent MakeRtdnEvent(string messageId, int daysAgo) => new()
    {
        MessageId = messageId,
        PurchaseTokenFingerprint = "fp",
        ProductId = "freaklete_premium",
        NotificationType = 4,
        ReceivedAtUtc = DateTime.UtcNow.AddDays(-daysAgo),
        ProcessingState = "Processed"
    };
}
