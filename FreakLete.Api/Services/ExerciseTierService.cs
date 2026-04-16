using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Tier;
using FreakLete.Api.Entities;
using FreakLete.Core.Tier;
using FreakLete.Services;
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

        if (string.Equals(def.TierType, "StrengthRatio", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(def.Mechanic, "isolation", StringComparison.OrdinalIgnoreCase))
            return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

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

    public async Task BackfillTiersFromPrEntriesAsync(int userId, CancellationToken ct = default)
    {
        // Only strength PRs carry weight/reps needed for StrengthRatio calculation.
        var prs = await _db.PrEntries
            .Where(p => p.UserId == userId && p.TrackingMode == "Strength" && p.Weight > 0 && p.Reps > 0)
            .ToListAsync(ct);

        if (prs.Count == 0) return;

        var defs = await _db.ExerciseDefinitions
            .Where(d => d.TierType == "StrengthRatio")
            .ToListAsync(ct);

        var defByCatalogId = defs.ToDictionary(d => d.CatalogId, StringComparer.OrdinalIgnoreCase);

        // Best 1RM per exercise name.
        var bestByExercise = prs
            .GroupBy(p => p.ExerciseName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .OrderByDescending(p => FreakLete.Core.Services.CalculationService.CalculateOneRm(p.Weight, p.Reps, p.RIR ?? 0))
                .First());

        foreach (var pr in bestByExercise)
        {
            // Normalize display name to catalog ID format: "Bench Press" → "benchpress".
            var normalized = pr.ExerciseName
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("'", "");

            if (!defByCatalogId.TryGetValue(normalized, out var def)) continue;

            await RecalculateTierAsync(
                userId, def.CatalogId, pr.ExerciseName,
                pr.TrackingMode, pr.Weight, pr.Reps, pr.RIR,
                athleticRawValue: null, ct);
        }
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
