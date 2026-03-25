using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class AthleteProfileIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AthleteProfileIntegrationTests(FreakLeteApiFactory factory)
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
    //  FULL ATHLETE ROUNDTRIP
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_FullRoundtrip_ReturnsUpdatedProfile()
    {
        var c = await RegisterAndAuthenticateAsync();

        var payload = new
        {
            dateOfBirth = "2000-06-15",
            weightKg = 82.5,
            bodyFatPercentage = 14.2,
            sportName = "Basketball",
            position = "Point Guard",
            gymExperienceLevel = "3-4 years"
        };

        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", payload);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Response should contain the full updated profile
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("2000-06-15", body.GetProperty("dateOfBirth").GetString());
        Assert.Equal(82.5, body.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal(14.2, body.GetProperty("bodyFatPercentage").GetDouble(), 0.01);
        Assert.Equal("Basketball", body.GetProperty("sportName").GetString());
        Assert.Equal("Point Guard", body.GetProperty("position").GetString());
        Assert.Equal("3-4 years", body.GetProperty("gymExperienceLevel").GetString());

        // Verify via GET
        var p = await GetProfileJsonAsync(c);
        Assert.Equal("2000-06-15", p.GetProperty("dateOfBirth").GetString());
        Assert.Equal(82.5, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal(14.2, p.GetProperty("bodyFatPercentage").GetDouble(), 0.01);
        Assert.Equal("Basketball", p.GetProperty("sportName").GetString());
        Assert.Equal("Point Guard", p.GetProperty("position").GetString());
        Assert.Equal("3-4 years", p.GetProperty("gymExperienceLevel").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  CLEAR NULLABLE FIELDS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_NullFields_ClearsValues()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set values first
        await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            dateOfBirth = "1995-01-01",
            weightKg = 80.0,
            bodyFatPercentage = 15.0,
            sportName = "Soccer",
            position = "Goalkeeper",
            gymExperienceLevel = "5+ years"
        });

        // Clear all by sending nulls
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            dateOfBirth = (string?)null,
            weightKg = (double?)null,
            bodyFatPercentage = (double?)null,
            sportName = (string?)null,
            position = (string?)null,
            gymExperienceLevel = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("dateOfBirth").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("weightKg").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("bodyFatPercentage").ValueKind);
        Assert.Equal("", p.GetProperty("sportName").GetString());
        Assert.Equal("", p.GetProperty("position").GetString());
        Assert.Equal("", p.GetProperty("gymExperienceLevel").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  INVALID SPORT => 400
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_InvalidSport_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            sportName = "Quidditch",
            position = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Unknown sport", body);
    }

    // ════════════════════════════════════════════════════════════════
    //  INVALID POSITION FOR SPORT => 400
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_InvalidPositionForSport_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            sportName = "Soccer",
            position = "Quarterback" // not a soccer position
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Invalid position", body);
    }

    // ════════════════════════════════════════════════════════════════
    //  SPORT WITHOUT POSITIONS => POSITION CLEARED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_SportWithoutPositions_ClearsPosition()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set sport with position first
        await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            sportName = "Soccer",
            position = "Goalkeeper"
        });

        // Switch to sport without positions, sending a position (should be force-cleared)
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            sportName = "Tennis",
            position = "Goalkeeper" // should be cleared since Tennis has no positions
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("Tennis", p.GetProperty("sportName").GetString());
        Assert.Equal("", p.GetProperty("position").GetString()); // cleared
    }

    // ════════════════════════════════════════════════════════════════
    //  OMITTED COACH FIELDS REMAIN UNCHANGED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_CoachFieldsPreserved()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set coach fields via the old endpoint
        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            trainingDaysPerWeek = 5,
            preferredSessionDurationMinutes = 90,
            primaryTrainingGoal = "Strength",
            injuryHistory = "ACL 2020"
        });

        // Save athlete profile
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 85.0,
            sportName = "Basketball",
            position = "Center"
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Coach fields must survive
        var p = await GetProfileJsonAsync(c);
        Assert.Equal(85.0, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Basketball", p.GetProperty("sportName").GetString());
        Assert.Equal(5, p.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal(90, p.GetProperty("preferredSessionDurationMinutes").GetInt32());
        Assert.Equal("Strength", p.GetProperty("primaryTrainingGoal").GetString());
        Assert.Equal("ACL 2020", p.GetProperty("injuryHistory").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  VALIDATION — weight & body fat ranges
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(19.9)]
    [InlineData(400.1)]
    [InlineData(-1)]
    public async Task SaveAthleteProfile_InvalidWeight_Returns400(double weight)
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new { weightKg = weight });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task SaveAthleteProfile_InvalidBodyFat_Returns400(double bf)
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new { bodyFatPercentage = bf });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  RESPONSE CONTAINS FULL PROFILE (including coach fields)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_ResponseContainsFullProfile()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set coach fields
        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            trainingDaysPerWeek = 4,
            primaryTrainingGoal = "Hypertrophy"
        });

        // Save athlete
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 75.0,
            sportName = "Powerlifting"
        });

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        // Athlete fields in response
        Assert.Equal(75.0, body.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Powerlifting", body.GetProperty("sportName").GetString());

        // Coach fields also in response
        Assert.Equal(4, body.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal("Hypertrophy", body.GetProperty("primaryTrainingGoal").GetString());

        // Core identity fields
        Assert.False(string.IsNullOrEmpty(body.GetProperty("firstName").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("email").GetString()));
    }

    // ════════════════════════════════════════════════════════════════
    //  REJECTED SAVE DOES NOT MUTATE STATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_RejectedSave_DoesNotMutateState()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set valid state
        await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 80.0,
            sportName = "Soccer",
            position = "Goalkeeper"
        });

        // Invalid save — bad weight
        var badResp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 999.0,
            sportName = "Basketball",
            position = "Center"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badResp.StatusCode);

        // Verify original values survived
        var p = await GetProfileJsonAsync(c);
        Assert.Equal(80.0, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Soccer", p.GetProperty("sportName").GetString());
        Assert.Equal("Goalkeeper", p.GetProperty("position").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  DATE OF BIRTH ROUNDTRIP via athlete endpoint
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_DateOfBirth_RoundtripsCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            dateOfBirth = "1998-12-25"
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("1998-12-25", p.GetProperty("dateOfBirth").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  AUTH REQUIRED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_NoAuth_Returns401()
    {
        var resp = await _client.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            weightKg = 80.0
        });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  DATE OF BIRTH VALIDATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_FutureDateOfBirth_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            dateOfBirth = future.ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("future", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAthleteProfile_UnreasonablyOldDob_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            dateOfBirth = "1899-12-31"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("unreasonably", body, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  GYM EXPERIENCE LEVEL VALIDATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAthleteProfile_InvalidGymExperience_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            gymExperienceLevel = "Super Elite"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Invalid gym experience level", body);
    }

    [Fact]
    public async Task SaveAthleteProfile_ValidGymExperience_Accepted()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            gymExperienceLevel = "1-2 years"
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var p = await GetProfileJsonAsync(c);
        Assert.Equal("1-2 years", p.GetProperty("gymExperienceLevel").GetString());
    }

    [Fact]
    public async Task SaveAthleteProfile_NullGymExperience_ClearsValue()
    {
        var c = await RegisterAndAuthenticateAsync();
        // Set first
        await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            gymExperienceLevel = "5+ years"
        });
        // Clear
        var resp = await c.PutAsJsonAsync("/api/auth/profile/athlete", new
        {
            gymExperienceLevel = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var p = await GetProfileJsonAsync(c);
        Assert.Equal("", p.GetProperty("gymExperienceLevel").GetString());
    }
}
