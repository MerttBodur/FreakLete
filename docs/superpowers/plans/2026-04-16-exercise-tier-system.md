# Exercise Tier System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every scoreable exercise produces a 6-level user tier (NeedImprovement → Freak) that is recalculated on every PR save and surfaced on the Profile page.

**Architecture:** Pure tier math lives in `FreakLete.Core` (no EF). `FreakLete.Api` adds per-exercise threshold columns to `ExerciseDefinitions`, a new `UserExerciseTiers` table (upserted on PR save), an `ExerciseTierService`, and two endpoints (`GET /api/profile/tiers` + tier payload on `POST /api/pr-entries`). Mobile consumes both via `IApiClient` and renders a tier-card section on `ProfilePage`.

**Tech Stack:** .NET 10, EF Core (PostgreSQL), xUnit, .NET MAUI (XAML + code-behind), violet-dark design tokens (`Accent`, `AccentGlow`, `CardBorder`, `SurfaceBorder`, `TextPrimary`, `TextSecondary`, `TextMuted`).

---

## Spec Reference

Spec: [docs/superpowers/specs/2026-04-16-exercise-tier-system-design.md](../specs/2026-04-16-exercise-tier-system-design.md)

## File Structure

**FreakLete.Core (pure logic):**
- Create: `FreakLete.Core/Tier/TierLevel.cs` — 6-value enum
- Create: `FreakLete.Core/Tier/ExerciseTierConfig.cs` — immutable record holding `CatalogId`, `TierType`, male/female threshold arrays, parent id, scale
- Create: `FreakLete.Core/Tier/TierResolver.cs` — static `Resolve`, `ResolveInverse`, `GetThresholds`

**FreakLete.Core.Tests:**
- Create: `FreakLete.Core.Tests/TierResolverTests.cs`

**FreakLete.Api (persistence + services):**
- Modify: `FreakLete.Api/Entities/ExerciseDefinition.cs` — add 5 tier fields
- Create: `FreakLete.Api/Entities/UserExerciseTier.cs`
- Modify: `FreakLete.Api/Data/AppDbContext.cs` — DbSet + ModelBuilder config for `UserExerciseTier`, property configs for new `ExerciseDefinition` fields
- Create: `FreakLete.Api/Migrations/*_ExerciseTierSystem.cs` (EF-generated schema migration)
- Create: `FreakLete.Api/Migrations/*_SeedTier1Thresholds.cs` (raw-SQL data migration)
- Create: `FreakLete.Api/DTOs/Tier/ExerciseTierDto.cs`
- Create: `FreakLete.Api/DTOs/Tier/TierResultDto.cs`
- Modify: `FreakLete.Api/DTOs/Performance/PrEntryRequest.cs` — add `CatalogId`
- Modify: `FreakLete.Api/DTOs/Performance/PrEntryResponse.cs` — add `TierResultDto? Tier`
- Create: `FreakLete.Api/Services/ExerciseTierService.cs` + `IExerciseTierService` interface
- Modify: `FreakLete.Api/Program.cs` — register `IExerciseTierService`
- Modify: `FreakLete.Api/Controllers/PrEntriesController.cs` — inject service, call `RecalculateTierAsync` after save
- Create: `FreakLete.Api/Controllers/ProfileController.cs` — `GET /api/profile/tiers` (or add to existing controller if one exists)

**FreakLete.Api.Tests:**
- Create: `FreakLete.Api.Tests/ExerciseTierIntegrationTests.cs`

**Mobile (FreakLete):**
- Create: `Models/ExerciseTierResponse.cs`
- Modify: `Services/IApiClient.cs` — add `GetExerciseTiersAsync`
- Modify: `Services/ApiClient.cs` (or concrete impl file)
- Modify: `Xaml/ProfilePage.xaml` — add `TierCardsSection`
- Modify: `CodeBehind/ProfilePage.xaml.cs` — load and render tiers

---

## Task 1: TierLevel + ExerciseTierConfig (FreakLete.Core)

**Files:**
- Create: `FreakLete.Core/Tier/TierLevel.cs`
- Create: `FreakLete.Core/Tier/ExerciseTierConfig.cs`

- [ ] **Step 1.1: Create TierLevel.cs**

```csharp
namespace FreakLete.Core.Tier;

public enum TierLevel
{
    NeedImprovement = 0,
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Elite = 4,
    Freak = 5
}
```

- [ ] **Step 1.2: Create ExerciseTierConfig.cs**

```csharp
namespace FreakLete.Core.Tier;

public sealed record ExerciseTierConfig(
    string CatalogId,
    string TierType,
    double[] ThresholdsMale,
    double[] ThresholdsFemale,
    string? TierParentId,
    double? TierScale);
```

- [ ] **Step 1.3: Build project**

Run: `dotnet build FreakLete.Core`
Expected: Build succeeded, 0 errors.

- [ ] **Step 1.4: Commit**

```bash
git add FreakLete.Core/Tier/TierLevel.cs FreakLete.Core/Tier/ExerciseTierConfig.cs
git commit -m "feat(core): add TierLevel enum and ExerciseTierConfig record"
```

---

## Task 2: TierResolver — standard direction (FreakLete.Core)

**Files:**
- Create: `FreakLete.Core/Tier/TierResolver.cs`
- Create: `FreakLete.Core.Tests/TierResolverTests.cs`

- [ ] **Step 2.1: Write failing test for Resolve (boundary values)**

Append to `FreakLete.Core.Tests/TierResolverTests.cs`:

```csharp
using FreakLete.Core.Tier;

namespace FreakLete.Core.Tests;

public class TierResolverTests
{
    private static readonly double[] BenchMale = [0.5, 1.0, 1.25, 1.5, 1.75];

    [Theory]
    [InlineData(0.49, TierLevel.NeedImprovement)]
    [InlineData(0.5,  TierLevel.Beginner)]
    [InlineData(0.99, TierLevel.Beginner)]
    [InlineData(1.0,  TierLevel.Intermediate)]
    [InlineData(1.24, TierLevel.Intermediate)]
    [InlineData(1.25, TierLevel.Advanced)]
    [InlineData(1.49, TierLevel.Advanced)]
    [InlineData(1.5,  TierLevel.Elite)]
    [InlineData(1.74, TierLevel.Elite)]
    [InlineData(1.75, TierLevel.Freak)]
    [InlineData(3.0,  TierLevel.Freak)]
    public void Resolve_ReturnsCorrectTier(double value, TierLevel expected)
    {
        Assert.Equal(expected, TierResolver.Resolve(value, BenchMale));
    }
}
```

- [ ] **Step 2.2: Run test — verify FAIL**

Run: `dotnet test FreakLete.Core.Tests --filter "FullyQualifiedName~TierResolverTests"`
Expected: FAIL — `TierResolver` does not exist.

- [ ] **Step 2.3: Implement TierResolver.Resolve**

Create `FreakLete.Core/Tier/TierResolver.cs`:

```csharp
namespace FreakLete.Core.Tier;

public static class TierResolver
{
    public static TierLevel Resolve(double value, double[] thresholds)
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value < thresholds[i]) return (TierLevel)i;
        }
        return TierLevel.Freak;
    }
}
```

- [ ] **Step 2.4: Run test — verify PASS**

Run: `dotnet test FreakLete.Core.Tests --filter "FullyQualifiedName~TierResolverTests"`
Expected: PASS.

- [ ] **Step 2.5: Commit**

```bash
git add FreakLete.Core/Tier/TierResolver.cs FreakLete.Core.Tests/TierResolverTests.cs
git commit -m "feat(core): add TierResolver.Resolve with boundary tests"
```

---

## Task 3: TierResolver.ResolveInverse (athletic sprint)

**Files:**
- Modify: `FreakLete.Core/Tier/TierResolver.cs`
- Modify: `FreakLete.Core.Tests/TierResolverTests.cs`

- [ ] **Step 3.1: Write failing test**

Append to `TierResolverTests`:

```csharp
// Sprint 40yd (seconds) — lower is better. Descending thresholds:
// Freak boundary 4.4, Elite 4.6, Advanced 4.9, Intermediate 5.3, Beginner 5.8
private static readonly double[] SprintMale = [5.8, 5.3, 4.9, 4.6, 4.4];

[Theory]
[InlineData(6.0, TierLevel.NeedImprovement)]
[InlineData(5.8, TierLevel.NeedImprovement)]
[InlineData(5.7, TierLevel.Beginner)]
[InlineData(5.3, TierLevel.Beginner)]
[InlineData(5.2, TierLevel.Intermediate)]
[InlineData(4.9, TierLevel.Intermediate)]
[InlineData(4.8, TierLevel.Advanced)]
[InlineData(4.6, TierLevel.Advanced)]
[InlineData(4.5, TierLevel.Elite)]
[InlineData(4.4, TierLevel.Elite)]
[InlineData(4.3, TierLevel.Freak)]
public void ResolveInverse_ReturnsCorrectTier(double value, TierLevel expected)
{
    Assert.Equal(expected, TierResolver.ResolveInverse(value, SprintMale));
}
```

- [ ] **Step 3.2: Run test — verify FAIL**

Run: `dotnet test FreakLete.Core.Tests --filter "ResolveInverse"`
Expected: FAIL — method missing.

- [ ] **Step 3.3: Implement ResolveInverse**

Append to `TierResolver.cs`:

```csharp
// Thresholds are given worst → best (descending metric values).
// value <= thresholds[i] means user is "better than i-th boundary".
public static TierLevel ResolveInverse(double value, double[] thresholds)
{
    for (int i = 0; i < thresholds.Length; i++)
    {
        if (value > thresholds[i]) return (TierLevel)i;
    }
    return TierLevel.Freak;
}
```

- [ ] **Step 3.4: Run test — verify PASS**

Run: `dotnet test FreakLete.Core.Tests --filter "ResolveInverse"`
Expected: PASS.

- [ ] **Step 3.5: Commit**

```bash
git add FreakLete.Core/Tier/TierResolver.cs FreakLete.Core.Tests/TierResolverTests.cs
git commit -m "feat(core): add TierResolver.ResolveInverse for inverse metrics"
```

---

## Task 4: TierResolver.GetThresholds (Tier-2 scaling)

**Files:**
- Modify: `FreakLete.Core/Tier/TierResolver.cs`
- Modify: `FreakLete.Core.Tests/TierResolverTests.cs`

- [ ] **Step 4.1: Write failing test**

Append to `TierResolverTests`:

```csharp
[Fact]
public void GetThresholds_Tier1_ReturnsOwnArrayForMale()
{
    var deadlift = new ExerciseTierConfig(
        "conventionaldeadlift",
        "StrengthRatio",
        ThresholdsMale: [1.0, 1.5, 2.0, 2.5, 3.0],
        ThresholdsFemale: [0.7, 1.0, 1.4, 1.8, 2.2],
        TierParentId: null,
        TierScale: null);
    var all = new Dictionary<string, ExerciseTierConfig> { [deadlift.CatalogId] = deadlift };

    var result = TierResolver.GetThresholds(deadlift, "Male", all);

    Assert.Equal([1.0, 1.5, 2.0, 2.5, 3.0], result);
}

[Fact]
public void GetThresholds_Tier1_ReturnsFemaleWhenSexIsFemale()
{
    var deadlift = new ExerciseTierConfig(
        "conventionaldeadlift", "StrengthRatio",
        [1.0, 1.5, 2.0, 2.5, 3.0],
        [0.7, 1.0, 1.4, 1.8, 2.2],
        null, null);
    var all = new Dictionary<string, ExerciseTierConfig> { [deadlift.CatalogId] = deadlift };

    var result = TierResolver.GetThresholds(deadlift, "Female", all);

    Assert.Equal([0.7, 1.0, 1.4, 1.8, 2.2], result);
}

[Fact]
public void GetThresholds_Tier2_ScalesParentArray()
{
    var deadlift = new ExerciseTierConfig(
        "conventionaldeadlift", "StrengthRatio",
        [1.0, 1.5, 2.0, 2.5, 3.0],
        [0.7, 1.0, 1.4, 1.8, 2.2],
        null, null);
    var rackPull = new ExerciseTierConfig(
        "rackpull", "StrengthRatio",
        [], [],
        TierParentId: "conventionaldeadlift",
        TierScale: 1.1);
    var all = new Dictionary<string, ExerciseTierConfig>
    {
        [deadlift.CatalogId] = deadlift,
        [rackPull.CatalogId] = rackPull
    };

    var result = TierResolver.GetThresholds(rackPull, "Male", all);

    Assert.Equal([1.1, 1.65, 2.2, 2.75, 3.3], result);
}

[Fact]
public void GetThresholds_Tier2_MissingParent_ReturnsEmptyArray()
{
    var orphan = new ExerciseTierConfig(
        "orphan", "StrengthRatio", [], [],
        TierParentId: "missingparent", TierScale: 1.0);
    var all = new Dictionary<string, ExerciseTierConfig> { [orphan.CatalogId] = orphan };

    var result = TierResolver.GetThresholds(orphan, "Male", all);

    Assert.Empty(result);
}

[Fact]
public void GetThresholds_SexEmpty_DefaultsToMale()
{
    var bench = new ExerciseTierConfig(
        "benchpress", "StrengthRatio",
        [0.5, 1.0, 1.25, 1.5, 1.75],
        [0.35, 0.7, 0.9, 1.1, 1.35],
        null, null);
    var all = new Dictionary<string, ExerciseTierConfig> { [bench.CatalogId] = bench };

    var result = TierResolver.GetThresholds(bench, "", all);

    Assert.Equal([0.5, 1.0, 1.25, 1.5, 1.75], result);
}
```

- [ ] **Step 4.2: Run test — verify FAIL**

Run: `dotnet test FreakLete.Core.Tests --filter "GetThresholds"`
Expected: FAIL — method missing.

- [ ] **Step 4.3: Implement GetThresholds**

Append to `TierResolver.cs`:

```csharp
public static double[] GetThresholds(
    ExerciseTierConfig config,
    string sex,
    IReadOnlyDictionary<string, ExerciseTierConfig> allConfigs)
{
    if (config.TierParentId is not null && config.TierScale.HasValue)
    {
        if (!allConfigs.TryGetValue(config.TierParentId, out var parent))
        {
            return [];
        }
        var parentArr = string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase)
            ? parent.ThresholdsFemale
            : parent.ThresholdsMale;
        double scale = config.TierScale.Value;
        return parentArr.Select(t => t * scale).ToArray();
    }

    return string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase)
        ? config.ThresholdsFemale
        : config.ThresholdsMale;
}
```

- [ ] **Step 4.4: Run test — verify PASS**

Run: `dotnet test FreakLete.Core.Tests --filter "GetThresholds"`
Expected: PASS (all 5 new tests).

- [ ] **Step 4.5: Commit**

```bash
git add FreakLete.Core/Tier/TierResolver.cs FreakLete.Core.Tests/TierResolverTests.cs
git commit -m "feat(core): add TierResolver.GetThresholds with Tier-2 scaling"
```

---

## Task 5: ExerciseDefinition tier fields + UserExerciseTier entity + DbContext

**Files:**
- Modify: `FreakLete.Api/Entities/ExerciseDefinition.cs`
- Create: `FreakLete.Api/Entities/UserExerciseTier.cs`
- Modify: `FreakLete.Api/Entities/User.cs`
- Modify: `FreakLete.Api/Data/AppDbContext.cs`

- [ ] **Step 5.1: Add tier fields to ExerciseDefinition**

Append inside `FreakLete.Api/Entities/ExerciseDefinition.cs` (before closing brace of the class):

```csharp
    public string TierType { get; set; } = string.Empty;
    public string TierThresholdsMale { get; set; } = string.Empty;
    public string TierThresholdsFemale { get; set; } = string.Empty;
    public string? TierParentId { get; set; }
    public double? TierScale { get; set; }
```

- [ ] **Step 5.2: Create UserExerciseTier.cs**

```csharp
namespace FreakLete.Api.Entities;

public class UserExerciseTier
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CatalogId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public double RawValue { get; set; }
    public double? BasisValue { get; set; }
    public double? Ratio { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
```

- [ ] **Step 5.3: Add navigation to User**

Modify `FreakLete.Api/Entities/User.cs`, add next to other collections:

```csharp
    public ICollection<UserExerciseTier> ExerciseTiers { get; set; } = [];
```

- [ ] **Step 5.4: Wire DbContext**

In `FreakLete.Api/Data/AppDbContext.cs`, add DbSet next to the others:

```csharp
    public DbSet<UserExerciseTier> UserExerciseTiers => Set<UserExerciseTier>();
```

Inside the existing `ExerciseDefinition` model block (locate `modelBuilder.Entity<ExerciseDefinition>(e => { ... });`), add:

```csharp
            e.Property(d => d.TierType).HasMaxLength(30);
            e.Property(d => d.TierThresholdsMale).HasMaxLength(200);
            e.Property(d => d.TierThresholdsFemale).HasMaxLength(200);
            e.Property(d => d.TierParentId).HasMaxLength(100);
```

After the `ExerciseDefinition` block, add a new configuration block:

