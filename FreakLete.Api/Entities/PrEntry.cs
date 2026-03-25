namespace FreakLete.Api.Entities;

public class PrEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseCategory { get; set; } = string.Empty;
    public string TrackingMode { get; set; } = "Strength";
    public int Weight { get; set; }
    public int Reps { get; set; }
    public int? RIR { get; set; }
    public double? Metric1Value { get; set; }
    public string? Metric1Unit { get; set; }
    public double? Metric2Value { get; set; }
    public string? Metric2Unit { get; set; }
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
