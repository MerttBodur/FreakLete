using SQLite;

namespace GymTracker.Models;

public class ExerciseEntry
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int WorkoutId { get; set; }

	public string ExerciseName { get; set; } = string.Empty;

	public int Sets { get; set; }

	public int Reps { get; set; }

	public int? RIR { get; set; }

	public int? RestSeconds { get; set; }
}
