using SQLite;

namespace GymTracker.Models;

public class User
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	[Unique]
	public string Email { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public DateTime? DateOfBirth { get; set; }

	public double? WeightKg { get; set; }

	public double? BodyFatPercentage { get; set; }

	public string SportName { get; set; } = string.Empty;

	public string GymExperienceLevel { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
