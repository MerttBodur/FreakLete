namespace FreakLete.Helpers;

public static class WorkoutImageResolver
{
    private static readonly Dictionary<string, string> TemplateImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Full Body Foundation 3-Day", "workout_fullbody" },
        { "Strength Base 5x5",          "workout_5x5" },
        { "Upper/Lower Performance 4-Day", "workout_upperlower" },
        { "In-Season Maintenance 2-Day", "workout_maintenance" },
        { "5/3/1 Strength 4-Day",        "workout_531" },
    };

    // Keyword-based fallback for user-named programs (e.g. "My 5x5 Push Pull").
    // Order matters: more specific patterns first.
    private static readonly (string Keyword, string Image)[] KeywordFallbacks =
    [
        ("5/3/1",        "workout_531"),
        ("5-3-1",        "workout_531"),
        ("531",          "workout_531"),
        ("5x5",          "workout_5x5"),
        ("5×5",          "workout_5x5"),
        ("upper/lower",  "workout_upperlower"),
        ("upper lower",  "workout_upperlower"),
        ("upper-lower",  "workout_upperlower"),
        ("push pull legs","workout_upperlower"),
        ("ppl",          "workout_upperlower"),
        ("in-season",    "workout_maintenance"),
        ("in season",    "workout_maintenance"),
        ("maintenance",  "workout_maintenance"),
        ("deload",       "workout_maintenance"),
        ("full body",    "workout_fullbody"),
        ("fullbody",     "workout_fullbody"),
        ("foundation",   "workout_fullbody"),
        ("beginner",     "workout_fullbody"),
        ("strength",     "workout_5x5"),
        ("hypertrophy",  "workout_upperlower"),
        ("athletic",     "workout_531"),
    ];

    private static readonly string[] FallbackColors =
    [
        "#2F2346", "#1B3A4B", "#2D1B2E", "#1A2E1A", "#3B2A1A"
    ];

    /// <summary>
    /// Returns an image filename for a program. Tries exact name match first,
    /// then keyword search across name and optional goal text.
    /// </summary>
    public static string? GetImageForProgram(string programName, string? goal = null)
    {
        if (TemplateImageMap.TryGetValue(programName, out var exact))
            return exact;

        var haystack = $"{programName} {goal}".ToLowerInvariant();
        foreach (var (keyword, image) in KeywordFallbacks)
            if (haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return image;

        return null;
    }

    public static string GetFallbackColor(string programName)
    {
        int hash = Math.Abs(programName.GetHashCode());
        return FallbackColors[hash % FallbackColors.Length];
    }
}
