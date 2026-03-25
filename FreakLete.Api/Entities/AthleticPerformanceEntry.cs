namespace FreakLete.Api.Entities;

public class AthleticPerformanceEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MovementName { get; set; } = string.Empty;
    public string MovementCategory { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double? SecondaryValue { get; set; }
    public string? SecondaryUnit { get; set; }
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
