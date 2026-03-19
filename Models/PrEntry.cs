using SQLite;

namespace GymTracker.Models;

public class PrEntry
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int UserId { get; set; }

	public string ExerciseName { get; set; } = string.Empty;

	public int Weight { get; set; }

	public int Reps { get; set; }

	public int? RIR { get; set; }

	public DateTime CreatedAt { get; set; }
}
