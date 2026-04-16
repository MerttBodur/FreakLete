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
}
