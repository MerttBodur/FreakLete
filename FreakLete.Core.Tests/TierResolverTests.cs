using System.Collections.Generic;
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

    [Fact]
    public void GetThresholds_Tier1_ReturnsOwnArrayForMale()
    {
        var deadlift = new ExerciseTierConfig(
            "conventionaldeadlift",
            "StrengthRatio",
            ThresholdsMale: [1.0, 1.5, 2.0, 2.5, 3.0],
            ThresholdsFemale: [0.7, 1.0, 1.4, 1.8, 2.2],
            TierParentId: null,
            TierScale: null);
        var all = new Dictionary<string, ExerciseTierConfig> { [deadlift.CatalogId] = deadlift };

        var result = TierResolver.GetThresholds(deadlift, "Male", all);

        Assert.Equal([1.0, 1.5, 2.0, 2.5, 3.0], result);
    }

    [Fact]
    public void GetThresholds_Tier1_ReturnsFemaleWhenSexIsFemale()
    {
        var deadlift = new ExerciseTierConfig(
            "conventionaldeadlift", "StrengthRatio",
            [1.0, 1.5, 2.0, 2.5, 3.0],
            [0.7, 1.0, 1.4, 1.8, 2.2],
            null, null);
        var all = new Dictionary<string, ExerciseTierConfig> { [deadlift.CatalogId] = deadlift };

        var result = TierResolver.GetThresholds(deadlift, "Female", all);

        Assert.Equal([0.7, 1.0, 1.4, 1.8, 2.2], result);
    }

    [Fact]
    public void GetThresholds_Tier2_ScalesParentArray()
    {
        var deadlift = new ExerciseTierConfig(
            "conventionaldeadlift", "StrengthRatio",
            [1.0, 1.5, 2.0, 2.5, 3.0],
            [0.7, 1.0, 1.4, 1.8, 2.2],
            null, null);
        var rackPull = new ExerciseTierConfig(
            "rackpull", "StrengthRatio",
            [], [],
            TierParentId: "conventionaldeadlift",
            TierScale: 1.1);
        var all = new Dictionary<string, ExerciseTierConfig>
        {
            [deadlift.CatalogId] = deadlift,
            [rackPull.CatalogId] = rackPull
        };

        var result = TierResolver.GetThresholds(rackPull, "Male", all);

        double[] expected = [1.1, 1.65, 2.2, 2.75, 3.3];
        Assert.Equal(expected.Length, result.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.InRange(result[i], expected[i] - 1e-9, expected[i] + 1e-9);
    }

    [Fact]
    public void GetThresholds_Tier2_MissingParent_ReturnsEmptyArray()
    {
        var orphan = new ExerciseTierConfig(
            "orphan", "StrengthRatio", [], [],
            TierParentId: "missingparent", TierScale: 1.0);
        var all = new Dictionary<string, ExerciseTierConfig> { [orphan.CatalogId] = orphan };

        var result = TierResolver.GetThresholds(orphan, "Male", all);

        Assert.Empty(result);
    }

    [Fact]
    public void GetThresholds_SexEmpty_DefaultsToMale()
    {
        var bench = new ExerciseTierConfig(
            "benchpress", "StrengthRatio",
            [0.5, 1.0, 1.25, 1.5, 1.75],
            [0.35, 0.7, 0.9, 1.1, 1.35],
            null, null);
        var all = new Dictionary<string, ExerciseTierConfig> { [bench.CatalogId] = bench };

        var result = TierResolver.GetThresholds(bench, "", all);

        Assert.Equal([0.5, 1.0, 1.25, 1.5, 1.75], result);
    }
}
