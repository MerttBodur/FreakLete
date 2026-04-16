namespace FreakLete.Core.Tier;

public sealed record ExerciseTierConfig(
    string CatalogId,
    string TierType,
    double[] ThresholdsMale,
    double[] ThresholdsFemale,
    string? TierParentId,
    double? TierScale);
