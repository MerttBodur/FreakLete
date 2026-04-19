# Design: Tier Congrats Popup + Profile Tier Removal

**Date:** 2026-04-19
**Status:** Approved, ready for planning

## Goal

Move exercise tier feedback from a dedicated Profile section into the PR-save flow on `CalculationsPage`. On tier-level change or first tier earned, show a congratulations popup with the next milestone target. Remove the Profile tier UI. Fix the startup seed warning: `ExerciseDefinitions table has no StrengthRatio rows`.

## User Flow

1. User logs a PR in `CalculationsPage` → `POST /api/pr-entries`.
2. Server saves `PrEntry`, computes new tier, **reads previous `UserExerciseTier.Level` before writing** to detect change, computes next-milestone context.
3. Response includes an extended `TierResultDto` with previous level, level-up flag, next-level target, delta to target, and progress percent.
4. Client:
   - If `IsLevelUp == true` **or** `PreviousLevel == null` → open `TierCongratsPopup`.
   - Always (tier non-null): render a small "Next: **Advanced** — +12 kg" line below the save confirmation text.
5. Profile page no longer shows tier cards at all.

## Non-Goals

- Re-processing historical PRs to backfill tier history beyond what already happens on startup.
- A separate tier-history viewer/graph.
- Languages beyond existing Turkish/English via `AppLanguage`.
- Changing the tier threshold data itself.

## Architecture

### Server

**DTO change — `FreakLete.Api/DTOs/Tier/ExerciseTierDto.cs`:**

Extend `TierResultDto` with five new fields:

| Field | Type | Meaning |
|---|---|---|
| `PreviousLevel` | `string?` | Level before this PR; `null` = first tier ever for this catalog |
| `IsLevelUp` | `bool` | `PreviousLevel != null && PreviousLevel != Level` |
| `NextLevel` | `string?` | Next tier name; `null` = already at max tier |
| `NextTargetRaw` | `double?` | Absolute target in raw units (kg / cm / m / s), `null` at max |
| `NextDelta` | `double?` | Distance to `NextTargetRaw` from current `RawValue`; positive value represents "how much more is needed"; `null` at max |
| `ProgressPercent` | `double` | `0-100`, clamped; position within the current tier band |

**Service change — `FreakLete.Api/Services/ExerciseTierService.cs` `RecalculateTierAsync`:**

- Load the existing `UserExerciseTier` row for `(userId, catalogId)` *before* updating it → capture `PreviousLevel`.
- After resolving the new `Level`, derive next-milestone values from the same `thresholds[]` array already used by `TierResolver.Resolve`:
  - Find `currentIndex = Array.IndexOf(levels, Level)`.
  - `nextIndex = currentIndex + 1`.
  - If `nextIndex >= thresholds.Length` → max tier: `NextLevel = null`, `NextTargetRaw = null`, `NextDelta = null`, `ProgressPercent = 100`.
  - Otherwise:
    - For `StrengthRatio`: `NextTargetRaw = thresholds[nextIndex] * user.WeightKg`
    - For `AthleticAbsolute`: `NextTargetRaw = thresholds[nextIndex]`
    - For `AthleticInverse`: `NextTargetRaw = thresholds[nextIndex]` (lower is better)
    - For `StrengthRatio` / `AthleticAbsolute`: `NextDelta = NextTargetRaw - RawValue`
    - For `AthleticInverse`: `NextDelta = RawValue - NextTargetRaw`
    - `ProgressPercent`: with `lo = thresholds[currentIndex]`, `hi = thresholds[nextIndex]`:
      - `StrengthRatio` / `AthleticAbsolute`: `((Ratio ?? RawValue) - lo) / (hi - lo) * 100`, clamped `[0, 100]`
      - `AthleticInverse`: `(lo - (Ratio ?? RawValue)) / (lo - hi) * 100`, clamped `[0, 100]`
- Write the new `UserExerciseTier` row (unchanged logic).
- Return populated `TierResultDto`.

**Controller — `FreakLete.Api/Controllers/PrEntriesController.cs`:**

No change to `Create` method's existing flow — it already calls `RecalculateTierAsync` and attaches the result. The richer DTO flows through automatically.

**Obsolete endpoint — `FreakLete.Api/Controllers/ProfileTiersController.cs`:**

With the Profile UI removed, `POST /api/profile/tiers/recalculate` has no caller. Delete the controller. Also delete `IApiClient.RecalculateTiersAsync` + implementation.

