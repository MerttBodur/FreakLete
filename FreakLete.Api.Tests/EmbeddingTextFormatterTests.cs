using FreakLete.Api.Entities;
using FreakLete.Api.Services.Embeddings;

namespace FreakLete.Api.Tests;

public class EmbeddingTextFormatterTests
{
    [Fact]
    public void FormatWorkout_IncludesSportPositionAndExercises()
    {
        var user = new User
        {
            Id = 1,
            SportName = "Football",
            Position = "Wide Receiver",
            PrimaryTrainingGoal = "Strength"
        };

        var workout = new Workout
        {
            Id = 10,
            UserId = 1,
            WorkoutName = "Lower",
            WorkoutDate = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            ExerciseEntries =
            [
                new ExerciseEntry
                {
                    ExerciseName = "Squat",
                    SetsCount = 4,
                    Reps = 5,
                    Metric1Value = 120,
                    Metric1Unit = "kg"
                }
            ]
        };

        var text = EmbeddingTextFormatter.FormatWorkout(workout, user);

        Assert.Contains("Sport: Football", text);
        Assert.Contains("Position: Wide Receiver", text);
        Assert.Contains("Squat", text);
        Assert.Contains("4x5", text);
        Assert.Contains("120", text);
        Assert.Contains("Date: 2026-04-20", text);
    }

    [Fact]
    public void FormatUserSnapshot_IncludesProfileGoalsAndProgram()
    {
        var user = new User
        {
            Id = 2,
            SportName = "Football",
            Position = "Wide Receiver",
            GymExperienceLevel = "Intermediate",
            PrimaryTrainingGoal = "Athletic Performance",
            SecondaryTrainingGoal = "Muscle Gain",
            TrainingDaysPerWeek = 4,
            PreferredSessionDurationMinutes = 75,
            AvailableEquipment = "Commercial Gym",
            PhysicalLimitations = "",
            InjuryHistory = "Left knee sprain (2024)"
        };

        var prs = new List<PrEntry>
        {
            new() { ExerciseName = "Squat", Weight = 140, Reps = 1 },
            new() { ExerciseName = "Bench Press", Weight = 100, Reps = 1 }
        };

        var program = new TrainingProgram
        {
            Name = "4-Day Athletic Hypertrophy",
            Status = "active"
        };

        var text = EmbeddingTextFormatter.FormatUserSnapshot(user, prs, program);

        Assert.Contains("Sport: Football", text);
        Assert.Contains("Experience: Intermediate", text);
        Assert.Contains("Primary Goal: Athletic Performance", text);
        Assert.Contains("Training Days: 4/week", text);
        Assert.Contains("Equipment: Commercial Gym", text);
        Assert.Contains("Squat 140kg", text);
        Assert.Contains("Active Program: 4-Day Athletic Hypertrophy", text);
        Assert.Contains("Left knee sprain", text);
    }
}
