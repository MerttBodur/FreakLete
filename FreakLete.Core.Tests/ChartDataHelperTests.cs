using FreakLete.Services;
using FreakLete.Models;

namespace FreakLete.Tests;

public class ChartDataHelperTests
{
    private static readonly string[] DayAbbr   = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
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

    // ── Daily buckets — workout source ───────────────────────────────────────

    [Fact]
    public void BuildBuckets_Daily_WorkoutStrength_PicksMaxPerDay()
    {
        var today = new DateTime(2026, 4, 10);

        var workouts = new List<WorkoutResponse>
        {
            new()
            {
                WorkoutDate = today,
                Exercises =
                [
                    new() { ExerciseName = "Bench Press", TrackingMode = "Strength", Metric1Value = 80 },
                    new() { ExerciseName = "Bench Press", TrackingMode = "Strength", Metric1Value = 100 }
                ]
            }
        };

        var (data, labels) = ChartDataHelper.BuildBuckets(
            "Bench Press", ChartDataHelper.ChartRange.Days14, today,
            workouts, null, null, DayAbbr, MonthAbbr);

        Assert.Equal(14, data.Count);
        Assert.Equal(100f, data[^1]); // last bucket = today
    }

    [Fact]
    public void BuildBuckets_Daily_WorkoutOnly_OtherDaysZero()
    {
        var today = new DateTime(2026, 4, 10);

        var workouts = new List<WorkoutResponse>
        {
            new()
            {
                WorkoutDate = today.AddDays(-3),
                Exercises = [ new() { ExerciseName = "Squat", TrackingMode = "Strength", Metric1Value = 60 } ]
            }
        };

        var (data, _) = ChartDataHelper.BuildBuckets(
            "Squat", ChartDataHelper.ChartRange.Days14, today,
            workouts, null, null, DayAbbr, MonthAbbr);

        Assert.Equal(14, data.Count);
        Assert.Equal(60f, data[^4]); // 3 days ago
        Assert.Equal(0f,  data[^1]); // today
    }

    // ── PR source fills chart when no workout data ────────────────────────────

    [Fact]
    public void BuildBuckets_Daily_PrEntriesStrength_FillsWhenWorkoutMissing()
    {
        var today = new DateTime(2026, 4, 10);

        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Deadlift", TrackingMode = "Strength", Weight = 180, CreatedAt = today }
        };

        var (data, _) = ChartDataHelper.BuildBuckets(
            "Deadlift", ChartDataHelper.ChartRange.Days14, today,
            null, prEntries, null, DayAbbr, MonthAbbr);

        Assert.Equal(180f, data[^1]); // today from PR
    }

    // ── Athletic source fills chart ──────────────────────────────────────────

    [Fact]
    public void BuildBuckets_Daily_AthleticEntries_FillsWhenNoWorkoutOrPr()
    {
        var today = new DateTime(2026, 4, 10);

        var athletic = new List<AthleticPerformanceResponse>
        {
            new() { MovementName = "Sprint 40m", Value = 4.8, RecordedAt = today.AddDays(-1) }
        };

        var (data, _) = ChartDataHelper.BuildBuckets(
            "Sprint 40m", ChartDataHelper.ChartRange.Days14, today,
            null, null, athletic, DayAbbr, MonthAbbr);

        Assert.Equal(4.8f, data[^2], precision: 4); // yesterday
        Assert.Equal(0f,   data[^1]);               // today
    }

    // ── Merged: best from all sources per bucket ─────────────────────────────

    [Fact]
    public void BuildBuckets_Daily_MergedSources_TakesBestValue()
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

        var (data, _) = ChartDataHelper.BuildBuckets(
            "Bench Press", ChartDataHelper.ChartRange.Days14, today,
            workouts, prEntries, null, DayAbbr, MonthAbbr);

        Assert.Equal(110f, data[^1]); // PR wins over workout
    }

    // ── Weekly buckets ────────────────────────────────────────────────────────

    [Fact]
    public void BuildBuckets_Weekly_Months3_HasCorrectBucketCount()
    {
        var today = new DateTime(2026, 4, 10);

        var (data, labels) = ChartDataHelper.BuildBuckets(
            "Squat", ChartDataHelper.ChartRange.Months3, today,
            null, null, null, DayAbbr, MonthAbbr);

        Assert.Equal(13, data.Count);
        Assert.Equal(13, labels.Count);
    }

    [Fact]
    public void BuildBuckets_Weekly_PrInCurrentWeek_AppearsInLastBucket()
    {
        var today = new DateTime(2026, 4, 10); // Friday

        var prEntries = new List<PrEntryResponse>
        {
            new() { ExerciseName = "Squat", TrackingMode = "Strength", Weight = 140, CreatedAt = today.AddDays(-1) } // Thursday
        };

        var (data, _) = ChartDataHelper.BuildBuckets(
            "Squat", ChartDataHelper.ChartRange.Months3, today,
            null, prEntries, null, DayAbbr, MonthAbbr);

        Assert.Equal(140f, data[^1]); // last bucket covers this week
    }

    // ── Monthly buckets ───────────────────────────────────────────────────────

    [Fact]
    public void BuildBuckets_Monthly_Months6_HasCorrectBucketCount()
    {
        var today = new DateTime(2026, 4, 10);

        var (data, labels) = ChartDataHelper.BuildBuckets(
            "Bench Press", ChartDataHelper.ChartRange.Months6, today,
            null, null, null, DayAbbr, MonthAbbr);

        Assert.Equal(6, data.Count);
        Assert.Equal(6, labels.Count);
    }

    [Fact]
    public void BuildBuckets_Monthly_AthleticInCurrentMonth_AppearsInLastBucket()
    {
        var today = new DateTime(2026, 4, 10);

        var athletic = new List<AthleticPerformanceResponse>
        {
            new() { MovementName = "Box Jump", Value = 65, RecordedAt = new DateTime(2026, 4, 3) }
        };

        var (data, _) = ChartDataHelper.BuildBuckets(
            "Box Jump", ChartDataHelper.ChartRange.Months6, today,
            null, null, athletic, DayAbbr, MonthAbbr);

        Assert.Equal(65f, data[^1]);
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

    // ── Label count matches data count ────────────────────────────────────────

    [Theory]
    [InlineData(ChartDataHelper.ChartRange.Days14,  14)]
    [InlineData(ChartDataHelper.ChartRange.Month1,  30)]
    [InlineData(ChartDataHelper.ChartRange.Months3, 13)]
    [InlineData(ChartDataHelper.ChartRange.Months6,  6)]
    public void BuildBuckets_LabelCountMatchesDataCount(ChartDataHelper.ChartRange range, int expectedCount)
    {
        var today = new DateTime(2026, 4, 10);
        var (data, labels) = ChartDataHelper.BuildBuckets(
            "Any", range, today, null, null, null, DayAbbr, MonthAbbr);

        Assert.Equal(expectedCount, data.Count);
        Assert.Equal(expectedCount, labels.Count);
    }
}