**Startup seed guard — `FreakLete.Api/Program.cs`:**

After `db.Database.Migrate()` (or wherever migrations apply), add an idempotent seed check:

```csharp
if (!await db.ExerciseDefinitions.AnyAsync(d => d.TierType == "StrengthRatio"))
{
    // Re-apply the same seed data the migration would have inserted.
    // Use UPSERT (ON CONFLICT (CatalogId) DO UPDATE) so partially-populated rows get TierType columns filled.
    // Rows data can come from SeedTierEligibleExerciseDefinitions.Rows (make internal static) or inline constant.
}
```

The migration file itself (`20260417120000_SeedTierEligibleExerciseDefinitions.cs`) stays unchanged. The guard fixes existing-environment drift where the migration ran before the seed rows were valid, or where rows existed without tier columns filled.

### Client

**Deletions (MAUI project root):**

- `Xaml/ProfilePage.xaml` — remove the `<!-- Exercise Tiers -->` `Border` block (currently ~lines 244-278) including `TierEmptyLabel`, `TierCardsContainer`, refresh button, and surrounding container.
- `CodeBehind/ProfilePage.xaml.cs`:
  - Remove methods: `RenderTierCards`, `BuildTierCard`, `OnRefreshTiersTapped`.
  - Remove `await _api.RecalculateTiersAsync()` call from `LoadProfileAsync` (line ~254).
  - Keep `BackfillTiersFromPrEntriesAsync` if it's invoked from startup / elsewhere; otherwise remove.
- `Services/IApiClient.cs` + `Services/ApiClient.cs` — remove `RecalculateTiersAsync`.
- `Models/ExerciseTierResponse.cs` — remove if no remaining consumer.

**Additions:**

1. **`Models/TierResult.cs`** (or update existing client model that mirrors `TierResultDto`) to include the five new fields matching the server DTO.

2. **`Helpers/TierDisplayFormatter.cs`**:

```csharp
public static string FormatDelta(string trackingMode, double delta)
{
    return trackingMode switch
    {
        "Strength"         => $"+{delta:0} kg",
        "AthleticHeight"   => $"+{delta:0} cm",
        "AthleticDistance" => $"+{delta:0} cm",   // broad jump stored in cm; confirm at impl time
        "AthleticIndex"    => $"+{delta:0.00}",   // RSI unitless
        "AthleticTime"     => $"-{delta:0.00} s",
        _ => $"+{delta:0.##}"
    };
}
```

Confirm actual `TrackingMode` string values and units at implementation time by reading the existing `CalculationsPage` display code.

3. **`Xaml/Controls/TierCongratsPopup.xaml`** — a `CommunityToolkit.Maui.Views.Popup` with dark-theme styling:

- Shell: `Surface` background, `SurfaceBorder` stroke 1px, `24` corner radius, padding `24`, width `320`.
- Content stack (spacing `16`):
  - Header label: `"Tebrikler!" / "Congratulations!"` — `SubHeadline` style, center.
  - Tier badge: large (`60x60`) rounded rectangle. Colors by tier level:
    - `Untrained` → `SurfaceStrong`
    - `Novice` → `Info`
    - `Intermediate` → `Success`
    - `Advanced` → `AccentGlow`
    - `Elite` → `Accent`
    - `Godlike` → gradient `Accent → AccentGlow`
  - Subtitle: if `PreviousLevel == null` → *"Your first tier: **{Level}**!"*; else → *"You reached **{Level}**!"*.
  - Divider (`BorderSubtle`).
  - Next milestone block:
    - If `NextLevel != null`: *"Next: **{NextLevel}** — {FormatDelta(TrackingMode, NextDelta)}"* + thin progress bar filled to `ProgressPercent` in `AccentGlow`.
    - If `NextLevel == null`: *"Highest tier achieved."*.
  - Close CTA: primary button, `Accent`, full-width, `"Close" / "Kapat"`.

Static helper: `TierCongratsPopup.ShowAsync(Page page, TierResult tier)` creates and displays the popup via `page.ShowPopup(...)`.

4. **`CodeBehind/CalculationsPage.xaml.cs` — `OnSavePrClicked` modification:**

After existing success branch (`_api.CreatePrEntryAsync` returns OK):

