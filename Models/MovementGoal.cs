using SQLite;

namespace GymTracker.Models;

public class MovementGoal
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int UserId { get; set; }

	public string MovementName { get; set; } = string.Empty;

	public double TargetValue { get; set; }

	public string Unit { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
