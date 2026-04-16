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
}
