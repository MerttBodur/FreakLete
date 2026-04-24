using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class SetDetailsAggregatorTests
{
    [Fact]
    public void Aggregate_ThreeIdenticalSets_ReturnsCountRepsAndWeight()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 10, Weight = 100 },
            new() { SetNumber = 2, Reps = 10, Weight = 100 },
            new() { SetNumber = 3, Reps = 10, Weight = 100 }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Equal(3, result.Sets);
        Assert.Equal(10, result.Reps);
        Assert.Equal(100, result.MaxWeight);
    }

    [Fact]
    public void Aggregate_VaryingWeight_ReturnsMaxWeight()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 10, Weight = 60 },
            new() { SetNumber = 2, Reps = 10, Weight = 80 },
            new() { SetNumber = 3, Reps = 10, Weight = 100 }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Equal(100, result.MaxWeight);
    }

    [Fact]
    public void Aggregate_VaryingReps_ReturnsLastSetReps()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 12, Weight = 100 },
            new() { SetNumber = 2, Reps = 10, Weight = 100 },
            new() { SetNumber = 3, Reps = 8, Weight = 100 }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Equal(8, result.Reps);
    }

    [Fact]
    public void Aggregate_AllWeightsNull_ReturnsNullMaxWeight()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 10, Weight = null },
            new() { SetNumber = 2, Reps = 10, Weight = null }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Null(result.MaxWeight);
        Assert.Equal(2, result.Sets);
        Assert.Equal(10, result.Reps);
    }

    [Fact]
    public void Aggregate_EmptyList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SetDetailsAggregator.Aggregate(new List<SetDetail>()));
    }
}
