# Phase 1 — Backend Extend ExerciseSet with RIR/Rest/Concentric

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ExerciseSet` entity/DTO'yu `RIR`, `RestSeconds`, `ConcentricTimeSeconds` alanlarıyla genişlet. Migration mevcut set satırlarını legacy değerlerle doldur. Controller mapping bu alanları per-set persist etsin, legacy ExerciseEntry alanlarını last-set'ten türet.

**Architecture:** `f722217` commit'inde `ExerciseSet` iki alanlı (Reps/Weight) eklenmişti. Bu plan 3 alan daha ekler ve migration mevcut veriye backfill uygular. Controller POST/PUT/GET mapping'i bu alanları okur/yazar.

**Tech Stack:** EF Core 10, ASP.NET Core, PostgreSQL, xUnit.

---

## Task 1: Extend `ExerciseSet` entity

**Files:**
- Modify: `FreakLete.Api/Entities/ExerciseSet.cs`

- [ ] **Step 1: Entity'ye 3 alan ekle**

`Weight` property'sinin altına:
```csharp
public int? RIR { get; set; }
public int? RestSeconds { get; set; }
public double? ConcentricTimeSeconds { get; set; }
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/Entities/ExerciseSet.cs
git commit -m "feat: add RIR/Rest/Concentric fields to ExerciseSet entity"
```

---

## Task 2: Extend `ExerciseSetDto`

**Files:**
- Modify: `FreakLete.Api/DTOs/Workout/ExerciseSetDto.cs`

- [ ] **Step 1: Dto'ya alanları ekle**

`Weight` altına:
```csharp
public int? RIR { get; set; }
public int? RestSeconds { get; set; }
public double? ConcentricTimeSeconds { get; set; }
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.Api`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/DTOs/Workout/ExerciseSetDto.cs
git commit -m "feat: extend ExerciseSetDto with RIR/Rest/Concentric"
```

---

## Task 3: Update Controller mapping

**Files:**
- Modify: `FreakLete.Api/Controllers/WorkoutsController.cs`

- [ ] **Step 1: `MapToResponse` içindeki set mapping**

Mevcut ExerciseSetDto projection:
```csharp
.Select(s => new ExerciseSetDto
{
    SetNumber = s.SetNumber,
    Reps = s.Reps,
    Weight = s.Weight
})
```

Değiştir:
```csharp
.Select(s => new ExerciseSetDto
{
    SetNumber = s.SetNumber,
    Reps = s.Reps,
    Weight = s.Weight,
    RIR = s.RIR,
    RestSeconds = s.RestSeconds,
    ConcentricTimeSeconds = s.ConcentricTimeSeconds
})
```

- [ ] **Step 2: `MapToExerciseEntry` içindeki set mapping**

Mevcut ExerciseSet constructor:
```csharp
.Select((s, i) => new ExerciseSet
{
    SetNumber = s.SetNumber > 0 ? s.SetNumber : i + 1,
    Reps = s.Reps,
    Weight = s.Weight
})
```

Değiştir:
```csharp
.Select((s, i) => new ExerciseSet
{
    SetNumber = s.SetNumber > 0 ? s.SetNumber : i + 1,
    Reps = s.Reps,
    Weight = s.Weight,
    RIR = s.RIR,
    RestSeconds = s.RestSeconds,
    ConcentricTimeSeconds = s.ConcentricTimeSeconds
})
```

- [ ] **Step 3: Legacy alanları last-set'ten türet**

`MapToExerciseEntry` içindeki entry constructor'ında:
- `RIR = dto.RIR` → `RIR = sets.Count > 0 ? sets[^1].RIR : dto.RIR`
- `RestSeconds = dto.RestSeconds` → `RestSeconds = sets.Count > 0 ? sets[^1].RestSeconds : dto.RestSeconds`
- `ConcentricTimeSeconds = dto.ConcentricTimeSeconds` → `ConcentricTimeSeconds = sets.Count > 0 ? sets[^1].ConcentricTimeSeconds : dto.ConcentricTimeSeconds`

`GroundContactTimeMs` değişmez (per-set değil, entry-level).

- [ ] **Step 4: Build**

Run: `dotnet build FreakLete.Api`
Expected: 0 error.

- [ ] **Step 5: Commit**

```bash
git add FreakLete.Api/Controllers/WorkoutsController.cs
git commit -m "feat: persist per-set RIR/Rest/Concentric in controller mapping"
```

---

## Task 4: Migration + backfill

**Files:**
- Create: `FreakLete.Api/Migrations/<timestamp>_ExtendExerciseSet.cs`

- [ ] **Step 1: Migration üret**

