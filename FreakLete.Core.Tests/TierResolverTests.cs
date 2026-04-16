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
}
