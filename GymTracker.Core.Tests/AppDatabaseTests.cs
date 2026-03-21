using GymTracker.Data;
using GymTracker.Models;

namespace GymTracker.Core.Tests;

public class AppDatabaseTests
{
	private static string CreateDatabasePath()
	{
		return Path.Combine(Path.GetTempPath(), $"gymtracker-tests-{Guid.NewGuid():N}.db3");
	}

	[Fact]
	public async Task SaveMovementGoalAsync_SameMovementAndCategory_UpdatesExistingGoal()
	{
		string databasePath = CreateDatabasePath();
		AppDatabase? database = null;

		try
		{
			database = new AppDatabase(databasePath);

			await database.SaveMovementGoalAsync(new MovementGoal
			{
				UserId = 7,
				MovementName = "Power Clean",
				MovementCategory = "Olympic Lifts",
				GoalMetricLabel = "Load",
				TargetValue = 90,
				Unit = "kg"
			});

			await database.SaveMovementGoalAsync(new MovementGoal
			{
				UserId = 7,
				MovementName = "Power Clean",
				MovementCategory = "Olympic Lifts",
				GoalMetricLabel = "Load",
				TargetValue = 100,
				Unit = "kg"
			});

			List<MovementGoal> goals = await database.GetMovementGoalsByUserAsync(7);

			Assert.Single(goals);
			Assert.Equal(100, goals[0].TargetValue);
			Assert.Equal("kg", goals[0].Unit);
		}
		finally
		{
			if (database is not null)
			{
				await database.CloseAsync();
			}

			File.Delete(databasePath);
		}
	}

	[Fact]
	public async Task SaveMovementGoalAsync_BlankExistingCategory_IsTreatedAsMatch()
	{
		string databasePath = CreateDatabasePath();
		AppDatabase? database = null;

		try
		{
			database = new AppDatabase(databasePath);

			await database.SaveMovementGoalAsync(new MovementGoal
			{
				UserId = 3,
				MovementName = "Power Clean",
				MovementCategory = string.Empty,
				GoalMetricLabel = "Load",
				TargetValue = 80,
				Unit = "kg"
			});

			await database.SaveMovementGoalAsync(new MovementGoal
			{
				UserId = 3,
				MovementName = "Power Clean",
				MovementCategory = "Olympic Lifts",
				GoalMetricLabel = "Load",
				TargetValue = 95,
				Unit = "kg"
			});

			List<MovementGoal> goals = await database.GetMovementGoalsByUserAsync(3);

			Assert.Single(goals);
			Assert.Equal("Olympic Lifts", goals[0].MovementCategory);
			Assert.Equal(95, goals[0].TargetValue);
		}
		finally
		{
			if (database is not null)
			{
				await database.CloseAsync();
			}

			File.Delete(databasePath);
		}
	}

	[Fact]
	public async Task GetWorkoutsByUserAndDateAsync_ReturnsOnlyMatchingUserAndDate()
	{
		string databasePath = CreateDatabasePath();
		AppDatabase? database = null;

		try
		{
			database = new AppDatabase(databasePath);
			DateTime targetDate = new(2026, 3, 21);

			await database.SaveWorkoutAsync(
				new Workout
				{
					UserId = 1,
					WorkoutName = "Lower A",
					WorkoutDate = targetDate
				},
				[
					new ExerciseEntry
					{
						ExerciseName = "Back Squat",
						ExerciseCategory = "Squat Variation",
						TrackingMode = nameof(ExerciseTrackingMode.Strength),
						Sets = 3,
						Reps = 5
					}
				]);

			await database.SaveWorkoutAsync(
				new Workout
				{
					UserId = 1,
					WorkoutName = "Upper B",
					WorkoutDate = targetDate.AddDays(1)
				},
				[
					new ExerciseEntry
					{
						ExerciseName = "Bench Press",
						ExerciseCategory = "Push",
						TrackingMode = nameof(ExerciseTrackingMode.Strength),
						Sets = 3,
						Reps = 5
					}
				]);

			await database.SaveWorkoutAsync(
				new Workout
				{
					UserId = 2,
					WorkoutName = "Other User Workout",
					WorkoutDate = targetDate
				},
				[
					new ExerciseEntry
					{
						ExerciseName = "Deadlift",
						ExerciseCategory = "Deadlift Variation",
						TrackingMode = nameof(ExerciseTrackingMode.Strength),
						Sets = 2,
						Reps = 3
					}
				]);

			List<Workout> workouts = await database.GetWorkoutsByUserAndDateAsync(1, targetDate);

			Assert.Single(workouts);
			Assert.Equal("Lower A", workouts[0].WorkoutName);
		}
		finally
		{
			if (database is not null)
			{
				await database.CloseAsync();
			}

			File.Delete(databasePath);
		}
	}

	[Fact]
	public async Task DeleteWorkoutAsync_RemovesWorkoutAndItsExercises()
	{
		string databasePath = CreateDatabasePath();
		AppDatabase? database = null;

		try
		{
			database = new AppDatabase(databasePath);

			int workoutId = await database.SaveWorkoutAsync(
				new Workout
				{
					UserId = 5,
					WorkoutName = "Sprint Session",
					WorkoutDate = new DateTime(2026, 3, 21)
				},
				[
					new ExerciseEntry
					{
						ExerciseName = "0-20m Sprint",
						ExerciseCategory = "Sprint",
						TrackingMode = nameof(ExerciseTrackingMode.Custom),
						Metric1Value = 20,
						Metric1Unit = "m",
						Metric2Value = 3.12,
						Metric2Unit = "s"
					}
				]);

			int deleted = await database.DeleteWorkoutAsync(workoutId);
			Workout? workout = await database.GetWorkoutByIdAsync(workoutId);
			List<ExerciseEntry> exercises = await database.GetExercisesByWorkoutIdAsync(workoutId);

			Assert.Equal(1, deleted);
			Assert.Null(workout);
			Assert.Empty(exercises);
		}
		finally
		{
			if (database is not null)
			{
				await database.CloseAsync();
			}

			File.Delete(databasePath);
		}
	}
}
