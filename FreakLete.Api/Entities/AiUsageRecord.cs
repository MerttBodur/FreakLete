namespace FreakLete.Api.Entities;

public class AiUsageRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Intent { get; set; } = string.Empty;         // FreakAiUsageIntent value as string
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public bool WasBlocked { get; set; }
    public string PlanAtTime { get; set; } = string.Empty;     // "free" or "premium"
    public string? Notes { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
