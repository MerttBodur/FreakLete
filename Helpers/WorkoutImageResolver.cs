namespace FreakLete.Helpers;

public static class WorkoutImageResolver
{
    private static readonly Dictionary<string, string> TemplateImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Full Body Foundation 3-Day", "workout_fullbody_foundation" },
        { "In-Season Maintenance 2-Day", "workout_inseason_maintenance" },
        { "Strength Base 5x5", "workout_fullbody_foundation" },
        { "Upper/Lower Performance 4-Day", "workout_fullbody_foundation" },
        { "5/3/1 Strength 4-Day", "workout_fullbody_foundation" },
    };

    // Deterministic fallback colors for templates without a specific image.
    private static readonly string[] FallbackColors =
    [
        "#2F2346", "#1B3A4B", "#2D1B2E", "#1A2E1A", "#3B2A1A"
    ];

    /// <summary>
    /// Returns the image filename for a program name, or null if no mapping exists.
    /// </summary>
    public static string? GetImageForProgram(string programName)
    {
        return TemplateImageMap.TryGetValue(programName, out var filename) ? filename : null;
    }

    /// <summary>
    /// Returns a deterministic fallback background color hex for programs without an image.
    /// </summary>
    public static string GetFallbackColor(string programName)
    {
        int hash = Math.Abs(programName.GetHashCode());
        return FallbackColors[hash % FallbackColors.Length];
    }
}
