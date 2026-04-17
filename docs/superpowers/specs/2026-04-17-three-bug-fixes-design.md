# Design: Three Bug Fixes — Videos, Workout Photos, Exercise Tiers

**Date:** 2026-04-17
**Status:** Approved

## Context

Three persistent, independent bugs affecting the mobile app:

1. Exercise detail page shows "Demo video yakında" for all exercises despite MediaUrl being seeded in the DB.
2. Quick workout rail on HomePage renders fallback color/letter for all programs — no photos visible.
3. Exercise Tiers section on ProfilePage is always empty.

---

## Fix 1 — Exercise Media Videos

### Root Cause

`ExerciseDetailPage` reads `_exercise.MediaUrl` from `ExerciseCatalogItem`, which is loaded exclusively from the embedded JSON file `Resources/Raw/exercise_catalog.json`. The DB migration `SeedExerciseMediaUrls` populates `MediaUrl` in the backend, but the client never calls the Exercise Catalog API — it uses the local JSON only. The JSON has zero `mediaUrl` fields.

### Fix

Add `mediaUrl` fields to `Resources/Raw/exercise_catalog.json` for the 19 tier-eligible exercises.

URL pattern: `https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/{catalogId}.mp4`

Client JSON `id` values to update (verified against actual JSON):
- `pushbarbellbenchpress` → `benchpress.mp4`
- backsquat, conventionaldeadlift, sumodeadlift, overheadpress, powerclean, powersnatch, frontsquat, romaniandeadlift, barbellrow, pullup, trapbardeadlift, hipthrust, pushpress — IDs to be confirmed from JSON grep
- verticaljump, standingbroadjump, rsi, fortyyarddash, tenmetersprint — IDs to be confirmed from JSON grep

**Changed file:** `Resources/Raw/exercise_catalog.json` (19 entries, `mediaUrl` field added)

**No additional code changes.** `ExerciseCatalogItem.MediaUrl` property and `ExerciseDetailPage` binding already work correctly.

---

## Fix 2 — Quick Workout Photos

### Root Cause

All cards render with fallback color/letter — no image appears. Two compounding failures:

**A:** Dynamically created `Image` control has no explicit `WidthRequest`/`HeightRequest`. `Aspect.AspectFill` inside a `Grid` may not size correctly on Android without explicit dimensions.

**B:** `ImageSource.FromFile` is more reliable than implicit string conversion for dynamically created Image controls in MAUI on Android.

### Fix

In `BuildQuickWorkoutCards` in `CodeBehind/HomePage.xaml.cs`, change Image creation:
- Use `ImageSource.FromFile(imageName + ".png")` instead of string assignment
- Add explicit `WidthRequest = 180, HeightRequest = 90`
- Add one `Debug.WriteLine` log when image is resolved (logcat diagnostic)

**Changed file:** `CodeBehind/HomePage.xaml.cs` (~5 lines, Image creation block)

---

## Fix 3 — Exercise Tiers Empty

### Root Causes

**A — Client sends wrong catalogId format:**
Client sends `_selectedPrExerciseItem?.Id` = e.g. `"pushbarbellbenchpress"`. Backend `ExerciseDefinitions.CatalogId` = `"benchpress"`. Lookup fails silently, no tier row created.

**B — DB may have no ExerciseDefinitions rows:**
Older migrations used `UPDATE` only. New migration `SeedTierEligibleExerciseDefinitions` (2026-04-17) uses `INSERT ... ON CONFLICT DO UPDATE` — seeds rows on next deploy.

### Fix

**Fix 3a — Backend normalize fallback** in `FreakLete.Api/Services/ExerciseTierService.cs`:

Extract `NormalizeName` helper (already exists inline in `BackfillTiersFromPrEntriesAsync`):
```csharp
private static string NormalizeName(string name) =>
    name.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("'", "");
```

In `RecalculateTierAsync`, after direct `def` lookup fails, try normalized `exerciseName`:
```csharp
if (def is null)
    def = await _db.ExerciseDefinitions
        .FirstOrDefaultAsync(d => d.CatalogId == NormalizeName(exerciseName), ct);
if (def is null || string.IsNullOrWhiteSpace(def.TierType)) return null;
```

Also replace the inline normalization in `BackfillTiersFromPrEntriesAsync` with the shared helper.

**Fix 3b — Deploy migration:** `SeedTierEligibleExerciseDefinitions` already committed — runs on next Railway deploy.

**Fix 3c — Backfill existing users:** No new code. "Refresh Tiers" button on ProfilePage already calls `POST /api/profile/tiers/recalculate`.

**Changed file:** `FreakLete.Api/Services/ExerciseTierService.cs` (~12 lines)

---

## Validation

| Fix | Verification |
|---|---|
| Fix 1 | Emulator: Exercise Catalog → tap Bench Press → video plays |
| Fix 2 | Emulator: Home → quick workout rail shows starter card photos |
| Fix 3 | Submit Bench Press PR → ProfilePage Tiers not empty; OR tap Refresh Tiers |

## Out of Scope

- Client catalog ↔ backend DB sync
- New DTOs or endpoints
- Changing client JSON `id` format