```csharp
var tier = result.Data?.Tier;
if (tier is not null)
{
    // Inline "Next: ... +X kg" line below save confirmation label
    UpdateNextMilestoneLabel(tier);
    if (tier.IsLevelUp || tier.PreviousLevel is null)
        await TierCongratsPopup.ShowAsync(this, tier);
}
```

Add:
- `Xaml/CalculationsPage.xaml` → new `Label x:Name="NextMilestoneLabel"` (muted style, 12px) below the existing save-result text, hidden when tier null.
- `UpdateNextMilestoneLabel(TierResult tier)` helper: sets text to `"Next: {NextLevel} — {FormatDelta(...)}"` or hides label when `NextLevel == null`.

5. **`Services/AppLanguage.cs`** — add strings:

| Key | EN | TR |
|---|---|---|
| `TierCongratsTitle` | `Congratulations!` | `Tebrikler!` |
| `TierFirstTierText` | `Your first tier: {0}!` | `İlk seviyen: {0}!` |
| `TierLevelUpText` | `You reached {0}!` | `{0} seviyesine ulaştın!` |
| `TierNextMilestonePrefix` | `Next: {0}` | `Sıradaki: {0}` |
| `TierMaxTierText` | `Highest tier achieved.` | `En yüksek seviyeye ulaşıldı.` |
| `TierCloseButton` | `Close` | `Kapat` |

### Data Flow Summary

```
CalculationsPage.OnSavePrClicked
  └─ ApiClient.CreatePrEntryAsync(PrEntryRequest)
       └─ POST /api/pr-entries
            ├─ PrEntriesController.Create
            │    ├─ Save PrEntry
            │    └─ ExerciseTierService.RecalculateTierAsync
            │         ├─ Load previous UserExerciseTier → PreviousLevel
            │         ├─ Compute RawValue, Ratio, new Level
            │         ├─ Compute NextLevel, NextTargetRaw, NextDelta, ProgressPercent
            │         └─ Save new UserExerciseTier
            └─ Return PrEntryResponse { Tier = TierResultDto }
  └─ Handle response
       ├─ UpdateNextMilestoneLabel(tier)
       └─ if IsLevelUp || PreviousLevel == null → TierCongratsPopup.ShowAsync
```

## Error Handling

- Tier service returns `null` for isolation lifts, missing bodyweight, non-eligible exercises, or when `ExerciseDefinitions` seed is missing. Client treats `null` as "no tier feedback, skip popup and milestone line" — no error surfaced.
- Popup failures (e.g., popup toolkit not initialized): caught and logged; save success message still shows.
- Server DTO fields are nullable so existing test fixtures and partial data don't crash serialization.

## Testing

### Unit — `FreakLete.Core.Tests/TierResolverTests.cs` (new cases)

1. StrengthRatio next-target: ratio `1.10`, thresholds `[0.5, 1.0, 1.25, 1.5, 1.75]`, bodyweight `80` → current `Novice`, next `Intermediate`, `NextTargetRaw == 100`, `NextDelta == 12` (given RawValue = 88), `ProgressPercent ≈ 40`.
2. AthleticInverse (sprint): time `5.5`, thresholds `[5.8, 5.3, 4.9, 4.6, 4.4]` → current `Novice`, next `Intermediate`, `NextTargetRaw == 4.9`, `NextDelta == 0.6`, `ProgressPercent ≈ 60`.
3. Max-tier: ratio `2.0` with `[0.5, 1.0, 1.25, 1.5, 1.75]` → `NextLevel == null`, `NextDelta == null`, `ProgressPercent == 100`.
4. Level-up detection: previous `Novice`, new `Intermediate` → `IsLevelUp == true`. Previous `Novice`, new `Novice` → `IsLevelUp == false`. Previous `null` → `IsLevelUp == false` (first-tier path handled by `PreviousLevel == null` check at call site).

### Integration — `FreakLete.Api.Tests/PrEntryIntegrationTests.cs` (new cases)

1. First-time tier-eligible PR → `response.Tier.PreviousLevel == null`, `IsLevelUp == false`, `Level` populated, `NextLevel` populated, `NextDelta > 0`.
2. Second PR that crosses threshold → `PreviousLevel == "Novice"`, `Level == "Intermediate"`, `IsLevelUp == true`.
3. Second PR within same tier band → `PreviousLevel == "Novice"`, `Level == "Novice"`, `IsLevelUp == false`.
4. Max-tier PR → `NextLevel == null`, `NextDelta == null`, `ProgressPercent == 100`.
5. Isolation / non-eligible exercise → `response.Tier == null`.

