namespace FreakLete.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public string SportName { get; set; } = string.Empty;
    public string GymExperienceLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Workout> Workouts { get; set; } = [];
    public ICollection<PrEntry> PrEntries { get; set; } = [];
    public ICollection<AthleticPerformanceEntry> AthleticPerformanceEntries { get; set; } = [];
    public ICollection<MovementGoal> MovementGoals { get; set; } = [];
}
