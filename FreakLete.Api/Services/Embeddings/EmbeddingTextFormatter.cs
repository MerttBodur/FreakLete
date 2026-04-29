using System.Globalization;
using System.Text;
using FreakLete.Api.Entities;

namespace FreakLete.Api.Services.Embeddings;

public static class EmbeddingTextFormatter
{
    public static string FormatWorkout(Workout workout, User user)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(user.SportName, "Unknown"));
        sb.Append(" | Position: ").Append(NonEmpty(user.Position, "Unknown"));
        sb.Append(" | Focus: ").Append(NonEmpty(user.PrimaryTrainingGoal, "General"));
        sb.AppendLine();

        sb.Append("Exercises: ");
        var exercises = workout.ExerciseEntries
            .Select(FormatExercise)
            .Where(static x => !string.IsNullOrWhiteSpace(x));
        sb.AppendLine(string.Join(", ", exercises));

        if (!string.IsNullOrWhiteSpace(workout.WorkoutName))
        {
            sb.Append("Session: ").AppendLine(workout.WorkoutName);
        }

        sb.Append("Date: ")
            .Append(workout.WorkoutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    public static string FormatUserSnapshot(User user, IEnumerable<PrEntry> prs, TrainingProgram? activeProgram)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(user.SportName, "Unknown"));
        sb.Append(" | Position: ").Append(NonEmpty(user.Position, "Unknown"));
        sb.Append(" | Experience: ").Append(NonEmpty(user.GymExperienceLevel, "Unknown"));
        sb.AppendLine();

        sb.Append("Primary Goal: ").Append(NonEmpty(user.PrimaryTrainingGoal, "General"));
        sb.Append(" | Secondary Goal: ").Append(NonEmpty(user.SecondaryTrainingGoal, "None"));
        sb.AppendLine();

        sb.Append("Training Days: ")
            .Append(user.TrainingDaysPerWeek?.ToString(CultureInfo.InvariantCulture) ?? "?");
        sb.Append("/week | Session Duration: ")
            .Append(user.PreferredSessionDurationMinutes?.ToString(CultureInfo.InvariantCulture) ?? "?");
        sb.AppendLine(" min");

        sb.Append("Equipment: ").AppendLine(NonEmpty(user.AvailableEquipment, "Not specified"));

        sb.Append("Physical Limitations: ").Append(NonEmpty(user.PhysicalLimitations, "None"));
        sb.Append(" | Injury History: ").AppendLine(NonEmpty(user.InjuryHistory, "None"));

        var topPrs = prs
            .GroupBy(x => x.ExerciseName)
            .Select(g => g.OrderByDescending(x => x.Weight).First())
            .Take(5)
            .Select(x => $"{x.ExerciseName} {x.Weight.ToString(CultureInfo.InvariantCulture)}kg")
            .ToList();

        if (topPrs.Count > 0)
        {
            sb.Append("Top PRs: ").AppendLine(string.Join(", ", topPrs));
        }

        if (activeProgram is not null)
        {
            sb.Append("Active Program: ").AppendLine(activeProgram.Name);
        }

        return sb.ToString();
    }

    private static string FormatExercise(ExerciseEntry exercise)
    {
        var sets = $"{exercise.SetsCount.ToString(CultureInfo.InvariantCulture)}x{exercise.Reps.ToString(CultureInfo.InvariantCulture)}";
        var metric = exercise.Metric1Value.HasValue
            ? $" @{exercise.Metric1Value.Value.ToString("0.#", CultureInfo.InvariantCulture)}{exercise.Metric1Unit ?? string.Empty}"
            : string.Empty;

        return $"{exercise.ExerciseName} {sets}{metric}".Trim();
    }

    private static string NonEmpty(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
