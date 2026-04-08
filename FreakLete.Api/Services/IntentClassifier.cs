namespace FreakLete.Api.Services;

/// <summary>
/// Lightweight fallback classifier for FreakAI intent when client doesn't send one.
/// Keyword-based heuristic — runs before Gemini call, no ML dependency.
/// </summary>
public static class IntentClassifier
{
    /// <summary>
    /// Classifies user message into a usage intent bucket.
    /// Returns the explicit intent if provided and valid, otherwise falls back to heuristics.
    /// </summary>
    public static string Classify(string? explicitIntent, string userMessage)
    {
        // If client sent a valid intent, trust it
        if (FreakAiUsageIntent.IsValid(explicitIntent))
        {
            // program_view maps to general_chat for quota purposes
            if (explicitIntent == FreakAiUsageIntent.ProgramView)
                return FreakAiUsageIntent.GeneralChat;

            return explicitIntent!;
        }

        // Fallback: keyword heuristic
        var lower = userMessage.ToLowerInvariant();

        if (MatchesProgramGenerate(lower))
            return FreakAiUsageIntent.ProgramGenerate;

        if (MatchesProgramAnalyze(lower))
            return FreakAiUsageIntent.ProgramAnalyze;

        if (MatchesNutritionGuidance(lower))
            return FreakAiUsageIntent.NutritionGuidance;

        return FreakAiUsageIntent.GeneralChat;
    }

    private static bool MatchesProgramGenerate(string lower)
    {
        // Program creation/generation/modification patterns
        string[] patterns =
        [
            "program oluştur", "program yaz", "program hazırla", "program yap",
            "antrenman programı oluştur", "antrenman programı yaz",
            "bana bir program", "yeni program", "program değiştir",
            "create program", "create a program", "write a program",
            "make me a program", "generate program", "new program",
            "build a program", "design a program",
            "change my program", "modify my program", "update my program",
            "haftalık program", "weekly program", "training plan",
            "4-week", "4 week", "6-week", "6 week", "8-week", "8 week",
            "split program", "push pull", "upper lower"
        ];

        return ContainsAny(lower, patterns);
    }

    private static bool MatchesProgramAnalyze(string lower)
    {
        string[] patterns =
        [
            "analiz et", "programımı analiz", "programımı değerlendir",
            "analyze my program", "review my program", "evaluate my program",
            "critique my program", "assess my program",
            "what do you think of my program", "feedback on my program",
            "programım nasıl", "programım hakkında"
        ];

        return ContainsAny(lower, patterns);
    }

    private static bool MatchesNutritionGuidance(string lower)
    {
        string[] patterns =
        [
            "beslenme", "diyet", "kalori", "makro", "protein",
            "nutrition", "diet", "calorie", "macro", "meal plan",
            "what should i eat", "eating plan", "food",
            "ne yemeliyim", "öğün", "yemek planı"
        ];

        return ContainsAny(lower, patterns);
    }

    private static bool ContainsAny(string text, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (text.Contains(pattern, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
