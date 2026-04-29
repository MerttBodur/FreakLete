# FreakAI RAG Personal Coach Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Inject relevant user context into every Gemini call before tool-call loop begins, replacing reactive tool-call discovery with proactive RAG-style enrichment (structured + vector similarity).

**Architecture:** A new `ContextBuilder` runs after `IntentClassifier` and before Gemini. It returns a `FreakAiContext` DTO carrying structured user data (profile, goals, equipment, limitations, current program, recent PRs) and vector results (similar workouts + user snapshot). A background `EmbeddingBackgroundService` consumes domain events (workout saved, PR entered, profile updated, program changed) and refreshes embeddings off the hot path. `FreakAiSystemPrompt.Build(context)` prepends the context block to the existing static prompt. Failure modes degrade gracefully — embedding failure falls back to structured-only; ContextBuilder failure falls back to the static prompt.

**Tech Stack:** ASP.NET Core 10, EF Core 10, PostgreSQL with `pgvector` extension via `Pgvector.EntityFrameworkCore`, `System.Threading.Channels` for async queueing, Gemini `text-embedding-004` model (768-dim) via existing `GeminiClient`. Tests use xUnit + `WebApplicationFactory<Program>` + real test Postgres database (`freaklete_test`).

---

## Shipping Phases

The plan is organized into eight phases. Each phase is independently committable. The senior developer can pause and ship at any phase boundary.

| Phase | Outcome |
|-------|---------|
| A. Data layer | pgvector extension + entities + migration |
| B. Text formatter | Deterministic text-snapshot builders for workout / user |
| C. Gemini embed API | `GeminiClient.EmbedAsync` method |
| D. Background pipeline | Channel + sink + hosted service |
| E. ContextBuilder | Structured + vector context per intent |
| F. Orchestrator wiring | System prompt overload + intent forwarding |
| G. Domain event triggers | Controllers fire embedding events |
| H. Integration tests | End-to-end coverage |

---

## File Map

### New files

| File | Responsibility |
|------|---------------|
| `FreakLete.Api/Entities/WorkoutEmbedding.cs` | Per-workout pgvector row |
| `FreakLete.Api/Entities/UserSnapshotEmbedding.cs` | Per-user latest-snapshot pgvector row |
| `FreakLete.Api/Services/Embeddings/EmbeddingTextFormatter.cs` | Text-snapshot builders (workout / user) |
| `FreakLete.Api/Services/Embeddings/EmbeddingJob.cs` | Job record + kind enum |
| `FreakLete.Api/Services/Embeddings/EmbeddingChannel.cs` | Singleton wrapper around `Channel<EmbeddingJob>` |
| `FreakLete.Api/Services/Embeddings/IUserSnapshotEventSink.cs` | Domain event trigger for user-snapshot re-embed |
| `FreakLete.Api/Services/Embeddings/IWorkoutEmbeddingEnqueuer.cs` | Trigger for per-workout re-embed |
| `FreakLete.Api/Services/Embeddings/EmbeddingEventSink.cs` | Implements both interfaces; writes to channel |
| `FreakLete.Api/Services/Embeddings/EmbeddingBackgroundService.cs` | `BackgroundService` consuming channel |
| `FreakLete.Api/Services/Rag/FreakAiContext.cs` | Context DTO returned by ContextBuilder |
| `FreakLete.Api/Services/Rag/IContextBuilder.cs` | Public interface |
| `FreakLete.Api/Services/Rag/ContextBuilder.cs` | RAG service (structured + vector) |
| `FreakLete.Api/Migrations/<timestamp>_AddRagEmbeddings.cs` | Migration enabling pgvector + creating tables |
| `FreakLete.Api.Tests/EmbeddingTextFormatterTests.cs` | Unit tests for text snapshots |
| `FreakLete.Api.Tests/ContextBuilderTests.cs` | Unit tests for context per intent |
| `FreakLete.Api.Tests/RagIntegrationTests.cs` | End-to-end integration |

### Modified files

| File | Change |
|------|--------|
| `FreakLete.Api/FreakLete.Api.csproj` | Add `Pgvector.EntityFrameworkCore` package |
| `FreakLete.Api/Services/GeminiClient.cs` | Add `EmbeddingModel` option + `EmbedAsync(text, ct)` method |
| `FreakLete.Api/Data/AppDbContext.cs` | Add embedding DbSets + `vector(768)` mapping |
| `FreakLete.Api/Program.cs` | Register pgvector, embedding services, hosted service, ContextBuilder |
| `FreakLete.Api/Services/FreakAiSystemPrompt.cs` | Add `Build(FreakAiContext?)` overload |
| `FreakLete.Api/Services/FreakAiOrchestrator.cs` | Inject `IContextBuilder`; new optional `intent` param on `ChatAsync` |
| `FreakLete.Api/Controllers/FreakAiController.cs` | Pass classified intent to `ChatAsync` |
| `FreakLete.Api/Controllers/WorkoutsController.cs` | Fire workout + user-snapshot events on Create/Update |
| `FreakLete.Api/Controllers/PrEntriesController.cs` | Fire user-snapshot event on Create |
| `FreakLete.Api/Controllers/AuthController.cs` | Fire user-snapshot event on profile update (athlete + coach) |
| `FreakLete.Api/Controllers/TrainingProgramController.cs` | Fire user-snapshot event on status change |
| `FreakLete.Api/Services/FreakAiToolExecutor.cs` | Fire user-snapshot event on `create_program` / `adjust_program` |
| `FreakLete.Api.Tests/FreakAiIntegrationTests.cs` | Update assertions for enriched system prompt |

---

## Phase A — Data layer

### Task A1: Add Pgvector EF Core package

**Files:**
- Modify: `FreakLete.Api/FreakLete.Api.csproj`

- [ ] **Step 1: Add the package reference**

Inside the existing `<ItemGroup>` containing `PackageReference` entries, add two new lines before `</ItemGroup>`:

```xml
    <PackageReference Include="Pgvector" Version="0.3.0" />
    <PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.1" />
```

- [ ] **Step 2: Restore packages**

Run: `dotnet restore FreakLete.Api/FreakLete.Api.csproj`
Expected: succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/FreakLete.Api.csproj
git commit -m "chore: add Pgvector EF Core package for RAG embeddings"
```

---

### Task A2: Create WorkoutEmbedding entity

**Files:**
- Create: `FreakLete.Api/Entities/WorkoutEmbedding.cs`

- [ ] **Step 1: Create the entity**

```csharp
using Pgvector;

namespace FreakLete.Api.Entities;

