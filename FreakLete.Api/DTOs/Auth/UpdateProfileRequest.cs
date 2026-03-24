using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Auth;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }

    [MaxLength(100)]
    public string? SportName { get; set; }

    [MaxLength(100)]
    public string? Position { get; set; }

    [MaxLength(50)]
    public string? GymExperienceLevel { get; set; }

    // Coach profile fields
    public int? TrainingDaysPerWeek { get; set; }
    public int? PreferredSessionDurationMinutes { get; set; }

    [MaxLength(1000)]
    public string? AvailableEquipment { get; set; }

    [MaxLength(1000)]
    public string? PhysicalLimitations { get; set; }

    [MaxLength(1000)]
    public string? InjuryHistory { get; set; }

    [MaxLength(1000)]
    public string? CurrentPainPoints { get; set; }

    [MaxLength(200)]
    public string? PrimaryTrainingGoal { get; set; }

    [MaxLength(200)]
    public string? SecondaryTrainingGoal { get; set; }

    [MaxLength(200)]
    public string? DietaryPreference { get; set; }
}
