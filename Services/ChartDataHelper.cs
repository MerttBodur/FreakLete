using FreakLete.Models;

namespace FreakLete.Services;

/// <summary>
/// Testable logic for the exercise comparison chart.
/// No MAUI dependencies — plain .NET only.
/// </summary>
public static class ChartDataHelper
{
    public enum ChartRange { Days14, Month1, Months3, Months6 }

    public sealed record BucketConfig(int BucketCount, bool IsDaily, bool IsWeekly, bool IsMonthly);

    public static BucketConfig GetBucketConfig(ChartRange range) => range switch
    {
        ChartRange.Days14  => new BucketConfig(14, true,  false, false),
        ChartRange.Month1  => new BucketConfig(30, true,  false, false),
        ChartRange.Months3 => new BucketConfig(13, false, true,  false), // 13 weeks
        ChartRange.Months6 => new BucketConfig(6,  false, false, true),
        _                  => new BucketConfig(14, true,  false, false)
    };

    public static int RangeDays(ChartRange range) => range switch
    {
        ChartRange.Days14  => 14,
        ChartRange.Month1  => 30,
        ChartRange.Months3 => 91,
        ChartRange.Months6 => 182,
        _                  => 14
    };

    /// <summary>
    /// Determines the best value from a workout ExerciseEntryDto.
    /// Strength → Weight (Metric1Value if Weight is 0); Custom → Metric1Value.
    /// </summary>
    public static float BestValueFromWorkoutEntry(ExerciseEntryDto entry)
    {
        if (string.Equals(entry.TrackingMode, nameof(ExerciseTrackingMode.Strength), StringComparison.OrdinalIgnoreCase))
        {
            // Weight is stored in Metric1Value for workout entries
            if (entry.Metric1Value.HasValue && entry.Metric1Value.Value > 0)
                return (float)entry.Metric1Value.Value;
            return 0f;
        }
        // Custom / athletic
        if (entry.Metric1Value.HasValue && entry.Metric1Value.Value > 0)
            return (float)entry.Metric1Value.Value;
        return 0f;
    }

    /// <summary>
    /// Determines the best value from a PrEntryResponse.
    /// Strength → Weight; Custom → Metric1Value.
    /// </summary>
    public static float BestValueFromPrEntry(PrEntryResponse entry)
    {
        if (string.Equals(entry.TrackingMode, nameof(ExerciseTrackingMode.Strength), StringComparison.OrdinalIgnoreCase))
        {
            if (entry.Weight > 0) return entry.Weight;
            if (entry.Metric1Value.HasValue && entry.Metric1Value.Value > 0)
                return (float)entry.Metric1Value.Value;
            return 0f;
        }
        if (entry.Metric1Value.HasValue && entry.Metric1Value.Value > 0)
            return (float)entry.Metric1Value.Value;
        return 0f;
    }

    /// <summary>
    /// Determines the best value from an AthleticPerformanceResponse.
    /// Always uses Value.
    /// </summary>
    public static float BestValueFromAthleticEntry(AthleticPerformanceResponse entry)
    {
        return entry.Value > 0 ? (float)entry.Value : 0f;
    }

    /// <summary>
    /// Returns the display unit for a series.
    /// Strength → "kg"; Custom/athletic → Metric1Unit or Unit or "".
    /// </summary>
    public static string UnitForExercise(
        string exerciseName,
        IReadOnlyList<PrEntryResponse>? prEntries,
        IReadOnlyList<AthleticPerformanceResponse>? athleticEntries,
        IReadOnlyList<WorkoutResponse>? workouts)
    {
        // Check PR entries first
        var pr = prEntries?.FirstOrDefault(p =>
            string.Equals(p.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase));
        if (pr is not null)
        {
            if (string.Equals(pr.TrackingMode, nameof(ExerciseTrackingMode.Strength), StringComparison.OrdinalIgnoreCase))
                return "kg";
            if (!string.IsNullOrWhiteSpace(pr.Metric1Unit)) return pr.Metric1Unit;
        }

        // Check workout exercises
        var exEntry = workouts?
            .SelectMany(w => w.Exercises)
            .FirstOrDefault(e => string.Equals(e.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase));
        if (exEntry is not null)
        {
            if (string.Equals(exEntry.TrackingMode, nameof(ExerciseTrackingMode.Strength), StringComparison.OrdinalIgnoreCase))
                return "kg";
            if (!string.IsNullOrWhiteSpace(exEntry.Metric1Unit)) return exEntry.Metric1Unit;
        }

        // Check athletic
        var ath = athleticEntries?.FirstOrDefault(a =>
            string.Equals(a.MovementName, exerciseName, StringComparison.OrdinalIgnoreCase));
        if (ath is not null && !string.IsNullOrWhiteSpace(ath.Unit))
            return ath.Unit;

        return "kg"; // safe default
    }

