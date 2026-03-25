using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class CoachProfileIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CoachProfileIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> RegisterAndAuthenticateAsync()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);
        return c;
    }

    private static async Task<JsonElement> GetProfileJsonAsync(HttpClient client)
    {
        var resp = await client.GetAsync("/api/auth/profile");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    // ════════════════════════════════════════════════════════════════
    //  FULL COACH ROUNDTRIP
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveCoachProfile_FullRoundtrip_ReturnsUpdatedProfile()
    {
        var c = await RegisterAndAuthenticateAsync();

        var payload = new
        {
            trainingDaysPerWeek = 5,
            preferredSessionDurationMinutes = 90,
            primaryTrainingGoal = "Strength",
            secondaryTrainingGoal = "Hypertrophy",
            dietaryPreference = "High Protein",
            availableEquipment = "Barbell, Dumbbells, Rack",
            physicalLimitations = "None",
            injuryHistory = "ACL 2020",
            currentPainPoints = "Mild knee discomfort"
        };

        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", payload);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(5, body.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal(90, body.GetProperty("preferredSessionDurationMinutes").GetInt32());
        Assert.Equal("Strength", body.GetProperty("primaryTrainingGoal").GetString());
        Assert.Equal("Hypertrophy", body.GetProperty("secondaryTrainingGoal").GetString());
        Assert.Equal("High Protein", body.GetProperty("dietaryPreference").GetString());
        Assert.Equal("Barbell, Dumbbells, Rack", body.GetProperty("availableEquipment").GetString());
        Assert.Equal("None", body.GetProperty("physicalLimitations").GetString());
        Assert.Equal("ACL 2020", body.GetProperty("injuryHistory").GetString());
        Assert.Equal("Mild knee discomfort", body.GetProperty("currentPainPoints").GetString());

        // Verify via GET
        var p = await GetProfileJsonAsync(c);
        Assert.Equal(5, p.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal("Strength", p.GetProperty("primaryTrainingGoal").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  INVALID OPTION VALUES REJECTED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveCoachProfile_InvalidTrainingGoal_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            primaryTrainingGoal = "Become Superhero"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Invalid primary training goal", body);
    }

    [Fact]
    public async Task SaveCoachProfile_InvalidDietaryPreference_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            dietaryPreference = "Carnivore Only"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Invalid dietary preference", body);
    }

    [Fact]
    public async Task SaveCoachProfile_InvalidSessionDuration_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            preferredSessionDurationMinutes = 55
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Invalid session duration", body);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public async Task SaveCoachProfile_InvalidTrainingDays_Returns400(int days)
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            trainingDaysPerWeek = days
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  ATHLETE FIELDS PRESERVED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveCoachProfile_AthleteFieldsPreserved()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set athlete fields
        await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 85.0,
            sportName = "Basketball",
            position = "Center",
            gymExperienceLevel = "5+ years"
        });

        // Save coach profile
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            trainingDaysPerWeek = 4,
            primaryTrainingGoal = "Strength"
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Athlete fields must survive
        var p = await GetProfileJsonAsync(c);
        Assert.Equal(85.0, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Basketball", p.GetProperty("sportName").GetString());
        Assert.Equal("Center", p.GetProperty("position").GetString());
        Assert.Equal("5+ years", p.GetProperty("gymExperienceLevel").GetString());
        Assert.Equal(4, p.GetProperty("trainingDaysPerWeek").GetInt32());
    }

    // ════════════════════════════════════════════════════════════════
    //  CLEAR/EMPTY HANDLING FOR OPTIONAL TEXT FIELDS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveCoachProfile_NullFields_ClearsValues()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set values first
        await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            trainingDaysPerWeek = 5,
            primaryTrainingGoal = "Strength",
            availableEquipment = "Full gym",
            injuryHistory = "ACL 2020"
        });

        // Clear all by sending nulls
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            trainingDaysPerWeek = (int?)null,
            preferredSessionDurationMinutes = (int?)null,
            primaryTrainingGoal = (string?)null,
            secondaryTrainingGoal = (string?)null,
            dietaryPreference = (string?)null,
            availableEquipment = (string?)null,
            physicalLimitations = (string?)null,
            injuryHistory = (string?)null,
            currentPainPoints = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("trainingDaysPerWeek").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("preferredSessionDurationMinutes").ValueKind);
        Assert.Equal("", p.GetProperty("primaryTrainingGoal").GetString());
        Assert.Equal("", p.GetProperty("availableEquipment").GetString());
        Assert.Equal("", p.GetProperty("injuryHistory").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  AUTH REQUIRED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveCoachProfile_NoAuth_Returns401()
    {
        var resp = await _client.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            trainingDaysPerWeek = 3
        });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  RESPONSE CONTAINS FULL PROFILE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveCoachProfile_ResponseContainsFullProfile()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set athlete fields first
        await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 75.0,
            sportName = "Powerlifting"
        });

        // Save coach
        var resp = await c.PutAsJsonAsync("/api/auth/profile/coach", new
        {
            trainingDaysPerWeek = 6,
            primaryTrainingGoal = "Powerlifting"
        });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        // Coach fields in response
        Assert.Equal(6, body.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal("Powerlifting", body.GetProperty("primaryTrainingGoal").GetString());

        // Athlete fields also in response
        Assert.Equal(75.0, body.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Powerlifting", body.GetProperty("sportName").GetString());

        // Core identity fields
        Assert.False(string.IsNullOrEmpty(body.GetProperty("firstName").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("email").GetString()));
    }
}
