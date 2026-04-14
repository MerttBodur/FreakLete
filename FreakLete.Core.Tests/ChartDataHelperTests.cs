using FreakLete.Services;
using FreakLete.Models;

namespace FreakLete.Tests;

public class ChartDataHelperTests
{
    private static readonly string[] MonthAbbr = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    // ── BestValue helpers ────────────────────────────────────────────────────

    [Fact]
    public void BestValueFromWorkoutEntry_Strength_ReturnsMetric1Value()
    {
        var entry = new ExerciseEntryDto { TrackingMode = "Strength", Metric1Value = 100 };
        Assert.Equal(100f, ChartDataHelper.BestValueFromWorkoutEntry(entry));
    }

    [Fact]
    public void BestValueFromWorkoutEntry_Custom_ReturnsMetric1Value()
    {
        var entry = new ExerciseEntryDto { TrackingMode = "Custom", Metric1Value = 42.5 };
        Assert.Equal(42.5f, ChartDataHelper.BestValueFromWorkoutEntry(entry));
    }

    [Fact]
    public void BestValueFromWorkoutEntry_NoValue_ReturnsZero()
    {
        var entry = new ExerciseEntryDto { TrackingMode = "Strength", Metric1Value = null };
        Assert.Equal(0f, ChartDataHelper.BestValueFromWorkoutEntry(entry));
    }

    [Fact]
    public void BestValueFromPrEntry_Strength_PrefersWeight()
    {
        var entry = new PrEntryResponse { TrackingMode = "Strength", Weight = 120, Metric1Value = 50 };
        Assert.Equal(120f, ChartDataHelper.BestValueFromPrEntry(entry));
    }

    [Fact]
    public void BestValueFromPrEntry_Strength_FallsBackToMetric1WhenWeightZero()
    {
        var entry = new PrEntryResponse { TrackingMode = "Strength", Weight = 0, Metric1Value = 80 };
        Assert.Equal(80f, ChartDataHelper.BestValueFromPrEntry(entry));
    }

    [Fact]
    public void BestValueFromPrEntry_Custom_ReturnsMetric1Value()
    {
        var entry = new PrEntryResponse { TrackingMode = "Custom", Weight = 0, Metric1Value = 9.5 };
        Assert.Equal(9.5f, ChartDataHelper.BestValueFromPrEntry(entry));
    }

    [Fact]
    public void BestValueFromAthleticEntry_ReturnsValue()
    {
        var entry = new AthleticPerformanceResponse { Value = 7.3 };
        Assert.Equal(7.3f, ChartDataHelper.BestValueFromAthleticEntry(entry), precision: 4);
    }

    [Fact]
    public void BestValueFromAthleticEntry_ZeroValue_ReturnsZero()
    {
        var entry = new AthleticPerformanceResponse { Value = 0 };
        Assert.Equal(0f, ChartDataHelper.BestValueFromAthleticEntry(entry));
    }

    // ── Bucket config ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ChartDataHelper.ChartRange.Days14,  14, true,  false, false)]
    [InlineData(ChartDataHelper.ChartRange.Month1,  30, true,  false, false)]
    [InlineData(ChartDataHelper.ChartRange.Months3, 13, false, true,  false)]
    [InlineData(ChartDataHelper.ChartRange.Months6,  6, false, false, true)]
    public void GetBucketConfig_ReturnsExpected(ChartDataHelper.ChartRange range,
        int count, bool daily, bool weekly, bool monthly)
    {
        var cfg = ChartDataHelper.GetBucketConfig(range);
        Assert.Equal(count,   cfg.BucketCount);
        Assert.Equal(daily,   cfg.IsDaily);
        Assert.Equal(weekly,  cfg.IsWeekly);
        Assert.Equal(monthly, cfg.IsMonthly);
    }

    // ── UnitForExercise ───────────────────────────────────────────────────────

    [Fact]
    public void UnitForExercise_Strength_ReturnsKg()
    {
        var pr = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Bench Press", TrackingMode = "Strength" }
        };

