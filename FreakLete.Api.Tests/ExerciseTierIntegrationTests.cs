using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class ExerciseTierIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    public ExerciseTierIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await SeedBenchPressDefinitionAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;

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

    private async Task<HttpClient> RegisterAndAuthWithWeightAsync(double? weightKg, string sex = "Male")
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
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
    public async Task PostPr_ReturnsTierPayload_ForStrengthExercise()
    {
        var c = await RegisterAndAuthWithWeightAsync(80);

        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            exerciseCategory = "Push",
            trackingMode = "Strength",
            weight = 100,
            reps = 5,
            rir = 1
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var tier = json.GetProperty("tier");
        Assert.Equal("benchpress", tier.GetProperty("catalogId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(tier.GetProperty("tierLevel").GetString()));
    }

    [Fact]
    public async Task PostPr_UserWithoutWeight_ReturnsNullTier()
    {
        var c = await RegisterAndAuthWithWeightAsync(null);

        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 100,
            reps = 5,
            rir = 1
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, json.GetProperty("tier").ValueKind);
    }

    [Fact]
    public async Task PostPr_WithoutCatalogId_ReturnsNullTier()
    {
        var c = await RegisterAndAuthWithWeightAsync(80);

        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 100,
            reps = 5,
            rir = 1
        });

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, json.GetProperty("tier").ValueKind);
    }

    [Fact]
    public async Task PostPr_LevelUp_SetsLeveledUpTrue()
    {
        var c = await RegisterAndAuthWithWeightAsync(80);

        // 40kg × 5 reps × 1 RIR → 1RM ≈ 48 → ratio 0.6 → Beginner
        var first = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 40, reps = 5, rir = 1
        });
        var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement;
        var firstLevel = firstJson.GetProperty("tier").GetProperty("tierLevel").GetString();

        // 120kg × 3 × 0 → 1RM ≈ 132 → ratio 1.65 → Elite
        var second = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 120, reps = 3, rir = 0
        });
        var secondTier = JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement
            .GetProperty("tier");

        Assert.Equal(firstLevel, secondTier.GetProperty("previousTierLevel").GetString());
        Assert.True(secondTier.GetProperty("leveledUp").GetBoolean());
    }

    [Fact]
    public async Task GetProfileTiers_ReturnsSnapshot()
    {
        var c = await RegisterAndAuthWithWeightAsync(80);

        await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 100, reps = 5, rir = 1
        });

        var resp = await c.GetAsync("/api/profile/tiers");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal("benchpress", arr[0].GetProperty("catalogId").GetString());
    }
}