public class WorkoutEmbedding
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int UserId { get; set; }
    public Vector Embedding { get; set; } = new(new float[768]);
    public string TextSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workout Workout { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/Entities/WorkoutEmbedding.cs
git commit -m "feat(rag): add WorkoutEmbedding entity"
```

---

### Task A3: Create UserSnapshotEmbedding entity

**Files:**
- Create: `FreakLete.Api/Entities/UserSnapshotEmbedding.cs`

- [ ] **Step 1: Create the entity**

```csharp
using Pgvector;

namespace FreakLete.Api.Entities;

public class UserSnapshotEmbedding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public Vector Embedding { get; set; } = new(new float[768]);
    public string TextSnapshot { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/Entities/UserSnapshotEmbedding.cs
git commit -m "feat(rag): add UserSnapshotEmbedding entity"
```

---

### Task A4: Wire entities into AppDbContext

**Files:**
- Modify: `FreakLete.Api/Data/AppDbContext.cs`

- [ ] **Step 1: Add DbSets**

After the existing `public DbSet<UserExerciseTier> UserExerciseTiers => Set<UserExerciseTier>();` line (around line 26), add:

```csharp
    public DbSet<WorkoutEmbedding> WorkoutEmbeddings => Set<WorkoutEmbedding>();
    public DbSet<UserSnapshotEmbedding> UserSnapshotEmbeddings => Set<UserSnapshotEmbedding>();
```

- [ ] **Step 2: Add EF Core mapping in `OnModelCreating`**

At the end of `OnModelCreating`, immediately before the closing `}` of the method, add:

```csharp
        // WorkoutEmbedding (pgvector)
        modelBuilder.Entity<WorkoutEmbedding>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.WorkoutId).IsUnique();
            e.Property(x => x.Embedding).HasColumnType("vector(768)");
            e.Property(x => x.TextSnapshot).HasMaxLength(4000);
            e.HasOne(x => x.Workout)
             .WithMany()
             .HasForeignKey(x => x.WorkoutId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSnapshotEmbedding (pgvector) — one row per user
        modelBuilder.Entity<UserSnapshotEmbedding>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.Embedding).HasColumnType("vector(768)");
            e.Property(x => x.TextSnapshot).HasMaxLength(4000);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Data/AppDbContext.cs
git commit -m "feat(rag): map embedding entities in DbContext"
```

---

### Task A5: Enable pgvector in `UseNpgsql` configuration

**Files:**
- Modify: `FreakLete.Api/Program.cs:32-33`

- [ ] **Step 1: Update Program.cs DbContext registration**

Find this block in `FreakLete.Api/Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Replace with:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npg => npg.UseVector()));
```

- [ ] **Step 2: Add the using directive at the top of Program.cs**

Add after the existing `using FreakLete.Api.Services;` line:

```csharp
using Pgvector.EntityFrameworkCore;
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Program.cs
git commit -m "feat(rag): enable pgvector in DbContext options"
```

---

### Task A6: Create EF migration that enables `vector` extension and creates tables

**Files:**
- Create (auto-generated): `FreakLete.Api/Migrations/<timestamp>_AddRagEmbeddings.cs`

- [ ] **Step 1: Generate the migration**

Run: `dotnet ef migrations add AddRagEmbeddings -p FreakLete.Api -s FreakLete.Api`
Expected: a new `<timestamp>_AddRagEmbeddings.cs` and `.Designer.cs` are created in `FreakLete.Api/Migrations/`.

- [ ] **Step 2: Edit the generated `Up` method to enable the extension first**

Open the new `<timestamp>_AddRagEmbeddings.cs`. Add this line as the FIRST line inside `protected override void Up(MigrationBuilder migrationBuilder)`:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
```

Add this line as the LAST line inside `protected override void Down(MigrationBuilder migrationBuilder)` (after table drops):

```csharp
migrationBuilder.Sql("DROP EXTENSION IF EXISTS vector;");
```

- [ ] **Step 3: Apply the migration to local dev DB**

Run: `dotnet ef database update -p FreakLete.Api -s FreakLete.Api`
Expected: applies cleanly. Verify with `psql` — `WorkoutEmbeddings` and `UserSnapshotEmbeddings` tables exist; `\dx` shows `vector` extension installed.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Migrations/
git commit -m "feat(rag): migration enabling pgvector and embedding tables"
```

---

## Phase B — Embedding text formatter (TDD)

### Task B1: Create test file scaffold (failing)

**Files:**
- Create: `FreakLete.Api.Tests/EmbeddingTextFormatterTests.cs`

- [ ] **Step 1: Create the test file with two failing test stubs**

```csharp
using FreakLete.Api.Entities;
using FreakLete.Api.Services.Embeddings;

namespace FreakLete.Api.Tests;

public class EmbeddingTextFormatterTests
{
    [Fact]
    public void FormatWorkout_IncludesSportPositionAndExercises()
    {
        var user = new User
        {
            Id = 1,
            SportName = "Football",
            Position = "Wide Receiver",
            PrimaryTrainingGoal = "Strength"
        };
        var workout = new Workout
        {
            Id = 10,
            UserId = 1,
            WorkoutName = "Lower",
            WorkoutDate = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            ExerciseEntries =
            [
                new ExerciseEntry
                {
                    ExerciseName = "Squat",
                    SetsCount = 4,
                    Reps = 5,
                    Metric1Value = 120,
                    Metric1Unit = "kg"
                }
            ]
        };

        string text = EmbeddingTextFormatter.FormatWorkout(workout, user);

        Assert.Contains("Sport: Football", text);
        Assert.Contains("Position: Wide Receiver", text);
        Assert.Contains("Squat", text);
        Assert.Contains("4x5", text);
        Assert.Contains("120", text);
        Assert.Contains("Date: 2026-04-20", text);
    }

    [Fact]
    public void FormatUserSnapshot_IncludesProfileGoalsAndProgram()
    {
        var user = new User
        {
            Id = 2,
            SportName = "Football",
            Position = "Wide Receiver",
            GymExperienceLevel = "Intermediate",
            PrimaryTrainingGoal = "Athletic Performance",
            SecondaryTrainingGoal = "Muscle Gain",
            TrainingDaysPerWeek = 4,
            PreferredSessionDurationMinutes = 75,
            AvailableEquipment = "Commercial Gym",
            PhysicalLimitations = "",
            InjuryHistory = "Left knee sprain (2024)"
        };
        var prs = new List<PrEntry>
        {
            new() { ExerciseName = "Squat", Weight = 140, Reps = 1 },
            new() { ExerciseName = "Bench Press", Weight = 100, Reps = 1 }
        };
        var program = new TrainingProgram
        {
            Name = "4-Day Athletic Hypertrophy",
            Status = "active"
        };

        string text = EmbeddingTextFormatter.FormatUserSnapshot(user, prs, program);

        Assert.Contains("Sport: Football", text);
        Assert.Contains("Experience: Intermediate", text);
        Assert.Contains("Primary Goal: Athletic Performance", text);
        Assert.Contains("Training Days: 4/week", text);
        Assert.Contains("Equipment: Commercial Gym", text);
        Assert.Contains("Squat 140kg", text);
        Assert.Contains("Active Program: 4-Day Athletic Hypertrophy", text);
        Assert.Contains("Left knee sprain", text);
    }
}
```

- [ ] **Step 2: Run the tests — they should fail to compile**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~EmbeddingTextFormatterTests"`
Expected: FAIL — type or namespace `EmbeddingTextFormatter` not found.

---

### Task B2: Implement EmbeddingTextFormatter

**Files:**
- Create: `FreakLete.Api/Services/Embeddings/EmbeddingTextFormatter.cs`

- [ ] **Step 1: Implement the formatter**

