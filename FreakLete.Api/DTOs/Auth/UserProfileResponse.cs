namespace FreakLete.Api.DTOs.Auth;

public class UserProfileResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public string SportName { get; set; } = string.Empty;
    public string GymExperienceLevel { get; set; } = string.Empty;
    public int TotalWorkouts { get; set; }
    public int TotalPrs { get; set; }
    public DateTime CreatedAt { get; set; }
}
