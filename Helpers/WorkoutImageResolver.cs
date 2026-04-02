namespace FreakLete.Helpers;

public static class WorkoutImageResolver
{
    private static readonly Dictionary<string, string> TemplateImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Full Body Foundation 3-Day", "workout_fullbody_foundation" },
        { "In-Season Maintenance 2-Day", "workout_inseason_maintenance" },
    };

    /// <summary>
    /// Returns the image filename for a program name, or null if no mapping exists.
    /// </summary>
    public static string? GetImageForProgram(string programName)
    {
        return TemplateImageMap.TryGetValue(programName, out var filename) ? filename : null;
    }
}
