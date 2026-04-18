# Video & Exercise Tier Diagnostics + Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Surface the actual errors causing (1) exercise videos to never display and (2) exercise tiers to remain empty despite Strength PRs and body weight being set.

**Architecture:** Both bugs are silent failures — code paths that fall back to empty/placeholder state with no diagnostic output. The fix is to (a) expose the real error from each failure path, then (b) apply the correct targeted fix once the root cause is confirmed.

**Tech Stack:** .NET MAUI (C#, XAML), CommunityToolkit.Maui.MediaElement, ASP.NET Core Web API, EF Core PostgreSQL

---

## Background: Identified Failure Points

### Videos
- `ExerciseDetailPage.xaml.cs` calls `MediaSource.FromUri(MediaUrl)` when `MediaUrl` is non-empty.
- On failure, `OnVideoMediaFailed` fires and hides the player — no error is shown or logged.
- Likely cause: R2 bucket MP4 files not uploaded (returning 404), OR Android TLS/codec issue.

### Tiers
- `ProfilePage.xaml.cs` calls `RecalculateTiersAsync()` → POST `api/profile/tiers/recalculate`.
- Server calls `BackfillTiersFromPrEntriesAsync` then `GetTiersForUserAsync`.
- `BackfillTiersFromPrEntriesAsync` silently returns early when either:
  - No PRs with `TrackingMode == "Strength"` AND `Weight > 0` AND `Reps > 0`
  - `ExerciseDefinitions` table is empty (migration `SeedTierEligibleExerciseDefinitions` not applied on prod)
  - Exercise names in PRs don't normalize to any seeded CatalogId
- Server produces zero logs for any of these cases.

---

## File Map

| File | Change |
|------|--------|
| `Xaml/ExerciseDetailPage.xaml` | Add named error label inside NoVideoPlaceholder |
| `CodeBehind/ExerciseDetailPage.xaml.cs` | Populate error label in OnVideoMediaFailed |
| `FreakLete.Api/Services/ExerciseTierService.cs` | Add structured logging to BackfillTiersFromPrEntriesAsync |

---

## Task 1: Surface video error in UI

**Files:**
- Modify: `Xaml/ExerciseDetailPage.xaml`
- Modify: `CodeBehind/ExerciseDetailPage.xaml.cs`

- [ ] **Step 1: Add named error label inside NoVideoPlaceholder**

In `Xaml/ExerciseDetailPage.xaml`, replace the `NoVideoPlaceholder` Border (lines 59-69):

```xml
<!-- No-video placeholder -->
<Border x:Name="NoVideoPlaceholder"
        HeightRequest="80"
        BackgroundColor="{StaticResource Surface}"
        IsVisible="True">
    <VerticalStackLayout HorizontalOptions="Center" VerticalOptions="Center" Spacing="4">
        <Label Text="Demo video yakında"
               FontSize="13"
               FontFamily="OpenSansRegular"
               TextColor="{StaticResource TextMuted}"
               HorizontalOptions="Center" />
        <Label x:Name="VideoErrorLabel"
               FontSize="11"
               FontFamily="OpenSansRegular"
               TextColor="#FF6B6B"
               HorizontalOptions="Center"
               IsVisible="False" />
    </VerticalStackLayout>
</Border>
```

- [ ] **Step 2: Populate the error label in OnVideoMediaFailed**

In `CodeBehind/ExerciseDetailPage.xaml.cs`, replace `OnVideoMediaFailed` (line 128):

```csharp
private void OnVideoMediaFailed(object? sender, MediaFailedEventArgs e)
{
    VideoPlayer.IsVisible = false;
    NoVideoPlaceholder.IsVisible = true;
    VideoErrorLabel.Text = $"Error: {e.ErrorMessage}";
    VideoErrorLabel.IsVisible = true;
}
```

- [ ] **Step 3: Build**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: 0 errors.

- [ ] **Step 4: Navigate to Bench Press on device and read the error**

Observe the red error text under "Demo video yakında":

| Error shown | Root cause | Next step |
|-------------|------------|-----------|
| `HTTP 404` | R2 files not uploaded | Task 4 (upload MP4s) |
| `HTTP 403` | R2 bucket permissions wrong | Fix R2 bucket public access policy |
| Codec / format error | Android ExoPlayer / media issue | Check ExoPlayer config |
| No error shown at all | `MediaUrl` is null/empty — deserialization issue | Step 5 below |

- [ ] **Step 5: If no error label appears — verify MediaUrl deserialization**

Add one line inside `BindExercise()` in `ExerciseDetailPage.xaml.cs`, right after `_exercise` is bound:

```csharp
System.Diagnostics.Debug.WriteLine($"[Video] MediaUrl={_exercise.MediaUrl ?? "(null)"}");
```

Run with USB debug, check VS Output window. If it logs `(null)` for Bench Press → JSON deserialization is broken (unlikely since `PropertyNameCaseInsensitive = true` is set, but confirms it).

- [ ] **Step 6: Commit diagnostic changes**

```bash
git add Xaml/ExerciseDetailPage.xaml CodeBehind/ExerciseDetailPage.xaml.cs
git commit -m "fix: surface video error message in placeholder for diagnosis"
```

---

## Task 2: Add structured logging to BackfillTiersFromPrEntriesAsync

**Files:**
- Modify: `FreakLete.Api/Services/ExerciseTierService.cs`

- [ ] **Step 1: Replace BackfillTiersFromPrEntriesAsync with a logged version**

In `FreakLete.Api/Services/ExerciseTierService.cs`, replace `BackfillTiersFromPrEntriesAsync` (lines 161-195) with:

```csharp
public async Task BackfillTiersFromPrEntriesAsync(int userId, CancellationToken ct = default)
{
    var prs = await _db.PrEntries
        .Where(p => p.UserId == userId && p.TrackingMode == "Strength" && p.Weight > 0 && p.Reps > 0)
        .ToListAsync(ct);

    _log.LogInformation("[Tiers] User {UserId}: found {PrCount} eligible Strength PRs", userId, prs.Count);

    if (prs.Count == 0)
    {
        _log.LogWarning("[Tiers] User {UserId}: no eligible PRs — verify TrackingMode=Strength, Weight>0, Reps>0 in PrEntries table", userId);
        return;
    }

    var defs = await _db.ExerciseDefinitions
        .Where(d => d.TierType == "StrengthRatio")
        .ToListAsync(ct);

    _log.LogInformation("[Tiers] ExerciseDefinitions with TierType=StrengthRatio: {DefCount}", defs.Count);

    if (defs.Count == 0)
    {
        _log.LogError("[Tiers] ExerciseDefinitions table has no StrengthRatio rows — migration SeedTierEligibleExerciseDefinitions may not be applied on this database");
        return;
    }

    var defByCatalogId = defs.ToDictionary(d => d.CatalogId, StringComparer.OrdinalIgnoreCase);

    var bestByExercise = prs
        .GroupBy(p => p.ExerciseName, StringComparer.OrdinalIgnoreCase)
        .Select(g => g
            .OrderByDescending(p => FreakLete.Services.CalculationService.CalculateOneRm(p.Weight, p.Reps, p.RIR ?? 0))
            .First());

    foreach (var pr in bestByExercise)
    {
        var normalized = NormalizeName(pr.ExerciseName);
        if (!defByCatalogId.TryGetValue(normalized, out var def))
        {
            _log.LogInformation("[Tiers] '{ExerciseName}' → normalized '{Normalized}': NO match. Known IDs: {KnownIds}",
                pr.ExerciseName, normalized, string.Join(", ", defByCatalogId.Keys.Take(8)));
            continue;
        }

        _log.LogInformation("[Tiers] '{ExerciseName}' → normalized '{Normalized}': matched '{CatalogId}', computing tier",
            pr.ExerciseName, normalized, def.CatalogId);

        await RecalculateTierAsync(
            userId, def.CatalogId, pr.ExerciseName,
            pr.TrackingMode, pr.Weight, pr.Reps, pr.RIR,
            athleticRawValue: null, ct);
    }
}
```

- [ ] **Step 2: Build API**

```bash
dotnet build FreakLete.Api
```

Expected: 0 errors.

- [ ] **Step 3: Deploy to Railway and trigger recalculate**

Push and deploy. Open the app → Profile page (triggers recalculate). Check Railway logs.

**Healthy output:**
```
[Tiers] User 1: found 3 eligible Strength PRs
[Tiers] ExerciseDefinitions with TierType=StrengthRatio: 14
[Tiers] 'Bench Press' → normalized 'benchpress': matched 'benchpress', computing tier
```

**Migration missing:**
```
[Tiers] ExerciseDefinitions table has no StrengthRatio rows — migration ... may not be applied
```
→ Go to Task 3.

**Name mismatch:**
```
[Tiers] 'Squat' → normalized 'squat': NO match. Known IDs: benchpress, backsquat, ...
```
→ Go to Task 4.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/ExerciseTierService.cs
git commit -m "fix: add structured logging to BackfillTiersFromPrEntriesAsync"
```

---

## Task 3: Apply ExerciseDefinitions migration on production (if needed)

Run only if Task 2 logs show 0 ExerciseDefinitions.

- [ ] **Step 1: Verify migration status via Railway DB**

```sql
SELECT COUNT(*) FROM "ExerciseDefinitions" WHERE "TierType" = 'StrengthRatio';
```

If result is 0 → migration not applied.

- [ ] **Step 2: Apply migration**

```bash
# From FreakLete.Api directory:
dotnet ef database update --connection "<Railway PostgreSQL connection string>"
```

Or if the app runs `MigrateDatabase()` on startup, just re-deploy after ensuring the migration is compiled in.

- [ ] **Step 3: Verify**

```sql
SELECT "CatalogId", "TierType" FROM "ExerciseDefinitions" WHERE "TierType" != '' LIMIT 5;
```

Expected: rows like `benchpress | StrengthRatio`, `backsquat | StrengthRatio`.

- [ ] **Step 4: Re-trigger recalculate from app**

Profile page → tiers should populate. Confirm in Railway logs.

---

## Task 4: Fix exercise name mismatch (if migration is applied but names don't match)

Run only if Task 2 logs show `NO match` for user's exercise names.

**Files:**
- Modify: `FreakLete.Api/Services/ExerciseTierService.cs`

- [ ] **Step 1: Read the mismatch names from Railway logs**

Logs will show exactly which exercise names the user logged (e.g., `'Squat'`, `'Deadlift'`, `'OHP'`) and which normalized IDs don't exist.

- [ ] **Step 2: Add alias map and use it in the foreach loop**

In `ExerciseTierService.cs`, add a static field after the class opening brace:

```csharp
private static readonly Dictionary<string, string> _nameAliases = new(StringComparer.OrdinalIgnoreCase)
{
    { "squat",            "backsquat" },
    { "barbell squat",    "backsquat" },
    { "deadlift",         "conventionaldeadlift" },
    { "rdl",              "romaniandeadlift" },
    { "ohp",              "overheadpress" },
    { "bench",            "benchpress" },
    { "row",              "barbellrow" },
    { "barbell row",      "barbellrow" },
    { "pull-up",          "pullup" },
    { "pull up",          "pullup" },
    { "hip thrust",       "hipthrust" },
    { "trap bar deadlift","trapbardeadlift" },
    { "push press",       "pushpress" },
    { "front squat",      "frontsquat" },
};
```

Then in the `foreach (var pr in bestByExercise)` loop, replace the lookup block:

```csharp
var normalized = NormalizeName(pr.ExerciseName);
// Try alias lookup if direct normalization has no match
if (!defByCatalogId.ContainsKey(normalized) &&
    _nameAliases.TryGetValue(pr.ExerciseName, out var aliased))
    normalized = aliased;

if (!defByCatalogId.TryGetValue(normalized, out var def))
{
    _log.LogInformation("[Tiers] '{ExerciseName}' → '{Normalized}': NO match after alias check", pr.ExerciseName, normalized);
    continue;
}
```

- [ ] **Step 3: Build, deploy, retest**

```bash
dotnet build FreakLete.Api
```

Deploy → Profile page → tiers should now appear.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/ExerciseTierService.cs
git commit -m "fix: add exercise name alias map for tier backfill normalization"
```

---

## Task 5: Upload R2 videos (if video error is HTTP 404)

Run only if Task 1 shows `HTTP 404` in the error label.

- [ ] **Step 1: Identify which files are needed**

The following filenames must exist at `https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/<name>`:

```
benchpress.mp4        backsquat.mp4         conventionaldeadlift.mp4
romaniandeadlift.mp4  trapbardeadlift.mp4   hipthrust.mp4
verticaljump.mp4      standingbroadjump.mp4 powerclean.mp4
powersnatch.mp4       pushpress.mp4         pullup.mp4
barbellrow.mp4        overheadpress.mp4     rsi.mp4
fortyyarddash.mp4     tenmetersprint.mp4    frontsquat.mp4
```

- [ ] **Step 2: Upload via Cloudflare R2 dashboard or wrangler**

```bash
wrangler r2 object put <bucket-name>/benchpress.mp4 --file ./videos/benchpress.mp4
# Repeat for each missing file
```

Or use Cloudflare dashboard: R2 → your bucket → Upload.

- [ ] **Step 3: Verify public access**

```bash
curl -I https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/benchpress.mp4
```

Expected: `HTTP/2 200` with `Content-Type: video/mp4`.

- [ ] **Step 4: Retest in app**

Open Bench Press exercise detail. Video should auto-play and loop.

---

## Self-Review

**Spec coverage:**
- [x] Video error surfaced in UI (Task 1)
- [x] Tier diagnostic logging added (Task 2)
- [x] Migration verification path (Task 3)
- [x] Name mismatch fix with alias map (Task 4)
- [x] R2 upload guide (Task 5)

**No placeholders** — all code blocks are complete and compilable.

**Type consistency** — `MediaFailedEventArgs` from `CommunityToolkit.Maui.Core` (already imported in ExerciseDetailPage.xaml.cs). `VideoErrorLabel` is an `x:Name` Label defined in XAML. `_log` field already exists in `ExerciseTierService`.
