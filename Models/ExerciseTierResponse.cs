namespace FreakLete.Models;

public class ExerciseTierResponse
{
    public string CatalogId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public double RawValue { get; set; }
    public double? Ratio { get; set; }
    public DateTime CalculatedAt { get; set; }
}
