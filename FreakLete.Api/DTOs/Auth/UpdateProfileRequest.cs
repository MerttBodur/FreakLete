using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Auth;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }

    [MaxLength(100)]
    public string SportName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string GymExperienceLevel { get; set; } = string.Empty;
}
