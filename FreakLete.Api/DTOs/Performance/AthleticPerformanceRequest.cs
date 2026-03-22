using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Performance;

public class AthleticPerformanceRequest
{
    [Required, MaxLength(200)]
    public string MovementName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string MovementCategory { get; set; } = string.Empty;

    public double Value { get; set; }

    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    public double? SecondaryValue { get; set; }
    public string SecondaryUnit { get; set; } = string.Empty;
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
}