```csharp
using System.Globalization;
using System.Text;
using FreakLete.Api.Entities;

namespace FreakLete.Api.Services.Embeddings;

public static class EmbeddingTextFormatter
{
    public static string FormatWorkout(Workout workout, User user)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(user.SportName, "Unknown"));
        sb.Append(" | Position: ").Append(NonEmpty(user.Position, "Unknown"));
        sb.Append(" | Focus: ").Append(NonEmpty(user.PrimaryTrainingGoal, "General"));
        sb.AppendLine();

        sb.Append("Exercises: ");
        var rendered = workout.ExerciseEntries
            .Select(FormatExercise)
            .Where(s => !string.IsNullOrWhiteSpace(s));
        sb.AppendLine(string.Join(", ", rendered));

        if (!string.IsNullOrWhiteSpace(workout.WorkoutName))
            sb.Append("Session: ").AppendLine(workout.WorkoutName);

        sb.Append("Date: ").Append(workout.WorkoutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public static string FormatUserSnapshot(User user, IEnumerable<PrEntry> prs, TrainingProgram? activeProgram)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(user.SportName, "Unknown"));
        sb.Append(" | Position: ").Append(NonEmpty(user.Position, "Unknown"));
        sb.Append(" | Experience: ").Append(NonEmpty(user.GymExperienceLevel, "Unknown"));
        sb.AppendLine();

        sb.Append("Primary Goal: ").Append(NonEmpty(user.PrimaryTrainingGoal, "General"));
        sb.Append(" | Secondary Goal: ").Append(NonEmpty(user.SecondaryTrainingGoal, "None"));
        sb.AppendLine();

        sb.Append("Training Days: ").Append(user.TrainingDaysPerWeek?.ToString(CultureInfo.InvariantCulture) ?? "?");
        sb.Append("/week | Session Duration: ").Append(user.PreferredSessionDurationMinutes?.ToString(CultureInfo.InvariantCulture) ?? "?");
        sb.AppendLine(" min");

        sb.Append("Equipment: ").AppendLine(NonEmpty(user.AvailableEquipment, "Not specified"));

        sb.Append("Physical Limitations: ").Append(NonEmpty(user.PhysicalLimitations, "None"));
        sb.Append(" | Injury History: ").AppendLine(NonEmpty(user.InjuryHistory, "None"));

        var topPrs = prs
            .GroupBy(p => p.ExerciseName)
            .Select(g => g.OrderByDescending(p => p.Weight).First())
            .Take(5)
            .Select(p => $"{p.ExerciseName} {p.Weight}kg")
            .ToList();
        if (topPrs.Count > 0)
            sb.Append("Top PRs: ").AppendLine(string.Join(", ", topPrs));

        if (activeProgram is not null)
            sb.Append("Active Program: ").AppendLine(activeProgram.Name);

        return sb.ToString();
    }

    private static string FormatExercise(ExerciseEntry e)
    {
        var sets = $"{e.SetsCount}x{e.Reps}";
        var weight = e.Metric1Value.HasValue
            ? $" @{e.Metric1Value.Value.ToString("0.#", CultureInfo.InvariantCulture)}{e.Metric1Unit ?? ""}"
            : "";
        return $"{e.ExerciseName} {sets}{weight}".Trim();
    }

    private static string NonEmpty(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value!;
}
```

- [ ] **Step 2: Run the tests**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~EmbeddingTextFormatterTests"`
Expected: PASS (2/2).

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/Services/Embeddings/EmbeddingTextFormatter.cs FreakLete.Api.Tests/EmbeddingTextFormatterTests.cs
git commit -m "feat(rag): EmbeddingTextFormatter with workout + user snapshot tests"
```

---

## Phase C — Gemini embed API

### Task C1: Add embedding model option + EmbedAsync method

**Files:**
- Modify: `FreakLete.Api/Services/GeminiClient.cs`

- [ ] **Step 1: Extend `GeminiOptions` with embedding model**

Replace the existing `GeminiOptions` class (around lines 7-11) with:

```csharp
public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash-lite";
    public string EmbeddingModel { get; set; } = "text-embedding-004";
}
```

- [ ] **Step 2: Add `EmbedAsync` method to `GeminiClient`**

Inside `GeminiClient`, after the existing `GenerateContentAsync` method (which ends around line 56) and before the closing `}` of the class, add:

```csharp
    public async Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.EmbeddingModel}:embedContent?key={_options.ApiKey}";

        var payload = new
        {
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync(url, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini embed error {Status} from model {Model}", response.StatusCode, _options.EmbeddingModel);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("embedding", out var embedding) ||
                !embedding.TryGetProperty("values", out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Gemini embed response missing embedding.values");
                return null;
            }

            var floats = new float[values.GetArrayLength()];
            int i = 0;
            foreach (var v in values.EnumerateArray())
                floats[i++] = v.GetSingle();

            return floats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini EmbedAsync failed");
            return null;
        }
    }
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/GeminiClient.cs
git commit -m "feat(rag): GeminiClient.EmbedAsync for text-embedding-004"
```

---

## Phase D — Background pipeline

### Task D1: Define `EmbeddingJob` and channel wrapper

**Files:**
- Create: `FreakLete.Api/Services/Embeddings/EmbeddingJob.cs`
- Create: `FreakLete.Api/Services/Embeddings/EmbeddingChannel.cs`

- [ ] **Step 1: Create EmbeddingJob.cs**

```csharp
namespace FreakLete.Api.Services.Embeddings;

public enum EmbeddingJobKind
{
    UserSnapshot,
    Workout
}

public sealed record EmbeddingJob(EmbeddingJobKind Kind, int UserId, int? WorkoutId);
```

- [ ] **Step 2: Create EmbeddingChannel.cs**

```csharp
using System.Threading.Channels;

namespace FreakLete.Api.Services.Embeddings;

