using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class PrEntryIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    public PrEntryIntegrationTests(FreakLeteApiFactory factory)
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

    private static object MakeStrengthPr(
        string name = "Bench Press",
        string category = "Chest",
        int weight = 100,
        int reps = 5,
        int? rir = 1) =>
        new
        {
            exerciseName = name,
            exerciseCategory = category,
            trackingMode = "Strength",
            weight,
            reps,
            rir
        };

    private static object MakeAthleticPr(
        string name = "Vertical Jump",
        string category = "Plyometric") =>
        new
        {
            exerciseName = name,
            exerciseCategory = category,
            trackingMode = "Athletic",
            weight = 0,
            reps = 0,
            metric1Value = 85.0,
            metric1Unit = "cm",
            metric2Value = 3.2,
            metric2Unit = "m/s",
            groundContactTimeMs = 140.0,
            concentricTimeSeconds = 0.65
        };

    // ════════════════════════════════════════════════════════════════
    //  CREATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreatePr_Returns201_WithCorrectData()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr());
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Bench Press", json.GetProperty("exerciseName").GetString());
        Assert.Equal(100, json.GetProperty("weight").GetInt32());
        Assert.Equal(5, json.GetProperty("reps").GetInt32());
        Assert.True(json.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task CreatePr_Unauthenticated_Returns401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr());
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  LIST
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListPrs_ReturnsAllForUser()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr("Bench Press"));
        await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr("Squat", "Legs", 140, 3));
        await c.PostAsJsonAsync("/api/pr-entries", MakeAthleticPr());

        var resp = await c.GetAsync("/api/pr-entries");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(3, arr.GetArrayLength());
    }

    [Fact]
    public async Task GetPrById_ReturnsCorrectEntry()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr("Deadlift", "Back", 180, 1));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await c.GetAsync($"/api/pr-entries/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Deadlift", json.GetProperty("exerciseName").GetString());
        Assert.Equal(180, json.GetProperty("weight").GetInt32());
    }

    // ════════════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdatePr_ChangesFields()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr("Bench Press", weight: 100, reps: 5));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var updateResp = await c.PutAsJsonAsync($"/api/pr-entries/{id}",
            MakeStrengthPr("Bench Press", weight: 110, reps: 3, rir: 0));
        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

        var getResp = await c.GetAsync($"/api/pr-entries/{id}");
        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(110, json.GetProperty("weight").GetInt32());
        Assert.Equal(3, json.GetProperty("reps").GetInt32());
        Assert.Equal(0, json.GetProperty("rir").GetInt32());
    }

    [Fact]
    public async Task UpdatePr_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/pr-entries/99999", MakeStrengthPr());
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  DELETE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeletePr_RemovesIt()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var delResp = await c.DeleteAsync($"/api/pr-entries/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await c.GetAsync($"/api/pr-entries/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeletePr_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.DeleteAsync("/api/pr-entries/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  STRENGTH vs ATHLETIC METRIC BEHAVIOR
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task StrengthPr_WeightAndReps_Roundtrip()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr("Squat", "Legs", 160, 3, rir: 2));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var getResp = await c.GetAsync($"/api/pr-entries/{id}");
        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("Strength", json.GetProperty("trackingMode").GetString());
        Assert.Equal(160, json.GetProperty("weight").GetInt32());
        Assert.Equal(3, json.GetProperty("reps").GetInt32());
        Assert.Equal(2, json.GetProperty("rir").GetInt32());
    }

    [Fact]
    public async Task AthleticPr_MetricFields_Roundtrip()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/pr-entries", MakeAthleticPr());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var getResp = await c.GetAsync($"/api/pr-entries/{id}");
        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("Athletic", json.GetProperty("trackingMode").GetString());
        Assert.Equal(85.0, json.GetProperty("metric1Value").GetDouble());
        Assert.Equal("cm", json.GetProperty("metric1Unit").GetString());
        Assert.Equal(3.2, json.GetProperty("metric2Value").GetDouble());
        Assert.Equal("m/s", json.GetProperty("metric2Unit").GetString());
        Assert.Equal(140.0, json.GetProperty("groundContactTimeMs").GetDouble());
        Assert.Equal(0.65, json.GetProperty("concentricTimeSeconds").GetDouble());
    }

    // ════════════════════════════════════════════════════════════════
    //  USER ISOLATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserIsolation_CannotSeeOtherUsersPrs()
    {
        var user1 = await RegisterAndAuthenticateAsync("pr1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("pr2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var getResp = await user2.GetAsync($"/api/pr-entries/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        var listResp = await user2.GetAsync("/api/pr-entries");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task UserIsolation_CannotUpdateOtherUsersPr()
    {
        var user1 = await RegisterAndAuthenticateAsync("pru1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("pru2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.PutAsJsonAsync($"/api/pr-entries/{id}", MakeStrengthPr(weight: 999));
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_CannotDeleteOtherUsersPr()
    {
        var user1 = await RegisterAndAuthenticateAsync("prd1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("prd2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/pr-entries", MakeStrengthPr());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.DeleteAsync($"/api/pr-entries/{id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        // Original still exists
        var getResp = await user1.GetAsync($"/api/pr-entries/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  TIER NEXT-MILESTONE FIELDS
    // ════════════════════════════════════════════════════════════════

    private async Task SeedBenchPressDefinitionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.ExerciseDefinitions.Any(d => d.CatalogId == "benchpress"))
        {
            db.ExerciseDefinitions.Add(new ExerciseDefinition
            {
                CatalogId = "benchpress",
                Name = "Bench Press",
                DisplayName = "Bench Press",
                Category = "Push",
                Mechanic = "compound",
                TrackingMode = "Strength",
                TierType = "StrengthRatio",
                TierThresholdsMale = "[0.5,1.0,1.25,1.5,1.75]",
                TierThresholdsFemale = "[0.35,0.7,0.9,1.1,1.35]"
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task<HttpClient> RegisterWithWeightAsync(double weightKg, string sex = "Male", string? email = null)
    {
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = db.Users.Single(x => x.Id == auth.UserId);
            u.WeightKg = weightKg;
            u.Sex = sex;
            await db.SaveChangesAsync();
        }
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);
        return c;
    }

    [Fact]
    public async Task CreatePr_FirstTimeTierEligible_ReturnsPopulatedTierWithNullPrevious()
    {
        await SeedBenchPressDefinitionAsync();
        var c = await RegisterWithWeightAsync(80.0);

        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            exerciseCategory = "Push",
            trackingMode = "Strength",
            weight = 80,
            reps = 5,
            rir = 1
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var tier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tier");

        Assert.Equal(JsonValueKind.Null, tier.GetProperty("previousTierLevel").ValueKind);
        Assert.False(tier.GetProperty("leveledUp").GetBoolean());
        Assert.False(string.IsNullOrEmpty(tier.GetProperty("tierLevel").GetString()));
        Assert.False(string.IsNullOrEmpty(tier.GetProperty("nextLevel").GetString()));
        Assert.True(tier.GetProperty("nextDelta").GetDouble() >= 0);
        Assert.Equal("Strength", tier.GetProperty("trackingMode").GetString());
    }

    [Fact]
    public async Task CreatePr_CrossesThreshold_ReturnsLeveledUpTrue()
    {
        await SeedBenchPressDefinitionAsync();
        var c = await RegisterWithWeightAsync(80.0);

        // ~60kg x5 @1 → 1RM ≈ 72 → ratio 0.9 → Beginner
        await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 60, reps = 5, rir = 1
        });

        // 100kg x5 @1 → 1RM ≈ 120 → ratio 1.5 → Elite
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 100, reps = 5, rir = 1
        });
        var tier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tier");
        Assert.True(tier.GetProperty("leveledUp").GetBoolean());
    }

    [Fact]
    public async Task CreatePr_SameBand_LeveledUpFalseWithPreviousSet()
    {
        await SeedBenchPressDefinitionAsync();
        var c = await RegisterWithWeightAsync(80.0);

        // 50kg x5 @1 → 1RM ≈ 60 → ratio 0.75 → Beginner
        await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 50, reps = 5, rir = 1
        });
        // 60kg x5 @1 → 1RM ≈ 72 → ratio 0.9 → still Beginner
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 60, reps = 5, rir = 1
        });
        var tier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tier");
        Assert.Equal("Beginner", tier.GetProperty("tierLevel").GetString());
        Assert.False(tier.GetProperty("leveledUp").GetBoolean());
    }

    [Fact]
    public async Task CreatePr_AtFreakTier_NullNextLevelAndFullProgress()
    {
        await SeedBenchPressDefinitionAsync();
        var c = await RegisterWithWeightAsync(80.0);

        // 200kg x1 @0 → 1RM = 200 → ratio 2.5 → Freak
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 200, reps = 1, rir = 0
        });
        var tier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tier");
        Assert.Equal("Freak", tier.GetProperty("tierLevel").GetString());
        Assert.Equal(JsonValueKind.Null, tier.GetProperty("nextLevel").ValueKind);
        Assert.Equal(JsonValueKind.Null, tier.GetProperty("nextDelta").ValueKind);
        Assert.Equal(100, tier.GetProperty("progressPercent").GetDouble());
    }

    [Fact]
    public async Task CreatePr_NonEligibleExercise_TierIsNull()
    {
        await SeedBenchPressDefinitionAsync();
        var c = await RegisterWithWeightAsync(80.0);

        // "dumbbellcurl" has no ExerciseDefinition in the test DB → no tier config → null tier
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "dumbbellcurl",
            exerciseName = "Dumbbell Curl",
            exerciseCategory = "Pull",
            trackingMode = "Strength",
            weight = 20, reps = 10, rir = 1
        });
        var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("tier").ValueKind);
    }
}
