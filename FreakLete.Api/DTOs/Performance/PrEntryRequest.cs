using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Performance;

public class PrEntryRequest
{
    [Required, MaxLength(200)]
    public string ExerciseName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ExerciseCategory { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TrackingMode { get; set; } = "Strength";

    public int Weight { get; set; }
    public int Reps { get; set; }
    public int? RIR { get; set; }
    public double? Metric1Value { get; set; }
    public string Metric1Unit { get; set; } = string.Empty;
    public double? Metric2Value { get; set; }
    public string Metric2Unit { get; set; } = string.Empty;
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
}
