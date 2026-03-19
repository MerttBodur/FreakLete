using SQLite;

namespace GymTracker.Models;

public class User
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public string Username { get; set; } = string.Empty;

	[Unique]
	public string Email { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }
}
