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
            .Where(w => w.UserId == userId && w.WorkoutDate >= cutoff)
            .Include(w => w.ExerciseEntries)
            .OrderByDescending(w => w.WorkoutDate)
            .ToListAsync();

        var allPrs = await _db.PrEntries
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var recentPerformances = await _db.AthleticPerformanceEntries
            .Where(a => a.UserId == userId && a.RecordedAt >= cutoff)
            .OrderByDescending(a => a.RecordedAt)
            .ToListAsync();

        var goals = await _db.MovementGoals
            .Where(g => g.UserId == userId)
            .ToListAsync();

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

        // Recent PRs
        var recentPrs = allPrs.Where(p => p.CreatedAt >= cutoff).ToList();

        return new TrainingSummary
        {
            PeriodDays = recentDays,
            TotalWorkouts = recentWorkouts.Count,
            WorkoutDays = workoutDays,
            WeeklyFrequency = Math.Round(weeklyFrequency, 1),
            AverageExercisesPerWorkout = Math.Round(avgExercisesPerWorkout, 1),
            CategoryDistribution = categoryDistribution,
            TotalPrs = allPrs.Count,
            RecentPrCount = recentPrs.Count,
            RecentAthleticPerformanceCount = recentPerformances.Count,
            ActiveGoalCount = goals.Count
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
