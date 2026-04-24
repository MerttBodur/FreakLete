using FreakLete.Models;

namespace FreakLete.Core.Tests;

public class WorkoutSessionStateTests
{
	// ═══════════════════════════════════════════════════════
	//  Factory: Empty
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void Empty_DefaultName_IsSerbest()
	{
		var state = WorkoutSessionState.Empty();
		Assert.Equal("Serbest Antrenman", state.WorkoutName);
	}

	[Fact]
	public void Empty_CustomName_IsUsed()
	{
		var state = WorkoutSessionState.Empty("Sabah Antrenmanı");
		Assert.Equal("Sabah Antrenmanı", state.WorkoutName);
	}

	[Fact]
	public void Empty_StartsActive()
	{
		var state = WorkoutSessionState.Empty();
		Assert.True(state.IsActive);
	}

	[Fact]
	public void Empty_HasNoExercises()
	{
		var state = WorkoutSessionState.Empty();
		Assert.Empty(state.Exercises);
	}

	[Fact]
	public void Empty_StartedAt_IsRecent()
	{
		var before = DateTime.Now;
		var state = WorkoutSessionState.Empty();
		var after = DateTime.Now;

		Assert.InRange(state.StartedAt, before, after);
	}

	// ═══════════════════════════════════════════════════════
	//  Factory: FromTemplate
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void FromTemplate_SetsName()
	{
		var exercises = new List<ExerciseEntry>
		{
			new() { ExerciseName = "Bench Press", SetsCount = 3, Reps = 10 }
		};

		var state = WorkoutSessionState.FromTemplate("Push Day - Week 1", exercises);
		Assert.Equal("Push Day - Week 1", state.WorkoutName);
	}

	[Fact]
	public void FromTemplate_PreloadsExercises()
	{
		var exercises = new List<ExerciseEntry>
		{
			new() { ExerciseName = "Squat", SetsCount = 5, Reps = 5 },
			new() { ExerciseName = "Deadlift", SetsCount = 3, Reps = 3 }
		};

		var state = WorkoutSessionState.FromTemplate("Strength", exercises);
		Assert.Equal(2, state.Exercises.Count);
		Assert.Equal("Squat", state.Exercises[0].ExerciseName);
		Assert.Equal("Deadlift", state.Exercises[1].ExerciseName);
	}

	[Fact]
	public void FromTemplate_StartsActive()
	{
		var state = WorkoutSessionState.FromTemplate("Test", []);
		Assert.True(state.IsActive);
	}

	// ═══════════════════════════════════════════════════════
	//  Elapsed
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void Elapsed_WhenActive_ReturnsPositive()
	{
		var state = new WorkoutSessionState
		{
			StartedAt = DateTime.Now.AddMinutes(-5),
			IsActive = true
		};

		Assert.True(state.Elapsed.TotalMinutes >= 4.9);
	}

	[Fact]
	public void Elapsed_WhenStopped_ReturnsZero()
	{
		var state = new WorkoutSessionState
		{
			StartedAt = DateTime.Now.AddMinutes(-10),
			IsActive = false
		};

		Assert.Equal(TimeSpan.Zero, state.Elapsed);
	}

	// ═══════════════════════════════════════════════════════
	//  Stop
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void Stop_SetsInactive()
	{
		var state = WorkoutSessionState.Empty();
		Assert.True(state.IsActive);

		state.Stop();
		Assert.False(state.IsActive);
	}

	[Fact]
	public void Stop_ElapsedBecomesZero()
	{
		var state = new WorkoutSessionState
		{
			StartedAt = DateTime.Now.AddMinutes(-5),
			IsActive = true
		};

		state.Stop();
		Assert.Equal(TimeSpan.Zero, state.Elapsed);
	}

	// ═══════════════════════════════════════════════════════
	//  Exercise mutation
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void Exercises_CanAddDynamically()
	{
		var state = WorkoutSessionState.Empty();
		Assert.Empty(state.Exercises);

		state.Exercises.Add(new ExerciseEntry { ExerciseName = "Pull-up", SetsCount = 3, Reps = 8 });
		Assert.Single(state.Exercises);
		Assert.Equal("Pull-up", state.Exercises[0].ExerciseName);
	}

	[Fact]
	public void Exercises_CanReplaceList()
	{
		var state = WorkoutSessionState.FromTemplate("Test", [
			new() { ExerciseName = "A", SetsCount = 1, Reps = 1 }
		]);

		state.Exercises = [
			new() { ExerciseName = "B", SetsCount = 2, Reps = 2 },
			new() { ExerciseName = "C", SetsCount = 3, Reps = 3 }
		];

		Assert.Equal(2, state.Exercises.Count);
		Assert.Equal("B", state.Exercises[0].ExerciseName);
	}
}
