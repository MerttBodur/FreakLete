using FreakLete.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

public class TrainingSummaryService
{
    private readonly AppDbContext _db;

    public TrainingSummaryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TrainingSummary> GetSummaryAsync(int userId, int recentDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-recentDays);

        var recentWorkouts = await _db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.WorkoutDate >= cutoff)
            .Include(w => w.ExerciseEntries)
            .OrderByDescending(w => w.WorkoutDate)
            .ToListAsync();

        var totalPrCount = await _db.PrEntries
            .CountAsync(p => p.UserId == userId);

        var recentPrCount = await _db.PrEntries
            .CountAsync(p => p.UserId == userId && p.CreatedAt >= cutoff);

        var recentPerformanceCount = await _db.AthleticPerformanceEntries
            .CountAsync(a => a.UserId == userId && a.RecordedAt >= cutoff);

        var goalCount = await _db.MovementGoals
            .CountAsync(g => g.UserId == userId);

        // Workout frequency
        var workoutDays = recentWorkouts.Select(w => w.WorkoutDate.Date).Distinct().Count();
        var weeksInPeriod = Math.Max(1, recentDays / 7.0);
        var weeklyFrequency = workoutDays / weeksInPeriod;

        // Exercise distribution
        var allExercises = recentWorkouts.SelectMany(w => w.ExerciseEntries).ToList();
        var categoryDistribution = allExercises
            .GroupBy(e => e.ExerciseCategory)
            .ToDictionary(g => g.Key, g => g.Count());

        // Volume per workout
        var avgExercisesPerWorkout = recentWorkouts.Count > 0
            ? allExercises.Count / (double)recentWorkouts.Count
            : 0;

        return new TrainingSummary
        {
            PeriodDays = recentDays,
            TotalWorkouts = recentWorkouts.Count,
            WorkoutDays = workoutDays,
            WeeklyFrequency = Math.Round(weeklyFrequency, 1),
            AverageExercisesPerWorkout = Math.Round(avgExercisesPerWorkout, 1),
            CategoryDistribution = categoryDistribution,
            TotalPrs = totalPrCount,
            RecentPrCount = recentPrCount,
            RecentAthleticPerformanceCount = recentPerformanceCount,
            ActiveGoalCount = goalCount
        };
    }
}

public class TrainingSummary
{
    public int PeriodDays { get; set; }
    public int TotalWorkouts { get; set; }
    public int WorkoutDays { get; set; }
    public double WeeklyFrequency { get; set; }
    public double AverageExercisesPerWorkout { get; set; }
    public Dictionary<string, int> CategoryDistribution { get; set; } = [];
    public int TotalPrs { get; set; }
    public int RecentPrCount { get; set; }
    public int RecentAthleticPerformanceCount { get; set; }
    public int ActiveGoalCount { get; set; }
}
