namespace FreakLete.Api.DTOs.Tier;

public class TierResultDto
{
    public string CatalogId { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public string? PreviousTierLevel { get; set; }
    public bool LeveledUp { get; set; }
}
