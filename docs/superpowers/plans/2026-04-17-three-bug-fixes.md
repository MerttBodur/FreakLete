# Three Bug Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three independent bugs: missing exercise videos, invisible quick-workout photos, and always-empty exercise tiers on ProfilePage.

**Architecture:** Three self-contained changes — one JSON data fix, one MAUI UI fix, one backend service fix. No shared state, no new migrations, no new endpoints.

**Tech Stack:** C# / .NET MAUI (Fix 2), ASP.NET Core + EF Core (Fix 3), JSON (Fix 1).

---

## Task 1: Verify exercise_catalog.json mediaUrl completeness (Fix 1)

**Files:**
- Verify/Modify: `Resources/Raw/exercise_catalog.json`

The file already has unstaged changes (17 insertions per `git diff`). This task confirms every tier-eligible exercise has a `mediaUrl` field and adds any that are still missing.

URL pattern: `https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/{filename}`

Expected 19 entries:

| JSON `id` (exact key — grep to confirm) | Expected mp4 |
|---|---|
| `pushbarbellbenchpress` | `benchpress.mp4` |
| `squatvariationbacksquat` | `backsquat.mp4` |
| `deadliftvariationconventionaldeadlift` | `conventionaldeadlift.mp4` |
| ID containing `sumodeadlift` | `sumodeadlift.mp4` |
| ID containing `romaniandeadlift` | `romaniandeadlift.mp4` |
| ID containing `trapbardeadlift` | `trapbardeadlift.mp4` |
| ID containing `powerclean` | `powerclean.mp4` |
| ID containing `powersnatch` | `powersnatch.mp4` |
| ID containing `frontsquat` | `frontsquat.mp4` |
| ID containing `overheadpress` | `overheadpress.mp4` |
| ID containing `pushpress` | `pushpress.mp4` |
| ID containing `pullup` | `pullup.mp4` |
| ID containing `barbellrow` | `barbellrow.mp4` |
| ID containing `hipthrust` | `hipthrust.mp4` |
| ID containing `verticaljump` | `verticaljump.mp4` |
| ID containing `standingbroadjump` | `standingbroadjump.mp4` |
| ID containing `rsi` | `rsi.mp4` |
| ID containing `fortyyard` | `fortyyarddash.mp4` |
| ID containing `tenm` or `10m` sprint | `tenmetersprint.mp4` |

- [ ] **Step 1: Count current mediaUrl entries**

```bash
grep -c "mediaUrl" Resources/Raw/exercise_catalog.json
```

Expected: 19. If less, the missing ones need adding.

- [ ] **Step 2: Identify exact IDs for any missing entries**

```bash
grep -n "\"id\"" Resources/Raw/exercise_catalog.json | grep -i "rsi\|fortyyard\|tenm\|sprint"
```

Use this to find the exact `id` string for the three athletic exercises.

- [ ] **Step 3: For any exercise missing mediaUrl, add the field**

Add `"mediaUrl"` immediately after the `"id"` field in the JSON object. Example:

```json
{
  "id": "plyometricsrsi",
  "mediaUrl": "https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/rsi.mp4",
  "name": "RSI Drop Jump",
  ...
}
```

Repeat for each missing entry.

- [ ] **Step 4: Validate JSON is still well-formed**

```bash
python -c "import json; json.load(open('Resources/Raw/exercise_catalog.json')); print('JSON valid')"
```

Expected: `JSON valid`

- [ ] **Step 5: Commit**

```bash
git add Resources/Raw/exercise_catalog.json
git commit -m "fix: add mediaUrl to all 19 tier-eligible exercises in exercise catalog"
```

---

## Task 2: Fix quick workout card images (Fix 2)

**Files:**
- Modify: `CodeBehind/HomePage.xaml.cs` (Image creation block inside `BuildQuickWorkoutCards`, line ~245)

**Root cause:** `Source = imageName + ".png"` uses implicit string→ImageSource conversion. On Android, dynamically created Image controls require `ImageSource.FromFile(...)`. The Image also lacks explicit `WidthRequest`/`HeightRequest`, causing sizing failures with `Aspect.AspectFill` inside a `Grid`.

- [ ] **Step 1: Locate the Image creation block**

Find this code in `BuildQuickWorkoutCards` (~line 245):

```csharp
imageArea.Children.Add(new Image
{
    Source = imageName + ".png",
    Aspect = Aspect.AspectFill,
    HorizontalOptions = LayoutOptions.Fill,
    VerticalOptions = LayoutOptions.Fill
});
```

- [ ] **Step 2: Replace with explicit FromFile, dimensions, and diagnostic log**

Replace the block with:

```csharp
Debug.WriteLine($"[QuickWorkout] Resolving image: {imageName}.png");
imageArea.Children.Add(new Image
{
    Source = ImageSource.FromFile(imageName + ".png"),
    Aspect = Aspect.AspectFill,
    WidthRequest = 180,
    HeightRequest = 90,
    HorizontalOptions = LayoutOptions.Fill,
    VerticalOptions = LayoutOptions.Fill
});
```

Ensure `using System.Diagnostics;` is present at the top of the file (add if missing).

- [ ] **Step 3: Build to verify no compile errors**

```bash
dotnet build FreakLete/FreakLete.csproj -f net10.0-android --no-restore 2>&1 | tail -10
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add CodeBehind/HomePage.xaml.cs
git commit -m "fix: use ImageSource.FromFile and explicit dimensions for quick workout card images"
```

