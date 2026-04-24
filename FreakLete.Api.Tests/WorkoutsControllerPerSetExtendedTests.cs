using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

/// <summary>
/// Integration tests verifying that per-set RIR, RestSeconds, and ConcentricTimeSeconds
/// are persisted and returned correctly, and that entry-level legacy fields are derived
/// from the last set.
/// </summary>
[Collection("Api")]
public class WorkoutsControllerPerSetExtendedTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WorkoutsControllerPerSetExtendedTests(FreakLeteApiFactory factory)
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

    [Fact]
    public async Task PerSetFields_RoundtripCorrectly_AndLegacyDerivedFromLastSet()
    {
        // Arrange
        var c = await RegisterAndAuthenticateAsync();

        var payload = new
        {
            workoutName = "Per-Set Test Workout",
            workoutDate = "2026-04-24T00:00:00",
            exercises = new[]
            {
                new
                {
                    exerciseName = "Squat",
                    exerciseCategory = "Legs",
                    trackingMode = "Strength",
                    setsCount = 3,
                    reps = 5,
                    metric1Unit = "kg",
                    sets = new[]
                    {
                        new { setNumber = 1, reps = 5, weight = 90.0,  rir = 3, restSeconds = 90,  concentricTimeSeconds = 1.5 },
                        new { setNumber = 2, reps = 5, weight = 100.0, rir = 2, restSeconds = 120, concentricTimeSeconds = 1.4 },
                        new { setNumber = 3, reps = 5, weight = 110.0, rir = 1, restSeconds = 150, concentricTimeSeconds = 1.2 }
                    }
                }
            }
        };

        // Act
        var createResp = await c.PostAsJsonAsync("/api/workouts", payload);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var json = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement;
        var ex = json.GetProperty("exercises")[0];

        // Assert — per-set array
        var setsArr = ex.GetProperty("sets");
        Assert.Equal(3, setsArr.GetArrayLength());

        // Set 1 (index 0)
        var set0 = setsArr[0];
        Assert.Equal(3, set0.GetProperty("rir").GetInt32());
        Assert.Equal(90, set0.GetProperty("restSeconds").GetInt32());
        Assert.Equal(1.5, set0.GetProperty("concentricTimeSeconds").GetDouble());

        // Set 3 (index 2) — last set
        var set2 = setsArr[2];
        Assert.Equal(1, set2.GetProperty("rir").GetInt32());
        Assert.Equal(150, set2.GetProperty("restSeconds").GetInt32());
        Assert.Equal(1.2, set2.GetProperty("concentricTimeSeconds").GetDouble());

        // Assert — legacy entry-level fields derived from last set
        Assert.Equal(1, ex.GetProperty("rir").GetInt32());
        Assert.Equal(150, ex.GetProperty("restSeconds").GetInt32());
        Assert.Equal(1.2, ex.GetProperty("concentricTimeSeconds").GetDouble());
    }

    [Fact]
    public async Task PerSetFields_RoundtripCorrectly_ViaGetById()
    {
        // Arrange
        var c = await RegisterAndAuthenticateAsync();

        var payload = new
        {
            workoutName = "Per-Set GET Test",
            workoutDate = "2026-04-24T00:00:00",
            exercises = new[]
            {
                new
                {
                    exerciseName = "Deadlift",
                    exerciseCategory = "Back",
                    trackingMode = "Strength",
                    setsCount = 3,
                    reps = 5,
                    metric1Unit = "kg",
                    sets = new[]
                    {
                        new { setNumber = 1, reps = 5, weight = 90.0,  rir = 3, restSeconds = 90,  concentricTimeSeconds = 1.5 },
                        new { setNumber = 2, reps = 5, weight = 100.0, rir = 2, restSeconds = 120, concentricTimeSeconds = 1.4 },
                        new { setNumber = 3, reps = 5, weight = 110.0, rir = 1, restSeconds = 150, concentricTimeSeconds = 1.2 }
                    }
                }
            }
        };

        // Act — create then GET by id
        var createResp = await c.PostAsJsonAsync("/api/workouts", payload);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement;
        var id = created.GetProperty("id").GetInt32();

        var getResp = await c.GetAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;
        var ex = json.GetProperty("exercises")[0];

        // Assert — per-set array (ordered by SetNumber)
        var setsArr = ex.GetProperty("sets");
        Assert.Equal(3, setsArr.GetArrayLength());

        var set0 = setsArr[0];
        Assert.Equal(1, set0.GetProperty("setNumber").GetInt32());
        Assert.Equal(3, set0.GetProperty("rir").GetInt32());
        Assert.Equal(90, set0.GetProperty("restSeconds").GetInt32());
        Assert.Equal(1.5, set0.GetProperty("concentricTimeSeconds").GetDouble());

        var set2 = setsArr[2];
        Assert.Equal(3, set2.GetProperty("setNumber").GetInt32());
        Assert.Equal(1, set2.GetProperty("rir").GetInt32());
        Assert.Equal(150, set2.GetProperty("restSeconds").GetInt32());
        Assert.Equal(1.2, set2.GetProperty("concentricTimeSeconds").GetDouble());

        // Assert — legacy entry-level derived from last set
        Assert.Equal(1, ex.GetProperty("rir").GetInt32());
        Assert.Equal(150, ex.GetProperty("restSeconds").GetInt32());
        Assert.Equal(1.2, ex.GetProperty("concentricTimeSeconds").GetDouble());
    }
}
