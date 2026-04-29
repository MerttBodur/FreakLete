namespace FreakLete.Api.Services.Rag;

public sealed class FreakAiContext
{
    public string? UserProfile { get; init; }
    public string? Goals { get; init; }
    public string? Equipment { get; init; }
    public string? PhysicalLimitations { get; init; }
    public string? CurrentProgram { get; init; }
    public string? RecentPrSummary { get; init; }
    public List<string> SimilarWorkouts { get; init; } = [];
    public string? UserSnapshotContext { get; init; }
}
