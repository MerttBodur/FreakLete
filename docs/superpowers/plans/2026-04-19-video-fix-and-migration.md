# Video Fix & Migration Plan (Revised)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** (1) Upload Power Clean and Power Snatch MP4s to R2 so videos play in-app. (2) Add exercise name aliases ("clean"→"powerclean", "snatch"→"powersnatch") to tier service so short names match. (3) Apply SeedTierEligibleExerciseDefinitions migration on local DB.

**Architecture:** Three independent root causes confirmed by diagnostics. Videos fail because R2 files are missing (not because catalog is wrong — `mediaUrl` stays). Tiers may miss exercises logged with short names ("Clean", "Snatch") because NormalizeName produces "clean"/"snatch" but CatalogIds are "powerclean"/"powersnatch" — fix with an alias map. Migration is pending on local PostgreSQL, not Railway.

**Tech Stack:** .NET MAUI (C#, XAML), ASP.NET Core, EF Core, Cloudflare R2, local PostgreSQL

---

## Diagnosis Summary

| Symptom | Root Cause | Fix |
|---------|-----------|-----|
| Power Snatch / Power Clean: `ERROR_CODE_IO_BAD_HTTP_STATUS` (2004) | MP4 files not uploaded to R2 bucket | Task 1: upload videos to R2 |
| Tier names "Clean"/"Snatch" may not match CatalogId | `NormalizeName("Clean")` = "clean" ≠ "powerclean" | Task 2: add alias map in ExerciseTierService |
| `ExerciseDefinitions table has no StrengthRatio rows` | Migration not applied on local DB | Task 3: run `dotnet ef database update` locally |

---

## File Map

| File | Change |
|------|--------|
| `FreakLete.Api/Services/ExerciseTierService.cs` | Add `_nameAliases` static dict, use it in BackfillTiersFromPrEntriesAsync |
| Local PostgreSQL | Apply `SeedTierEligibleExerciseDefinitions` migration |
| Cloudflare R2 bucket | Upload `powerclean.mp4` and `powersnatch.mp4` |

---

## Task 1: Upload Power Clean and Power Snatch videos to R2

The `mediaUrl` fields in `exercise_catalog.json` are correct — do NOT remove them. The files just need to be uploaded.

- [ ] **Step 1: Verify which files are missing**

```bash
curl -I https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/powerclean.mp4
curl -I https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/powersnatch.mp4
```

Expected (missing): `HTTP/2 404` or `HTTP/2 403`
Expected (present): `HTTP/2 200` with `Content-Type: video/mp4`

- [ ] **Step 2: Upload via Cloudflare R2 dashboard**

Go to: Cloudflare dashboard → R2 → your bucket → Upload

Upload:
- `powerclean.mp4` → key must be exactly `powerclean.mp4`
- `powersnatch.mp4` → key must be exactly `powersnatch.mp4`

Or via wrangler CLI:
```bash
wrangler r2 object put <bucket-name>/powerclean.mp4 --file ./videos/powerclean.mp4
wrangler r2 object put <bucket-name>/powersnatch.mp4 --file ./videos/powersnatch.mp4
```

- [ ] **Step 3: Verify public access**

```bash
curl -I https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/powerclean.mp4
curl -I https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/powersnatch.mp4
```

Expected: `HTTP/2 200` with `Content-Type: video/mp4`.

- [ ] **Step 4: Test in app**

Navigate to Power Clean and Power Snatch detail pages. Videos should auto-play. No error label should appear.

---

## Task 2: Add exercise name aliases to ExerciseTierService

**Files:**
- Modify: `FreakLete.Api/Services/ExerciseTierService.cs`

`NormalizeName("Clean")` produces `"clean"` which has no match in ExerciseDefinitions (CatalogId is `"powerclean"`). Add an alias map.

- [ ] **Step 1: Add _nameAliases static field**

In `FreakLete.Api/Services/ExerciseTierService.cs`, add after the class opening brace (before the constructor):

```csharp
private static readonly Dictionary<string, string> _nameAliases = new(StringComparer.OrdinalIgnoreCase)
{
    { "clean",             "powerclean" },
    { "snatch",            "powersnatch" },
    { "squat",             "backsquat" },
    { "barbell squat",     "backsquat" },
    { "deadlift",          "conventionaldeadlift" },
    { "rdl",               "romaniandeadlift" },
    { "ohp",               "overheadpress" },
    { "bench",             "benchpress" },
    { "row",               "barbellrow" },
    { "barbell row",       "barbellrow" },
    { "pull-up",           "pullup" },
    { "pull up",           "pullup" },
    { "hip thrust",        "hipthrust" },
    { "trap bar deadlift", "trapbardeadlift" },
    { "push press",        "pushpress" },
    { "front squat",       "frontsquat" },
};
```

- [ ] **Step 2: Use alias map in BackfillTiersFromPrEntriesAsync**

Find the `foreach (var pr in bestByExercise)` loop in `BackfillTiersFromPrEntriesAsync`. Replace the lookup block:

Before:
```csharp
var normalized = NormalizeName(pr.ExerciseName);
if (!defByCatalogId.TryGetValue(normalized, out var def))
{
    _log.LogInformation("[Tiers] '{ExerciseName}' → normalized '{Normalized}': NO match. Known IDs: {KnownIds}",
        pr.ExerciseName, normalized, string.Join(", ", defByCatalogId.Keys.Take(8)));
    continue;
}
```

After:
```csharp
var normalized = NormalizeName(pr.ExerciseName);
if (!defByCatalogId.ContainsKey(normalized) &&
    _nameAliases.TryGetValue(pr.ExerciseName.Trim(), out var aliased))
    normalized = aliased;

if (!defByCatalogId.TryGetValue(normalized, out var def))
{
    _log.LogInformation("[Tiers] '{ExerciseName}' → '{Normalized}': NO match after alias check. Known IDs: {KnownIds}",
        pr.ExerciseName, normalized, string.Join(", ", defByCatalogId.Keys.Take(8)));
    continue;
}
```

- [ ] **Step 3: Build API**

```bash
dotnet build FreakLete.Api
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api/Services/ExerciseTierService.cs
git commit -m "fix: add exercise name alias map for clean/snatch and common short names in tier backfill"
```

---

## Task 3: Apply migration on local DB

- [ ] **Step 1: Apply all pending migrations**

```bash
cd FreakLete.Api
dotnet ef database update
```

Expected output includes: `Applying migration '20260417120000_SeedTierEligibleExerciseDefinitions'` then `Done.`

- [ ] **Step 2: Verify row count**

Connect to local PostgreSQL and run:

```sql
SELECT COUNT(*) FROM "ExerciseDefinitions" WHERE "TierType" = 'StrengthRatio';
```

Expected: `14`.

- [ ] **Step 3: Trigger recalculate from app**

Run API locally → open app → Profile page → check logs:

```
[Tiers] User X: found N eligible Strength PRs
[Tiers] ExerciseDefinitions with TierType=StrengthRatio: 14
[Tiers] 'Power Clean' → normalized 'powerclean': matched 'powerclean', computing tier
```

---

## Self-Review

**Spec coverage:**
- [x] Power Clean / Power Snatch videos → Task 1 uploads to R2 (mediaUrl NOT removed)
- [x] Short name aliases "clean"/"snatch" → Task 2 alias map in ExerciseTierService
- [x] Local DB migration → Task 3 applies locally

**No placeholders** — all steps show exact commands and expected output.

**Type consistency** — `_nameAliases` keys match user-typed exercise names; alias values match CatalogIds seeded by the migration.
