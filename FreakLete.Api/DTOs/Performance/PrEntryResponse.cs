namespace FreakLete.Api.DTOs.Performance;

public class PrEntryResponse
{
    public int Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseCategory { get; set; } = string.Empty;
    public string TrackingMode { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int Reps { get; set; }
    public int? RIR { get; set; }
    public double? Metric1Value { get; set; }
    public string Metric1Unit { get; set; } = string.Empty;
    public double? Metric2Value { get; set; }
    public string Metric2Unit { get; set; } = string.Empty;
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}
