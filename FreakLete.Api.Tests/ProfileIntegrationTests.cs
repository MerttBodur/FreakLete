using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class ProfileIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProfileIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Register a user & return an authenticated client.
    /// </summary>
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
    //  PERSISTENCE ROUNDTRIP — every profile field
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Roundtrip_AllProfileFields_PersistCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var updatePayload = new
        {
            firstName = "Mert",
            lastName = "Yılmaz",
            dateOfBirth = "2000-06-15",
            weightKg = 82.5,
            bodyFatPercentage = 14.2,
            sportName = "Basketball",
            position = "Point Guard",
            gymExperienceLevel = "Intermediate",
            trainingDaysPerWeek = 5,
            preferredSessionDurationMinutes = 90,
            availableEquipment = "Barbell, Dumbbells, Pull-up bar",
            physicalLimitations = "Mild lower back tightness",
            injuryHistory = "ACL surgery 2019",
            currentPainPoints = "Right knee discomfort",
            primaryTrainingGoal = "Increase vertical jump",
            secondaryTrainingGoal = "Build upper body strength",
            dietaryPreference = "High protein"
        };

        var putResp = await c.PutAsJsonAsync("/api/auth/profile", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, putResp.StatusCode);

        // Fresh GET — must reflect all values
        var p = await GetProfileJsonAsync(c);

        Assert.Equal("Mert", p.GetProperty("firstName").GetString());
        Assert.Equal("Yılmaz", p.GetProperty("lastName").GetString());
        Assert.Equal("2000-06-15", p.GetProperty("dateOfBirth").GetString());
        Assert.Equal(82.5, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal(14.2, p.GetProperty("bodyFatPercentage").GetDouble(), 0.01);
        Assert.Equal("Basketball", p.GetProperty("sportName").GetString());
        Assert.Equal("Point Guard", p.GetProperty("position").GetString());
        Assert.Equal("Intermediate", p.GetProperty("gymExperienceLevel").GetString());
        Assert.Equal(5, p.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal(90, p.GetProperty("preferredSessionDurationMinutes").GetInt32());
        Assert.Equal("Barbell, Dumbbells, Pull-up bar", p.GetProperty("availableEquipment").GetString());
        Assert.Equal("Mild lower back tightness", p.GetProperty("physicalLimitations").GetString());
        Assert.Equal("ACL surgery 2019", p.GetProperty("injuryHistory").GetString());
        Assert.Equal("Right knee discomfort", p.GetProperty("currentPainPoints").GetString());
        Assert.Equal("Increase vertical jump", p.GetProperty("primaryTrainingGoal").GetString());
        Assert.Equal("Build upper body strength", p.GetProperty("secondaryTrainingGoal").GetString());
        Assert.Equal("High protein", p.GetProperty("dietaryPreference").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  PARTIAL UPDATE PRESERVATION — omitted fields stay unchanged
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PartialUpdate_OmittedFieldsRemainUnchanged()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Step 1: set several fields
        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 80.0,
            sportName = "Football",
            position = "Striker",
            primaryTrainingGoal = "Speed"
        });

        // Step 2: update only weight — other fields must survive
        var putResp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 85.0
        });
        Assert.Equal(HttpStatusCode.NoContent, putResp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal(85.0, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Football", p.GetProperty("sportName").GetString());
        Assert.Equal("Striker", p.GetProperty("position").GetString());
        Assert.Equal("Speed", p.GetProperty("primaryTrainingGoal").GetString());
    }

    [Fact]
    public async Task PartialUpdate_DateOfBirthPreserved_WhenOtherFieldsUpdated()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            dateOfBirth = "1995-03-20",
            weightKg = 75.0
        });

        // Update only weight — DOB must survive
        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 78.0
        });

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("1995-03-20", p.GetProperty("dateOfBirth").GetString());
        Assert.Equal(78.0, p.GetProperty("weightKg").GetDouble(), 0.01);
    }

    [Fact]
    public async Task PartialUpdate_StringFieldPreserved_WhenNumericFieldUpdated()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            injuryHistory = "Torn meniscus 2022",
            currentPainPoints = "Left shoulder impingement",
            trainingDaysPerWeek = 4
        });

        // Update only training days
        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            trainingDaysPerWeek = 6
        });

        var p = await GetProfileJsonAsync(c);
        Assert.Equal(6, p.GetProperty("trainingDaysPerWeek").GetInt32());
        Assert.Equal("Torn meniscus 2022", p.GetProperty("injuryHistory").GetString());
        Assert.Equal("Left shoulder impingement", p.GetProperty("currentPainPoints").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  INVALID INPUT REJECTION — 400 for out-of-range values
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(19.9)]   // below 20
    [InlineData(400.1)]  // above 400
    [InlineData(-1)]
    [InlineData(0)]
    public async Task UpdateProfile_InvalidWeight_Returns400(double weight)
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile", new { weightKg = weight });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public async Task UpdateProfile_InvalidBodyFat_Returns400(double bf)
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile", new { bodyFatPercentage = bf });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public async Task UpdateProfile_InvalidTrainingDays_Returns400(int days)
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/auth/profile", new { trainingDaysPerWeek = days });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ValidBoundaryValues_Succeeds()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Exact boundary: weight 20 & 400, bodyFat 0 & 100, training 1 & 7
        var resp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 20.0,
            bodyFatPercentage = 0.0,
            trainingDaysPerWeek = 1
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        resp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 400.0,
            bodyFatPercentage = 100.0,
            trainingDaysPerWeek = 7
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal(400.0, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal(100.0, p.GetProperty("bodyFatPercentage").GetDouble(), 0.01);
        Assert.Equal(7, p.GetProperty("trainingDaysPerWeek").GetInt32());
    }

    // ════════════════════════════════════════════════════════════════
    //  DATE-ONLY DOB BEHAVIOR
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DateOfBirth_DateOnly_RoundtripsCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            dateOfBirth = "1998-12-25"
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        // Must come back as "1998-12-25", not a DateTime with time component
        var dob = p.GetProperty("dateOfBirth").GetString();
        Assert.Equal("1998-12-25", dob);
    }

    [Fact]
    public async Task DateOfBirth_MultipleUpdates_LatestWins()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PutAsJsonAsync("/api/auth/profile", new { dateOfBirth = "2000-01-01" });
        await c.PutAsJsonAsync("/api/auth/profile", new { dateOfBirth = "1999-06-15" });

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("1999-06-15", p.GetProperty("dateOfBirth").GetString());
    }

    [Fact]
    public async Task DateOfBirth_LeapDay_PersistsCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            dateOfBirth = "2000-02-29"
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("2000-02-29", p.GetProperty("dateOfBirth").GetString());
    }

    [Fact]
    public async Task DateOfBirth_NotCorruptedByTimezoneOrUtcNormalization()
    {
        // This is the key regression test for the DateOfBirth root-cause fix.
        // Previously DateOfBirth was DateTime/timestamptz, and NormalizeDateTimesToUtc
        // could shift the date. With DateOnly/date, there is no timezone involved.
        var c = await RegisterAndAuthenticateAsync();

        // Set DOB to a date near midnight — this used to be problematic
        var resp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            dateOfBirth = "2000-12-31"
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("2000-12-31", p.GetProperty("dateOfBirth").GetString());
    }

    [Fact]
    public async Task DateOfBirth_CombinedWithOtherFields_AllPersist()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            dateOfBirth = "1990-07-04",
            weightKg = 92.3,
            sportName = "Weightlifting",
            gymExperienceLevel = "Advanced"
        });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var p = await GetProfileJsonAsync(c);
        Assert.Equal("1990-07-04", p.GetProperty("dateOfBirth").GetString());
        Assert.Equal(92.3, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Weightlifting", p.GetProperty("sportName").GetString());
        Assert.Equal("Advanced", p.GetProperty("gymExperienceLevel").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  FRESH PROFILE — defaults for a newly registered user
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FreshProfile_ReturnsRegistrationDataAndNullOptionalFields()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client,
            firstName: "Fresh", lastName: "User");
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);

        var p = await GetProfileJsonAsync(c);

        Assert.Equal("Fresh", p.GetProperty("firstName").GetString());
        Assert.Equal("User", p.GetProperty("lastName").GetString());
        Assert.Equal(auth.Email, p.GetProperty("email").GetString());

        // Optional fields should be null or empty/default
        Assert.Equal(JsonValueKind.Null, p.GetProperty("dateOfBirth").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("weightKg").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("bodyFatPercentage").ValueKind);
        Assert.Equal("", p.GetProperty("sportName").GetString());
        Assert.Equal("", p.GetProperty("position").GetString());
        Assert.Equal("", p.GetProperty("gymExperienceLevel").GetString());
        Assert.Equal(JsonValueKind.Null, p.GetProperty("trainingDaysPerWeek").ValueKind);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("preferredSessionDurationMinutes").ValueKind);
        Assert.Equal(0, p.GetProperty("totalWorkouts").GetInt32());
        Assert.Equal(0, p.GetProperty("totalPrs").GetInt32());
    }

    // ════════════════════════════════════════════════════════════════
    //  NO FALSE-SUCCESS — rejected updates don't mutate state
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RejectedUpdate_DoesNotMutateExistingProfile()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Set valid state
        await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 80.0,
            sportName = "Running"
        });

        // Send invalid weight — should be rejected
        var badResp = await c.PutAsJsonAsync("/api/auth/profile", new
        {
            weightKg = 999.0,   // > 400 → rejected
            sportName = "Swimming"  // this should NOT persist either
        });
        Assert.Equal(HttpStatusCode.BadRequest, badResp.StatusCode);

        // Verify original values survived
        var p = await GetProfileJsonAsync(c);
        Assert.Equal(80.0, p.GetProperty("weightKg").GetDouble(), 0.01);
        Assert.Equal("Running", p.GetProperty("sportName").GetString());
    }
}
