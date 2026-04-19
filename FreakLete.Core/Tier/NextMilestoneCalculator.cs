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
