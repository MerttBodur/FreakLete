using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class ExerciseCatalogIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExerciseCatalogIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await SeedExerciseCatalogAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedExerciseCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ExerciseDefinitions has no FK to Users, so TRUNCATE Users CASCADE doesn't clear it
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "ExerciseDefinitions" CASCADE""");

        db.ExerciseDefinitions.AddRange(
            new ExerciseDefinition
            {
                CatalogId = "bench-press",
                Name = "Bench Press",
                DisplayName = "Bench Press",
                TurkishName = "Bench Press",
                EnglishName = "Bench Press",
                Category = "Chest",
                Force = "Push",
                Level = "Intermediate",
                Mechanic = "Compound",
                Equipment = "Barbell",
                PrimaryMusclesText = "Chest, Triceps",
                SecondaryMusclesText = "Shoulders",
                InstructionsText = "Lie on bench, press barbell up.",
                TrackingMode = "Strength",
                PrimaryLabel = "Weight",
                PrimaryUnit = "kg",
                SecondaryLabel = "Reps",
                SecondaryUnit = "",
                SupportsGroundContactTime = false,
                SupportsConcentricTime = true,
                MovementPattern = "Horizontal Push",
                AthleticQuality = "Strength",
                NervousSystemLoad = "Medium",
                SportRelevance = "General",
                GctProfile = "",
                LoadPrescription = "Progressive overload",
                CommonMistakes = "Bouncing off chest",
                Progression = "Incline Bench Press",
                Regression = "Push-ups",
                RecommendedRank = 1
            },
            new ExerciseDefinition
            {
                CatalogId = "squat",
                Name = "Barbell Squat",
                DisplayName = "Squat",
                TurkishName = "Squat",
                EnglishName = "Barbell Squat",
                Category = "Legs",
                Force = "Push",
                Level = "Intermediate",
                Mechanic = "Compound",
                Equipment = "Barbell",
                PrimaryMusclesText = "Quadriceps, Glutes",
                SecondaryMusclesText = "Hamstrings, Core",
                InstructionsText = "Stand with barbell on back, squat down.",
                TrackingMode = "Strength",
                PrimaryLabel = "Weight",
                PrimaryUnit = "kg",
                SecondaryLabel = "Reps",
                SecondaryUnit = "",
                SupportsGroundContactTime = false,
                SupportsConcentricTime = true,
                MovementPattern = "Squat",
                AthleticQuality = "Strength",
                NervousSystemLoad = "High",
                SportRelevance = "General",
                GctProfile = "",
                LoadPrescription = "Progressive overload",
                CommonMistakes = "Knees caving in",
                Progression = "Front Squat",
                Regression = "Goblet Squat",
                RecommendedRank = 1
            },
            new ExerciseDefinition
            {
                CatalogId = "depth-jump",
                Name = "Depth Jump",
                DisplayName = "Depth Jump",
                TurkishName = "Derinlik Atlayışı",
                EnglishName = "Depth Jump",
                Category = "Plyometrics",
                Force = "Push",
                Level = "Advanced",
                Mechanic = "Compound",
                Equipment = "Box",
                PrimaryMusclesText = "Quadriceps, Glutes, Calves",
                SecondaryMusclesText = "Hamstrings",
                InstructionsText = "Step off box, land and immediately jump.",
                TrackingMode = "Power",
                PrimaryLabel = "Height",
                PrimaryUnit = "cm",
                SecondaryLabel = "GCT",
                SecondaryUnit = "ms",
                SupportsGroundContactTime = true,
                SupportsConcentricTime = false,
                MovementPattern = "Vertical Jump",
                AthleticQuality = "Reactive Strength",
                NervousSystemLoad = "Very High",
                SportRelevance = "Basketball, Volleyball",
                GctProfile = "Short",
                LoadPrescription = "Low volume, high intensity",
                CommonMistakes = "Excessive ground contact time",
                Progression = "Weighted Depth Jump",
                Regression = "Box Jump",
                RecommendedRank = 3
            }
        );

        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> AuthenticateAsync()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);
        return c;
    }

    // ════════════════════════════════════════════════════════════════
    //  AUTH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/ExerciseCatalog");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET ALL
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsSeededExercises()
    {
        var client = await AuthenticateAsync();
        var response = await client.GetAsync("/api/ExerciseCatalog");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var exercises = JsonSerializer.Deserialize<List<JsonElement>>(body, JsonOpts)!;

        Assert.Equal(3, exercises.Count);
    }

    [Fact]
    public async Task GetAll_OrderedByCategoryThenRank()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>("/api/ExerciseCatalog", JsonOpts);

        // Chest < Legs < Plyometrics (alphabetical)
        Assert.Equal("Chest", exercises![0].GetProperty("category").GetString());
        Assert.Equal("Legs", exercises[1].GetProperty("category").GetString());
        Assert.Equal("Plyometrics", exercises[2].GetProperty("category").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  RESPONSE SHAPE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ResponseShape_HasAllExpectedFields()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>("/api/ExerciseCatalog", JsonOpts);

        var bench = exercises!.First(e => e.GetProperty("catalogId").GetString() == "bench-press");

        Assert.Equal("Bench Press", bench.GetProperty("name").GetString());
        Assert.Equal("Bench Press", bench.GetProperty("displayName").GetString());
        Assert.Equal("Bench Press", bench.GetProperty("turkishName").GetString());
        Assert.Equal("Bench Press", bench.GetProperty("englishName").GetString());
        Assert.Equal("Chest", bench.GetProperty("category").GetString());
        Assert.Equal("Push", bench.GetProperty("force").GetString());
        Assert.Equal("Intermediate", bench.GetProperty("level").GetString());
        Assert.Equal("Compound", bench.GetProperty("mechanic").GetString());
        Assert.Equal("Barbell", bench.GetProperty("equipment").GetString());
        Assert.Equal("Chest, Triceps", bench.GetProperty("primaryMusclesText").GetString());
        Assert.Equal("Shoulders", bench.GetProperty("secondaryMusclesText").GetString());
        Assert.Equal("Strength", bench.GetProperty("trackingMode").GetString());
        Assert.Equal("Weight", bench.GetProperty("primaryLabel").GetString());
        Assert.Equal("kg", bench.GetProperty("primaryUnit").GetString());
        Assert.False(bench.GetProperty("supportsGroundContactTime").GetBoolean());
        Assert.True(bench.GetProperty("supportsConcentricTime").GetBoolean());
        Assert.Equal("Horizontal Push", bench.GetProperty("movementPattern").GetString());
        Assert.Equal("Strength", bench.GetProperty("athleticQuality").GetString());
        Assert.Equal("Medium", bench.GetProperty("nervousSystemLoad").GetString());
        Assert.Equal(1, bench.GetProperty("recommendedRank").GetInt32());
    }

    // ════════════════════════════════════════════════════════════════
    //  BY CATEGORY
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByCategory_FiltersCorrectly()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/by-category/Chest", JsonOpts);

        var single = Assert.Single(exercises!);
        Assert.Equal("bench-press", single.GetProperty("catalogId").GetString());
    }

    [Fact]
    public async Task GetByCategory_NonExistent_ReturnsEmptyList()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/by-category/NonExistentCategory", JsonOpts);

        Assert.Empty(exercises!);
    }

    // ════════════════════════════════════════════════════════════════
    //  SEARCH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Search_ByName_FindsMatch()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/search?q=bench", JsonOpts);

        var single = Assert.Single(exercises!);
        Assert.Equal("bench-press", single.GetProperty("catalogId").GetString());
    }

    [Fact]
    public async Task Search_ByMuscle_FindsMatch()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/search?q=quadriceps", JsonOpts);

        Assert.Equal(2, exercises!.Count); // Squat + Depth Jump both have quadriceps
    }

    [Fact]
    public async Task Search_WithCategoryFilter_NarrowsResults()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/search?q=quadriceps&category=Legs", JsonOpts);

        var single = Assert.Single(exercises!);
        Assert.Equal("squat", single.GetProperty("catalogId").GetString());
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/search?q=zzzznonexistent", JsonOpts);

        Assert.Empty(exercises!);
    }

    [Fact]
    public async Task Search_CaseInsensitive()
    {
        var client = await AuthenticateAsync();
        var exercises = await client.GetFromJsonAsync<List<JsonElement>>(
            "/api/ExerciseCatalog/search?q=BENCH", JsonOpts);

        Assert.Single(exercises!);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET BY ID
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_ReturnsExercise()
    {
        var client = await AuthenticateAsync();
        var response = await client.GetAsync("/api/ExerciseCatalog/squat");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var exercise = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        Assert.Equal("squat", exercise.GetProperty("catalogId").GetString());
        Assert.Equal("Barbell Squat", exercise.GetProperty("name").GetString());
        Assert.Equal("Legs", exercise.GetProperty("category").GetString());
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var client = await AuthenticateAsync();
        var response = await client.GetAsync("/api/ExerciseCatalog/nonexistent-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  TRACKING MODE VARIANTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PowerExercise_HasCorrectTrackingFields()
    {
        var client = await AuthenticateAsync();
        var response = await client.GetAsync("/api/ExerciseCatalog/depth-jump");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var exercise = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        Assert.Equal("Power", exercise.GetProperty("trackingMode").GetString());
        Assert.True(exercise.GetProperty("supportsGroundContactTime").GetBoolean());
        Assert.False(exercise.GetProperty("supportsConcentricTime").GetBoolean());
        Assert.Equal("cm", exercise.GetProperty("primaryUnit").GetString());
    }
}