        var unit = ChartDataHelper.UnitForExercise("Bench Press", pr, null, null);
        Assert.Equal("kg", unit);
    }

    [Fact]
    public void UnitForExercise_Athletic_ReturnsUnit()
    {
        var athletic = new List<AthleticPerformanceResponse>
        {
            new() { MovementName = "Sprint 40m", Unit = "s" }
        };

        var unit = ChartDataHelper.UnitForExercise("Sprint 40m", null, athletic, null);
        Assert.Equal("s", unit);
    }

    [Fact]
    public void UnitForExercise_CustomPr_ReturnsMetric1Unit()
    {
        var pr = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Broad Jump", TrackingMode = "Custom", Metric1Unit = "cm" }
        };

        var unit = ChartDataHelper.UnitForExercise("Broad Jump", pr, null, null);
        Assert.Equal("cm", unit);
    }

    [Fact]
    public void UnitForExercise_UnknownExercise_DefaultsToKg()
    {
        var unit = ChartDataHelper.UnitForExercise("Unknown Move", null, null, null);
        Assert.Equal("kg", unit);
    }

    // ── BuildSparsePoints — no zero-fill ─────────────────────────────────────

    [Fact]
    public void BuildSparsePoints_NoData_ReturnsEmpty()
    {
        var today = new DateTime(2026, 4, 10);
        var (values, labels) = ChartDataHelper.BuildSparsePoints(
            "Bench Press", ChartDataHelper.ChartRange.Days14, today,
            null, null, null, MonthAbbr);

        Assert.Empty(values);
        Assert.Empty(labels);
    }

    [Fact]
    public void BuildSparsePoints_TwoRealDays_ReturnsTwoPoints()
    {
        var today = new DateTime(2026, 4, 10);
        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Back Squat", TrackingMode = "Strength", Weight = 150, CreatedAt = new DateTime(2026, 4, 1) },
            new() { ExerciseName = "Back Squat", TrackingMode = "Strength", Weight = 160, CreatedAt = new DateTime(2026, 4, 8) }
        };

        var (values, labels) = ChartDataHelper.BuildSparsePoints(
            "Back Squat", ChartDataHelper.ChartRange.Days14, today,
            null, prEntries, null, MonthAbbr);

        // Only 2 real event days — no zero padding
        Assert.Equal(2, values.Count);
        Assert.Equal(2, labels.Count);
        Assert.Equal(150f, values[0]);
        Assert.Equal(160f, values[1]);
    }

    [Fact]
    public void BuildSparsePoints_NoZeroFillForNonLiftDays()
    {
        // Only 1 Feb and 15 Feb logged; 14-day range from April 10 should return only in-range events
        var today = new DateTime(2026, 4, 10);
        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Deadlift", TrackingMode = "Strength", Weight = 200, CreatedAt = new DateTime(2026, 4, 5) }
        };

        var (values, _) = ChartDataHelper.BuildSparsePoints(
            "Deadlift", ChartDataHelper.ChartRange.Days14, today,
            null, prEntries, null, MonthAbbr);

        // 1 real point — no zeros for the other 13 days
        Assert.Single(values);
        Assert.Equal(200f, values[0]);
        Assert.DoesNotContain(0f, values);
    }

    [Fact]
    public void BuildSparsePoints_EventsOutsideRange_Excluded()
    {
        var today = new DateTime(2026, 4, 10);
        var prEntries = new List<PrEntryResponse>
        {
            // 20 days ago — outside 14-day window
            new() { ExerciseName = "Bench Press", TrackingMode = "Strength", Weight = 100, CreatedAt = today.AddDays(-20) },
            // 5 days ago — inside
            new() { ExerciseName = "Bench Press", TrackingMode = "Strength", Weight = 110, CreatedAt = today.AddDays(-5) }
        };

        var (values, _) = ChartDataHelper.BuildSparsePoints(
            "Bench Press", ChartDataHelper.ChartRange.Days14, today,
            null, prEntries, null, MonthAbbr);

        Assert.Single(values);
        Assert.Equal(110f, values[0]);
    }

    [Fact]
    public void BuildSparsePoints_SameDayMultipleEntries_TakesBest()
    {
        var today = new DateTime(2026, 4, 10);
        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Squat", TrackingMode = "Strength", Weight = 120, CreatedAt = today },
            new() { ExerciseName = "Squat", TrackingMode = "Strength", Weight = 140, CreatedAt = today }
        };

        var (values, _) = ChartDataHelper.BuildSparsePoints(
            "Squat", ChartDataHelper.ChartRange.Days14, today,
            null, prEntries, null, MonthAbbr);

        Assert.Single(values);
        Assert.Equal(140f, values[0]);
    }

    [Fact]
    public void BuildSparsePoints_MergedSources_TakesBestAcrossSources()
    {
        var today = new DateTime(2026, 4, 10);
        var workouts = new List<WorkoutResponse>
        {
            new()
            {
                WorkoutDate = today,
                Exercises = [ new() { ExerciseName = "Bench Press", TrackingMode = "Strength", Metric1Value = 90 } ]
            }
        };
        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Bench Press", TrackingMode = "Strength", Weight = 110, CreatedAt = today }
        };

        var (values, _) = ChartDataHelper.BuildSparsePoints(
            "Bench Press", ChartDataHelper.ChartRange.Days14, today,
            workouts, prEntries, null, MonthAbbr);

        Assert.Single(values);
        Assert.Equal(110f, values[0]); // PR wins
    }

    [Fact]
    public void BuildSparsePoints_LabelsAndValuesAlwaysSameLength()
    {
        var today = new DateTime(2026, 4, 10);
        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Squat", TrackingMode = "Strength", Weight = 100, CreatedAt = today.AddDays(-10) },
            new() { ExerciseName = "Squat", TrackingMode = "Strength", Weight = 110, CreatedAt = today.AddDays(-5)  },
            new() { ExerciseName = "Squat", TrackingMode = "Strength", Weight = 115, CreatedAt = today              }
        };

        var (values, labels) = ChartDataHelper.BuildSparsePoints(
            "Squat", ChartDataHelper.ChartRange.Days14, today,
            null, prEntries, null, MonthAbbr);

        Assert.Equal(values.Count, labels.Count);
    }

    [Fact]
    public void BuildSparsePoints_Monthly_TwoMonths_ReturnsTwoPoints()
    {
        var today = new DateTime(2026, 4, 10);
        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Deadlift", TrackingMode = "Strength", Weight = 180, CreatedAt = new DateTime(2026, 2, 15) },
            new() { ExerciseName = "Deadlift", TrackingMode = "Strength", Weight = 190, CreatedAt = new DateTime(2026, 4, 1)  }
        };

        var (values, labels) = ChartDataHelper.BuildSparsePoints(
            "Deadlift", ChartDataHelper.ChartRange.Months6, today,
            null, prEntries, null, MonthAbbr);

        Assert.Equal(2, values.Count);
        Assert.Equal(180f, values[0]);
        Assert.Equal(190f, values[1]);
        // Labels should be month abbreviations
        Assert.Contains("Feb", labels[0]);
        Assert.Contains("Apr", labels[1]);
    }

    // ── ComputeDelta — first vs last real point ───────────────────────────────

    [Fact]
    public void ComputeDelta_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ChartDataHelper.ComputeDelta(null!));
        Assert.Null(ChartDataHelper.ComputeDelta([]));
        Assert.Null(ChartDataHelper.ComputeDelta([100f]));
    }

    [Fact]
    public void ComputeDelta_TwoPoints_ReturnsLastMinusFirst()
    {
        var delta = ChartDataHelper.ComputeDelta([150f, 160f]);
        Assert.Equal(10f, delta);
    }

    [Fact]
    public void ComputeDelta_Decrease_ReturnsNegative()
    {
        var delta = ChartDataHelper.ComputeDelta([160f, 150f]);
        Assert.Equal(-10f, delta);
    }

    [Fact]
    public void ComputeDelta_MultiplePoints_UsesFirstAndLast()
    {
        var delta = ChartDataHelper.ComputeDelta([100f, 120f, 110f, 130f]);
        Assert.Equal(30f, delta); // 130 - 100
    }

    [Fact]
    public void ComputeDelta_FirstOrLastIsZero_ReturnsNull()
    {
        Assert.Null(ChartDataHelper.ComputeDelta([0f, 100f]));
        Assert.Null(ChartDataHelper.ComputeDelta([100f, 0f]));
    }
}