---

## Task 3: Fix ExerciseTierService normalized catalogId fallback (Fix 3)

**Files:**
- Modify: `FreakLete.Api/Services/ExerciseTierService.cs`
- Modify: `FreakLete.Api.Tests/ExerciseTierIntegrationTests.cs`

**Root cause:** Client sends `catalogId = "pushbarbellbenchpress"` (JSON `id`). DB `ExerciseDefinitions.CatalogId` = `"benchpress"`. Direct lookup returns null → tier never written. Fix: if direct lookup fails, normalize `exerciseName` ("Bench Press" → "benchpress") and retry. The normalization logic already exists inline in `BackfillTiersFromPrEntriesAsync` — extract it as `NormalizeName` and reuse in both places.

### Step 3a: Write the failing test first (TDD — RED)

- [ ] **Step 1: Add the failing test to ExerciseTierIntegrationTests.cs**

The file already seeds a `benchpress` `ExerciseDefinition` with `CatalogId = "benchpress"` in `InitializeAsync`. Add this test:

```csharp
[Fact]
public async Task RecalculateTier_WithClientFormatCatalogId_ReturnsTierViaFallback()
{
    // Arrange
    var client = await RegisterAndAuthWithWeightAsync(80.0, "Male");
    var payload = new
    {
        catalogId = "pushbarbellbenchpress",  // client JSON id — DB has "benchpress"
        exerciseName = "Bench Press",
        trackingMode = "Strength",
        weight = 100,
        reps = 5,
        rir = 0
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/pr-entries", payload);

    // Assert
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(body);
    var tierResult = doc.RootElement.GetProperty("tierResult");
    Assert.False(
        tierResult.ValueKind == JsonValueKind.Null,
        "Expected a tier result but got null — fallback normalization is not working");
    Assert.Equal("benchpress", tierResult.GetProperty("catalogId").GetString());
}
```

- [ ] **Step 2: Run the test — must fail**

```bash
dotnet test FreakLete.Api.Tests --filter "RecalculateTier_WithClientFormatCatalogId_ReturnsTierViaFallback" -v normal 2>&1 | tail -20
```

Expected: **FAIL** — `tierResult` is null because `"pushbarbellbenchpress"` matches nothing in DB directly.

### Step 3b: Implement (GREEN)

- [ ] **Step 3: Extract NormalizeName helper**

In `FreakLete.Api/Services/ExerciseTierService.cs`, add this private static method before the closing `}` of the class:

```csharp
private static string NormalizeName(string name) =>
    name.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("'", "");
```

- [ ] **Step 4: Add fallback lookup in RecalculateTierAsync**

Find this block (~line 35):

```csharp
var def = await _db.ExerciseDefinitions
    .FirstOrDefaultAsync(d => d.CatalogId == catalogId, ct);
if (def is null || string.IsNullOrWhiteSpace(def.TierType)) return null;
```

Replace with:

```csharp
var def = await _db.ExerciseDefinitions
    .FirstOrDefaultAsync(d => d.CatalogId == catalogId, ct);
if (def is null)
    def = await _db.ExerciseDefinitions
        .FirstOrDefaultAsync(d => d.CatalogId == NormalizeName(exerciseName), ct);
if (def is null || string.IsNullOrWhiteSpace(def.TierType)) return null;
```

- [ ] **Step 5: Replace inline normalization in BackfillTiersFromPrEntriesAsync**

Find this block (~line 182):

```csharp
var normalized = pr.ExerciseName
    .ToLowerInvariant()
    .Replace(" ", "")
    .Replace("-", "")
    .Replace("'", "");
```

Replace with:

```csharp
var normalized = NormalizeName(pr.ExerciseName);
```

### Step 3c: Verify (PASS)

- [ ] **Step 6: Run the new test — must pass**

```bash
dotnet test FreakLete.Api.Tests --filter "RecalculateTier_WithClientFormatCatalogId_ReturnsTierViaFallback" -v normal 2>&1 | tail -20
```

Expected: **PASS**

- [ ] **Step 7: Run full tier test suite (regression check)**

```bash
dotnet test FreakLete.Api.Tests --filter "ExerciseTier" -v normal 2>&1 | tail -20
```

Expected: All pass.

- [ ] **Step 8: Run core tests**

```bash
dotnet test FreakLete.Core.Tests 2>&1 | tail -10
```

Expected: All pass.

- [ ] **Step 9: Commit**

```bash
git add FreakLete.Api/Services/ExerciseTierService.cs FreakLete.Api.Tests/ExerciseTierIntegrationTests.cs
git commit -m "fix: extract NormalizeName helper and add fallback catalogId lookup in RecalculateTierAsync"
```

---

## Validation Checklist

| Fix | File | Emulator Verification |
|---|---|---|
| Fix 1 — Videos | `Resources/Raw/exercise_catalog.json` | Exercise Catalog → tap Bench Press → video plays |
| Fix 2 — Photos | `CodeBehind/HomePage.xaml.cs` | Home → quick workout rail → photos visible |
| Fix 3 — Tiers | `FreakLete.Api/Services/ExerciseTierService.cs` | Submit Bench Press PR → ProfilePage Tiers not empty |

## Out of Scope

- Client catalog ↔ backend DB sync
- New DTOs or API endpoints
- Changing client JSON `id` format
- Full test suite run (only tier + core tests per task)
