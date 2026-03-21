using SQLite;

namespace FreakLete.Models;

public class Workout
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int UserId { get; set; }

	public string WorkoutName { get; set; } = string.Empty;

	public DateTime WorkoutDate { get; set; }
}
