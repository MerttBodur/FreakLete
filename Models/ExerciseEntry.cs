using SQLite;

namespace GymTracker.Models;

public class ExerciseEntry
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int WorkoutId { get; set; }

	public string ExerciseName { get; set; } = string.Empty;

	public string ExerciseCategory { get; set; } = string.Empty;

	public string TrackingMode { get; set; } = nameof(ExerciseTrackingMode.Strength);

	public int Sets { get; set; }

	public int Reps { get; set; }

	public int? RIR { get; set; }

	public int? RestSeconds { get; set; }

	public double? Metric1Value { get; set; }

	public string Metric1Unit { get; set; } = string.Empty;

	public double? Metric2Value { get; set; }

	public string Metric2Unit { get; set; } = string.Empty;
}