    /// <summary>
    /// Builds bucketed (data, labels) from merged sources for one exercise.
    /// </summary>
    public static (List<float> Data, List<string> Labels) BuildBuckets(
        string exerciseName,
        ChartRange range,
        DateTime today,
        IReadOnlyList<WorkoutResponse>? workouts,
        IReadOnlyList<PrEntryResponse>? prEntries,
        IReadOnlyList<AthleticPerformanceResponse>? athleticEntries,
        string[] dayAbbr,
        string[] monthAbbr)
    {
        var cfg = GetBucketConfig(range);
        int totalDays = RangeDays(range);
        var cutoff = today.AddDays(-totalDays);

        var data = new List<float>();
        var labels = new List<string>();

        if (cfg.IsDaily)
        {
            // One bucket per day
            for (int i = cfg.BucketCount - 1; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                labels.Add(dayAbbr[(int)day.DayOfWeek]);
                float best = 0f;

                // Workout entries
                if (workouts is not null)
                    foreach (var w in workouts.Where(w => w.WorkoutDate.Date == day.Date))
                        foreach (var ex in w.Exercises.Where(e =>
                            string.Equals(e.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                            best = Math.Max(best, BestValueFromWorkoutEntry(ex));

                // PR entries
                if (prEntries is not null)
                    foreach (var pr in prEntries.Where(p =>
                        p.CreatedAt.Date == day.Date &&
                        string.Equals(p.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                        best = Math.Max(best, BestValueFromPrEntry(pr));

                // Athletic
                if (athleticEntries is not null)
                    foreach (var ath in athleticEntries.Where(a =>
                        a.RecordedAt.Date == day.Date &&
                        string.Equals(a.MovementName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                        best = Math.Max(best, BestValueFromAthleticEntry(ath));

                data.Add(best);
            }
        }
        else if (cfg.IsWeekly)
        {
            // Buckets: cfg.BucketCount weeks back from today
            for (int i = cfg.BucketCount - 1; i >= 0; i--)
            {
                var weekEnd = today.AddDays(-i * 7);
                var weekStart = weekEnd.AddDays(-6);
                labels.Add($"W{monthAbbr[(int)weekEnd.Month - 1]}");
                float best = 0f;

                if (workouts is not null)
                    foreach (var w in workouts.Where(w => w.WorkoutDate.Date >= weekStart.Date && w.WorkoutDate.Date <= weekEnd.Date))
                        foreach (var ex in w.Exercises.Where(e =>
                            string.Equals(e.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                            best = Math.Max(best, BestValueFromWorkoutEntry(ex));

                if (prEntries is not null)
                    foreach (var pr in prEntries.Where(p =>
                        p.CreatedAt.Date >= weekStart.Date && p.CreatedAt.Date <= weekEnd.Date &&
                        string.Equals(p.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                        best = Math.Max(best, BestValueFromPrEntry(pr));

                if (athleticEntries is not null)
                    foreach (var ath in athleticEntries.Where(a =>
                        a.RecordedAt.Date >= weekStart.Date && a.RecordedAt.Date <= weekEnd.Date &&
                        string.Equals(a.MovementName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                        best = Math.Max(best, BestValueFromAthleticEntry(ath));

                data.Add(best);
            }
        }
        else // Monthly
        {
            for (int i = cfg.BucketCount - 1; i >= 0; i--)
            {
                var bucketDate = today.AddMonths(-i);
                int yr = bucketDate.Year;
                int mo = bucketDate.Month;
                var monthStart = new DateTime(yr, mo, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                labels.Add(monthAbbr[mo - 1]);
                float best = 0f;

                if (workouts is not null)
                    foreach (var w in workouts.Where(w => w.WorkoutDate.Year == yr && w.WorkoutDate.Month == mo))
                        foreach (var ex in w.Exercises.Where(e =>
                            string.Equals(e.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                            best = Math.Max(best, BestValueFromWorkoutEntry(ex));

                if (prEntries is not null)
                    foreach (var pr in prEntries.Where(p =>
                        p.CreatedAt.Year == yr && p.CreatedAt.Month == mo &&
                        string.Equals(p.ExerciseName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                        best = Math.Max(best, BestValueFromPrEntry(pr));

                if (athleticEntries is not null)
                    foreach (var ath in athleticEntries.Where(a =>
                        a.RecordedAt.Year == yr && a.RecordedAt.Month == mo &&
                        string.Equals(a.MovementName, exerciseName, StringComparison.OrdinalIgnoreCase)))
                        best = Math.Max(best, BestValueFromAthleticEntry(ath));

                data.Add(best);
            }
        }

        return (data, labels);
    }
}
