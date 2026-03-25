using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class TrainingProgramIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TrainingProgramIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(HttpClient Client, int UserId)> RegisterAndAuthenticateAsync(string? email = null)
    {
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email);
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);
        return (c, auth.UserId);
    }

    /// <summary>
    /// Inserts a training program with a full hierarchy directly into the DB.
    /// Returns the created program's Id.
    /// </summary>
    private async Task<int> SeedProgramAsync(
        int userId,
        string name = "Strength Block",
        string status = "active",
        string goal = "Build strength",
        int daysPerWeek = 4,
        bool includeWeeks = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var program = new TrainingProgram
        {
            UserId = userId,
            Name = name,
            Description = "A 4-week strength block",
            Goal = goal,
            DaysPerWeek = daysPerWeek,
            SessionDurationMinutes = 60,
            Status = status,
            Sport = "Powerlifting",
            Position = "",
            Notes = "Focus on compound lifts",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (includeWeeks)
        {
            program.Weeks = new List<ProgramWeek>
            {
                new()
                {
                    WeekNumber = 1,
                    Focus = "Hypertrophy",
                    IsDeload = false,
                    Sessions = new List<ProgramSession>
                    {
                        new()
                        {
                            DayNumber = 1,
                            SessionName = "Upper Body A",
                            Focus = "Push emphasis",
                            Notes = "Start with bench",
                            Exercises = new List<ProgramExercise>
                            {
                                new()
                                {
                                    Order = 1,
                                    ExerciseName = "Bench Press",
                                    ExerciseCategory = "Chest",
                                    Sets = 4,
                                    RepsOrDuration = "6-8",
                                    IntensityGuidance = "75% 1RM",
                                    RestSeconds = 180,
                                    Notes = "Pause at bottom",
                                    SupersetGroup = ""
                                },
                                new()
                                {
                                    Order = 2,
                                    ExerciseName = "Barbell Row",
                                    ExerciseCategory = "Back",
                                    Sets = 4,
                                    RepsOrDuration = "8-10",
                                    IntensityGuidance = "RPE 7",
                                    RestSeconds = 120,
                                    Notes = "",
                                    SupersetGroup = ""
                                }
                            }
                        },
                        new()
                        {
                            DayNumber = 2,
                            SessionName = "Lower Body A",
                            Focus = "Squat emphasis",
                            Notes = "",
                            Exercises = new List<ProgramExercise>
                            {
                                new()
                                {
                                    Order = 1,
                                    ExerciseName = "Barbell Squat",
                                    ExerciseCategory = "Legs",
                                    Sets = 5,
                                    RepsOrDuration = "5",
                                    IntensityGuidance = "80% 1RM",
                                    RestSeconds = 240,
                                    Notes = "Belt up",
                                    SupersetGroup = ""
                                }
                            }
                        }
                    }
                },
                new()
                {
                    WeekNumber = 2,
                    Focus = "Deload",
                    IsDeload = true,
                    Sessions = new List<ProgramSession>()
                }
            };
        }

        db.TrainingPrograms.Add(program);
        await db.SaveChangesAsync();
        return program.Id;
    }

    // ════════════════════════════════════════════════════════════════
    //  AUTH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/TrainingProgram");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  LIST
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_NoProgramsExist_ReturnsEmptyList()
    {
        var (client, _) = await RegisterAndAuthenticateAsync();
        var programs = await client.GetFromJsonAsync<List<JsonElement>>("/api/TrainingProgram", JsonOpts);
        Assert.NotNull(programs);
        Assert.Empty(programs);
    }

    [Fact]
    public async Task GetAll_ReturnsUserPrograms()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();
        await SeedProgramAsync(userId, "Program A", "active");
        await SeedProgramAsync(userId, "Program B", "draft");

        var programs = await client.GetFromJsonAsync<List<JsonElement>>("/api/TrainingProgram", JsonOpts);
        Assert.Equal(2, programs!.Count);
    }

    [Fact]
    public async Task GetAll_ListResponseShape_HasExpectedFields()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();
        await SeedProgramAsync(userId);

        var programs = await client.GetFromJsonAsync<List<JsonElement>>("/api/TrainingProgram", JsonOpts);
        var first = programs![0];

        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("goal", out _));
        Assert.True(first.TryGetProperty("status", out _));
        Assert.True(first.TryGetProperty("daysPerWeek", out _));
        Assert.True(first.TryGetProperty("createdAt", out _));
    }

    // ════════════════════════════════════════════════════════════════
    //  GET BY ID
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_ReturnsFullHierarchy()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();
        var programId = await SeedProgramAsync(userId);

        var response = await client.GetAsync($"/api/TrainingProgram/{programId}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var program = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        // Top-level fields
        Assert.Equal("Strength Block", program.GetProperty("name").GetString());
        Assert.Equal("Build strength", program.GetProperty("goal").GetString());
        Assert.Equal("active", program.GetProperty("status").GetString());
        Assert.Equal(4, program.GetProperty("daysPerWeek").GetInt32());
        Assert.Equal(60, program.GetProperty("sessionDurationMinutes").GetInt32());
        Assert.Equal("Powerlifting", program.GetProperty("sport").GetString());
        Assert.Equal("Focus on compound lifts", program.GetProperty("notes").GetString());

        // Weeks
        var weeks = program.GetProperty("weeks").EnumerateArray().ToList();
        Assert.Equal(2, weeks.Count);

        var week1 = weeks[0];
        Assert.Equal(1, week1.GetProperty("weekNumber").GetInt32());
        Assert.Equal("Hypertrophy", week1.GetProperty("focus").GetString());
        Assert.False(week1.GetProperty("isDeload").GetBoolean());

        // Sessions in week 1
        var sessions = week1.GetProperty("sessions").EnumerateArray().ToList();
        Assert.Equal(2, sessions.Count);

        var session1 = sessions[0];
        Assert.Equal(1, session1.GetProperty("dayNumber").GetInt32());
        Assert.Equal("Upper Body A", session1.GetProperty("sessionName").GetString());

        // Exercises in session 1
        var exercises = session1.GetProperty("exercises").EnumerateArray().ToList();
        Assert.Equal(2, exercises.Count);

        var ex1 = exercises[0];
        Assert.Equal(1, ex1.GetProperty("order").GetInt32());
        Assert.Equal("Bench Press", ex1.GetProperty("exerciseName").GetString());
        Assert.Equal("Chest", ex1.GetProperty("exerciseCategory").GetString());
        Assert.Equal(4, ex1.GetProperty("sets").GetInt32());
        Assert.Equal("6-8", ex1.GetProperty("repsOrDuration").GetString());
        Assert.Equal("75% 1RM", ex1.GetProperty("intensityGuidance").GetString());
        Assert.Equal(180, ex1.GetProperty("restSeconds").GetInt32());
        Assert.Equal("Pause at bottom", ex1.GetProperty("notes").GetString());

        // Week 2 is deload
        var week2 = weeks[1];
        Assert.Equal(2, week2.GetProperty("weekNumber").GetInt32());
        Assert.True(week2.GetProperty("isDeload").GetBoolean());
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var (client, _) = await RegisterAndAuthenticateAsync();
        var response = await client.GetAsync("/api/TrainingProgram/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET ACTIVE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActive_ReturnsActiveProgram()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();
        await SeedProgramAsync(userId, "Draft Program", "draft");
        var activeId = await SeedProgramAsync(userId, "Active Program", "active");

        var response = await client.GetAsync("/api/TrainingProgram/active");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var program = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        Assert.Equal("Active Program", program.GetProperty("name").GetString());
        Assert.Equal("active", program.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetActive_NoActiveProgram_Returns404()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();
        await SeedProgramAsync(userId, "Draft Only", "draft");

        var response = await client.GetAsync("/api/TrainingProgram/active");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetActive_NoProgramsAtAll_Returns404()
    {
        var (client, _) = await RegisterAndAuthenticateAsync();
        var response = await client.GetAsync("/api/TrainingProgram/active");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetActive_IncludesFullHierarchy()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();
        await SeedProgramAsync(userId);

        var response = await client.GetAsync("/api/TrainingProgram/active");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var program = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        // Active endpoint should include weeks/sessions/exercises
        var weeks = program.GetProperty("weeks").EnumerateArray().ToList();
        Assert.True(weeks.Count > 0);

        var sessions = weeks[0].GetProperty("sessions").EnumerateArray().ToList();
        Assert.True(sessions.Count > 0);

        var exercises = sessions[0].GetProperty("exercises").EnumerateArray().ToList();
        Assert.True(exercises.Count > 0);
    }

    // ════════════════════════════════════════════════════════════════
    //  USER ISOLATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserIsolation_CannotSeeOtherUsersPrograms()
    {
        var (_, user1Id) = await RegisterAndAuthenticateAsync("user1@test.com");
        await SeedProgramAsync(user1Id, "User1 Program");

        var (client2, _) = await RegisterAndAuthenticateAsync("user2@test.com");
        var programs = await client2.GetFromJsonAsync<List<JsonElement>>("/api/TrainingProgram", JsonOpts);

        Assert.Empty(programs!);
    }

    [Fact]
    public async Task UserIsolation_CannotGetOtherUsersProgramById()
    {
        var (_, user1Id) = await RegisterAndAuthenticateAsync("user1b@test.com");
        var programId = await SeedProgramAsync(user1Id, "User1 Program");

        var (client2, _) = await RegisterAndAuthenticateAsync("user2b@test.com");
        var response = await client2.GetAsync($"/api/TrainingProgram/{programId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_CannotSeeOtherUsersActiveProgram()
    {
        var (_, user1Id) = await RegisterAndAuthenticateAsync("user1c@test.com");
        await SeedProgramAsync(user1Id, "User1 Active", "active");

        var (client2, _) = await RegisterAndAuthenticateAsync("user2c@test.com");
        var response = await client2.GetAsync("/api/TrainingProgram/active");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  ORDERING
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_OrderedByUpdatedAtDescending()
    {
        var (client, userId) = await RegisterAndAuthenticateAsync();

        // Seed with different UpdatedAt times
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.TrainingPrograms.AddRange(
                new TrainingProgram
                {
                    UserId = userId, Name = "Old Program", Description = "", Goal = "Old",
                    Status = "completed", Sport = "", Position = "", Notes = "",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new TrainingProgram
                {
                    UserId = userId, Name = "New Program", Description = "", Goal = "New",
                    Status = "active", Sport = "", Position = "", Notes = "",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            );
            await db.SaveChangesAsync();
        }

        var programs = await client.GetFromJsonAsync<List<JsonElement>>("/api/TrainingProgram", JsonOpts);

        Assert.Equal("New Program", programs![0].GetProperty("name").GetString());
        Assert.Equal("Old Program", programs[1].GetProperty("name").GetString());
    }
}
