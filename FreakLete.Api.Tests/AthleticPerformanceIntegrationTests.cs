using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class AthleticPerformanceIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    public AthleticPerformanceIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> RegisterAndAuthenticateAsync(string? email = null)
    {
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email);
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);
        return c;
    }

    private static object MakePerformanceEntry(
        string movementName = "Vertical Jump",
        string movementCategory = "Plyometric",
        double value = 85.0,
        string unit = "cm",
        double? secondaryValue = null,
        string? secondaryUnit = null,
        double? groundContactTimeMs = null,
        double? concentricTimeSeconds = null) =>
        new
        {
            movementName,
            movementCategory,
            value,
            unit,
            secondaryValue,
            secondaryUnit,
            groundContactTimeMs,
            concentricTimeSeconds
        };

    // ════════════════════════════════════════════════════════════════
    //  CREATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreatePerformance_Returns201_WithCorrectData()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PostAsJsonAsync("/api/athleticperformance",
            MakePerformanceEntry(secondaryValue: 3.2, secondaryUnit: "m/s"));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Vertical Jump", json.GetProperty("movementName").GetString());
        Assert.Equal(85.0, json.GetProperty("value").GetDouble());
        Assert.Equal("cm", json.GetProperty("unit").GetString());
        Assert.True(json.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task CreatePerformance_Unauthenticated_Returns401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry());
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  LIST
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListPerformance_ReturnsAllForUser()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Vertical Jump"));
        await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Broad Jump", value: 2.5, unit: "m"));
        await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("40m Sprint", "Speed", 4.8, "s"));

        var resp = await c.GetAsync("/api/athleticperformance");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(3, arr.GetArrayLength());
    }

    [Fact]
    public async Task GetByMovement_FiltersCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Vertical Jump", value: 80));
        await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Vertical Jump", value: 85));
        await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Broad Jump", value: 2.4, unit: "m"));

        var resp = await c.GetAsync("/api/athleticperformance/by-movement/Vertical%20Jump");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, arr.GetArrayLength());
    }

    // ════════════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdatePerformance_ChangesFields()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry(value: 80));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var updateResp = await c.PutAsJsonAsync($"/api/athleticperformance/{id}",
            MakePerformanceEntry(value: 90, groundContactTimeMs: 130.0, concentricTimeSeconds: 0.7));
        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

        var getResp = await c.GetAsync("/api/athleticperformance");
        var arr = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;
        // Find updated entry
        var entry = arr.EnumerateArray().First(e => e.GetProperty("id").GetInt32() == id);
        Assert.Equal(90.0, entry.GetProperty("value").GetDouble());
        Assert.Equal(130.0, entry.GetProperty("groundContactTimeMs").GetDouble());
        Assert.Equal(0.7, entry.GetProperty("concentricTimeSeconds").GetDouble());
    }

    [Fact]
    public async Task UpdatePerformance_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/athleticperformance/99999", MakePerformanceEntry());
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  DELETE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeletePerformance_RemovesIt()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var delResp = await c.DeleteAsync($"/api/athleticperformance/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Verify removed from list
        var listResp = await c.GetAsync("/api/athleticperformance");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task DeletePerformance_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.DeleteAsync("/api/athleticperformance/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  METRIC / TIMING FIELDS ROUNDTRIP
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllFields_RoundtripCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/athleticperformance",
            MakePerformanceEntry(
                "Reactive Strength Index",
                "Plyometric",
                2.8,
                "ratio",
                secondaryValue: 45.0,
                secondaryUnit: "cm",
                groundContactTimeMs: 160.5,
                concentricTimeSeconds: 0.55));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        // Re-fetch from list to verify persistence
        var listResp = await c.GetAsync("/api/athleticperformance");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        var entry = arr.EnumerateArray().First(e => e.GetProperty("id").GetInt32() == id);

        Assert.Equal("Reactive Strength Index", entry.GetProperty("movementName").GetString());
        Assert.Equal("Plyometric", entry.GetProperty("movementCategory").GetString());
        Assert.Equal(2.8, entry.GetProperty("value").GetDouble());
        Assert.Equal("ratio", entry.GetProperty("unit").GetString());
        Assert.Equal(45.0, entry.GetProperty("secondaryValue").GetDouble());
        Assert.Equal("cm", entry.GetProperty("secondaryUnit").GetString());
        Assert.Equal(160.5, entry.GetProperty("groundContactTimeMs").GetDouble());
        Assert.Equal(0.55, entry.GetProperty("concentricTimeSeconds").GetDouble());
    }

    [Fact]
    public async Task NullableFields_StayNull_WhenNotProvided()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/athleticperformance",
            MakePerformanceEntry("Sprint", "Speed", 10.5, "s"));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var listResp = await c.GetAsync("/api/athleticperformance");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        var entry = arr.EnumerateArray().First(e => e.GetProperty("id").GetInt32() == id);

        Assert.Equal(JsonValueKind.Null, entry.GetProperty("secondaryValue").ValueKind);
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("secondaryUnit").ValueKind);
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("groundContactTimeMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("concentricTimeSeconds").ValueKind);
    }

    // ════════════════════════════════════════════════════════════════
    //  USER ISOLATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserIsolation_CannotSeeOtherUsersEntries()
    {
        var user1 = await RegisterAndAuthenticateAsync("ap1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("ap2@test.com");

        await user1.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry());

        var listResp = await user2.GetAsync("/api/athleticperformance");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task UserIsolation_CannotUpdateOtherUsersEntry()
    {
        var user1 = await RegisterAndAuthenticateAsync("apu1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("apu2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.PutAsJsonAsync($"/api/athleticperformance/{id}", MakePerformanceEntry(value: 999));
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_CannotDeleteOtherUsersEntry()
    {
        var user1 = await RegisterAndAuthenticateAsync("apd1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("apd2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.DeleteAsync($"/api/athleticperformance/{id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        // Original still exists
        var listResp = await user1.GetAsync("/api/athleticperformance");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, arr.GetArrayLength());
    }

    [Fact]
    public async Task UserIsolation_ByMovementFilter_OnlyShowsOwnEntries()
    {
        var user1 = await RegisterAndAuthenticateAsync("apm1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("apm2@test.com");

        await user1.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Vertical Jump", value: 80));
        await user2.PostAsJsonAsync("/api/athleticperformance", MakePerformanceEntry("Vertical Jump", value: 70));

        var resp = await user1.GetAsync("/api/athleticperformance/by-movement/Vertical%20Jump");
        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal(80.0, arr[0].GetProperty("value").GetDouble());
    }
}
