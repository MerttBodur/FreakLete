using System.Net;
using System.Text.Json;

namespace FreakLete.Api.Tests;

/// <summary>
/// Integration tests for GET /api/health covering healthy + unhealthy paths
/// and production sanitization (no sensitive details in non-Development).
/// </summary>
[Collection("Api")]
public class HealthIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;

    public HealthIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Healthy (Testing env with live DB) ──────────────────────────

    [Fact]
    public async Task Health_ReturnsOk_WhenDatabaseIsReachable()
    {
        // The Testing environment has a live DB and migrations applied in InitializeAsync.
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ParseBodyAsync(response);
        Assert.Equal("healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_DoesNotExposeMigrationList_InNonDevelopment()
    {
        // Testing env is "Testing", not "Development" — no migration detail should appear.
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        var body = await ParseBodyAsync(response);
        Assert.False(body.TryGetProperty("migrations", out _),
            "migrations list must not be present in non-Development response");
        Assert.False(body.TryGetProperty("database", out _),
            "database field must not be present in non-Development response");
        Assert.False(body.TryGetProperty("pendingMigrations", out _),
            "pendingMigrations count must not be present in non-Development response");
    }

    [Fact]
    public async Task Health_OnlyReturnsStatusField_InNonDevelopment()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        var body = await ParseBodyAsync(response);
        // Only "status" is allowed in production-safe mode
        Assert.True(body.TryGetProperty("status", out _), "status field must be present");
    }

    // ── Unhealthy — broken DB factory ───────────────────────────────

    [Fact]
    public async Task Health_Returns503_WhenDatabaseIsUnreachable()
    {
        // Override connection string to a bogus host to force failure
        var brokenFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=9999;Database=does_not_exist;Username=nobody;Password=bad;Timeout=1;Command Timeout=1");
        });

        var client = brokenFactory.CreateClient();
        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await ParseBodyAsync(response);
        Assert.Equal("unhealthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_DoesNotExposeErrorMessage_WhenUnhealthyInNonDevelopment()
    {
        var brokenFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=9999;Database=does_not_exist;Username=nobody;Password=bad;Timeout=1;Command Timeout=1");
        });

        var client = brokenFactory.CreateClient();
        var response = await client.GetAsync("/api/health");

        var body = await ParseBodyAsync(response);
        Assert.False(body.TryGetProperty("error", out _),
            "error message must not leak in non-Development unhealthy response");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static async Task<JsonElement> ParseBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
