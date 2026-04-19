namespace FreakLete.Models;

public class TierResult
{
    public string CatalogId { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public string? PreviousTierLevel { get; set; }
    public bool LeveledUp { get; set; }
    public string? NextLevel { get; set; }
    public double? NextTargetRaw { get; set; }
    public double? NextDelta { get; set; }
    public double ProgressPercent { get; set; }
    public string TrackingMode { get; set; } = string.Empty;
}
