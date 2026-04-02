namespace FreakLete.Helpers;

public static class WorkoutImageResolver
{
    private static readonly Dictionary<string, string> TemplateImageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add entries here when image assets are placed in Resources/Images/
        // Example: { "Full Body Foundation 3-Day", "workout_fullbody.png" },
    };

    /// <summary>
    /// Returns the image filename for a program name, or null if no mapping exists.
    /// </summary>
    public static string? GetImageForProgram(string programName)
    {
        return TemplateImageMap.TryGetValue(programName, out var filename) ? filename : null;
    }
}