public sealed class EmbeddingChannel
{
    private readonly Channel<EmbeddingJob> _channel = Channel.CreateBounded<EmbeddingJob>(
        new BoundedChannelOptions(capacity: 256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<EmbeddingJob> Reader => _channel.Reader;

    public bool TryWrite(EmbeddingJob job) => _channel.Writer.TryWrite(job);
}
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task D2: Define event sink interfaces

**Files:**
- Create: `FreakLete.Api/Services/Embeddings/IUserSnapshotEventSink.cs`
- Create: `FreakLete.Api/Services/Embeddings/IWorkoutEmbeddingEnqueuer.cs`

- [ ] **Step 1: IUserSnapshotEventSink.cs**

```csharp
namespace FreakLete.Api.Services.Embeddings;

public interface IUserSnapshotEventSink
{
    void OnUserUpdated(int userId);
}
```

- [ ] **Step 2: IWorkoutEmbeddingEnqueuer.cs**

```csharp
namespace FreakLete.Api.Services.Embeddings;

public interface IWorkoutEmbeddingEnqueuer
{
    void EnqueueWorkout(int userId, int workoutId);
}
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task D3: Implement combined EmbeddingEventSink

**Files:**
- Create: `FreakLete.Api/Services/Embeddings/EmbeddingEventSink.cs`

- [ ] **Step 1: Create the implementation**

```csharp
namespace FreakLete.Api.Services.Embeddings;

public sealed class EmbeddingEventSink : IUserSnapshotEventSink, IWorkoutEmbeddingEnqueuer
{
    private readonly EmbeddingChannel _channel;
    private readonly ILogger<EmbeddingEventSink> _logger;

    public EmbeddingEventSink(EmbeddingChannel channel, ILogger<EmbeddingEventSink> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public void OnUserUpdated(int userId)
    {
        var ok = _channel.TryWrite(new EmbeddingJob(EmbeddingJobKind.UserSnapshot, userId, null));
        if (!ok)
            _logger.LogDebug("Embedding channel full; dropped UserSnapshot job for user {UserId}", userId);
    }

    public void EnqueueWorkout(int userId, int workoutId)
    {
        var ok = _channel.TryWrite(new EmbeddingJob(EmbeddingJobKind.Workout, userId, workoutId));
        if (!ok)
            _logger.LogDebug("Embedding channel full; dropped Workout job for user {UserId} workout {WorkoutId}", userId, workoutId);
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task D4: Implement EmbeddingBackgroundService

**Files:**
- Create: `FreakLete.Api/Services/Embeddings/EmbeddingBackgroundService.cs`

- [ ] **Step 1: Create the hosted service**

```csharp
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace FreakLete.Api.Services.Embeddings;

public sealed class EmbeddingBackgroundService : BackgroundService
{
    private readonly EmbeddingChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingBackgroundService> _logger;

    public EmbeddingBackgroundService(
        EmbeddingChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmbeddingBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Embedding job failed: {Kind} user={UserId} workout={WorkoutId}",
                    job.Kind, job.UserId, job.WorkoutId);
            }
        }
    }

    private async Task ProcessAsync(EmbeddingJob job, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gemini = scope.ServiceProvider.GetRequiredService<GeminiClient>();

        if (job.Kind == EmbeddingJobKind.Workout && job.WorkoutId.HasValue)
        {
            await ProcessWorkoutAsync(db, gemini, job.UserId, job.WorkoutId.Value, ct);
        }
        else if (job.Kind == EmbeddingJobKind.UserSnapshot)
        {
            await ProcessUserSnapshotAsync(db, gemini, job.UserId, ct);
        }
    }

    private static async Task ProcessWorkoutAsync(
        AppDbContext db, GeminiClient gemini, int userId, int workoutId, CancellationToken ct)
    {
        var workout = await db.Workouts
            .Include(w => w.ExerciseEntries)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId, ct);
        var user = await db.Users.FindAsync([userId], ct);
        if (workout is null || user is null) return;

        var text = EmbeddingTextFormatter.FormatWorkout(workout, user);
        var floats = await gemini.EmbedAsync(text, ct);
        if (floats is null || floats.Length != 768) return;

        var existing = await db.WorkoutEmbeddings
            .FirstOrDefaultAsync(e => e.WorkoutId == workoutId, ct);

        if (existing is null)
        {
            db.WorkoutEmbeddings.Add(new WorkoutEmbedding
            {
                WorkoutId = workoutId,
                UserId = userId,
                Embedding = new Vector(floats),
                TextSnapshot = text,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Embedding = new Vector(floats);
            existing.TextSnapshot = text;
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task ProcessUserSnapshotAsync(
        AppDbContext db, GeminiClient gemini, int userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return;

        var prs = await db.PrEntries.Where(p => p.UserId == userId).ToListAsync(ct);
        var program = await db.TrainingPrograms
            .Where(p => p.UserId == userId && p.Status == "active")
            .FirstOrDefaultAsync(ct);

        var text = EmbeddingTextFormatter.FormatUserSnapshot(user, prs, program);
        var floats = await gemini.EmbedAsync(text, ct);
        if (floats is null || floats.Length != 768) return;

        var existing = await db.UserSnapshotEmbeddings
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);

        if (existing is null)
        {
            db.UserSnapshotEmbeddings.Add(new UserSnapshotEmbedding
            {
                UserId = userId,
                Embedding = new Vector(floats),
                TextSnapshot = text,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Embedding = new Vector(floats);
            existing.TextSnapshot = text;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task D5: Register embedding services in DI

**Files:**
- Modify: `FreakLete.Api/Program.cs`

- [ ] **Step 1: Add registrations**

After the existing line `builder.Services.AddScoped<IExerciseTierService, ExerciseTierService>();` add:

```csharp
// RAG embedding pipeline
builder.Services.AddSingleton<EmbeddingChannel>();
builder.Services.AddSingleton<EmbeddingEventSink>();
builder.Services.AddSingleton<IUserSnapshotEventSink>(sp => sp.GetRequiredService<EmbeddingEventSink>());
builder.Services.AddSingleton<IWorkoutEmbeddingEnqueuer>(sp => sp.GetRequiredService<EmbeddingEventSink>());
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<EmbeddingBackgroundService>();
```

- [ ] **Step 2: Add the using directive**

At the top of `Program.cs` add:

```csharp
using FreakLete.Api.Services.Embeddings;
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/Embeddings/ FreakLete.Api/Program.cs
git commit -m "feat(rag): background embedding pipeline (channel + sink + hosted service)"
```

---

## Phase E — ContextBuilder

### Task E1: Create FreakAiContext DTO

**Files:**
- Create: `FreakLete.Api/Services/Rag/FreakAiContext.cs`

- [ ] **Step 1: Create the DTO**

```csharp
namespace FreakLete.Api.Services.Rag;

public sealed class FreakAiContext
{
    public string? UserProfile { get; init; }
    public string? Goals { get; init; }
    public string? Equipment { get; init; }
    public string? PhysicalLimitations { get; init; }
    public string? CurrentProgram { get; init; }
    public string? RecentPrSummary { get; init; }
    public List<string> SimilarWorkouts { get; init; } = [];
    public string? UserSnapshotContext { get; init; }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task E2: Define IContextBuilder interface

**Files:**
- Create: `FreakLete.Api/Services/Rag/IContextBuilder.cs`

- [ ] **Step 1: Create the interface**

```csharp
namespace FreakLete.Api.Services.Rag;

public interface IContextBuilder
{
    Task<FreakAiContext?> BuildAsync(int userId, string intent, string userMessage, CancellationToken ct = default);
}
```

---

### Task E3: Implement ContextBuilder

**Files:**
- Create: `FreakLete.Api/Services/Rag/ContextBuilder.cs`

- [ ] **Step 1: Implement the class**

```csharp
using System.Text;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace FreakLete.Api.Services.Rag;

public sealed class ContextBuilder : IContextBuilder
{
    private readonly AppDbContext _db;
    private readonly GeminiClient _gemini;
    private readonly ILogger<ContextBuilder> _logger;

    public ContextBuilder(AppDbContext db, GeminiClient gemini, ILogger<ContextBuilder> logger)
    {
        _db = db;
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<FreakAiContext?> BuildAsync(int userId, string intent, string userMessage, CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return null;

            return intent switch
            {
                FreakAiUsageIntent.ProgramGenerate => await BuildProgramGenerateAsync(user, userMessage, ct),
                FreakAiUsageIntent.ProgramAnalyze => await BuildProgramAnalyzeAsync(user, userMessage, ct),
                FreakAiUsageIntent.NutritionGuidance => BuildNutritionGuidance(user),
                _ => BuildGeneralChat(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ContextBuilder failed for user {UserId}, intent {Intent}; falling back to no context", userId, intent);
            return null;
        }
    }

    // ── Intent-specific builders ────────────────────────────────

    private async Task<FreakAiContext> BuildProgramGenerateAsync(User user, string userMessage, CancellationToken ct)
    {
        var program = await GetActiveProgramAsync(user.Id, ct);
        var prSummary = await GetRecentPrSummaryAsync(user.Id, ct);
        var snapshot = await GetUserSnapshotContextAsync(user.Id, ct);

        return new FreakAiContext
        {
            UserProfile = FormatProfile(user, includeBody: true),
            Goals = FormatGoals(user),
            Equipment = NullIfEmpty(user.AvailableEquipment),
            PhysicalLimitations = FormatLimitations(user),
            CurrentProgram = program,
            RecentPrSummary = prSummary,
            UserSnapshotContext = snapshot
        };
    }

    private async Task<FreakAiContext> BuildProgramAnalyzeAsync(User user, string userMessage, CancellationToken ct)
    {
        var program = await GetActiveProgramAsync(user.Id, ct);
        var prSummary = await GetRecentPrSummaryAsync(user.Id, ct);
        var similar = await GetSimilarWorkoutsAsync(user.Id, userMessage, ct);

        return new FreakAiContext
        {
            UserProfile = FormatProfile(user, includeBody: false),
            Goals = FormatGoals(user),
            CurrentProgram = program,
            RecentPrSummary = prSummary,
            SimilarWorkouts = similar
        };
    }

    private static FreakAiContext BuildNutritionGuidance(User user) => new()
    {
        UserProfile = FormatProfileMinimalNutrition(user),
        Goals = FormatGoals(user),
        Equipment = null
    };

    private static FreakAiContext BuildGeneralChat(User user) => new()
    {
        UserProfile = FormatProfileMinimal(user),
        Goals = FormatGoals(user)
    };

    // ── Vector similarity (pgvector) ────────────────────────────

    private async Task<List<string>> GetSimilarWorkoutsAsync(int userId, string userMessage, CancellationToken ct)
    {
        var floats = await _gemini.EmbedAsync(userMessage, ct);
        if (floats is null || floats.Length != 768) return [];

        var query = new Vector(floats);

        try
        {
            var results = await _db.WorkoutEmbeddings
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.Embedding.CosineDistance(query))
                .Take(3)
                .Select(e => e.TextSnapshot)
                .ToListAsync(ct);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workout similarity query failed for user {UserId}", userId);
            return [];
        }
    }

    private async Task<string?> GetUserSnapshotContextAsync(int userId, CancellationToken ct)
    {
        try
        {
            var snapshot = await _db.UserSnapshotEmbeddings
                .Where(e => e.UserId == userId)
                .Select(e => e.TextSnapshot)
                .FirstOrDefaultAsync(ct);
            return string.IsNullOrWhiteSpace(snapshot) ? null : snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UserSnapshotEmbedding fetch failed for user {UserId}", userId);
            return null;
        }
    }

    // ── Structured context blocks ───────────────────────────────

    private async Task<string?> GetActiveProgramAsync(int userId, CancellationToken ct)
    {
        var program = await _db.TrainingPrograms
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == "active")
            .Select(p => new { p.Name, p.Goal, p.DaysPerWeek })
            .FirstOrDefaultAsync(ct);
        return program is null
            ? null
            : $"{program.Name} | Goal: {program.Goal} | {program.DaysPerWeek} days/week";
    }

    private async Task<string?> GetRecentPrSummaryAsync(int userId, CancellationToken ct)
    {
        var prs = await _db.PrEntries
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new { p.ExerciseName, p.Weight, p.Reps })
            .ToListAsync(ct);

        if (prs.Count == 0) return null;
        return string.Join(", ", prs.Select(p => $"{p.ExerciseName} {p.Weight}kg x{p.Reps}"));
    }

    private static string FormatProfile(User u, bool includeBody)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(u.SportName, "Unknown"));
        sb.Append(" | Position: ").Append(NonEmpty(u.Position, "Unknown"));
        sb.Append(" | Experience: ").Append(NonEmpty(u.GymExperienceLevel, "Unknown"));
        if (includeBody)
        {
            if (u.WeightKg.HasValue) sb.Append(" | Weight: ").Append(u.WeightKg.Value).Append("kg");
            if (u.HeightCm.HasValue) sb.Append(" | Height: ").Append(u.HeightCm.Value).Append("cm");
            if (u.BodyFatPercentage.HasValue) sb.Append(" | BF%: ").Append(u.BodyFatPercentage.Value);
        }
        return sb.ToString();
    }

    private static string FormatProfileMinimal(User u)
        => $"Sport: {NonEmpty(u.SportName, "Unknown")} | Position: {NonEmpty(u.Position, "Unknown")}";

    private static string FormatProfileMinimalNutrition(User u)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(u.SportName, "Unknown"));
        if (u.WeightKg.HasValue) sb.Append(" | Weight: ").Append(u.WeightKg.Value).Append("kg");
        if (u.BodyFatPercentage.HasValue) sb.Append(" | BF%: ").Append(u.BodyFatPercentage.Value);
        if (!string.IsNullOrWhiteSpace(u.DietaryPreference)) sb.Append(" | Diet: ").Append(u.DietaryPreference);
        return sb.ToString();
    }

    private static string FormatGoals(User u)
    {
        var primary = NonEmpty(u.PrimaryTrainingGoal, "Not set");
        var secondary = NonEmpty(u.SecondaryTrainingGoal, "None");
        return $"Primary: {primary} | Secondary: {secondary}";
    }

    private static string? FormatLimitations(User u)
    {
        var limits = NonEmpty(u.PhysicalLimitations, "");
        var pain = NonEmpty(u.CurrentPainPoints, "");
        if (string.IsNullOrEmpty(limits) && string.IsNullOrEmpty(pain)) return null;
        return $"Limitations: {(string.IsNullOrEmpty(limits) ? "None" : limits)} | Current pain: {(string.IsNullOrEmpty(pain) ? "None" : pain)}";
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    private static string NonEmpty(string? s, string fallback) => string.IsNullOrWhiteSpace(s) ? fallback : s!;
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task E4: Unit-test ContextBuilder for each intent

**Files:**
- Create: `FreakLete.Api.Tests/ContextBuilderTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using FreakLete.Api.Services.Rag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class ContextBuilderTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private int _userId;

    public ContextBuilderTests(FreakLeteApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User
        {
            FirstName = "Test", LastName = "User", Email = "ctx@test.com", PasswordHash = "x",
            SportName = "Football", Position = "WR",
            GymExperienceLevel = "Intermediate",
            PrimaryTrainingGoal = "Athletic Performance",
            SecondaryTrainingGoal = "Muscle Gain",
            AvailableEquipment = "Commercial Gym",
            WeightKg = 82, HeightCm = 180,
            DietaryPreference = "Omnivore",
            TrainingDaysPerWeek = 4,
            PreferredSessionDurationMinutes = 75
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        _userId = user.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProgramGenerate_IncludesProfileGoalsEquipment()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();
        var ctx = await sut.BuildAsync(_userId, FreakAiUsageIntent.ProgramGenerate, "Build me a 4-day plan");
        Assert.NotNull(ctx);
        Assert.Contains("Football", ctx!.UserProfile!);
        Assert.Contains("Athletic Performance", ctx.Goals!);
        Assert.Equal("Commercial Gym", ctx.Equipment);
    }

    [Fact]
    public async Task NutritionGuidance_IncludesBodyAndDiet_NotEquipment()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();
        var ctx = await sut.BuildAsync(_userId, FreakAiUsageIntent.NutritionGuidance, "What should I eat?");
        Assert.NotNull(ctx);
        Assert.Contains("82", ctx!.UserProfile!);
        Assert.Contains("Omnivore", ctx.UserProfile!);
        Assert.Null(ctx.Equipment);
    }

    [Fact]
    public async Task GeneralChat_IsMinimal()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();
        var ctx = await sut.BuildAsync(_userId, FreakAiUsageIntent.GeneralChat, "Hi");
        Assert.NotNull(ctx);
        Assert.Contains("Football", ctx!.UserProfile!);
        Assert.Null(ctx.Equipment);
        Assert.Null(ctx.CurrentProgram);
    }

    [Fact]
    public async Task UnknownUser_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();
        var ctx = await sut.BuildAsync(int.MaxValue, FreakAiUsageIntent.GeneralChat, "Hi");
        Assert.Null(ctx);
    }
}
```

- [ ] **Step 2: Run the tests — they will fail because IContextBuilder is not registered yet**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~ContextBuilderTests"`
Expected: FAIL — no service for `IContextBuilder` registered.

---

### Task E5: Register ContextBuilder in DI

**Files:**
- Modify: `FreakLete.Api/Program.cs`

- [ ] **Step 1: Register the service**

After the embedding pipeline registrations from Task D5, add:

```csharp
builder.Services.AddScoped<IContextBuilder, ContextBuilder>();
```

- [ ] **Step 2: Add the using directive**

Add at top of `Program.cs`:

```csharp
using FreakLete.Api.Services.Rag;
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~ContextBuilderTests"`
Expected: PASS (4/4).

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/Rag/ FreakLete.Api/Program.cs FreakLete.Api.Tests/ContextBuilderTests.cs
git commit -m "feat(rag): ContextBuilder with intent-specific context assembly + tests"
```

---

## Phase F — Orchestrator wiring

### Task F1: Add `Build(FreakAiContext?)` overload

**Files:**
- Modify: `FreakLete.Api/Services/FreakAiSystemPrompt.cs`

- [ ] **Step 1: Add the overload at the bottom of the class**

Add immediately before the final closing `}` of `FreakAiSystemPrompt`:

```csharp
    public static string Build(FreakAiContext? context)
    {
        var basePrompt = Build();
        if (context is null) return basePrompt;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## USER CONTEXT (pre-fetched, no tool call needed)");
        if (!string.IsNullOrWhiteSpace(context.UserProfile))
            sb.Append("Profile: ").AppendLine(context.UserProfile);
        if (!string.IsNullOrWhiteSpace(context.Goals))
            sb.Append("Goals: ").AppendLine(context.Goals);
        if (!string.IsNullOrWhiteSpace(context.Equipment))
            sb.Append("Equipment: ").AppendLine(context.Equipment);
        if (!string.IsNullOrWhiteSpace(context.PhysicalLimitations))
            sb.Append("Limitations: ").AppendLine(context.PhysicalLimitations);
        if (!string.IsNullOrWhiteSpace(context.CurrentProgram))
            sb.Append("Active Program: ").AppendLine(context.CurrentProgram);
        if (!string.IsNullOrWhiteSpace(context.RecentPrSummary))
            sb.Append("Recent PRs: ").AppendLine(context.RecentPrSummary);
        if (!string.IsNullOrWhiteSpace(context.UserSnapshotContext))
            sb.AppendLine("User snapshot:").AppendLine(context.UserSnapshotContext);
        if (context.SimilarWorkouts.Count > 0)
        {
            sb.AppendLine("Similar past workouts:");
            foreach (var w in context.SimilarWorkouts)
                sb.Append("- ").AppendLine(w);
        }
        sb.AppendLine();
        sb.AppendLine("Use this context proactively. You may still call tools when you need data not present here.");
        sb.AppendLine();

        return sb + basePrompt;
    }
```

- [ ] **Step 2: Add the using at top of file**

```csharp
using FreakLete.Api.Services.Rag;
```

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task F2: Inject ContextBuilder into FreakAiOrchestrator and accept intent

**Files:**
- Modify: `FreakLete.Api/Services/FreakAiOrchestrator.cs`

- [ ] **Step 1: Update constructor and field**

Replace lines 6-24 (the field declarations and constructor) with:

```csharp
public class FreakAiOrchestrator
{
    private readonly GeminiClient _gemini;
    private readonly FreakAiToolExecutor _toolExecutor;
    private readonly IContextBuilder _contextBuilder;
    private readonly ILogger<FreakAiOrchestrator> _logger;

    private const int MaxToolRounds = 5;
    private const int MaxHistoryMessages = 20;
    private static readonly TimeSpan MaxChatDuration = TimeSpan.FromSeconds(40);

    public FreakAiOrchestrator(
        GeminiClient gemini,
        FreakAiToolExecutor toolExecutor,
        IContextBuilder contextBuilder,
        ILogger<FreakAiOrchestrator> logger)
    {
        _gemini = gemini;
        _toolExecutor = toolExecutor;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }
```

- [ ] **Step 2: Add `intent` parameter to `ChatAsync` (optional, default = `null`)**

Replace the existing `ChatAsync` signature at line 26-30 with:

```csharp
    public async Task<string> ChatAsync(
        int userId,
        string userMessage,
        List<ChatMessage>? history,
        string? intent = null,
        CancellationToken cancellationToken = default)
    {
```

- [ ] **Step 3: Replace `BuildLanguageAwarePrompt` body to take a context**

Find this line inside `ChatAsync` (around line 47):

```csharp
        var systemPrompt = BuildLanguageAwarePrompt(detectedLang, langName);
```

Replace with:

```csharp
        var resolvedIntent = intent ?? FreakAiUsageIntent.GeneralChat;
        var ragContext = await _contextBuilder.BuildAsync(userId, resolvedIntent, userMessage, timeoutCts.Token);
        var systemPrompt = BuildLanguageAwarePrompt(detectedLang, langName, ragContext);
```

Then replace the existing `BuildLanguageAwarePrompt` method (around lines 173-189) with:

```csharp
    private static string BuildLanguageAwarePrompt(string langCode, string langName, FreakAiContext? context)
    {
        var basePrompt = FreakAiSystemPrompt.Build(context);

        string langDirective = $"""
            ## MANDATORY RESPONSE LANGUAGE: {langName} ({langCode})
            The user's latest message is in {langName}. You MUST write your ENTIRE response in {langName}.
            This includes: explanations, coaching cues, program names, session names, notes, and all text output.
            Tool results are in English — translate/adapt them naturally into {langName}.
            Technical exercise names (Bench Press, Squat, Deadlift) may stay in English only if that is natural usage in {langName}.
            DO NOT switch to English unless the detected language IS English.

            """;

        return langDirective + basePrompt;
    }
```

- [ ] **Step 4: Add the using at top of file**

```csharp
using FreakLete.Api.Services.Rag;
```

- [ ] **Step 5: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task F3: Pass intent from controller to orchestrator

**Files:**
- Modify: `FreakLete.Api/Controllers/FreakAiController.cs`

- [ ] **Step 1: Update the `_orchestrator.ChatAsync` call**

Find the line (around line 74):

```csharp
            var reply = await _orchestrator.ChatAsync(userId, request.Message, request.History, ct);
```

Replace with:

```csharp
            var reply = await _orchestrator.ChatAsync(userId, request.Message, request.History, intent, ct);
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

- [ ] **Step 3: Run the existing FreakAI integration tests**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~FreakAiIntegrationTests"`
Expected: All existing tests still PASS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/FreakAiSystemPrompt.cs FreakLete.Api/Services/FreakAiOrchestrator.cs FreakLete.Api/Controllers/FreakAiController.cs
git commit -m "feat(rag): orchestrator injects per-intent user context into system prompt"
```

---

## Phase G — Domain event triggers

### Task G1: Fire events from WorkoutsController

**Files:**
- Modify: `FreakLete.Api/Controllers/WorkoutsController.cs`

- [ ] **Step 1: Inject the sinks**

Replace the constructor and field block at lines 16-21 with:

```csharp
    private readonly AppDbContext _db;
    private readonly IUserSnapshotEventSink _snapshotSink;
    private readonly IWorkoutEmbeddingEnqueuer _workoutSink;

    public WorkoutsController(
        AppDbContext db,
        IUserSnapshotEventSink snapshotSink,
        IWorkoutEmbeddingEnqueuer workoutSink)
    {
        _db = db;
        _snapshotSink = snapshotSink;
        _workoutSink = workoutSink;
    }
```

- [ ] **Step 2: Fire after `Create`**

Find this block in `Create` (around lines 75-78):

```csharp
        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = workout.Id }, MapToResponse(workout));
```

Replace with:

```csharp
        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        _workoutSink.EnqueueWorkout(userId, workout.Id);
        _snapshotSink.OnUserUpdated(userId);

        return CreatedAtAction(nameof(GetById), new { id = workout.Id }, MapToResponse(workout));
```

- [ ] **Step 3: Fire after `Update`**

In the `Update` method (around line 99-100), find:

```csharp
        await _db.SaveChangesAsync();
        return NoContent();
```

Replace with:

```csharp
        await _db.SaveChangesAsync();

        _workoutSink.EnqueueWorkout(userId, workout.Id);
        _snapshotSink.OnUserUpdated(userId);

        return NoContent();
```

- [ ] **Step 4: Add the using directive**

```csharp
using FreakLete.Api.Services.Embeddings;
```

- [ ] **Step 5: Build**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

---

### Task G2: Fire events from PrEntriesController

**Files:**
- Modify: `FreakLete.Api/Controllers/PrEntriesController.cs`

- [ ] **Step 1: Inject the sink**

Replace the constructor and fields block at lines 16-23 with:

```csharp
    private readonly AppDbContext _db;
    private readonly IExerciseTierService _tierService;
    private readonly IUserSnapshotEventSink _snapshotSink;

    public PrEntriesController(
        AppDbContext db,
        IExerciseTierService tierService,
        IUserSnapshotEventSink snapshotSink)
    {
        _db = db;
        _tierService = tierService;
        _snapshotSink = snapshotSink;
    }
```

- [ ] **Step 2: Fire after `Create`**

Inside `Create`, immediately after `_db.PrEntries.Add(entry); await _db.SaveChangesAsync();` (around line 69), add:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

- [ ] **Step 3: Add the using**

```csharp
using FreakLete.Api.Services.Embeddings;
```

- [ ] **Step 4: Build + run PR-entry tests**

Run: `dotnet build FreakLete.Api && dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~PrEntryIntegrationTests"`
Expected: SUCCESS, all PR tests pass.

---

### Task G3: Fire events from AuthController profile updates

**Files:**
- Modify: `FreakLete.Api/Controllers/AuthController.cs`

- [ ] **Step 1: Inject the sink**

Add a new field near the existing fields:

```csharp
    private readonly IUserSnapshotEventSink _snapshotSink;
```

Replace the constructor with:

```csharp
    public AuthController(AppDbContext db, TokenService tokenService,
        AthleteProfileService athleteProfileService, CoachProfileService coachProfileService,
        IUserSnapshotEventSink snapshotSink)
    {
        _db = db;
        _tokenService = tokenService;
        _athleteProfileService = athleteProfileService;
        _coachProfileService = coachProfileService;
        _snapshotSink = snapshotSink;
    }
```

- [ ] **Step 2: Fire after athlete profile save**

In `SaveAthleteProfile`, immediately before the `return Ok(response);` (line 203), add:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

- [ ] **Step 3: Fire after coach profile save**

In `SaveCoachProfile`, immediately before the `return Ok(response);` (line 220), add:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

- [ ] **Step 4: Add the using**

```csharp
using FreakLete.Api.Services.Embeddings;
```

- [ ] **Step 5: Build + run auth tests**

Run: `dotnet build FreakLete.Api && dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~AthleteProfileIntegrationTests|FullyQualifiedName~CoachProfileIntegrationTests"`
Expected: SUCCESS.

---

### Task G4: Fire events from TrainingProgramController status changes

**Files:**
- Modify: `FreakLete.Api/Controllers/TrainingProgramController.cs`

- [ ] **Step 1: Inject the sink**

Replace constructor (lines 15-20):

```csharp
    private readonly AppDbContext _db;
    private readonly IUserSnapshotEventSink _snapshotSink;

    public TrainingProgramController(AppDbContext db, IUserSnapshotEventSink snapshotSink)
    {
        _db = db;
        _snapshotSink = snapshotSink;
    }
```

- [ ] **Step 2: Fire after every program-mutating SaveChangesAsync**

Read through `TrainingProgramController.cs`. For every endpoint that calls `_db.SaveChangesAsync()` and modifies a program (status change, create, delete, update), add immediately after the SaveChanges call:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

Read-only endpoints (`GetAll`, `GetActive`, `GetById`) do not need the call.

- [ ] **Step 3: Add the using**

```csharp
using FreakLete.Api.Services.Embeddings;
```

- [ ] **Step 4: Build + run program tests**

Run: `dotnet build FreakLete.Api && dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~TrainingProgramIntegrationTests"`
Expected: SUCCESS.

---

### Task G5: Fire events from FreakAiToolExecutor mutations

**Files:**
- Modify: `FreakLete.Api/Services/FreakAiToolExecutor.cs`

- [ ] **Step 1: Inject the sink**

Update field declarations:

```csharp
    private readonly AppDbContext _db;
    private readonly TrainingSummaryService _summaryService;
    private readonly IUserSnapshotEventSink _snapshotSink;
    private readonly ILogger<FreakAiToolExecutor> _logger;
```

Update constructor (lines 35-40):

```csharp
    public FreakAiToolExecutor(
        AppDbContext db,
        TrainingSummaryService summaryService,
        IUserSnapshotEventSink snapshotSink,
        ILogger<FreakAiToolExecutor> logger)
    {
        _db = db;
        _summaryService = summaryService;
        _snapshotSink = snapshotSink;
        _logger = logger;
    }
```

- [ ] **Step 2: Fire after `CreateProgram`**

Inside `CreateProgram`, immediately after `await _db.SaveChangesAsync();` (around line 603), add:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

- [ ] **Step 3: Fire after `AdjustProgram`**

Inside `AdjustProgram`, immediately after `await _db.SaveChangesAsync();` (around line 712), add:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

- [ ] **Step 4: Fire after `SetProgramStatus`**

Inside `SetProgramStatus`, immediately after `await _db.SaveChangesAsync();` (around line 829), add:

```csharp
        _snapshotSink.OnUserUpdated(userId);
```

- [ ] **Step 5: Add the using**

```csharp
using FreakLete.Api.Services.Embeddings;
```

- [ ] **Step 6: Build + run FreakAI tests**

Run: `dotnet build FreakLete.Api && dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~FreakAiIntegrationTests"`
Expected: SUCCESS.

- [ ] **Step 7: Commit Phase G**

```bash
git add FreakLete.Api/Controllers/WorkoutsController.cs FreakLete.Api/Controllers/PrEntriesController.cs FreakLete.Api/Controllers/AuthController.cs FreakLete.Api/Controllers/TrainingProgramController.cs FreakLete.Api/Services/FreakAiToolExecutor.cs
git commit -m "feat(rag): fire embedding events from domain mutations"
```

---

## Phase H — Integration tests

### Task H1: Add RAG integration test file

**Files:**
- Create: `FreakLete.Api.Tests/RagIntegrationTests.cs`

- [ ] **Step 1: Create the file**

```csharp
using System.Net.Http.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class RagIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly FakeGeminiHandler _geminiHandler = new();
    private HttpClient _client = null!;

    public RagIntegrationTests(FreakLeteApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        var childFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<GeminiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => _geminiHandler);
            });
        });
        _client = childFactory.CreateClient();
        var auth = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, auth.Token);

        await _client.PutAsJsonAsync("/api/Auth/profile/athlete", new
        {
            firstName = "Test",
            lastName = "User",
            sportName = "Football",
            position = "WR",
            gymExperienceLevel = "Intermediate",
            primaryTrainingGoal = "Athletic Performance",
            secondaryTrainingGoal = "Muscle Gain",
            availableEquipment = "Commercial Gym"
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Chat_GeneralChat_SystemPromptContainsUserContextBlock()
    {
        _geminiHandler.SetupTextResponse("Reply");
        var resp = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello",
            intent = FreakAiUsageIntent.GeneralChat
        });
        resp.EnsureSuccessStatusCode();

        Assert.True(_geminiHandler.VerifySystemPromptContains("USER CONTEXT"));
        Assert.True(_geminiHandler.VerifySystemPromptContains("Football"));
    }

    [Fact]
    public async Task Chat_ProgramGenerate_SystemPromptIncludesGoalsAndEquipment()
    {
        _geminiHandler.SetupTextResponse("Reply");
        var resp = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Build me a 4-day program",
            intent = FreakAiUsageIntent.ProgramGenerate
        });
        resp.EnsureSuccessStatusCode();

        Assert.True(_geminiHandler.VerifySystemPromptContains("Athletic Performance"));
        Assert.True(_geminiHandler.VerifySystemPromptContains("Commercial Gym"));
    }

    [Fact]
    public async Task Chat_StaticPromptCorePresentEvenWithContext()
    {
        _geminiHandler.SetupTextResponse("Reply");
        var resp = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello",
            intent = FreakAiUsageIntent.GeneralChat
        });
        resp.EnsureSuccessStatusCode();

        _geminiHandler.AssertSystemPromptIncludesCoreProductRule();
    }
}
```

- [ ] **Step 2: Run the new tests**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~RagIntegrationTests"`
Expected: PASS (3/3).

- [ ] **Step 3: Run the full FreakAI test suite for regression**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~FreakAi"`
Expected: PASS — pre-existing tests still green.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api.Tests/RagIntegrationTests.cs
git commit -m "test(rag): integration coverage for context-enriched system prompt"
```

---

## Self-review checklist

Run this after Phase H to confirm the plan's intent is met:

| Check | How |
|-------|-----|
| Spec section 4 (pgvector + entities) implemented | Phase A produces migration + entities |
| Spec section 5 (ContextBuilder + intent mapping) implemented | Phase E covers all 4 intents |
| Spec section 6 (embedding pipeline) implemented | Phase D delivers Channel + sink + hosted service |
| Spec section 7 (orchestrator wiring) implemented | Phase F delivers Build(context) + intent forwarding |
| Spec section 8 (failure modes graceful) | ContextBuilder catches and returns null; EmbedAsync returns null on error; system prompt accepts null |
| Spec section 10 (out of scope) respected | No cross-user similarity, no nutrition macro replacement, no mobile changes, no multi-turn memory |
| Existing FreakAI tests still pass | Verified in F3, G5, H Step 3 |
| External `FreakAiOrchestrator.ChatAsync` contract preserved | New `intent` parameter is optional with default — pre-existing callers unchanged |
| All new code has tests | Phase B (formatter), Phase E (ContextBuilder), Phase H (integration) |

---

## Risks and notes

- **Test Postgres needs `pgvector` extension installed.** The migration creates the extension on first run, but the local test Postgres user must have permission to `CREATE EXTENSION`. If CI lacks this permission, install it once at provisioning time as a superuser and remove the `CREATE EXTENSION IF NOT EXISTS` from the migration.
- **Embedding latency budget.** `EmbedAsync` for the user's message in `program_analyze` happens inside the request hot path. If this becomes a bottleneck, drop similar-workouts retrieval from `program_analyze` or move it behind a feature flag.
- **Vector type compatibility.** The plan uses `Pgvector.Vector` for entity properties (the spec showed `float[]`). EF Core requires the `Vector` wrapper for proper column mapping. The conversion is a one-line `new Vector(floats)` and `.ToArray()` round-trip — does not change semantics.
- **Channel drop-oldest policy.** Under sustained burst, oldest jobs are dropped. Acceptable per spec section 8 ("silent fail → no retry on same request. Next domain event will re-trigger").
- **Snapshot writes are not de-duplicated.** Multiple events for the same user within seconds will each enqueue a job; the background service overwrites the same row each time. Acceptable for simplicity. If volume becomes an issue, add a debounce in `EmbeddingEventSink`.
