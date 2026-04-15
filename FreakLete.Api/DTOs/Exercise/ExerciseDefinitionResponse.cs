namespace FreakLete.Api.DTOs.Exercise;

public class ExerciseDefinitionResponse
{
    public string CatalogId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TurkishName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Force { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Mechanic { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string PrimaryMusclesText { get; set; } = string.Empty;
    public string SecondaryMusclesText { get; set; } = string.Empty;
    public string InstructionsText { get; set; } = string.Empty;
    public string TrackingMode { get; set; } = string.Empty;
    public string PrimaryLabel { get; set; } = string.Empty;
    public string PrimaryUnit { get; set; } = string.Empty;
    public string SecondaryLabel { get; set; } = string.Empty;
    public string SecondaryUnit { get; set; } = string.Empty;
    public bool SupportsGroundContactTime { get; set; }
    public bool SupportsConcentricTime { get; set; }
    public string MovementPattern { get; set; } = string.Empty;
    public string AthleticQuality { get; set; } = string.Empty;
    public string NervousSystemLoad { get; set; } = string.Empty;
    public int RecommendedRank { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
}
