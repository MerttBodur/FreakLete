using SQLite;

namespace GymTracker.Models;

public class ProfilePrEntry
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int UserId { get; set; }

	public string MovementName { get; set; } = string.Empty;

	public double Value { get; set; }

	public string Unit { get; set; } = string.Empty;

	public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
