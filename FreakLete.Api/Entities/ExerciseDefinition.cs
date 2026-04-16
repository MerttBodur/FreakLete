namespace FreakLete.Api.Entities;

public class ExerciseDefinition
{
    public string CatalogId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TurkishName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string SourceSection { get; set; } = string.Empty;
    public string Force { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Mechanic { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string PrimaryMusclesText { get; set; } = string.Empty;
    public string SecondaryMusclesText { get; set; } = string.Empty;
    public string InstructionsText { get; set; } = string.Empty;
    public string TrackingMode { get; set; } = "Strength";
    public string PrimaryLabel { get; set; } = string.Empty;
    public string PrimaryUnit { get; set; } = string.Empty;
    public string SecondaryLabel { get; set; } = string.Empty;
    public string SecondaryUnit { get; set; } = string.Empty;
    public bool SupportsGroundContactTime { get; set; }
    public bool SupportsConcentricTime { get; set; }
    public string MovementPattern { get; set; } = string.Empty;
    public string AthleticQuality { get; set; } = string.Empty;
    public string SportRelevance { get; set; } = string.Empty;
    public string NervousSystemLoad { get; set; } = string.Empty;
    public string GctProfile { get; set; } = string.Empty;
    public string LoadPrescription { get; set; } = string.Empty;
    public string CommonMistakes { get; set; } = string.Empty;
    public string Progression { get; set; } = string.Empty;
    public string Regression { get; set; } = string.Empty;
    public int RecommendedRank { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string TierType { get; set; } = string.Empty;
    public string TierThresholdsMale { get; set; } = string.Empty;
    public string TierThresholdsFemale { get; set; } = string.Empty;
    public string? TierParentId { get; set; }
    public double? TierScale { get; set; }
}