```csharp
        // UserExerciseTier
        modelBuilder.Entity<UserExerciseTier>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.UserId, t.CatalogId }).IsUnique();
            e.HasOne(t => t.User)
             .WithMany(u => u.ExerciseTiers)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(t => t.CatalogId).HasMaxLength(100);
            e.Property(t => t.ExerciseName).HasMaxLength(200);
            e.Property(t => t.TierLevel).HasMaxLength(30);
        });
```

- [ ] **Step 5.5: Build API**

Run: `dotnet build FreakLete.Api`
Expected: Build succeeded.

- [ ] **Step 5.6: Commit**

```bash
git add FreakLete.Api/Entities/ExerciseDefinition.cs FreakLete.Api/Entities/UserExerciseTier.cs FreakLete.Api/Entities/User.cs FreakLete.Api/Data/AppDbContext.cs
git commit -m "feat(api): add tier fields to ExerciseDefinition and UserExerciseTier entity"
```

---

## Task 6: EF schema migration

**Files:**
- Generated: `FreakLete.Api/Migrations/*_ExerciseTierSystem.cs` (+ `.Designer.cs`)
- Auto-updated: `FreakLete.Api/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 6.1: Generate migration**

Run from repo root:

```bash
dotnet ef migrations add ExerciseTierSystem --project FreakLete.Api --startup-project FreakLete.Api
```

Expected: new migration file created under `FreakLete.Api/Migrations/`.

- [ ] **Step 6.2: Inspect migration**

Open the generated `*_ExerciseTierSystem.cs`. Confirm in `Up()`:
- `AlterTable("ExerciseDefinitions")` / `AddColumn` for: `TierType`, `TierThresholdsMale`, `TierThresholdsFemale`, `TierParentId`, `TierScale`
- `CreateTable("UserExerciseTiers")` with `Id`, `UserId`, `CatalogId`, `ExerciseName`, `TierLevel`, `RawValue`, `BasisValue`, `Ratio`, `CalculatedAt`
- Unique index on `(UserId, CatalogId)`
- Foreign key on `UserId → Users.Id`, cascade

If anything is missing, fix the entity/model config (Task 5) and regenerate.

- [ ] **Step 6.3: Build**

Run: `dotnet build FreakLete.Api`
Expected: Build succeeded.

- [ ] **Step 6.4: Commit**

```bash
git add FreakLete.Api/Migrations/
git commit -m "feat(api): add ExerciseTierSystem schema migration"
```

---

## Task 7: Seed Tier-1 thresholds (data migration)

**Files:**
- Generated empty + hand-edited: `FreakLete.Api/Migrations/*_SeedTier1Thresholds.cs`

Thresholds below are the initial values to ship; they can be refined later per spec §7. Values are bodyweight multipliers for Strength, absolute cm/seconds for Athletic.

- [ ] **Step 7.1: Generate empty migration**

```bash
dotnet ef migrations add SeedTier1Thresholds --project FreakLete.Api --startup-project FreakLete.Api
```

- [ ] **Step 7.2: Edit Up()/Down()**

Replace the generated empty `Up(MigrationBuilder migrationBuilder)` body with:

```csharp
// Strength (StrengthRatio) — bodyweight multiplier, 5-value threshold array
UpdateTier(migrationBuilder, "benchpress",             "StrengthRatio", "[0.5,1.0,1.25,1.5,1.75]",  "[0.35,0.7,0.9,1.1,1.35]");
UpdateTier(migrationBuilder, "backsquat",              "StrengthRatio", "[0.75,1.25,1.5,2.0,2.5]",  "[0.5,0.9,1.1,1.5,1.9]");
UpdateTier(migrationBuilder, "conventionaldeadlift",   "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",    "[0.7,1.0,1.4,1.8,2.2]");
UpdateTier(migrationBuilder, "sumodeadlift",           "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",    "[0.7,1.0,1.4,1.8,2.2]");
UpdateTier(migrationBuilder, "overheadpress",          "StrengthRatio", "[0.35,0.55,0.75,0.95,1.15]","[0.2,0.4,0.55,0.7,0.85]");
UpdateTier(migrationBuilder, "powerclean",             "StrengthRatio", "[0.6,0.9,1.2,1.5,1.8]",    "[0.4,0.65,0.85,1.05,1.3]");
UpdateTier(migrationBuilder, "powersnatch",            "StrengthRatio", "[0.4,0.7,0.9,1.15,1.4]",   "[0.3,0.5,0.65,0.8,1.0]");
UpdateTier(migrationBuilder, "frontsquat",             "StrengthRatio", "[0.6,1.0,1.25,1.6,2.0]",   "[0.4,0.7,0.9,1.2,1.55]");
UpdateTier(migrationBuilder, "romaniandeadlift",       "StrengthRatio", "[0.8,1.2,1.6,2.0,2.4]",    "[0.55,0.8,1.1,1.4,1.75]");
UpdateTier(migrationBuilder, "barbellrow",             "StrengthRatio", "[0.5,0.9,1.15,1.4,1.7]",   "[0.35,0.6,0.8,1.0,1.2]");
UpdateTier(migrationBuilder, "pullup",                 "StrengthRatio", "[1.0,1.2,1.4,1.6,1.9]",    "[1.0,1.1,1.25,1.45,1.7]");
UpdateTier(migrationBuilder, "trapbardeadlift",        "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",    "[0.7,1.0,1.4,1.8,2.2]");
UpdateTier(migrationBuilder, "hipthrust",              "StrengthRatio", "[1.0,1.5,2.0,2.5,3.0]",    "[0.7,1.1,1.5,1.9,2.3]");
UpdateTier(migrationBuilder, "pushpress",              "StrengthRatio", "[0.5,0.75,0.95,1.2,1.5]",  "[0.3,0.5,0.7,0.9,1.1]");

// Athletic (AthleticAbsolute) — raw values in cm or unitless RSI
UpdateTier(migrationBuilder, "verticaljump",           "AthleticAbsolute", "[30,45,55,65,75]",   "[20,32,42,52,60]");
UpdateTier(migrationBuilder, "standingbroadjump",      "AthleticAbsolute", "[180,220,250,280,310]","[150,190,220,245,275]");
UpdateTier(migrationBuilder, "rsi",                    "AthleticAbsolute", "[1.5,2.0,2.5,3.0,3.5]","[1.2,1.6,2.0,2.5,3.0]");

// Athletic (AthleticInverse) — seconds, lower is better, stored descending (worst → best)
UpdateTier(migrationBuilder, "fortyyarddash",          "AthleticInverse", "[5.8,5.3,4.9,4.6,4.4]","[6.6,6.0,5.5,5.1,4.8]");
UpdateTier(migrationBuilder, "tenmetersprint",         "AthleticInverse", "[2.2,2.0,1.85,1.75,1.65]","[2.5,2.25,2.05,1.9,1.8]");
```

Add a private helper at the bottom of the migration class:

```csharp
private static void UpdateTier(
    MigrationBuilder b, string catalogId, string tierType, string male, string female)
{
    b.Sql($"""
        UPDATE "ExerciseDefinitions"
        SET "TierType" = '{tierType}',
            "TierThresholdsMale" = '{male}',
            "TierThresholdsFemale" = '{female}'
        WHERE "CatalogId" = '{catalogId}';
        """);
}
```

Replace the empty `Down()` body with:

```csharp
migrationBuilder.Sql("""
    UPDATE "ExerciseDefinitions"
    SET "TierType" = '',
        "TierThresholdsMale" = '',
        "TierThresholdsFemale" = ''
    WHERE "TierType" IN ('StrengthRatio','AthleticAbsolute','AthleticInverse');
    """);
```

> Note: Tier-2 variation seeding (rack pull, paused deadlift, block pull, deficit deadlift etc.) is deliberately deferred — can be added in a follow-up migration once CatalogIds for variations are confirmed. Tier-1 covers the spec §7 Tier-1 list and unblocks end-to-end tiering.

- [ ] **Step 7.3: Build**

Run: `dotnet build FreakLete.Api`
Expected: Build succeeded.

- [ ] **Step 7.4: Commit**

```bash
git add FreakLete.Api/Migrations/
git commit -m "feat(api): seed Tier-1 threshold data for 19 exercises"
```

---

## Task 8: DTOs

**Files:**
- Create: `FreakLete.Api/DTOs/Tier/ExerciseTierDto.cs`
- Create: `FreakLete.Api/DTOs/Tier/TierResultDto.cs`
- Modify: `FreakLete.Api/DTOs/Performance/PrEntryRequest.cs`
- Modify: `FreakLete.Api/DTOs/Performance/PrEntryResponse.cs`

- [ ] **Step 8.1: Create ExerciseTierDto.cs**

```csharp
namespace FreakLete.Api.DTOs.Tier;

public class ExerciseTierDto
{
    public string CatalogId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public double RawValue { get; set; }
    public double? Ratio { get; set; }
    public DateTime CalculatedAt { get; set; }
}
```

- [ ] **Step 8.2: Create TierResultDto.cs**

```csharp
namespace FreakLete.Api.DTOs.Tier;

public class TierResultDto
{
    public string CatalogId { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public string? PreviousTierLevel { get; set; }
    public bool LeveledUp { get; set; }
}
```

- [ ] **Step 8.3: Extend PrEntryRequest**

Add inside `PrEntryRequest` (before closing brace):

```csharp
    [MaxLength(100)]
    public string? CatalogId { get; set; }
```

- [ ] **Step 8.4: Extend PrEntryResponse**

Add a using at the top:

```csharp
using FreakLete.Api.DTOs.Tier;
```

Add inside `PrEntryResponse` (before closing brace):

```csharp
    public TierResultDto? Tier { get; set; }
```

- [ ] **Step 8.5: Build**

Run: `dotnet build FreakLete.Api`
Expected: Build succeeded.

- [ ] **Step 8.6: Commit**

```bash
git add FreakLete.Api/DTOs/
git commit -m "feat(api): add tier DTOs and CatalogId/Tier fields to PR DTOs"
```

---

## Task 9: ExerciseTierService

**Files:**
- Create: `FreakLete.Api/Services/IExerciseTierService.cs`
- Create: `FreakLete.Api/Services/ExerciseTierService.cs`
- Modify: `FreakLete.Api/Program.cs`

- [ ] **Step 9.1: Create interface**

`FreakLete.Api/Services/IExerciseTierService.cs`:

```csharp
using FreakLete.Api.DTOs.Tier;

namespace FreakLete.Api.Services;

public interface IExerciseTierService
{
    Task<TierResultDto?> RecalculateTierAsync(
        int userId,
        string? catalogId,
        string exerciseName,
        string trackingMode,
        int weight,
        int reps,
        int? rir,
        double? athleticRawValue,
        CancellationToken ct = default);

    Task<List<ExerciseTierDto>> GetTiersForUserAsync(int userId, CancellationToken ct = default);
}
```

- [ ] **Step 9.2: Create implementation**

`FreakLete.Api/Services/ExerciseTierService.cs`:

```csharp
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Tier;
using FreakLete.Api.Entities;
using FreakLete.Core.Tier;
using FreakLete.Services; // CalculationService lives in FreakLete.Core with namespace FreakLete.Services
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

public class ExerciseTierService : IExerciseTierService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ExerciseTierService> _log;

    public ExerciseTierService(AppDbContext db, ILogger<ExerciseTierService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<TierResultDto?> RecalculateTierAsync(
        int userId,
        string? catalogId,
        string exerciseName,
        string trackingMode,
        int weight,
        int reps,
        int? rir,
        double? athleticRawValue,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(catalogId)) return null;

        var def = await _db.ExerciseDefinitions
            .FirstOrDefaultAsync(d => d.CatalogId == catalogId, ct);
        if (def is null || string.IsNullOrWhiteSpace(def.TierType)) return null;

        // Isolation rule: strength tier applies only when mechanic != "isolation"
        if (string.Equals(def.TierType, "StrengthRatio", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(def.Mechanic, "isolation", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        // Load all configs (for Tier-2 parent lookup) — catalog is small, single-shot read is fine.
        var allDefs = await _db.ExerciseDefinitions
            .Where(d => d.TierType != "")
            .ToListAsync(ct);
        var configs = allDefs.ToDictionary(
            d => d.CatalogId,
            d => ToConfig(d),
            StringComparer.OrdinalIgnoreCase);

        if (!configs.TryGetValue(catalogId, out var cfg)) return null;

        double rawValue;
        double? basisValue = null;
        double? ratio = null;
        TierLevel tier;

        if (string.Equals(cfg.TierType, "StrengthRatio", StringComparison.OrdinalIgnoreCase))
        {
            if (user.WeightKg is null or <= 0)
            {
                _log.LogInformation("Skipping tier: user {UserId} has no weight", userId);
                return null;
            }
            if (weight <= 0 || reps <= 0) return null;

            rawValue = CalculationService.CalculateOneRm(weight, reps, rir ?? 0);
            basisValue = user.WeightKg.Value;
            ratio = rawValue / basisValue.Value;

            var thresholds = TierResolver.GetThresholds(cfg, user.Sex, configs);
            if (thresholds.Length == 0) return null;
            tier = TierResolver.Resolve(ratio.Value, thresholds);
        }
        else if (string.Equals(cfg.TierType, "AthleticAbsolute", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(cfg.TierType, "AthleticInverse", StringComparison.OrdinalIgnoreCase))
        {
            if (athleticRawValue is null or <= 0) return null;
            rawValue = athleticRawValue.Value;

            var thresholds = TierResolver.GetThresholds(cfg, user.Sex, configs);
            if (thresholds.Length == 0) return null;
            tier = cfg.TierType.Equals("AthleticInverse", StringComparison.OrdinalIgnoreCase)
                ? TierResolver.ResolveInverse(rawValue, thresholds)
                : TierResolver.Resolve(rawValue, thresholds);
        }
        else
        {
            return null;
        }

        string newLevel = tier.ToString();
        string? previousLevel = null;

        var existing = await _db.UserExerciseTiers
            .FirstOrDefaultAsync(t => t.UserId == userId && t.CatalogId == catalogId, ct);

        if (existing is null)
        {
            _db.UserExerciseTiers.Add(new UserExerciseTier
            {
                UserId = userId,
                CatalogId = catalogId,
                ExerciseName = exerciseName,
                TierLevel = newLevel,
                RawValue = rawValue,
                BasisValue = basisValue,
                Ratio = ratio,
                CalculatedAt = DateTime.UtcNow
            });
        }
        else
        {
            previousLevel = existing.TierLevel;
            existing.ExerciseName = exerciseName;
            existing.TierLevel = newLevel;
            existing.RawValue = rawValue;
            existing.BasisValue = basisValue;
            existing.Ratio = ratio;
            existing.CalculatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);

        bool leveledUp = previousLevel is not null &&
            Enum.TryParse<TierLevel>(previousLevel, out var prev) &&
            (int)tier > (int)prev;

        return new TierResultDto
        {
            CatalogId = catalogId,
            TierLevel = newLevel,
            PreviousTierLevel = previousLevel,
            LeveledUp = leveledUp
        };
    }

    public async Task<List<ExerciseTierDto>> GetTiersForUserAsync(int userId, CancellationToken ct = default)
    {
        return await _db.UserExerciseTiers
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CalculatedAt)
            .Select(t => new ExerciseTierDto
            {
                CatalogId = t.CatalogId,
                ExerciseName = t.ExerciseName,
                TierLevel = t.TierLevel,
                RawValue = t.RawValue,
                Ratio = t.Ratio,
                CalculatedAt = t.CalculatedAt
            })
            .ToListAsync(ct);
    }

    private static ExerciseTierConfig ToConfig(ExerciseDefinition d) => new(
        d.CatalogId,
        d.TierType,
        ParseArr(d.TierThresholdsMale),
        ParseArr(d.TierThresholdsFemale),
        string.IsNullOrWhiteSpace(d.TierParentId) ? null : d.TierParentId,
        d.TierScale);

    private static double[] ParseArr(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<double[]>(json) ?? []; }
        catch { return []; }
    }
}
```

- [ ] **Step 9.3: Register in DI**

In `FreakLete.Api/Program.cs`, next to other `builder.Services.AddScoped<...>()` registrations, add:

```csharp
builder.Services.AddScoped<IExerciseTierService, ExerciseTierService>();
```

- [ ] **Step 9.4: Build**

Run: `dotnet build FreakLete.Api`
Expected: Build succeeded.

- [ ] **Step 9.5: Commit**

```bash
git add FreakLete.Api/Services/ FreakLete.Api/Program.cs
git commit -m "feat(api): add ExerciseTierService with PR-triggered recalculation"
```

---

## Task 10: Controller wiring — PR endpoint + profile tiers endpoint

**Files:**
- Modify: `FreakLete.Api/Controllers/PrEntriesController.cs`
- Create: `FreakLete.Api/Controllers/ProfileTiersController.cs`

- [ ] **Step 10.1: Inject service and call on Create**

Replace the current `PrEntriesController` constructor + `Create` method with:

```csharp
private readonly AppDbContext _db;
private readonly IExerciseTierService _tierService;

public PrEntriesController(AppDbContext db, IExerciseTierService tierService)
{
    _db = db;
    _tierService = tierService;
}

[HttpPost]
public async Task<ActionResult<PrEntryResponse>> Create(PrEntryRequest request)
{
    var userId = User.GetUserId();
    var entry = new PrEntry
    {
        UserId = userId,
        ExerciseName = request.ExerciseName,
        ExerciseCategory = request.ExerciseCategory,
        TrackingMode = request.TrackingMode,
        Weight = request.Weight,
        Reps = request.Reps,
        RIR = request.RIR,
        Metric1Value = request.Metric1Value,
        Metric1Unit = request.Metric1Unit,
        Metric2Value = request.Metric2Value,
        Metric2Unit = request.Metric2Unit,
        GroundContactTimeMs = request.GroundContactTimeMs,
        ConcentricTimeSeconds = request.ConcentricTimeSeconds,
        CreatedAt = DateTime.UtcNow
    };

    _db.PrEntries.Add(entry);
    await _db.SaveChangesAsync();

    var tier = await _tierService.RecalculateTierAsync(
        userId,
        request.CatalogId,
        request.ExerciseName,
        request.TrackingMode,
        request.Weight,
        request.Reps,
        request.RIR,
        athleticRawValue: request.Metric1Value);

    var response = MapToResponse(entry);
    response.Tier = tier;
    return CreatedAtAction(nameof(GetById), new { id = entry.Id }, response);
}
```

Add `using FreakLete.Api.Services;` if not present.

- [ ] **Step 10.2: Create ProfileTiersController**

`FreakLete.Api/Controllers/ProfileTiersController.cs`:

```csharp
using FreakLete.Api.DTOs.Tier;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileTiersController : ControllerBase
{
    private readonly IExerciseTierService _service;

    public ProfileTiersController(IExerciseTierService service)
    {
        _service = service;
    }

    [HttpGet("tiers")]
    public async Task<ActionResult<List<ExerciseTierDto>>> GetTiers(CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await _service.GetTiersForUserAsync(userId, ct));
    }
}
```

- [ ] **Step 10.3: Build**

Run: `dotnet build FreakLete.Api`
Expected: Build succeeded.

- [ ] **Step 10.4: Commit**

```bash
git add FreakLete.Api/Controllers/
git commit -m "feat(api): wire tier recalc on PR create and GET /api/profile/tiers"
```

---

## Task 11: Integration tests

**Files:**
- Create: `FreakLete.Api.Tests/ExerciseTierIntegrationTests.cs`

- [ ] **Step 11.1: Write tests**

```csharp
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

        // First PR: 40kg × 5 reps × 1 RIR → 1RM ≈ 48 → ratio 0.6 → Beginner
        var first = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 40, reps = 5, rir = 1
        });
        var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync()).RootElement;
        var firstLevel = firstJson.GetProperty("tier").GetProperty("tierLevel").GetString();

        // Second PR: 120kg × 3 × 0 → 1RM ≈ 132 → ratio 1.65 → Elite
        var second = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress",
            exerciseName = "Bench Press",
            trackingMode = "Strength",
            weight = 120, reps = 3, rir = 0
        });
        var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement
            .GetProperty("tier");

        Assert.Equal(firstLevel, secondJson.GetProperty("previousTierLevel").GetString());
        Assert.True(secondJson.GetProperty("leveledUp").GetBoolean());
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
```

- [ ] **Step 11.2: Run tests — verify PASS**

Run: `dotnet test FreakLete.Api.Tests --filter "FullyQualifiedName~ExerciseTierIntegrationTests"`
Expected: PASS — all 5 tests green.

- [ ] **Step 11.3: Commit**

```bash
git add FreakLete.Api.Tests/ExerciseTierIntegrationTests.cs
git commit -m "test(api): add ExerciseTier integration tests"
```

---

## Task 12: Mobile — DTO + IApiClient + ApiClient

**Files:**
- Create: `Models/ExerciseTierResponse.cs`
- Modify: `Services/IApiClient.cs`
- Modify: the concrete `ApiClient` implementation (locate the class implementing `IApiClient`, typically `Services/ApiClient.cs`)

- [ ] **Step 12.1: Create DTO**

`Models/ExerciseTierResponse.cs`:

```csharp
namespace FreakLete.Models;

public class ExerciseTierResponse
{
    public string CatalogId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public double RawValue { get; set; }
    public double? Ratio { get; set; }
    public DateTime CalculatedAt { get; set; }
}
```

- [ ] **Step 12.2: Extend IApiClient**

Add inside `IApiClient`:

```csharp
    Task<ApiResult<List<ExerciseTierResponse>>> GetExerciseTiersAsync();
```

Add the using at the top if the interface file doesn't already import `FreakLete.Models`:

```csharp
using FreakLete.Models;
```

- [ ] **Step 12.3: Implement in ApiClient**

Locate the class implementing `IApiClient`. Follow the existing pattern used by e.g. `GetAthleticPerformancesAsync`. Add:

```csharp
public Task<ApiResult<List<ExerciseTierResponse>>> GetExerciseTiersAsync()
    => GetAsync<List<ExerciseTierResponse>>("/api/profile/tiers");
```

> If the concrete client uses a different helper (e.g. a manual `HttpClient` call), replicate the exact idiom used by another `GET` list endpoint in the same file. Do not introduce a new pattern.

- [ ] **Step 12.4: Build mobile**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: Build succeeded.

- [ ] **Step 12.5: Commit**

```bash
git add Models/ExerciseTierResponse.cs Services/IApiClient.cs Services/ApiClient.cs
git commit -m "feat(mobile): add GetExerciseTiersAsync to IApiClient"
```

---

## Task 13: Mobile — Profile page tier cards

**Files:**
- Modify: `Xaml/ProfilePage.xaml`
- Modify: `CodeBehind/ProfilePage.xaml.cs`

Visual target (per [DESIGN.md](../../../DESIGN.md) §2, §4):
- Section title: `SubHeadline` style (24px OpenSansSemibold)
- Each row: `CardBorder` style (`Surface` bg, `SurfaceBorder` stroke, radius 24, padding 18)
- Exercise name: `OpenSansSemibold` 15px, `TextPrimary`
- Tier label: `OpenSansSemibold` 13px, `AccentGlow`
- Raw metric line: `OpenSansRegular` 13px, `TextSecondary`
- Empty state text: `OpenSansRegular` 14px, `TextMuted`

- [ ] **Step 13.1: Add XAML section**

In `Xaml/ProfilePage.xaml`, add the following block inside the main scrollable content stack, after the existing metrics section (or wherever other dashboard cards are grouped):

```xml
<VerticalStackLayout Spacing="12" Padding="0,24,0,0">
    <Label Text="Exercise Tiers" Style="{StaticResource SubHeadline}" />

    <Label x:Name="TierEmptyLabel"
           Text="No tier yet — log a PR to see your level."
           FontFamily="OpenSansRegular"
           FontSize="14"
           TextColor="{StaticResource TextMuted}"
           IsVisible="False" />

    <VerticalStackLayout x:Name="TierCardsContainer" Spacing="10" />
</VerticalStackLayout>
```

- [ ] **Step 13.2: Load and render tiers in code-behind**

In `CodeBehind/ProfilePage.xaml.cs`, inside the existing `LoadProfileAsync()` method (or equivalent page-load entry point), add a parallel call after the existing profile fetch:

```csharp
var tierResult = await _apiClient.GetExerciseTiersAsync();
RenderTierCards(tierResult.IsSuccess ? tierResult.Data : null);
```

Then add this helper in the class:

```csharp
private void RenderTierCards(List<ExerciseTierResponse>? tiers)
{
    TierCardsContainer.Children.Clear();

    if (tiers is null || tiers.Count == 0)
    {
        TierEmptyLabel.IsVisible = true;
        return;
    }
    TierEmptyLabel.IsVisible = false;

    foreach (var t in tiers)
    {
        TierCardsContainer.Children.Add(BuildTierCard(t));
    }
}

private static Border BuildTierCard(ExerciseTierResponse t)
{
    var name = new Label
    {
        Text = t.ExerciseName,
        FontFamily = "OpenSansSemibold",
        FontSize = 15,
        TextColor = (Color)Application.Current!.Resources["TextPrimary"]
    };
    var tierLbl = new Label
    {
        Text = t.TierLevel,
        FontFamily = "OpenSansSemibold",
        FontSize = 13,
        TextColor = (Color)Application.Current!.Resources["AccentGlow"]
    };
    var metric = new Label
    {
        Text = t.Ratio is not null
            ? $"{t.RawValue:0.#} kg · x{t.Ratio:0.00} BW"
            : $"{t.RawValue:0.##}",
        FontFamily = "OpenSansRegular",
        FontSize = 13,
        TextColor = (Color)Application.Current!.Resources["TextSecondary"]
    };

    var header = new Grid
    {
        ColumnDefinitions =
        {
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Auto)
        }
    };
    Grid.SetColumn(name, 0);
    Grid.SetColumn(tierLbl, 1);
    header.Children.Add(name);
    header.Children.Add(tierLbl);

    var stack = new VerticalStackLayout { Spacing = 6 };
    stack.Children.Add(header);
    stack.Children.Add(metric);

    return new Border
    {
        Style = (Style)Application.Current!.Resources["CardBorder"],
        Content = stack
    };
}
```

If a `using FreakLete.Models;` is missing, add it.

- [ ] **Step 13.3: Build and smoke test**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: Build succeeded.

Manual smoke: deploy to emulator, log a bench press PR (with catalogId), open Profile page, verify the new "Exercise Tiers" section renders a card with a tier label. Record result verbally — do not claim success without visual verification per CLAUDE.md §4.

- [ ] **Step 13.4: Commit**

```bash
git add Xaml/ProfilePage.xaml CodeBehind/ProfilePage.xaml.cs
git commit -m "feat(mobile): render exercise tier cards on ProfilePage"
```

---

## Self-Review (completed)

**Spec coverage:**

| Spec section | Task |
|---|---|
| §1 Scoreable rule + Tier-1/Tier-2 | Tasks 4 (scaling), 7 (seed list), 9 (isolation filter) |
| §2 StrengthRatio / AthleticAbsolute / AthleticInverse thresholds | Tasks 2, 3, 7, 9 |
| §3 UserExerciseTier entity + ExerciseDefinition additions | Tasks 5, 6 |
| §4 TierLevel + TierResolver | Tasks 1-4 |
| §5 API flow (PR save → recalc → response field) + GET /api/profile/tiers | Tasks 8, 9, 10 |
| §6 Mobile profile tier cards | Tasks 12, 13 |
| §7 Tier-1 list thresholds | Task 7 |
| §8 Required tests | Tasks 2, 3, 4, 11 |
| §9 Edge cases (WeightKg null, Sex empty, not scoreable, Tier-2 parent missing, inverse) | Tasks 4, 9 (guards), 11 (weight-null test) |

**Placeholder scan:** No "TBD"/"similar to"/"add error handling" placeholders; every code step contains runnable code.

**Type consistency:** `TierLevel`, `ExerciseTierConfig`, `TierResolver.{Resolve, ResolveInverse, GetThresholds}`, `UserExerciseTier`, `ExerciseTierDto`, `TierResultDto`, `IExerciseTierService`, `ExerciseTierService`, `ExerciseTierResponse` — all referenced with identical names across tasks.

---

Plan complete and saved to `docs/superpowers/plans/2026-04-16-exercise-tier-system.md`. Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
