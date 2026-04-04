namespace FreakLete.Models;

/// <summary>
/// Holds the live state of an in-progress workout session.
/// Survives page navigation within the session flow.
/// </summary>
public class WorkoutSessionState
{
	public string WorkoutName { get; set; } = string.Empty;
	public DateTime StartedAt { get; set; }
	public bool IsActive { get; set; }
	public List<ExerciseEntry> Exercises { get; set; } = [];

	public TimeSpan Elapsed => IsActive ? DateTime.Now - StartedAt : TimeSpan.Zero;

	/// <summary>
	/// Creates a session state from a program template with pre-loaded exercises.
	/// </summary>
	public static WorkoutSessionState FromTemplate(string workoutName, List<ExerciseEntry> exercises)
	{
		return new WorkoutSessionState
		{
			WorkoutName = workoutName,
			StartedAt = DateTime.Now,
			IsActive = true,
			Exercises = exercises
		};
	}

	/// <summary>
	/// Creates an empty session state for free-form workouts.
	/// </summary>
	public static WorkoutSessionState Empty(string workoutName = "Serbest Antrenman")
	{
		return new WorkoutSessionState
		{
			WorkoutName = workoutName,
			StartedAt = DateTime.Now,
			IsActive = true,
			Exercises = []
		};
	}

	public void Stop()
	{
		IsActive = false;
	}
}
