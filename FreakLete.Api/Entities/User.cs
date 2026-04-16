namespace FreakLete.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public double? HeightCm { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string SportName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string GymExperienceLevel { get; set; } = string.Empty;
    public int TokenVersion { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Profile photo (stored as bytea in PostgreSQL; max 2 MB enforced at controller)
    public byte[]? ProfilePhotoBytes { get; set; }
    public string? ProfilePhotoContentType { get; set; }
    public DateTime? ProfilePhotoUpdatedAtUtc { get; set; }

    // Coach profile fields
    public int? TrainingDaysPerWeek { get; set; }
    public int? PreferredSessionDurationMinutes { get; set; }
    public string AvailableEquipment { get; set; } = string.Empty;
    public string PhysicalLimitations { get; set; } = string.Empty;
    public string InjuryHistory { get; set; } = string.Empty;
    public string CurrentPainPoints { get; set; } = string.Empty;
    public string PrimaryTrainingGoal { get; set; } = string.Empty;
    public string SecondaryTrainingGoal { get; set; } = string.Empty;
    public string DietaryPreference { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Workout> Workouts { get; set; } = [];
    public ICollection<PrEntry> PrEntries { get; set; } = [];
    public ICollection<AthleticPerformanceEntry> AthleticPerformanceEntries { get; set; } = [];
    public ICollection<MovementGoal> MovementGoals { get; set; } = [];
    public ICollection<TrainingProgram> TrainingPrograms { get; set; } = [];
    public ICollection<UserExerciseTier> ExerciseTiers { get; set; } = [];
}