### Manual smoke

- `dotnet ef database update` from clean state → no `ExerciseDefinitions table has no StrengthRatio rows` warning in startup log.
- Log bench PR as new user → popup opens showing "first tier", next milestone line visible.
- Log another bench PR crossing threshold → popup opens with level-up message; next milestone updates.
- Log athletic PR (vertical jump) → popup shows `+{cm} cm` delta.
- Log sprint PR → popup shows `-{seconds} s` delta.
- Open Profile → no Exercise Tiers block, no layout gap, no broken bindings.

## Risks

| ID | Risk | Likelihood | Mitigation |
|---|---|---|---|
| R1 | Removing Profile tier block orphans `BackfillTiersFromPrEntriesAsync` if it was only triggered from profile load | Low | Audit call sites; keep backfill in startup path if needed, remove otherwise |
| R2 | `CommunityToolkit.Maui` Popup package not yet referenced | Low | Add NuGet reference as Task 0 of implementation plan |
| R3 | AthleticInverse delta sign confusion | Low | Covered by unit test; `FormatDelta` prepends `-` prefix for time |
| R4 | Startup seed guard could duplicate rows if run against a DB that already has some but not all entries | Medium | Use `UPSERT` / `ON CONFLICT (CatalogId) DO UPDATE` in the guard SQL, not a blind insert |
| R5 | `TrackingMode` enum strings (`"AthleticHeight"` etc.) may differ from what's actually stored | Low | Verify at implementation by reading existing `ExerciseDefinitions` rows + `CalculationsPage` display code |

## File Map

| File | Action |
|---|---|
| `FreakLete.Api/DTOs/Tier/ExerciseTierDto.cs` | Extend `TierResultDto` with 5 new fields |
| `FreakLete.Api/Services/ExerciseTierService.cs` | Load previous level; compute next-milestone values |
| `FreakLete.Api/Controllers/ProfileTiersController.cs` | Delete (no remaining caller) |
| `FreakLete.Api/Program.cs` | Add idempotent seed guard after migrations |
| `FreakLete.Core.Tests/TierResolverTests.cs` | 4 new tests |
| `FreakLete.Api.Tests/PrEntryIntegrationTests.cs` | 5 new tests |
| `Models/TierResult.cs` | Add 5 new fields on client model |
| `Models/ExerciseTierResponse.cs` | Delete if unreferenced |
| `Helpers/TierDisplayFormatter.cs` | New — `FormatDelta(trackingMode, delta)` |
| `Xaml/Controls/TierCongratsPopup.xaml` + `.cs` | New popup control |
| `Xaml/ProfilePage.xaml` | Remove `<!-- Exercise Tiers -->` block |
| `CodeBehind/ProfilePage.xaml.cs` | Remove `RenderTierCards`, `BuildTierCard`, `OnRefreshTiersTapped`, `RecalculateTiersAsync` call |
| `Xaml/CalculationsPage.xaml` | Add `NextMilestoneLabel` below save-result text |
| `CodeBehind/CalculationsPage.xaml.cs` | Hook popup + next-milestone update in `OnSavePrClicked` |
| `Services/IApiClient.cs` + `Services/ApiClient.cs` | Remove `RecalculateTiersAsync` |
| `Services/AppLanguage.cs` | Add 6 tier-popup strings |

---

## Self-Review

**Placeholder scan:** No "TBD" / "TODO". Two "confirm at implementation time" notes on `TrackingMode` strings and broad-jump units — those are verification tasks, not missing specs.

**Internal consistency:** DTO field list, client model, `FormatDelta` branches, and test cases all reference the same 5 new fields and same `TrackingMode` values. Popup trigger condition (`IsLevelUp || PreviousLevel == null`) stated identically in flow and client changes sections.

**Scope:** Single implementation plan. Tasks cleanly separable: (0) NuGet popup package, (1) server DTO + service, (2) startup seed guard, (3) delete Profile tier UI, (4) add popup + milestone label, (5) tests.

**Ambiguity:** `BackfillTiersFromPrEntriesAsync` fate flagged as R1 — implementation plan must decide concretely during Task 3. `Broad jump cm vs m` flagged — decided at impl time by reading existing display code. No other open interpretation.
