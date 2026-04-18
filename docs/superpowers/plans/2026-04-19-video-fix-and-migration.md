# Video Fix & Production Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix Power Clean and Power Snatch video errors (HTTP bad status from R2), and apply the SeedTierEligibleExerciseDefinitions migration on the Railway production database so exercise tiers populate.

**Architecture:** Two independent root causes confirmed by diagnostics. (1) `powerclean.mp4` and `powersnatch.mp4` are referenced in `exercise_catalog.json` but do not exist in the R2 bucket → remove their `mediaUrl` fields until videos are ready. (2) The `SeedTierEligibleExerciseDefinitions` EF Core migration is compiled but not applied on the Railway PostgreSQL database → apply it via `dotnet ef database update`.

**Tech Stack:** .NET MAUI (C#, XAML), ASP.NET Core, EF Core, Cloudflare R2, Railway PostgreSQL

---

## Diagnosis Summary

| Symptom | Root Cause | Fix |
|---------|-----------|-----|
| Power Snatch / Power Clean: `ERROR_CODE_IO_BAD_HTTP_STATUS` (2004) | `mediaUrl` in catalog points to R2 objects that don't exist | Task 1: remove `mediaUrl` from those two entries |
| `[Tiers] ExerciseDefinitions table has no StrengthRatio rows` | Migration `SeedTierEligibleExerciseDefinitions` not applied on prod DB | Task 2: apply migration on Railway |

---

## File Map

| File | Change |
|------|--------|
| `Resources/Raw/exercise_catalog.json` | Remove `mediaUrl` from Power Clean and Power Snatch entries |
| Railway PostgreSQL | Apply pending EF Core migration |

---

## Task 1: Remove mediaUrl from Power Clean and Power Snatch

**Files:**
- Modify: `Resources/Raw/exercise_catalog.json` (lines ~2121–2270)

- [ ] **Step 1: Remove mediaUrl from Power Clean (line 2122)**

Find the entry with `"id": "olympicliftspowerclean"` and remove its `"mediaUrl"` line:

Before:
```json
{
  "id": "olympicliftspowerclean",
  "mediaUrl": "https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/powerclean.mp4",
  "name": "Power Clean",
```

After:
```json
{
  "id": "olympicliftspowerclean",
  "name": "Power Clean",
```

- [ ] **Step 2: Remove mediaUrl from Power Snatch (line 2269)**

Find `"id": "olympicliftspowersnatch"` and remove its `"mediaUrl"` line:

Before:
```json
{
  "id": "olympicliftspowersnatch",
  "mediaUrl": "https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/powersnatch.mp4",
  "name": "Power Snatch",
```

After:
```json
{
  "id": "olympicliftspowersnatch",
  "name": "Power Snatch",
```

- [ ] **Step 3: Build**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: 0 errors.

- [ ] **Step 4: Verify in app**

Navigate to Power Clean and Power Snatch detail pages. Each should show "Demo video yakında" placeholder cleanly with no error text underneath.

- [ ] **Step 5: Commit**

```bash
git add Resources/Raw/exercise_catalog.json
git commit -m "fix: remove missing R2 mediaUrl for Power Clean and Power Snatch"
```

---

## Task 2: Apply SeedTierEligibleExerciseDefinitions migration on Railway

- [ ] **Step 1: Get Railway connection string**

Railway dashboard → PostgreSQL service → Connect tab → copy connection string.

Format: `Host=<host>;Port=<port>;Database=<db>;Username=<user>;Password=<pass>`

- [ ] **Step 2: Verify migration is pending**

```bash
cd FreakLete.Api
dotnet ef migrations list --connection "Host=...;Port=...;Database=...;Username=...;Password=..."
```

Expected: `SeedTierEligibleExerciseDefinitions` listed without `[Applied]`.

- [ ] **Step 3: Apply migration**

```bash
dotnet ef database update --connection "Host=...;Port=...;Database=...;Username=...;Password=..."
```

Expected: `Done.` with no errors.

- [ ] **Step 4: Verify row count in DB**

```sql
SELECT COUNT(*) FROM "ExerciseDefinitions" WHERE "TierType" = 'StrengthRatio';
```

Expected: `14` (benchpress, backsquat, conventionaldeadlift, sumodeadlift, overheadpress, powerclean, powersnatch, frontsquat, romaniandeadlift, barbellrow, pullup, trapbardeadlift, hipthrust, pushpress).

- [ ] **Step 5: Trigger recalculate from app**

Open app → Profile page → tiers should populate for logged Strength PRs.

Check Railway logs for:
```
[Tiers] User X: found N eligible Strength PRs
[Tiers] ExerciseDefinitions with TierType=StrengthRatio: 14
[Tiers] 'Bench Press' → normalized 'benchpress': matched 'benchpress', computing tier
```

---

## Self-Review

**Spec coverage:**
- [x] Power Clean / Power Snatch video error → Task 1 removes broken mediaUrl
- [x] ExerciseDefinitions migration → Task 2 applies it on production

**No placeholders** — all steps show exact commands and expected output.

**Type consistency** — no code changes to typed APIs; JSON data removal only for Task 1.
