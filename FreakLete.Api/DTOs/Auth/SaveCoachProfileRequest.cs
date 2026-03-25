namespace FreakLete.Api.DTOs.Auth;

public class SaveCoachProfileRequest
{
    public int? TrainingDaysPerWeek { get; set; }
    public int? PreferredSessionDurationMinutes { get; set; }
    public string? PrimaryTrainingGoal { get; set; }
    public string? SecondaryTrainingGoal { get; set; }
    public string? DietaryPreference { get; set; }
    public string? AvailableEquipment { get; set; }
    public string? PhysicalLimitations { get; set; }
    public string? InjuryHistory { get; set; }
    public string? CurrentPainPoints { get; set; }
}
