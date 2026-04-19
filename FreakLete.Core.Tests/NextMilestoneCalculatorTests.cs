using FreakLete.Core.Tier;

namespace FreakLete.Core.Tests;

public class NextMilestoneCalculatorTests
{
    private static readonly double[] Bench = [0.5, 1.0, 1.25, 1.5, 1.75];

    [Fact]
    public void StrengthRatio_MidBand_ReturnsNextTierAndDelta()
    {
        // Ratio 1.10 on [0.5,1.0,1.25,1.5,1.75] → Intermediate (>=1.0, <1.25).
        // Next tier ratio = 1.25. Bodyweight 80 → next kg = 100.
        // RawValue 88 → delta = 12. Progress in [1.0, 1.25] band: (1.10-1.0)/0.25 = 40%.
        var r = NextMilestoneCalculator.Compute(
            tierType: "StrengthRatio",
            currentLevel: TierLevel.Intermediate,
            thresholds: Bench,
            rawValue: 88,
            ratio: 1.10,
            bodyWeight: 80);

        Assert.Equal(TierLevel.Advanced.ToString(), r.NextLevel);
        Assert.Equal(100, r.NextTargetRaw);
        Assert.Equal(12, r.NextDelta);
        Assert.InRange(r.ProgressPercent, 39.9, 40.1);
    }

    private static readonly double[] Sprint = [5.8, 5.3, 4.9, 4.6, 4.4];

    [Fact]
    public void AthleticInverse_MidBand_ReturnsNextAndPositiveDelta()
    {
        // time 5.5, thresholds [5.8,5.3,4.9,4.6,4.4] → Beginner (>5.3).
        // Next boundary thresholds[1] = 5.3. Delta = 5.5 - 5.3 = 0.2.
        // Progress in [lo=5.8, hi=5.3]: (5.8 - 5.5)/(5.8 - 5.3) = 60%.
        var r = NextMilestoneCalculator.Compute(
            tierType: "AthleticInverse",
            currentLevel: TierLevel.Beginner,
            thresholds: Sprint,
            rawValue: 5.5,
            ratio: null,
            bodyWeight: null);

        Assert.Equal(TierLevel.Intermediate.ToString(), r.NextLevel);
        Assert.Equal(5.3, r.NextTargetRaw);
        Assert.InRange(r.NextDelta!.Value, 0.19, 0.21);
        Assert.InRange(r.ProgressPercent, 59.9, 60.1);
    }

    [Fact]
    public void MaxTier_ReturnsNullsAndFullProgress()
    {
        var r = NextMilestoneCalculator.Compute(
            tierType: "StrengthRatio",
            currentLevel: TierLevel.Freak,
            thresholds: Bench,
            rawValue: 160,
            ratio: 2.0,
            bodyWeight: 80);

        Assert.Null(r.NextLevel);
        Assert.Null(r.NextTargetRaw);
        Assert.Null(r.NextDelta);
        Assert.Equal(100, r.ProgressPercent);
    }

    private static readonly double[] Jump = [30, 45, 55, 65, 75];

    [Fact]
    public void AthleticAbsolute_MidBand_ReturnsPositiveDelta()
    {
        // jump 48cm → Intermediate (>=45, <55). Next boundary 55. Delta = 55 - 48 = 7.
        // Progress in [45, 55]: (48-45)/10 = 30%.
        var r = NextMilestoneCalculator.Compute(
            tierType: "AthleticAbsolute",
            currentLevel: TierLevel.Intermediate,
            thresholds: Jump,
            rawValue: 48,
            ratio: null,
            bodyWeight: null);

        Assert.Equal(TierLevel.Advanced.ToString(), r.NextLevel);
        Assert.Equal(55, r.NextTargetRaw);
        Assert.Equal(7, r.NextDelta);
        Assert.InRange(r.ProgressPercent, 29.9, 30.1);
    }
}
