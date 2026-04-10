namespace FreakLete.Services;

/// <summary>
/// Deterministic, MAUI-free insight resolver for calculation results.
/// Produces band labels and contextual sentences from known norm profiles.
/// NEVER produces a fake tier for unsupported metrics or profiles.
/// </summary>
public static class CalculationInsightResolver
{
    // ── Band enum ────────────────────────────────────────────────────────────

    public enum InsightBand
    {
        NeedsWork,   // "Gelistirilmeli"
        Adequate,    // "Idare Eder"
        Good,        // "Iyi"
        Elite        // "Elit"
    }

    // ── Output record ────────────────────────────────────────────────────────

    /// <summary>
    /// Non-null only when a supported norm profile exists.
    /// If null, caller must show raw result only — no fake tier.
    /// </summary>
    public sealed record InsightResult(
        InsightBand Band,
        string BandLabel,
        string Summary,
        string SportContext,
        string GlobalContext);

    // ── 1RM Supported movements ──────────────────────────────────────────────

    /// <summary>
    /// Movements for which bodyweight-relative 1RM norms are defined.
    /// Canonical lowercase names for case-insensitive matching.
    /// </summary>
    private static readonly HashSet<string> SupportedOneRmMovements = new(StringComparer.OrdinalIgnoreCase)
    {
        "bench press",
        "back squat",
        "deadlift",
        "military press",
        "overhead press",
        "power clean"
    };

    /// <summary>
    /// Ratio thresholds (1RM / bodyweight) per movement.
    /// Bands: [NeedsWork, Adequate, Good, Elite+]
    /// Boundaries: if ratio >= Elite → Elite, >= Good → Good, >= Adequate → Adequate, else NeedsWork.
    /// Values derived from broad athlete-population data (intermediate to competitive recreational lifters).
    /// </summary>
    private static readonly Dictionary<string, (double Adequate, double Good, double Elite)>
        OneRmRatioThresholds = new(StringComparer.OrdinalIgnoreCase)
        {
            ["bench press"]     = (0.80, 1.10, 1.40),
            ["back squat"]      = (1.00, 1.40, 1.80),
            ["deadlift"]        = (1.20, 1.60, 2.00),
            ["military press"]  = (0.50, 0.70, 0.90),
            ["overhead press"]  = (0.50, 0.70, 0.90),
            ["power clean"]     = (0.75, 1.00, 1.25),
        };

    // ── 1RM Insight ──────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a 1RM insight given the estimated 1RM and optional bodyweight.
    /// Returns null if movement is unsupported or bodyweight is missing.
    /// </summary>
    public static InsightResult? ResolveOneRm(
        string movementName,
        double estimatedOneRmKg,
        double? bodyweightKg)
    {
        if (!IsSupportedOneRmMovement(movementName))
            return null;

        if (!bodyweightKg.HasValue || bodyweightKg.Value <= 0)
            return null;

        var ratio = estimatedOneRmKg / bodyweightKg.Value;
        var normalizedKey = NormalizeMovementName(movementName);

        if (!OneRmRatioThresholds.TryGetValue(normalizedKey, out var thresholds))
            return null;

        var band = ClassifyRatio(ratio, thresholds.Adequate, thresholds.Good, thresholds.Elite);
        return BuildOneRmInsight(band, movementName, ratio);
    }

    public static bool IsSupportedOneRmMovement(string movementName)
        => SupportedOneRmMovements.Contains(movementName.Trim());

    private static string NormalizeMovementName(string name)
    {
        var trimmed = name.Trim().ToLowerInvariant();
        // Alias: "military press" and "overhead press" share same thresholds
        return trimmed;
    }

    private static InsightResult BuildOneRmInsight(InsightBand band, string movement, double ratio)
    {
        string ratioStr = ratio.ToString("0.00");
        return new InsightResult(
            band,
            AppLanguage.InsightBandLabel(band),
            AppLanguage.InsightOneRmSummary(band, ratioStr),
            AppLanguage.InsightOneRmSportContext(band, movement),
            AppLanguage.InsightOneRmGlobalContext(band));
    }

    // ── RSI Insight ───────────────────────────────────────────────────────────

    /// <summary>
    /// RSI thresholds derived from broad athlete-population data.
    /// No sport-specific branching in V1 — global athlete baseline.
    /// </summary>
    private static readonly (double Adequate, double Good, double Elite) RsiThresholds = (1.0, 2.0, 3.0);

    /// <summary>
    /// Resolves an RSI insight. Always has a global baseline — returns null only if RSI is non-positive.
    /// Sex / sport context used for sport context line only when provided.
    /// </summary>
    public static InsightResult? ResolveRsi(double rsi, string? sport = null)
    {
        if (rsi <= 0)
            return null;

        var band = ClassifyRatio(rsi, RsiThresholds.Adequate, RsiThresholds.Good, RsiThresholds.Elite);
        return new InsightResult(
            band,
            AppLanguage.InsightBandLabel(band),
            AppLanguage.InsightRsiSummary(band, rsi.ToString("0.00")),
            AppLanguage.InsightRsiSportContext(band, sport),
            AppLanguage.InsightRsiGlobalContext(band));
    }

    // ── FFMI Insight ──────────────────────────────────────────────────────────

    /// <summary>
    /// FFMI thresholds are sex-aware.
    /// Male and Female baselines from athlete population data (recreational to competitive).
    /// Returns null when sex is unknown/unsupported — no fake tier.
    /// </summary>
    private static readonly (double Adequate, double Good, double Elite) FfmiThresholdsMale   = (18.0, 20.0, 22.5);
    private static readonly (double Adequate, double Good, double Elite) FfmiThresholdsFemale = (15.0, 17.0, 19.0);

    private static readonly HashSet<string> MaleCodes   = new(StringComparer.OrdinalIgnoreCase) { "male", "m", "erkek" };
    private static readonly HashSet<string> FemaleCodes = new(StringComparer.OrdinalIgnoreCase) { "female", "f", "kadin", "kadın" };

    /// <summary>
    /// Resolves an FFMI insight.
    /// Returns null when sex is null, empty, or unrecognized — no fake tier.
    /// </summary>
    public static InsightResult? ResolveFfmi(double normalizedFfmi, string? sex)
    {
        if (normalizedFfmi <= 0)
            return null;

        (double Adequate, double Good, double Elite) thresholds;

        if (sex is null || string.IsNullOrWhiteSpace(sex))
            return null;

        if (MaleCodes.Contains(sex))
            thresholds = FfmiThresholdsMale;
        else if (FemaleCodes.Contains(sex))
            thresholds = FfmiThresholdsFemale;
        else
            return null; // Unrecognized sex string — no fake tier

        var band = ClassifyRatio(normalizedFfmi, thresholds.Adequate, thresholds.Good, thresholds.Elite);
        return new InsightResult(
            band,
            AppLanguage.InsightBandLabel(band),
            AppLanguage.InsightFfmiSummary(band, normalizedFfmi.ToString("0.0")),
            AppLanguage.InsightFfmiSportContext(band),
            AppLanguage.InsightFfmiGlobalContext(band, sex));
    }

    // ── Shared classification ────────────────────────────────────────────────

    internal static InsightBand ClassifyRatio(double value, double adequate, double good, double elite)
    {
        if (value >= elite)   return InsightBand.Elite;
        if (value >= good)    return InsightBand.Good;
        if (value >= adequate) return InsightBand.Adequate;
        return InsightBand.NeedsWork;
    }
}
