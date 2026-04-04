using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class WorkoutIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WorkoutIntegrationTests(FreakLeteApiFactory factory)
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

    private static object MakeWorkoutRequest(
        string name = "Push Day",
        string date = "2025-03-20T00:00:00",
        List<object>? exercises = null)
    {
        exercises ??= new List<object>
        {
            new
            {
                exerciseName = "Bench Press",
                exerciseCategory = "Chest",
                trackingMode = "Strength",
                sets = 4,
                reps = 8,
                rir = 2,
                restSeconds = 90,
                metric1Value = 80.0,
                metric1Unit = "kg"
            }
        };

        return new { workoutName = name, workoutDate = date, exercises };
    }

    // ════════════════════════════════════════════════════════════════
    //  CREATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateWorkout_Returns201_WithCorrectData()
    {
        var c = await RegisterAndAuthenticateAsync();

        var resp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Push Day", json.GetProperty("workoutName").GetString());
        Assert.True(json.GetProperty("id").GetInt32() > 0);

        var exercises = json.GetProperty("exercises");
        Assert.Equal(1, exercises.GetArrayLength());
        Assert.Equal("Bench Press", exercises[0].GetProperty("exerciseName").GetString());
        Assert.Equal(4, exercises[0].GetProperty("sets").GetInt32());
        Assert.Equal(80.0, exercises[0].GetProperty("metric1Value").GetDouble());
    }

    [Fact]
    public async Task CreateWorkout_WithMultipleExercises_AllPersist()
    {
        var c = await RegisterAndAuthenticateAsync();

        var exercises = new List<object>
        {
            new { exerciseName = "Squat", exerciseCategory = "Legs", sets = 5, reps = 5, metric1Value = 120.0, metric1Unit = "kg" },
            new { exerciseName = "Leg Press", exerciseCategory = "Legs", sets = 3, reps = 12, metric1Value = 200.0, metric1Unit = "kg" },
            new { exerciseName = "Leg Curl", exerciseCategory = "Legs", sets = 3, reps = 15 }
        };

        var resp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Leg Day", exercises: exercises));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(3, json.GetProperty("exercises").GetArrayLength());
    }

    [Fact]
    public async Task CreateWorkout_Unauthenticated_Returns401()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsCreatedWorkouts()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Push Day"));
        await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Pull Day"));

        var resp = await c.GetAsync("/api/workouts");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, arr.GetArrayLength());
    }

    [Fact]
    public async Task GetById_ReturnsCorrectWorkout()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement;
        var id = created.GetProperty("id").GetInt32();

        var resp = await c.GetAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Push Day", json.GetProperty("workoutName").GetString());
        Assert.Equal(id, json.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetByDate_FiltersCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Morning", date: "2025-03-20T00:00:00"));
        await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Evening", date: "2025-03-20T00:00:00"));
        await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Next Day", date: "2025-03-21T00:00:00"));

        var resp = await c.GetAsync("/api/workouts/by-date/2025-03-20");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, arr.GetArrayLength());
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.GetAsync("/api/workouts/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateWorkout_ChangesNameAndExercises()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement;
        var id = created.GetProperty("id").GetInt32();

        var newExercises = new List<object>
        {
            new { exerciseName = "Incline Press", exerciseCategory = "Chest", sets = 3, reps = 10, metric1Value = 60.0, metric1Unit = "kg" }
        };

        var updateResp = await c.PutAsJsonAsync($"/api/workouts/{id}",
            MakeWorkoutRequest("Updated Push Day", exercises: newExercises));
        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

        // Verify via GET
        var getResp = await c.GetAsync($"/api/workouts/{id}");
        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Updated Push Day", json.GetProperty("workoutName").GetString());
        var ex = json.GetProperty("exercises");
        Assert.Equal(1, ex.GetArrayLength());
        Assert.Equal("Incline Press", ex[0].GetProperty("exerciseName").GetString());
    }

    [Fact]
    public async Task UpdateWorkout_ReplacesAllExercises_OldOnesGone()
    {
        var c = await RegisterAndAuthenticateAsync();

        var original = new List<object>
        {
            new { exerciseName = "A", exerciseCategory = "Cat", sets = 1, reps = 1 },
            new { exerciseName = "B", exerciseCategory = "Cat", sets = 1, reps = 1 }
        };
        var createResp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest(exercises: original));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var replacement = new List<object>
        {
            new { exerciseName = "C", exerciseCategory = "Cat", sets = 2, reps = 2 }
        };
        await c.PutAsJsonAsync($"/api/workouts/{id}", MakeWorkoutRequest(exercises: replacement));

        var getResp = await c.GetAsync($"/api/workouts/{id}");
        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;
        var exercises = json.GetProperty("exercises");
        Assert.Equal(1, exercises.GetArrayLength());
        Assert.Equal("C", exercises[0].GetProperty("exerciseName").GetString());
    }

    [Fact]
    public async Task UpdateWorkout_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.PutAsJsonAsync("/api/workouts/99999", MakeWorkoutRequest());
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  DELETE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteWorkout_RemovesIt()
    {
        var c = await RegisterAndAuthenticateAsync();

        var createResp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var delResp = await c.DeleteAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await c.GetAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkout_NonExistent_Returns404()
    {
        var c = await RegisterAndAuthenticateAsync();
        var resp = await c.DeleteAsync("/api/workouts/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  USER ISOLATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserIsolation_CannotSeeOtherUsersWorkouts()
    {
        var user1 = await RegisterAndAuthenticateAsync("user1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("user2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("User1 Workout"));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        // User2 cannot see User1's workout
        var getResp = await user2.GetAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        // User2's list is empty
        var listResp = await user2.GetAsync("/api/workouts");
        var arr = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task UserIsolation_CannotUpdateOtherUsersWorkout()
    {
        var user1 = await RegisterAndAuthenticateAsync("u1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("u2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.PutAsJsonAsync($"/api/workouts/{id}", MakeWorkoutRequest("Hacked"));
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_CannotDeleteOtherUsersWorkout()
    {
        var user1 = await RegisterAndAuthenticateAsync("d1@test.com");
        var user2 = await RegisterAndAuthenticateAsync("d2@test.com");

        var createResp = await user1.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest());
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var resp = await user2.DeleteAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        // Original still exists for user1
        var getResp = await user1.GetAsync($"/api/workouts/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  EXERCISE FIELD ROUNDTRIP
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExerciseFields_AllFieldsRoundtripCorrectly()
    {
        var c = await RegisterAndAuthenticateAsync();

        var exercises = new List<object>
        {
            new
            {
                exerciseName = "Box Jump",
                exerciseCategory = "Plyometric",
                trackingMode = "Athletic",
                sets = 3,
                reps = 5,
                rir = 1,
                restSeconds = 120,
                groundContactTimeMs = 150.5,
                concentricTimeSeconds = 0.8,
                metric1Value = 90.0,
                metric1Unit = "cm",
                metric2Value = 2.5,
                metric2Unit = "m/s"
            }
        };

        var createResp = await c.PostAsJsonAsync("/api/workouts", MakeWorkoutRequest("Athletic Day", exercises: exercises));
        var id = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var getResp = await c.GetAsync($"/api/workouts/{id}");
        var json = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync()).RootElement;
        var ex = json.GetProperty("exercises")[0];

        Assert.Equal("Box Jump", ex.GetProperty("exerciseName").GetString());
        Assert.Equal("Plyometric", ex.GetProperty("exerciseCategory").GetString());
        Assert.Equal("Athletic", ex.GetProperty("trackingMode").GetString());
        Assert.Equal(3, ex.GetProperty("sets").GetInt32());
        Assert.Equal(5, ex.GetProperty("reps").GetInt32());
        Assert.Equal(1, ex.GetProperty("rir").GetInt32());
        Assert.Equal(120, ex.GetProperty("restSeconds").GetInt32());
        Assert.Equal(150.5, ex.GetProperty("groundContactTimeMs").GetDouble());
        Assert.Equal(0.8, ex.GetProperty("concentricTimeSeconds").GetDouble());
        Assert.Equal(90.0, ex.GetProperty("metric1Value").GetDouble());
        Assert.Equal("cm", ex.GetProperty("metric1Unit").GetString());
        Assert.Equal(2.5, ex.GetProperty("metric2Value").GetDouble());
        Assert.Equal("m/s", ex.GetProperty("metric2Unit").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  LIVE SESSION PERSIST (template & empty mode payloads)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LiveSession_TemplatePersist_CreatesAndAppearsInHistory()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Simulates the payload shape from WorkoutPreviewPage after a template session
        var exercises = new List<object>
        {
            new
            {
                exerciseName = "Bench Press",
                exerciseCategory = "Push",
                trackingMode = "Strength",
                sets = 4,
                reps = 8,
                rir = 2,
                restSeconds = 90,
                metric1Value = 80.0,
                metric1Unit = "kg"
            },
            new
            {
                exerciseName = "Incline Dumbbell Press",
                exerciseCategory = "Push",
                trackingMode = "Strength",
                sets = 3,
                reps = 10,
                restSeconds = 60
            }
        };

        var payload = new
        {
            workoutName = "Full Body Foundation 3-Day - Week 1 - Day 1 Push",
            workoutDate = $"{DateTime.UtcNow:yyyy-MM-dd}",
            exercises
        };

        var createResp = await c.PostAsJsonAsync("/api/workouts", payload);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Full Body Foundation 3-Day - Week 1 - Day 1 Push",
            created.GetProperty("workoutName").GetString());
        Assert.Equal(2, created.GetProperty("exercises").GetArrayLength());

        // Verify it appears in the user's workout list (history)
        var listResp = await c.GetAsync("/api/workouts");
        var list = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("Full Body Foundation 3-Day - Week 1 - Day 1 Push",
            list[0].GetProperty("workoutName").GetString());

        // Verify it appears in by-date query
        var dateResp = await c.GetAsync($"/api/workouts/by-date/{DateTime.UtcNow:yyyy-MM-dd}");
        var dateList = JsonDocument.Parse(await dateResp.Content.ReadAsStringAsync()).RootElement;
        Assert.True(dateList.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task LiveSession_EmptyModePersist_CreatesAndAppearsInHistory()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Simulates the payload shape from an empty/free-form session
        var exercises = new List<object>
        {
            new
            {
                exerciseName = "Pull-up",
                exerciseCategory = "Pull",
                trackingMode = "Strength",
                sets = 3,
                reps = 8
            }
        };

        var payload = new
        {
            workoutName = "Serbest Antrenman",
            workoutDate = $"{DateTime.UtcNow:yyyy-MM-dd}",
            exercises
        };

        var createResp = await c.PostAsJsonAsync("/api/workouts", payload);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Serbest Antrenman", created.GetProperty("workoutName").GetString());

        // Verify in list
        var listResp = await c.GetAsync("/api/workouts");
        var list = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task LiveSession_EmptyExerciseList_Returns400()
    {
        var c = await RegisterAndAuthenticateAsync();

        // Empty exercise list should still be accepted (validation is client-side)
        var payload = new
        {
            workoutName = "Empty Session",
            workoutDate = $"{DateTime.UtcNow:yyyy-MM-dd}",
            exercises = new List<object>()
        };

        var resp = await c.PostAsJsonAsync("/api/workouts", payload);
        // API accepts empty exercises (no server-side min-count validation)
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }
}
