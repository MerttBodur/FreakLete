namespace FreakLete.Services;

public static class ColorResources
{
    public static Color GetColor(string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
            return color;
        return Color.FromArgb(fallback);
    }
}
