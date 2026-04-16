using System.Linq;

namespace FreakLete.Core.Tier;

public static class TierResolver
{
    public static TierLevel Resolve(double value, double[] thresholds)
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value < thresholds[i]) return (TierLevel)i;
        }
        return TierLevel.Freak;
    }

    // Thresholds are given worst → best (descending metric values).
    // value <= thresholds[i] means user is "better than i-th boundary".
    public static TierLevel ResolveInverse(double value, double[] thresholds)
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value > thresholds[i]) return (TierLevel)i;
        }
        return TierLevel.Freak;
    }

    public static double[] GetThresholds(
        ExerciseTierConfig config,
        string sex,
        IReadOnlyDictionary<string, ExerciseTierConfig> allConfigs)
    {
        if (config.TierParentId is not null && config.TierScale.HasValue)
        {
            if (!allConfigs.TryGetValue(config.TierParentId, out var parent))
            {
                return [];
            }
            var parentArr = string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase)
                ? parent.ThresholdsFemale
                : parent.ThresholdsMale;
            double scale = config.TierScale.Value;
            return parentArr.Select(t => t * scale).ToArray();
        }

        return string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase)
            ? config.ThresholdsFemale
            : config.ThresholdsMale;
    }
}
