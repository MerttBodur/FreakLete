using FreakLete.Core.Tier;

namespace FreakLete.Core.Tests;

public class TierResolverTests
{
    private static readonly double[] BenchMale = [0.5, 1.0, 1.25, 1.5, 1.75];

    [Theory]
    [InlineData(0.49, TierLevel.NeedImprovement)]
    [InlineData(0.5,  TierLevel.Beginner)]
    [InlineData(0.99, TierLevel.Beginner)]
    [InlineData(1.0,  TierLevel.Intermediate)]
    [InlineData(1.24, TierLevel.Intermediate)]
    [InlineData(1.25, TierLevel.Advanced)]
    [InlineData(1.49, TierLevel.Advanced)]
    [InlineData(1.5,  TierLevel.Elite)]
    [InlineData(1.74, TierLevel.Elite)]
    [InlineData(1.75, TierLevel.Freak)]
    [InlineData(3.0,  TierLevel.Freak)]
    public void Resolve_ReturnsCorrectTier(double value, TierLevel expected)
    {
        Assert.Equal(expected, TierResolver.Resolve(value, BenchMale));
    }

    // Sprint 40yd (seconds) — lower is better. Descending thresholds:
    // Freak boundary 4.4, Elite 4.6, Advanced 4.9, Intermediate 5.3, Beginner 5.8
    private static readonly double[] SprintMale = [5.8, 5.3, 4.9, 4.6, 4.4];

    [Theory]
    [InlineData(6.0, TierLevel.NeedImprovement)]
    [InlineData(5.8, TierLevel.Beginner)]
    [InlineData(5.7, TierLevel.Beginner)]
    [InlineData(5.3, TierLevel.Intermediate)]
    [InlineData(5.2, TierLevel.Intermediate)]
    [InlineData(4.9, TierLevel.Advanced)]
    [InlineData(4.8, TierLevel.Advanced)]
    [InlineData(4.6, TierLevel.Elite)]
    [InlineData(4.5, TierLevel.Elite)]
    [InlineData(4.4, TierLevel.Freak)]
    [InlineData(4.3, TierLevel.Freak)]
    public void ResolveInverse_ReturnsCorrectTier(double value, TierLevel expected)
    {
        Assert.Equal(expected, TierResolver.ResolveInverse(value, SprintMale));
    }
}
