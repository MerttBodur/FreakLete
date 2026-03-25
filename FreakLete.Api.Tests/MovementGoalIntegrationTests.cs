using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class MovementGoalIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    public MovementGoalIntegrationTests(FreakLeteApiFactory factory)
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

    private static object MakeGoalRequest(
        string movementName = "Vertical Jump",
        string movementCategory = "Plyometric",
        string goalMetricLabel = "Max Height",
        double targetValue = 90.0,
        string unit = "cm") =>
        new
        {
            movementName,
            movementCategory,
            goalMetricLabel,
            targetValue,
            unit
        };

    // ════════════════════════════════════════════════════════════════
    //  CREATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateGoal_Returns201_WithCorrectData()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest());
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Vertical Jump", json.GetProperty("movementName").GetString());
        Assert.Equal("Plyometric", json.GetProperty("movementCategory").GetString());
        Assert.Equal("Max Height", json.GetProperty("goalMetricLabel").GetString());
        Assert.Equal(90.0, json.GetProperty("targetValue").GetDouble());
        Assert.Equal("cm", json.GetProperty("unit").GetString());
        Assert.True(json.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task CreateGoal_Unauthenticated_Returns401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  LIST
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListGoals_ReturnsAllForUser()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest("Vertical Jump", targetValue: 90));
        await c.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest("Broad Jump", "Plyometric", "Distance", 2.8, "m"));
        await c.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest("40m Sprint", "Speed", "Time", 4.5, "s"));

        var resp = await c.GetAsync("/api/movementgoals");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(3, arr.GetArrayLength());
    }

    // ════════════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateGoal_ChangesFields()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest(targetValue: 85));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var updateResp = await c.PutAsJsonAsync($"/api/movementgoals/{id}",
            MakeGoalRequest(targetValue: 95, goalMetricLabel: "New Max"));
        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

        // Verify via list
        var listResp = await c.GetAsync("/api/movementgoals");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        var goal = arr.EnumerateArray().First(g => g.GetProperty("id").GetInt32() == id);
        Assert.Equal(95.0, goal.GetProperty("targetValue").GetDouble());
        Assert.Equal("New Max", goal.GetProperty("goalMetricLabel").GetString());
    }

    [Fact]
    public async Task UpdateGoal_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/movementgoals/99999", MakeGoalRequest());
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateGoal_DoesNotWipeUnrelatedFields()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Create with all fields
        var createResp = await c.PostAsJsonAsync("/api/movementgoals",
            MakeGoalRequest("Sprint", "Speed", "Best Time", 4.5, "s"));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        // Update only target value, sending all fields (full replace)
        var updateResp = await c.PutAsJsonAsync($"/api/movementgoals/{id}",
            MakeGoalRequest("Sprint", "Speed", "Best Time", 4.3, "s"));
        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

        var listResp = await c.GetAsync("/api/movementgoals");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        var goal = arr.EnumerateArray().First(g => g.GetProperty("id").GetInt32() == id);

        Assert.Equal("Sprint", goal.GetProperty("movementName").GetString());
        Assert.Equal("Speed", goal.GetProperty("movementCategory").GetString());
        Assert.Equal("Best Time", goal.GetProperty("goalMetricLabel").GetString());
        Assert.Equal(4.3, goal.GetProperty("targetValue").GetDouble());
        Assert.Equal("s", goal.GetProperty("unit").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  DELETE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteGoal_RemovesIt()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var delResp = await c.DeleteAsync($"/api/movementgoals/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var listResp = await c.GetAsync("/api/movementgoals");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task DeleteGoal_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.DeleteAsync("/api/movementgoals/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  METRIC / UNIT FIELDS ROUNDTRIP
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MetricAndUnitFields_RoundtripCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/movementgoals",
            MakeGoalRequest("RSI", "Plyometric", "Reactive Strength Index", 2.5, "ratio"));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var listResp = await c.GetAsync("/api/movementgoals");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        var goal = arr.EnumerateArray().First(g => g.GetProperty("id").GetInt32() == id);

        Assert.Equal("RSI", goal.GetProperty("movementName").GetString());
        Assert.Equal("Plyometric", goal.GetProperty("movementCategory").GetString());
        Assert.Equal("Reactive Strength Index", goal.GetProperty("goalMetricLabel").GetString());
        Assert.Equal(2.5, goal.GetProperty("targetValue").GetDouble());
        Assert.Equal("ratio", goal.GetProperty("unit").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  USER ISOLATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserIsolation_CannotSeeOtherUsersGoals()
    {
        var user1 = await RegisterAndAuthenticateAsync("mg1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("mg2@test.com");

        await user1.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest());

        var listResp = await user2.GetAsync("/api/movementgoals");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task UserIsolation_CannotUpdateOtherUsersGoal()
    {
        var user1 = await RegisterAndAuthenticateAsync("mgu1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("mgu2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.PutAsJsonAsync($"/api/movementgoals/{id}", MakeGoalRequest(targetValue: 999));
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_CannotDeleteOtherUsersGoal()
    {
        var user1 = await RegisterAndAuthenticateAsync("mgd1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("mgd2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/movementgoals", MakeGoalRequest());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.DeleteAsync($"/api/movementgoals/{id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        // Original still exists
        var listResp = await user1.GetAsync("/api/movementgoals");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, arr.GetArrayLength());
    }
}