Run: `dotnet ef migrations add ExtendExerciseSet -p FreakLete.Api -s FreakLete.Api`
Expected: EF 3 yeni kolon ekleyen migration üretir.

- [ ] **Step 2: Backfill SQL ekle**

Generated migration'ın `Up` metoduna, kolonlar eklendikten sonra:
```csharp
migrationBuilder.Sql(@"
    UPDATE ""ExerciseSets"" s
    SET ""RIR"" = e.""RIR"",
        ""RestSeconds"" = e.""RestSeconds"",
        ""ConcentricTimeSeconds"" = e.""ConcentricTimeSeconds""
    FROM ""ExerciseEntries"" e
    WHERE s.""ExerciseEntryId"" = e.""Id""
      AND e.""TrackingMode"" = 'Strength';
");
```

- [ ] **Step 3: Migrate**

Run: `dotnet ef database update -p FreakLete.Api -s FreakLete.Api`
Expected: Migration başarılı.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Migrations/*ExtendExerciseSet*
git commit -m "feat(db): extend ExerciseSet with RIR/Rest/Concentric + backfill"
```

---

## Task 5: Integration test

**Files:**
- Create: `FreakLete.Api.Tests/WorkoutsControllerPerSetExtendedTests.cs`

- [ ] **Step 1: Mevcut test fixture'ı incele**

Read mevcut API test dosyalarını (`FreakLete.Api.Tests/*.cs`) — fixture class adını ve auth pattern'ini öğren. Yeni fixture YAZMA; mevcut reuse et.

- [ ] **Step 2: Test yaz**

```csharp
using FreakLete.Api.DTOs.Workout;
using System.Net.Http.Json;

namespace FreakLete.Api.Tests;

public class WorkoutsControllerPerSetExtendedTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public WorkoutsControllerPerSetExtendedTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_WithPerSetRirRestConcentric_PersistsAllFields()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var workout = new WorkoutRequest
        {
            WorkoutName = "Test Workout",
            WorkoutDate = DateTime.UtcNow,
            Exercises = new List<ExerciseEntryDto>
            {
                new()
                {
                    ExerciseName = "Bench Press",
                    ExerciseCategory = "Push",
                    TrackingMode = "Strength",
                    Sets = new List<ExerciseSetDto>
                    {
                        new() { SetNumber = 1, Reps = 5, Weight = 90, RIR = 3, RestSeconds = 90, ConcentricTimeSeconds = 1.5 },
                        new() { SetNumber = 2, Reps = 5, Weight = 100, RIR = 2, RestSeconds = 120, ConcentricTimeSeconds = 1.4 },
                        new() { SetNumber = 3, Reps = 5, Weight = 110, RIR = 1, RestSeconds = 150, ConcentricTimeSeconds = 1.2 }
                    }
                }
            }
        };

        var post = await client.PostAsJsonAsync("/api/workouts", workout);
        post.EnsureSuccessStatusCode();
        var created = await post.Content.ReadFromJsonAsync<WorkoutResponse>();

        Assert.NotNull(created);
        var exercise = Assert.Single(created!.Exercises);
        Assert.Equal(3, exercise.Sets.Count);
        Assert.Equal(3, exercise.Sets[0].RIR);
        Assert.Equal(90, exercise.Sets[0].RestSeconds);
        Assert.Equal(1.5, exercise.Sets[0].ConcentricTimeSeconds);
        Assert.Equal(1, exercise.Sets[2].RIR);

        // legacy derivations from last set
        Assert.Equal(1, exercise.RIR);
        Assert.Equal(150, exercise.RestSeconds);
        Assert.Equal(1.2, exercise.ConcentricTimeSeconds);
    }
}
```

> **NOTE:** `ApiWebApplicationFactory` / `CreateAuthenticatedClientAsync` class adları farklıysa mevcut test'lerdeki adları kullan. Yapmazsan test runner bulamaz.

- [ ] **Step 3: Test koş**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~WorkoutsControllerPerSetExtendedTests"`
Expected: Test PASS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api.Tests/WorkoutsControllerPerSetExtendedTests.cs
git commit -m "test: per-set RIR/Rest/Concentric persist through controller"
```

---

## Risks

- **Backfill null koruma:** Bazı entries'te RIR/Rest/Concentric null olabilir; UPDATE null yazar — bu doğru davranış (legacy alanlar zaten null'dı).
- **Custom tracking:** `TrackingMode = 'Custom'` kayıtlarda `ExerciseSet` satırı yok. UPDATE bunlara etki etmez.
- **Fixture API:** Mevcut testleri taklit ederek yaz. Yeni DB setup yapma.
