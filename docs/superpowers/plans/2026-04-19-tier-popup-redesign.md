# Tier Congrats Popup + Profile Tier Removal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move exercise tier feedback from Profile into the PR-save flow on `CalculationsPage` via a congratulations popup + inline next-milestone label, and fix the startup seed drift warning.

**Architecture:** Server extends `TierResultDto` with previous-level / next-milestone fields and computes them inside `ExerciseTierService.RecalculateTierAsync`. Client consumes the richer DTO: shows a `TierCongratsPopup` on level-up or first tier earned, renders an inline next-milestone label below the save confirmation, and removes the Profile tier UI entirely. Startup gets an idempotent seed guard that re-UPSERTs tier-eligible ExerciseDefinitions if the `StrengthRatio` rows are missing.

**Tech Stack:** .NET MAUI (C# / XAML, CommunityToolkit.Maui Popup), ASP.NET Core (EF Core, PostgreSQL), xUnit tests.

---

## Spec-to-Code Pre-Reads (Do Not Skip)

Before Task 0, the implementer MUST be aware of these reality checks from this codebase (the spec was written at a higher abstraction):

1. **TierLevel enum uses different names than spec.** Actual values in `FreakLete.Core/Tier/TierLevel.cs`: `NeedImprovement=0, Beginner=1, Intermediate=2, Advanced=3, Elite=4, Freak=5`. The spec's `Untrained/Novice/Intermediate/Advanced/Elite/Godlike` wording must be mapped to these names in every test, popup color, and string. Use the names from the enum file, not from the spec prose.

2. **`TrackingMode` string on `PrEntryResponse` is `"Strength"` or `"Custom"` only** (MAUI-side enum). The athletic tracking-mode strings (`AthleticHeight`, `AthleticDistance`, `AthleticIndex`, `AthleticTime`) live on server-side `ExerciseDefinitions.TrackingMode`. The popup/label formatting needs the server value. Plan solves this by adding a new `TrackingMode` string field onto `TierResultDto` that carries `def.TrackingMode` (server side) through to the client.

3. **Seed migration `20260417120000_SeedTierEligibleExerciseDefinitions.cs` already uses `ON CONFLICT ("CatalogId") DO UPDATE`.** The seed rows list is inside the migration's `SeedRow[] Rows` constant. Startup guard should reuse the same row data — extracting `Rows` + the SQL block into a shared internal static class is the clean path. See Task 5.

4. **`LeveledUp` already exists on `TierResultDto`.** Current flag name is `LeveledUp`, not `IsLevelUp`. Plan keeps `LeveledUp` (rename is an unnecessary breaking change) and adds only the new fields.

5. **`PreviousTierLevel` already exists on `TierResultDto`.** Plan keeps `PreviousTierLevel` (spec says `PreviousLevel`). Use the existing name.

6. **Package `CommunityToolkit.Maui` is NOT yet referenced** — only `CommunityToolkit.Maui.MediaElement 8.0.1` is. Task 0 adds it.

7. **Mobile project's `Models/TierResult.cs` does not exist yet.** Current `Services/ApiClient.cs` defines its own `PrEntryResponse` class (at `Services/ApiClient.cs:574`) that does NOT expose a `Tier` property. You must add a client-side `Tier` property holding a new `TierResult` class (Task 8) for the mobile app to consume the new fields.

---

## File Map

| File | Action |
|---|---|
| `FreakLete.csproj` | Add `CommunityToolkit.Maui` package reference |
| `MauiProgram.cs` | Wire `UseMauiCommunityToolkit()` |
| `FreakLete.Api/DTOs/Tier/TierResultDto.cs` | Add 5 new fields |
| `FreakLete.Api/Services/ExerciseTierService.cs` | Populate new fields via `NextMilestoneCalculator` |
| `FreakLete.Api/Data/Seed/TierEligibleDefinitionsSeed.cs` | NEW — extract rows + UPSERT SQL helper from migration |
| `FreakLete.Api/Migrations/20260417120000_SeedTierEligibleExerciseDefinitions.cs` | Refactor to delegate to `TierEligibleDefinitionsSeed` (no behavior change) |
| `FreakLete.Api/Program.cs` | Add idempotent seed guard after `Migrate()` |
| `FreakLete.Api/Controllers/ProfileTiersController.cs` | Delete |
| `FreakLete.Core/Tier/NextMilestoneCalculator.cs` | NEW — pure function for unit testability |
| `FreakLete.Core.Tests/NextMilestoneCalculatorTests.cs` | NEW — unit tests for helper |
| `FreakLete.Api.Tests/PrEntryIntegrationTests.cs` | Add 5 new tests |
| `Services/IApiClient.cs` | Remove `GetExerciseTiersAsync` + `RecalculateTiersAsync` |
| `Services/ApiClient.cs` | Remove implementations; add `Tier` property on client `PrEntryResponse` |
| `Models/TierResult.cs` | NEW — mirrors server DTO on client |
| `Models/ExerciseTierResponse.cs` | Delete |
| `Helpers/TierDisplayFormatter.cs` | NEW — `FormatDelta(trackingMode, delta)` |
| `Xaml/Controls/TierCongratsPopup.xaml` + `.xaml.cs` | NEW popup control |
| `Xaml/ProfilePage.xaml` | Remove `<!-- Exercise Tiers -->` block (lines 244-278) |
| `CodeBehind/ProfilePage.xaml.cs` | Remove `RenderTierCards`, `BuildTierCard`, `OnRefreshTiersTapped`, `RecalculateTiersAsync` call |
| `Xaml/CalculationsPage.xaml` | Add `NextMilestoneLabel` below `PrStatusLabel` |
| `CodeBehind/CalculationsPage.xaml.cs` | Hook popup + milestone update in `OnSavePrClicked` |
| `Services/AppLanguage.cs` | Add 6 tier-popup strings |

---

## Task 0: Add CommunityToolkit.Maui package + wiring

**Files:**
- Modify: `FreakLete.csproj`
- Modify: `MauiProgram.cs`

- [ ] **Step 1: Add NuGet reference**

Open `FreakLete.csproj`. In the `<ItemGroup>` block containing `CommunityToolkit.Maui.MediaElement` (around line 70), add:

```xml
<PackageReference Include="CommunityToolkit.Maui" Version="12.0.0" />
```

Pick a version compatible with `.MediaElement 8.0.1` and targeting `net10.0-android`. If `12.0.0` does not yet target net10, drop to the latest that does (verify with `dotnet add FreakLete.csproj package CommunityToolkit.Maui`).

- [ ] **Step 2: Wire toolkit in MauiProgram**

Open `MauiProgram.cs`. Add `using CommunityToolkit.Maui;` at top. On the `MauiAppBuilder` chain, insert `.UseMauiCommunityToolkit()` right after `.UseMauiApp<App>()`:

```csharp
var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .UseMauiCommunityToolkit()
    .ConfigureFonts(fonts => { ... });
```

- [ ] **Step 3: Verify build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: succeeds, no package resolution errors.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.csproj MauiProgram.cs
git commit -m "chore: add CommunityToolkit.Maui package for popup support"
```

---

## Task 1: Extend `TierResultDto` with milestone fields

**Files:**
- Modify: `FreakLete.Api/DTOs/Tier/TierResultDto.cs`

- [ ] **Step 1: Extend DTO**

Replace `FreakLete.Api/DTOs/Tier/TierResultDto.cs` with:

```csharp
namespace FreakLete.Api.DTOs.Tier;

public class TierResultDto
{
    public string CatalogId { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public string? PreviousTierLevel { get; set; }
    public bool LeveledUp { get; set; }

    // New — next-milestone context
    public string? NextLevel { get; set; }
    public double? NextTargetRaw { get; set; }
    public double? NextDelta { get; set; }
    public double ProgressPercent { get; set; }

    // New — mirrors ExerciseDefinition.TrackingMode:
    // "Strength" | "AthleticHeight" | "AthleticDistance" | "AthleticIndex" | "AthleticTime"
    public string TrackingMode { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Verify compile**

Run: `dotnet build FreakLete.Api`
Expected: builds, no consumers broken (all consumers set properties explicitly).

- [ ] **Step 3: Commit**

```bash
git add FreakLete.Api/DTOs/Tier/TierResultDto.cs
git commit -m "feat(api): extend TierResultDto with next-milestone fields"
```

---

## Task 2: Extract pure `NextMilestoneCalculator` in Core with tests

Context: extracting a pure helper makes unit testing trivial (no DbContext, no user fetch). Service will call into it in Task 3.

**Files:**
- Create: `FreakLete.Core/Tier/NextMilestoneCalculator.cs`
- Create: `FreakLete.Core.Tests/NextMilestoneCalculatorTests.cs`

- [ ] **Step 1: Write failing test — StrengthRatio mid-band**

Create `FreakLete.Core.Tests/NextMilestoneCalculatorTests.cs`:

```csharp
using FreakLete.Core.Tier;

namespace FreakLete.Core.Tests;

public class NextMilestoneCalculatorTests
{
    private static readonly double[] Bench = [0.5, 1.0, 1.25, 1.5, 1.75];

    [Fact]
    public void StrengthRatio_MidBand_ReturnsNextTierAndDelta()
    {
        // Ratio 1.10 on [0.5,1.0,1.25,1.5,1.75] → Intermediate (>=1.0, <1.25).
        // Next tier ratio = 1.25. Bodyweight 80 → next kg = 100.
        // RawValue 88 → delta = 12. Progress in [1.0, 1.25] band: (1.10-1.0)/0.25 = 40%.
        var r = NextMilestoneCalculator.Compute(
            tierType: "StrengthRatio",
            currentLevel: TierLevel.Intermediate,
            thresholds: Bench,
            rawValue: 88,
            ratio: 1.10,
            bodyWeight: 80);

        Assert.Equal(TierLevel.Advanced.ToString(), r.NextLevel);
        Assert.Equal(100, r.NextTargetRaw);
        Assert.Equal(12, r.NextDelta);
        Assert.InRange(r.ProgressPercent, 39.9, 40.1);
    }
}
```

- [ ] **Step 2: Run test — verify fails**

Run: `dotnet test FreakLete.Core.Tests --filter NextMilestoneCalculatorTests`
Expected: FAIL — `NextMilestoneCalculator` type doesn't exist.

- [ ] **Step 3: Implement `NextMilestoneCalculator`**

Create `FreakLete.Core/Tier/NextMilestoneCalculator.cs`:

```csharp
namespace FreakLete.Core.Tier;

public sealed record NextMilestoneResult(
    string? NextLevel,
    double? NextTargetRaw,
    double? NextDelta,
    double ProgressPercent);

public static class NextMilestoneCalculator
{
    public static NextMilestoneResult Compute(
        string tierType,
        TierLevel currentLevel,
        double[] thresholds,
        double rawValue,
        double? ratio,
        double? bodyWeight)
    {
        // thresholds holds the 5 boundary values between 6 tiers (NeedImprovement..Freak).
        // For currentLevel N (0..5), the next boundary is thresholds[N] (when N < length).
        int cur = (int)currentLevel;
        if (cur >= thresholds.Length)
        {
            // Already at top tier (Freak) → no next milestone.
            return new NextMilestoneResult(null, null, null, 100);
        }

        double boundary = thresholds[cur];
        bool isStrength = string.Equals(tierType, "StrengthRatio", StringComparison.OrdinalIgnoreCase);
        bool isInverse = string.Equals(tierType, "AthleticInverse", StringComparison.OrdinalIgnoreCase);

        double targetRaw;
        double delta;

        if (isStrength)
        {
            if (bodyWeight is null or <= 0)
                return new NextMilestoneResult(null, null, null, 0);
            targetRaw = boundary * bodyWeight.Value;
            delta = targetRaw - rawValue;
        }
        else if (isInverse)
        {
            // Lower is better — delta is seconds still to cut.
            targetRaw = boundary;
            delta = rawValue - targetRaw;
        }
        else
        {
            // AthleticAbsolute — higher is better.
            targetRaw = boundary;
            delta = targetRaw - rawValue;
        }

        // ProgressPercent: position within [lowerBoundary, nextBoundary] of current tier band.
        double current = ratio ?? rawValue;
        double progress;
        if (cur == 0)
        {
            // No lower boundary for NeedImprovement — anchor at 0.
            progress = isInverse
                ? Math.Clamp((100.0 * (thresholds[0] - current)) / thresholds[0], 0, 100)
                : Math.Clamp((100.0 * current) / thresholds[0], 0, 100);
        }
        else
        {
            double lo = thresholds[cur - 1];
            double hi = thresholds[cur];
            progress = isInverse
                ? Math.Clamp(100.0 * (lo - current) / (lo - hi), 0, 100)
                : Math.Clamp(100.0 * (current - lo) / (hi - lo), 0, 100);
        }

        string nextName = ((TierLevel)(cur + 1)).ToString();
        return new NextMilestoneResult(nextName, targetRaw, delta, progress);
    }
}
```

- [ ] **Step 4: Run the first test — passes**

Run: `dotnet test FreakLete.Core.Tests --filter NextMilestoneCalculatorTests`
Expected: PASS.

- [ ] **Step 5: Add AthleticInverse test**

Append to `NextMilestoneCalculatorTests.cs`:

```csharp
    private static readonly double[] Sprint = [5.8, 5.3, 4.9, 4.6, 4.4];

    [Fact]
    public void AthleticInverse_MidBand_ReturnsNextAndPositiveDelta()
    {
        // time 5.5, thresholds [5.8,5.3,4.9,4.6,4.4] → Beginner (>5.3).
        // Next boundary thresholds[1] = 5.3. Delta = 5.5 - 5.3 = 0.2.
        // Progress in [lo=5.8, hi=5.3]: (5.8 - 5.5)/(5.8 - 5.3) = 60%.
        var r = NextMilestoneCalculator.Compute(
            tierType: "AthleticInverse",
            currentLevel: TierLevel.Beginner,
            thresholds: Sprint,
            rawValue: 5.5,
            ratio: null,
            bodyWeight: null);

        Assert.Equal(TierLevel.Intermediate.ToString(), r.NextLevel);
        Assert.Equal(5.3, r.NextTargetRaw);
        Assert.InRange(r.NextDelta!.Value, 0.19, 0.21);
        Assert.InRange(r.ProgressPercent, 59.9, 60.1);
    }
```

Run: `dotnet test FreakLete.Core.Tests --filter NextMilestoneCalculatorTests`
Expected: all pass.

- [ ] **Step 6: Add max-tier test**

Append:

```csharp
    [Fact]
    public void MaxTier_ReturnsNullsAndFullProgress()
    {
        var r = NextMilestoneCalculator.Compute(
            tierType: "StrengthRatio",
            currentLevel: TierLevel.Freak,
            thresholds: Bench,
            rawValue: 160,
            ratio: 2.0,
            bodyWeight: 80);

        Assert.Null(r.NextLevel);
        Assert.Null(r.NextTargetRaw);
        Assert.Null(r.NextDelta);
        Assert.Equal(100, r.ProgressPercent);
    }
```

Run: `dotnet test FreakLete.Core.Tests --filter NextMilestoneCalculatorTests`
Expected: all pass.

- [ ] **Step 7: Add AthleticAbsolute test**

Append:

```csharp
    private static readonly double[] Jump = [30, 45, 55, 65, 75];

    [Fact]
    public void AthleticAbsolute_MidBand_ReturnsPositiveDelta()
    {
        // jump 48cm → Intermediate (>=45, <55). Next boundary 55. Delta = 55 - 48 = 7.
        // Progress in [45, 55]: (48-45)/10 = 30%.
        var r = NextMilestoneCalculator.Compute(
            tierType: "AthleticAbsolute",
            currentLevel: TierLevel.Intermediate,
            thresholds: Jump,
            rawValue: 48,
            ratio: null,
            bodyWeight: null);

        Assert.Equal(TierLevel.Advanced.ToString(), r.NextLevel);
        Assert.Equal(55, r.NextTargetRaw);
        Assert.Equal(7, r.NextDelta);
        Assert.InRange(r.ProgressPercent, 29.9, 30.1);
    }
```

Run: `dotnet test FreakLete.Core.Tests --filter NextMilestoneCalculatorTests`
Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add FreakLete.Core/Tier/NextMilestoneCalculator.cs FreakLete.Core.Tests/NextMilestoneCalculatorTests.cs
git commit -m "feat(core): add NextMilestoneCalculator for tier progress math"
```

---

## Task 3: Populate milestone fields in `ExerciseTierService`

**Files:**
- Modify: `FreakLete.Api/Services/ExerciseTierService.cs`

- [ ] **Step 1: Add milestone computation to `RecalculateTierAsync`**

In `FreakLete.Api/Services/ExerciseTierService.cs`, locate the final `return new TierResultDto { ... }` block (currently lines 135-141). Replace the block, starting from the `bool leveledUp = ...` line (131), with:

```csharp
bool leveledUp = previousLevel is not null &&
    Enum.TryParse<TierLevel>(previousLevel, out var prev) &&
    (int)tier > (int)prev;

var thresholdsForMilestone = TierResolver.GetThresholds(cfg, user.Sex, configs);
var milestone = NextMilestoneCalculator.Compute(
    tierType: cfg.TierType,
    currentLevel: tier,
    thresholds: thresholdsForMilestone,
    rawValue: rawValue,
    ratio: ratio,
    bodyWeight: basisValue);

return new TierResultDto
{
    CatalogId = catalogId,
    TierLevel = newLevel,
    PreviousTierLevel = previousLevel,
    LeveledUp = leveledUp,
    TrackingMode = def.TrackingMode,
    NextLevel = milestone.NextLevel,
    NextTargetRaw = milestone.NextTargetRaw,
    NextDelta = milestone.NextDelta,
    ProgressPercent = milestone.ProgressPercent
};
```

The file already has `using FreakLete.Core.Tier;` — the new `NextMilestoneCalculator` is in the same namespace. No new usings needed.

- [ ] **Step 2: Verify build**

Run: `dotnet build FreakLete.Api`
Expected: success.

- [ ] **Step 3: Run existing tests — sanity**

Run: `dotnet test FreakLete.Api.Tests --filter PrEntryIntegrationTests`
Expected: existing tests still pass (new fields are additive, all setters have defaults).

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/ExerciseTierService.cs
git commit -m "feat(api): populate next-milestone fields in RecalculateTierAsync"
```

---

## Task 4: Integration tests for the richer `TierResultDto`

**Files:**
- Modify: `FreakLete.Api.Tests/PrEntryIntegrationTests.cs`

Read the existing file first to match its fixture helpers. Check `FreakLeteApiFactory`, `AuthTestHelper`, and find the route used to set user bodyweight (needed for StrengthRatio tier). Common candidates: `PATCH /api/profile/athlete`, `POST /api/profile/athlete`. Verify by grepping the `AthleteProfileController` route before writing new tests.

- [ ] **Step 1: Write failing test #1 — first-time tier-eligible PR**

Append to `PrEntryIntegrationTests.cs` (verify actual profile-save route first; placeholder below):

```csharp
    [Fact]
    public async Task CreatePr_FirstTimeTierEligible_ReturnsPopulatedTierWithNullPrevious()
    {
        var c = await RegisterAndAuthenticateAsync();
        // Set bodyweight so StrengthRatio tier can be computed.
        // NOTE: verify route from AthleteProfileController before running.
        await c.PostAsJsonAsync("/api/profile/athlete",
            new { weightKg = 80.0, sex = "Male", dateOfBirth = "2000-01-01" });

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
```

- [ ] **Step 2: Run — passes**

Run: `dotnet test FreakLete.Api.Tests --filter CreatePr_FirstTimeTierEligible_ReturnsPopulatedTierWithNullPrevious`
Expected: PASS (service was updated in Task 3). If the profile endpoint path differs, adjust.

- [ ] **Step 3: Add test #2 — level-up crosses threshold**

```csharp
    [Fact]
    public async Task CreatePr_CrossesThreshold_ReturnsLeveledUpTrue()
    {
        var c = await RegisterAndAuthenticateAsync();
        await c.PostAsJsonAsync("/api/profile/athlete",
            new { weightKg = 80.0, sex = "Male", dateOfBirth = "2000-01-01" });

        // First: 60kg x5 @1 → 1RM ~68, ratio ~0.85 → Beginner.
        await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 60, reps = 5, rir = 1
        });

        // Second: 100kg x5 @1 → 1RM ~113, ratio ~1.42 → Advanced.
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 100, reps = 5, rir = 1
        });
        var tier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tier");
        Assert.Equal("Beginner", tier.GetProperty("previousTierLevel").GetString());
        Assert.True(tier.GetProperty("leveledUp").GetBoolean());
    }
```

Run and verify pass.

- [ ] **Step 4: Add test #3 — same tier band, leveledUp=false**

```csharp
    [Fact]
    public async Task CreatePr_SameBand_LeveledUpFalseWithPreviousSet()
    {
        var c = await RegisterAndAuthenticateAsync();
        await c.PostAsJsonAsync("/api/profile/athlete",
            new { weightKg = 80.0, sex = "Male", dateOfBirth = "2000-01-01" });

        // Both PRs inside Beginner band (ratio < 1.0).
        await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 50, reps = 5, rir = 1
        });
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "benchpress", exerciseName = "Bench Press",
            exerciseCategory = "Push", trackingMode = "Strength",
            weight = 60, reps = 5, rir = 1
        });
        var tier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("tier");
        Assert.Equal("Beginner", tier.GetProperty("previousTierLevel").GetString());
        Assert.Equal("Beginner", tier.GetProperty("tierLevel").GetString());
        Assert.False(tier.GetProperty("leveledUp").GetBoolean());
    }
```

Run and verify pass.

- [ ] **Step 5: Add test #4 — max tier**

```csharp
    [Fact]
    public async Task CreatePr_AtFreakTier_NullNextLevelAndFullProgress()
    {
        var c = await RegisterAndAuthenticateAsync();
        await c.PostAsJsonAsync("/api/profile/athlete",
            new { weightKg = 80.0, sex = "Male", dateOfBirth = "2000-01-01" });

        // 200kg x1 RIR 0 → 1RM ~200 → ratio 2.5 → Freak.
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
```

Run and verify pass.

- [ ] **Step 6: Add test #5 — non-eligible exercise returns null tier**

```csharp
    [Fact]
    public async Task CreatePr_NonEligibleExercise_TierIsNull()
    {
        var c = await RegisterAndAuthenticateAsync();
        await c.PostAsJsonAsync("/api/profile/athlete",
            new { weightKg = 80.0, sex = "Male", dateOfBirth = "2000-01-01" });

        // Dumbbell Curl is isolation — no StrengthRatio row, excluded by mechanic filter.
        var resp = await c.PostAsJsonAsync("/api/pr-entries", new
        {
            catalogId = "dumbbellcurl", exerciseName = "Dumbbell Curl",
            exerciseCategory = "Pull", trackingMode = "Strength",
            weight = 20, reps = 10, rir = 1
        });
        var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("tier").ValueKind);
    }
```

Run and verify pass. If `dumbbellcurl` is not in the catalog, swap for any non-tier-eligible exercise — verify via the `SeedRow[] Rows` list in the seed migration.

- [ ] **Step 7: Commit**

```bash
git add FreakLete.Api.Tests/PrEntryIntegrationTests.cs
git commit -m "test(api): integration tests for tier next-milestone fields"
```

---

## Task 5: Extract seed rows + add startup seed guard

**Files:**
- Create: `FreakLete.Api/Data/Seed/TierEligibleDefinitionsSeed.cs`
- Modify: `FreakLete.Api/Migrations/20260417120000_SeedTierEligibleExerciseDefinitions.cs`
- Modify: `FreakLete.Api/Program.cs`

- [ ] **Step 1: Create the seed helper**

Create `FreakLete.Api/Data/Seed/TierEligibleDefinitionsSeed.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FreakLete.Api.Data.Seed;

internal static class TierEligibleDefinitionsSeed
{
    internal sealed record Row(
        string CatalogId, string Name, string Category, string Mechanic,
        string TrackingMode, string TierType, string Male, string Female);

    internal static readonly Row[] Rows =
    [
        new("benchpress",           "Bench Press",            "Push",                "compound", "Strength",         "StrengthRatio",     "[0.5,1.0,1.25,1.5,1.75]",   "[0.35,0.7,0.9,1.1,1.35]"),
        new("backsquat",            "Back Squat",             "Squat Variation",     "compound", "Strength",         "StrengthRatio",     "[0.75,1.25,1.5,2.0,2.5]",   "[0.5,0.9,1.1,1.5,1.9]"),
        new("conventionaldeadlift", "Conventional Deadlift",  "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]"),
        new("sumodeadlift",         "Sumo Deadlift",          "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]"),
        new("overheadpress",        "Overhead Press",         "Push",                "compound", "Strength",         "StrengthRatio",     "[0.35,0.55,0.75,0.95,1.15]","[0.2,0.4,0.55,0.7,0.85]"),
        new("powerclean",           "Power Clean",            "Olympic Lifts",       "compound", "Strength",         "StrengthRatio",     "[0.6,0.9,1.2,1.5,1.8]",     "[0.4,0.65,0.85,1.05,1.3]"),
        new("powersnatch",          "Power Snatch",           "Olympic Lifts",       "compound", "Strength",         "StrengthRatio",     "[0.4,0.7,0.9,1.15,1.4]",    "[0.3,0.5,0.65,0.8,1.0]"),
        new("frontsquat",           "Front Squat",            "Squat Variation",     "compound", "Strength",         "StrengthRatio",     "[0.6,1.0,1.25,1.6,2.0]",    "[0.4,0.7,0.9,1.2,1.55]"),
        new("romaniandeadlift",     "Romanian Deadlift",      "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[0.8,1.2,1.6,2.0,2.4]",     "[0.55,0.8,1.1,1.4,1.75]"),
        new("barbellrow",           "Barbell Row",            "Pull",                "compound", "Strength",         "StrengthRatio",     "[0.5,0.9,1.15,1.4,1.7]",    "[0.35,0.6,0.8,1.0,1.2]"),
        new("pullup",               "Pull-Up",                "Pull",                "compound", "Strength",         "StrengthRatio",     "[1.0,1.2,1.4,1.6,1.9]",     "[1.0,1.1,1.25,1.45,1.7]"),
        new("trapbardeadlift",      "Trap Bar Deadlift",      "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.0,1.4,1.8,2.2]"),
        new("hipthrust",            "Barbell Hip Thrust",     "Deadlift Variation",  "compound", "Strength",         "StrengthRatio",     "[1.0,1.5,2.0,2.5,3.0]",     "[0.7,1.1,1.5,1.9,2.3]"),
        new("pushpress",            "Push Press",             "Push",                "compound", "Strength",         "StrengthRatio",     "[0.5,0.75,0.95,1.2,1.5]",   "[0.3,0.5,0.7,0.9,1.1]"),
        new("verticaljump",         "Vertical Jump",          "Jumps",               "",         "AthleticHeight",   "AthleticAbsolute",  "[30,45,55,65,75]",          "[20,32,42,52,60]"),
        new("standingbroadjump",    "Standing Broad Jump",    "Jumps",               "",         "AthleticDistance", "AthleticAbsolute",  "[180,220,250,280,310]",     "[150,190,220,245,275]"),
        new("rsi",                  "RSI",                    "Plyometrics",         "",         "AthleticIndex",    "AthleticAbsolute",  "[1.5,2.0,2.5,3.0,3.5]",     "[1.2,1.6,2.0,2.5,3.0]"),
        new("fortyyarddash",        "40 Yard Dash",           "Sprints",             "",         "AthleticTime",     "AthleticInverse",   "[5.8,5.3,4.9,4.6,4.4]",     "[6.6,6.0,5.5,5.1,4.8]"),
        new("tenmetersprint",       "10 Meter Sprint",        "Sprints",             "",         "AthleticTime",     "AthleticInverse",   "[2.2,2.0,1.85,1.75,1.65]",  "[2.5,2.25,2.05,1.9,1.8]"),
    ];

    internal static string BuildUpsertSql(Row r) => $"""
        INSERT INTO "ExerciseDefinitions" (
            "CatalogId", "Name", "Category", "DisplayName", "TurkishName", "EnglishName",
            "SourceSection", "Force", "Level", "Mechanic", "Equipment",
            "PrimaryMusclesText", "SecondaryMusclesText", "InstructionsText",
            "TrackingMode", "PrimaryLabel", "PrimaryUnit", "SecondaryLabel", "SecondaryUnit",
            "SupportsGroundContactTime", "SupportsConcentricTime",
            "MovementPattern", "AthleticQuality", "SportRelevance", "NervousSystemLoad",
            "GctProfile", "LoadPrescription", "CommonMistakes", "Progression", "Regression",
            "RecommendedRank",
            "TierType", "TierThresholdsMale", "TierThresholdsFemale"
        )
        VALUES (
            '{r.CatalogId}', '{r.Name}', '{r.Category}', '{r.Name}', '', '{r.Name}',
            '', '', '', '{r.Mechanic}', '',
            '', '', '',
            '{r.TrackingMode}', '', '', '', '',
            false, false,
            '', '', '', '',
            '', '', '', '', '',
            0,
            '{r.TierType}', '{r.Male}', '{r.Female}'
        )
        ON CONFLICT ("CatalogId") DO UPDATE SET
            "Mechanic" = EXCLUDED."Mechanic",
            "TierType" = EXCLUDED."TierType",
            "TierThresholdsMale" = EXCLUDED."TierThresholdsMale",
            "TierThresholdsFemale" = EXCLUDED."TierThresholdsFemale";
        """;

    public static void ApplyViaMigration(MigrationBuilder mb)
    {
        foreach (var r in Rows) mb.Sql(BuildUpsertSql(r));
    }

    public static async Task EnsureAppliedAsync(DbContext db, CancellationToken ct = default)
    {
        var hasStrengthRatio = await db.Database
            .SqlQueryRaw<int>("""SELECT COUNT(*)::int AS "Value" FROM "ExerciseDefinitions" WHERE "TierType" = 'StrengthRatio'""")
            .FirstOrDefaultAsync(ct);
        if (hasStrengthRatio > 0) return;

        foreach (var r in Rows)
            await db.Database.ExecuteSqlRawAsync(BuildUpsertSql(r), ct);
    }
}
```

- [ ] **Step 2: Refactor migration to delegate**

Replace `FreakLete.Api/Migrations/20260417120000_SeedTierEligibleExerciseDefinitions.cs` contents with:

```csharp
using FreakLete.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakLete.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedTierEligibleExerciseDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            TierEligibleDefinitionsSeed.ApplyViaMigration(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var r in TierEligibleDefinitionsSeed.Rows)
                migrationBuilder.Sql($"""DELETE FROM "ExerciseDefinitions" WHERE "CatalogId" = '{r.CatalogId}';""");
        }
    }
}
```

The class name and file name are preserved — EF Core tracks migrations by file name; internal implementation change does not require regenerating the migration or the model snapshot.

- [ ] **Step 3: Verify compile (no new migration generated)**

Run: `dotnet build FreakLete.Api`
Expected: succeeds. DO NOT run `dotnet ef migrations add` — nothing about the model changed.

- [ ] **Step 4: Add the startup guard**

Open `FreakLete.Api/Program.cs`. Find the auto-migrate block (line 217-225). Replace:

```csharp
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<StarterTemplateSeedService>();
    await seeder.SeedAsync();
}
```

with:

```csharp
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await FreakLete.Api.Data.Seed.TierEligibleDefinitionsSeed.EnsureAppliedAsync(db);

    var seeder = scope.ServiceProvider.GetRequiredService<StarterTemplateSeedService>();
    await seeder.SeedAsync();
}
```

- [ ] **Step 5: Run integration tests (full)**

Run: `dotnet test FreakLete.Api.Tests`
Expected: all pass. The test fixture's DB reset exercises the seed path via the migration; guard is a no-op when rows exist.

- [ ] **Step 6: Commit**

```bash
git add FreakLete.Api/Data/Seed/TierEligibleDefinitionsSeed.cs FreakLete.Api/Migrations/20260417120000_SeedTierEligibleExerciseDefinitions.cs FreakLete.Api/Program.cs
git commit -m "fix(api): idempotent seed guard for tier-eligible ExerciseDefinitions"
```

---

## Task 6: Delete `ProfileTiersController` and client-side tier API

**Files:**
- Delete: `FreakLete.Api/Controllers/ProfileTiersController.cs`
- Modify: `Services/IApiClient.cs`
- Modify: `Services/ApiClient.cs`

- [ ] **Step 1: Delete the controller**

```bash
git rm FreakLete.Api/Controllers/ProfileTiersController.cs
```

- [ ] **Step 2: Remove interface methods**

Open `Services/IApiClient.cs`. Remove lines 27-28:

```csharp
Task<ApiResult<List<ExerciseTierResponse>>> GetExerciseTiersAsync();
Task<ApiResult<List<ExerciseTierResponse>>> RecalculateTiersAsync();
```

- [ ] **Step 3: Remove implementation**

Open `Services/ApiClient.cs`. Delete lines 192-196 (the `GetExerciseTiersAsync` and `RecalculateTiersAsync` methods). Keep the `// ── Movement Goals ──` separator comment on line 190 (it labels a different section).

- [ ] **Step 4: Verify backend builds; client build will fail**

Run: `dotnet build FreakLete.Api`
Expected: SUCCESS.

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: FAIL — `CodeBehind/ProfilePage.xaml.cs` still references `RecalculateTiersAsync`, `RenderTierCards`, `BuildTierCard`, `OnRefreshTiersTapped`, `ExerciseTierResponse`. Do NOT fix yet — Task 7 handles it.

- [ ] **Step 5: Run backend tests — no regressions**

Run: `dotnet test FreakLete.Api.Tests`
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add FreakLete.Api/Controllers/ProfileTiersController.cs Services/IApiClient.cs Services/ApiClient.cs
git commit -m "refactor: remove ProfileTiersController and client tier endpoints"
```

---

## Task 7: Remove Profile tier UI

**Files:**
- Modify: `Xaml/ProfilePage.xaml`
- Modify: `CodeBehind/ProfilePage.xaml.cs`
- Delete: `Models/ExerciseTierResponse.cs` (only if unreferenced)

- [ ] **Step 1: Remove XAML block**

Open `Xaml/ProfilePage.xaml`. Delete lines 244-278: the `<!-- Exercise Tiers -->` comment and the entire surrounding `<Border Style="{StaticResource CardBorder}">` block that contains `TierCardsContainer` and `TierEmptyLabel`.

Also inspect the `<BoxView>` divider (line ~281) that sat between the tier block and `Profile Details`. Remove it to avoid an orphaned line if it is no longer meaningful.

- [ ] **Step 2: Remove code-behind methods**

Open `CodeBehind/ProfilePage.xaml.cs`. Delete:
- Lines 1366-1370: `OnRefreshTiersTapped` method.
- Lines 1372-1385: `RenderTierCards` method.
- Lines 1387-1435: `BuildTierCard` method.
- Lines 254-255 inside `LoadProfileAsync`:
  ```csharp
  var tierResult = await _api.RecalculateTiersAsync();
  RenderTierCards(tierResult.Success ? tierResult.Data : null);
  ```
- Any `using FreakLete.Models;` or other `using` that was added only for `ExerciseTierResponse` — check the file for other `FreakLete.Models` references before removing; keep the `using` if other models are in use.

- [ ] **Step 3: Delete `ExerciseTierResponse` if unreferenced**

Check usage:
```bash
grep -rn "ExerciseTierResponse" FreakLete.csproj.d/../  --include="*.cs" 2>/dev/null
```
Equivalent with Grep tool: search pattern `ExerciseTierResponse` across repo.
Expected after Task 7 Step 2: zero matches.

Then:
```bash
git rm Models/ExerciseTierResponse.cs
```

- [ ] **Step 4: Verify Android build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: SUCCESS.

- [ ] **Step 5: Commit**

```bash
git add Xaml/ProfilePage.xaml CodeBehind/ProfilePage.xaml.cs Models/ExerciseTierResponse.cs
git commit -m "refactor: remove Exercise Tiers section from ProfilePage"
```

---

## Task 8: Client `TierResult` model + `PrEntryResponse.Tier` property

**Files:**
- Create: `Models/TierResult.cs`
- Modify: `Services/ApiClient.cs`

- [ ] **Step 1: Create client `TierResult`**

Create `Models/TierResult.cs`:

```csharp
namespace FreakLete.Models;

public class TierResult
{
    public string CatalogId { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public string? PreviousTierLevel { get; set; }
    public bool LeveledUp { get; set; }
    public string? NextLevel { get; set; }
    public double? NextTargetRaw { get; set; }
    public double? NextDelta { get; set; }
    public double ProgressPercent { get; set; }
    public string TrackingMode { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Add `Tier` property on client `PrEntryResponse`**

Open `Services/ApiClient.cs`. At the end of `PrEntryResponse` (line ~588, right before the closing `}` of that class), add:

```csharp
    public TierResult? Tier { get; set; }
```

Ensure the file has `using FreakLete.Models;` at the top (it does — the file already references `UserProfileResponse` etc. from the same namespace — verify `TierResult` resolves).

- [ ] **Step 3: Verify MAUI build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add Models/TierResult.cs Services/ApiClient.cs
git commit -m "feat: add client TierResult model and PrEntryResponse.Tier"
```

---

## Task 9: `TierDisplayFormatter`

**Files:**
- Create: `Helpers/TierDisplayFormatter.cs`

The MAUI project has no unit test wiring, so this helper is tested via manual smoke (Task 13). Keep it pure.

- [ ] **Step 1: Create formatter**

Create `Helpers/TierDisplayFormatter.cs`:

```csharp
using System.Globalization;

namespace FreakLete.Helpers;

public static class TierDisplayFormatter
{
    // trackingMode mirrors server ExerciseDefinition.TrackingMode values:
    // "Strength" | "AthleticHeight" | "AthleticDistance" | "AthleticIndex" | "AthleticTime"
    public static string FormatDelta(string trackingMode, double delta)
    {
        var inv = CultureInfo.InvariantCulture;
        return trackingMode switch
        {
            "Strength"         => $"+{delta.ToString("0", inv)} kg",
            "AthleticHeight"   => $"+{delta.ToString("0", inv)} cm",
            "AthleticDistance" => $"+{delta.ToString("0", inv)} cm",
            "AthleticIndex"    => $"+{delta.ToString("0.00", inv)}",
            "AthleticTime"     => $"-{delta.ToString("0.00", inv)} s",
            _                  => $"+{delta.ToString("0.##", inv)}"
        };
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add Helpers/TierDisplayFormatter.cs
git commit -m "feat: add TierDisplayFormatter for milestone delta strings"
```

---

## Task 10: `AppLanguage` strings for tier popup

**Files:**
- Modify: `Services/AppLanguage.cs`

- [ ] **Step 1: Append tier-popup strings**

Open `Services/AppLanguage.cs`. Locate the section around `CalcPrSaved` (line 302). After that property, add:

```csharp
// ── Tier Popup ──────────────────────────────────
public static string TierCongratsTitle => IsTurkish ? "Tebrikler!" : "Congratulations!";
public static string TierFirstTierText(string level) =>
    IsTurkish ? $"İlk seviyen: {level}!" : $"Your first tier: {level}!";
public static string TierLevelUpText(string level) =>
    IsTurkish ? $"{level} seviyesine ulaştın!" : $"You reached {level}!";
public static string TierNextMilestonePrefix(string nextLevel) =>
    IsTurkish ? $"Sıradaki: {nextLevel}" : $"Next: {nextLevel}";
public static string TierMaxTierText =>
    IsTurkish ? "En yüksek seviyeye ulaşıldı." : "Highest tier achieved.";
public static string TierCloseButton => IsTurkish ? "Kapat" : "Close";
```

The spec uses `{0}`-style placeholders; this codebase's convention is expression-bodied with inline interpolation, so the three parameterized strings are written as methods.

- [ ] **Step 2: Verify build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add Services/AppLanguage.cs
git commit -m "feat: add AppLanguage strings for tier congrats popup"
```

---

## Task 11: `TierCongratsPopup` control

**Files:**
- Create: `Xaml/Controls/TierCongratsPopup.xaml`
- Create: `Xaml/Controls/TierCongratsPopup.xaml.cs`

Tier badge color mapping (actual enum values, NOT spec's prose names):
- `NeedImprovement` → `SurfaceStrong`
- `Beginner` → `Info`
- `Intermediate` → `Success`
- `Advanced` → `AccentGlow`
- `Elite` → `Accent`
- `Freak` → `Accent`

- [ ] **Step 1: Create XAML**

Create `Xaml/Controls/TierCongratsPopup.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<toolkit:Popup xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
               xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
               xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
               x:Class="FreakLete.Xaml.Controls.TierCongratsPopup"
               CanBeDismissedByTappingOutsideOfPopup="True"
               Color="Transparent">
    <Border BackgroundColor="{StaticResource Surface}"
            Stroke="{StaticResource SurfaceBorder}"
            StrokeThickness="1"
            StrokeShape="RoundRectangle 24"
            Padding="24"
            WidthRequest="320">
        <VerticalStackLayout Spacing="16">
            <Label x:Name="TitleLabel"
                   Style="{StaticResource SubHeadline}"
                   HorizontalTextAlignment="Center" />

            <Border x:Name="TierBadge"
                    StrokeShape="RoundRectangle 12"
                    WidthRequest="60"
                    HeightRequest="60"
                    HorizontalOptions="Center"
                    StrokeThickness="0" />

            <Label x:Name="SubtitleLabel"
                   FontFamily="OpenSansSemibold"
                   FontSize="15"
                   TextColor="{StaticResource TextPrimary}"
                   HorizontalTextAlignment="Center" />

            <BoxView HeightRequest="1"
                     Color="{StaticResource BorderSubtle}" />

            <VerticalStackLayout x:Name="NextMilestoneStack" Spacing="8">
                <Label x:Name="NextMilestoneLabel"
                       FontFamily="OpenSansRegular"
                       FontSize="13"
                       TextColor="{StaticResource TextSecondary}"
                       HorizontalTextAlignment="Center" />

                <Border StrokeShape="RoundRectangle 4"
                        HeightRequest="6"
                        BackgroundColor="{StaticResource SurfaceStrong}"
                        StrokeThickness="0"
                        Padding="0">
                    <BoxView x:Name="ProgressBar"
                             Color="{StaticResource AccentGlow}"
                             HorizontalOptions="Start"
                             WidthRequest="0" />
                </Border>
            </VerticalStackLayout>

            <Label x:Name="MaxTierLabel"
                   FontFamily="OpenSansSemibold"
                   FontSize="13"
                   TextColor="{StaticResource AccentGlow}"
                   HorizontalTextAlignment="Center"
                   IsVisible="False" />

            <Button x:Name="CloseButton"
                    BackgroundColor="{StaticResource Accent}"
                    TextColor="{StaticResource TextPrimary}"
                    FontFamily="OpenSansSemibold"
                    FontSize="14"
                    CornerRadius="18"
                    HeightRequest="48"
                    Clicked="OnCloseClicked" />
        </VerticalStackLayout>
    </Border>
</toolkit:Popup>
```

- [ ] **Step 2: Create code-behind**

Create `Xaml/Controls/TierCongratsPopup.xaml.cs`:

```csharp
using CommunityToolkit.Maui.Views;
using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete.Xaml.Controls;

public partial class TierCongratsPopup : Popup
{
    private TierCongratsPopup(TierResult tier)
    {
        InitializeComponent();
        ApplyLanguage();
        Bind(tier);
    }

    public static Task ShowAsync(Page page, TierResult tier)
    {
        try
        {
            var popup = new TierCongratsPopup(tier);
            return page.ShowPopupAsync(popup);
        }
        catch
        {
            // Popup display failure must not break the save flow.
            return Task.CompletedTask;
        }
    }

    private void ApplyLanguage()
    {
        TitleLabel.Text = AppLanguage.TierCongratsTitle;
        CloseButton.Text = AppLanguage.TierCloseButton;
        MaxTierLabel.Text = AppLanguage.TierMaxTierText;
    }

    private void Bind(TierResult tier)
    {
        SubtitleLabel.Text = tier.PreviousTierLevel is null
            ? AppLanguage.TierFirstTierText(tier.TierLevel)
            : AppLanguage.TierLevelUpText(tier.TierLevel);

        TierBadge.BackgroundColor = GetTierBadgeColor(tier.TierLevel);

        if (string.IsNullOrEmpty(tier.NextLevel) || tier.NextDelta is null)
        {
            NextMilestoneStack.IsVisible = false;
            MaxTierLabel.IsVisible = true;
        }
        else
        {
            NextMilestoneStack.IsVisible = true;
            MaxTierLabel.IsVisible = false;
            NextMilestoneLabel.Text =
                $"{AppLanguage.TierNextMilestonePrefix(tier.NextLevel)} — {TierDisplayFormatter.FormatDelta(tier.TrackingMode, tier.NextDelta.Value)}";

            NextMilestoneStack.SizeChanged += (_, _) =>
            {
                var container = NextMilestoneStack.Width;
                if (container > 0)
                    ProgressBar.WidthRequest = container * Math.Clamp(tier.ProgressPercent, 0, 100) / 100;
            };
        }
    }

    private static Color GetTierBadgeColor(string tierLevel)
    {
        var res = Application.Current!.Resources;
        string key = tierLevel switch
        {
            "NeedImprovement" => "SurfaceStrong",
            "Beginner"        => "Info",
            "Intermediate"    => "Success",
            "Advanced"        => "AccentGlow",
            "Elite"           => "Accent",
            "Freak"           => "Accent",
            _                 => "SurfaceStrong"
        };
        return res.TryGetValue(key, out var v) && v is Color c ? c : Colors.Transparent;
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await CloseAsync();
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: SUCCESS. If the `xmlns:toolkit` URI fails to resolve, confirm the exact URI against the installed `CommunityToolkit.Maui` version's README.

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/TierCongratsPopup.xaml Xaml/Controls/TierCongratsPopup.xaml.cs
git commit -m "feat: add TierCongratsPopup control"
```

---

## Task 12: Hook popup + next-milestone label in `CalculationsPage`

**Files:**
- Modify: `Xaml/CalculationsPage.xaml`
- Modify: `CodeBehind/CalculationsPage.xaml.cs`

- [ ] **Step 1: Add `NextMilestoneLabel` in XAML**

Open `Xaml/CalculationsPage.xaml`. Find `PrStatusLabel` at line 435. Immediately after its closing `/>` (line 438), insert:

```xml
<Label x:Name="NextMilestoneLabel"
       FontFamily="OpenSansRegular"
       FontSize="12"
       TextColor="{StaticResource TextMuted}"
       IsVisible="False" />
```

- [ ] **Step 2: Wire popup + milestone label in `OnSavePrClicked`**

Open `CodeBehind/CalculationsPage.xaml.cs`. Locate `OnSavePrClicked` at line 611. Replace the inner `else` branch (lines 663-672) with:

```csharp
else
{
    var result = await _api.CreatePrEntryAsync(data);
    if (!result.Success)
    {
        ShowError(PrStatusLabel, result.Error ?? AppLanguage.CalcPrFailedSave);
        return;
    }
    ShowSuccess(PrStatusLabel, AppLanguage.CalcPrSaved);

    var tier = result.Data?.Tier;
    UpdateNextMilestoneLabel(tier);
    if (tier is not null && (tier.LeveledUp || tier.PreviousTierLevel is null))
        await TierCongratsPopup.ShowAsync(this, tier);
}
```

- [ ] **Step 3: Clear label on edit path**

In the `if (_editingPrEntryId.HasValue)` branch (line 653-662), after `ShowSuccess(PrStatusLabel, AppLanguage.CalcPrUpdated);`, add:

```csharp
UpdateNextMilestoneLabel(null);
```

Rationale: the update endpoint does not return a fresh tier, so showing a stale milestone after an update would mislead.

- [ ] **Step 4: Add `UpdateNextMilestoneLabel` helper**

Add to the same file, near other UI helpers (around line 800):

```csharp
private void UpdateNextMilestoneLabel(TierResult? tier)
{
    if (tier is null || string.IsNullOrEmpty(tier.NextLevel) || tier.NextDelta is null)
    {
        NextMilestoneLabel.Text = string.Empty;
        NextMilestoneLabel.IsVisible = false;
        return;
    }
    NextMilestoneLabel.Text =
        $"{AppLanguage.TierNextMilestonePrefix(tier.NextLevel)} — {TierDisplayFormatter.FormatDelta(tier.TrackingMode, tier.NextDelta.Value)}";
    NextMilestoneLabel.IsVisible = true;
}
```

Add at top of the file if missing: `using FreakLete.Helpers;`, `using FreakLete.Models;`, `using FreakLete.Xaml.Controls;`.

- [ ] **Step 5: Verify build**

Run: `dotnet build FreakLete.csproj -f net10.0-android -c Debug`
Expected: SUCCESS.

- [ ] **Step 6: Commit**

```bash
git add Xaml/CalculationsPage.xaml CodeBehind/CalculationsPage.xaml.cs
git commit -m "feat: show tier congrats popup and milestone label on PR save"
```

---

## Task 13: Manual smoke verification

**No code changes. Do not skip.**

- [ ] **Step 1: Clean DB + startup log check**

```bash
cd FreakLete.Api
dotnet run
```

Confirm startup log contains:
- `Applied migration '20260417120000_SeedTierEligibleExerciseDefinitions'`.
- NO occurrence of `ExerciseDefinitions table has no StrengthRatio rows`.

Stop API with Ctrl+C.

- [ ] **Step 2: Drift scenario**

In `psql` against the dev DB:
```sql
UPDATE "ExerciseDefinitions" SET "TierType" = '' WHERE "CatalogId" = 'benchpress';
```
Restart API. Confirm the guard re-applied the UPSERT:
```sql
SELECT "CatalogId", "TierType" FROM "ExerciseDefinitions" WHERE "CatalogId" = 'benchpress';
```
Expected: `TierType = StrengthRatio`.

- [ ] **Step 3: Install MAUI build on emulator**

```bash
dotnet build FreakLete.csproj -f net10.0-android -c Debug -t:Install
```

- [ ] **Step 4: UI flow verification**

As a new user (set bodyweight in Profile first):
1. Log a Bench Press PR (first time) → popup opens with `TierFirstTierText`, badge colored per current tier, next-milestone visible. Inline label under Save PR button shows `Sıradaki: X — +Y kg` (or English equivalent).
2. Log a larger Bench Press PR that crosses a threshold → popup opens with `TierLevelUpText`.
3. Log a Bench Press PR within the same band → NO popup; inline label still updates with remaining delta.
4. Log a Vertical Jump PR → inline label shows `+N cm`.
5. Log a 40-Yard Dash PR → inline label shows `-N.NN s`.
6. Log an isolation lift (Dumbbell Curl) → no popup, no inline label.
7. Open Profile → confirm Exercise Tiers section is gone, no layout gap, no broken bindings in logcat.

- [ ] **Step 5: Final verification**

If every step passes, plan is complete. If any step fails, fix in the originating task and re-verify before moving on.

---

## Execution Order Reasoning

- Task 0 (package) blocks Task 11 (popup).
- Task 1 (DTO shape) unblocks Tasks 3, 4, 8.
- Task 2 (pure helper + tests) can run in parallel with Task 1.
- Task 5 (seed guard) is independent of DTO work — can run in parallel with 1-4.
- Task 6 (delete controller + client API) must precede Task 7 (Profile UI cleanup) so the intermediate broken-build state lasts only one commit.
- Task 8 (client DTO mirror) must precede Tasks 9, 11, 12.
- Task 12 is the last code task. Task 13 (smoke) is last.

---

## Open Questions Resolved Inline

| Spec ambiguity | Resolution |
|---|---|
| Popup `Level` naming (`Untrained/Novice/…`) | Use actual enum names: `NeedImprovement/Beginner/Intermediate/Advanced/Elite/Freak`. |
| `IsLevelUp` vs existing `LeveledUp` | Use existing `LeveledUp`. |
| `PreviousLevel` vs existing `PreviousTierLevel` | Use existing `PreviousTierLevel`. |
| How client gets `TrackingMode` for unit formatting | Server adds `TrackingMode` (mirrors `ExerciseDefinition.TrackingMode`) to `TierResultDto`. |
| Standing broad jump unit | Seed uses cm (150-310 range). `FormatDelta` returns `cm`. |
| `BackfillTiersFromPrEntriesAsync` fate (Risk R1) | Leave method in place; its only caller (`ProfileTiersController`) is deleted, but keep as dead-for-now for a later cleanup pass — removing it here expands scope. |
| Seed guard duplicate risk (Risk R4) | `EnsureAppliedAsync` uses the same `ON CONFLICT DO UPDATE` SQL as the migration — idempotent. |

---

## Self-Review Notes

- **Spec coverage:** Every file in the spec's File Map has a corresponding task. The "confirm at implementation time" items are resolved in Pre-Reads + Open Questions.
- **Placeholders:** No TBD / TODO. Every code step has actual code.
- **Type consistency:** `TierResult` (client) and `TierResultDto` (server) match field-for-field. `NextMilestoneCalculator.Compute` signature is identical in tests and service call. `PrEntryResponse.Tier` is a new `TierResult?`.
- **Test seams:** Core math is a pure static method with 4 unit tests; service integration is covered by 5 new API tests. MAUI popup is verified via manual smoke in Task 13.
