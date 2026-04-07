namespace FreakLete.Api.DTOs.Auth;

/// <summary>
/// Typed request for saving all athlete profile fields at once.
/// All fields are always sent by the client.
/// null means "clear this field", a value means "set it".
/// No sentinel values (0 = clear) — explicit nullability only.
/// </summary>
public class SaveAthleteProfileRequest
{
    public DateOnly? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public double? HeightCm { get; set; }
    public string? Sex { get; set; }
    public string? SportName { get; set; }
    public string? Position { get; set; }
    public string? GymExperienceLevel { get; set; }
}
