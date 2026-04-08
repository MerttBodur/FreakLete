namespace FreakLete.Api.Services;

/// <summary>
/// Defines the usage intent buckets for FreakAI quota enforcement.
/// </summary>
public static class FreakAiUsageIntent
{
    public const string ProgramGenerate = "program_generate";
    public const string ProgramView = "program_view";
    public const string ProgramAnalyze = "program_analyze";
    public const string NutritionGuidance = "nutrition_guidance";
    public const string GeneralChat = "general_chat";

    public static readonly HashSet<string> All =
    [
        ProgramGenerate,
        ProgramView,
        ProgramAnalyze,
        NutritionGuidance,
        GeneralChat
    ];

    public static bool IsValid(string? intent) =>
        intent is not null && All.Contains(intent);
}
