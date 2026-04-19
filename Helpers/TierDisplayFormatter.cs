using System.Globalization;

namespace FreakLete.Helpers;

public static class TierDisplayFormatter
{
    // trackingMode mirrors server ExerciseDefinition.TrackingMode values:
    // "Strength" | "AthleticHeight" | "AthleticDistance" | "AthleticIndex" | "AthleticTime"
    public static string FormatDelta(string trackingMode, double delta)
    {
        var inv = CultureInfo.InvariantCulture;
        return trackingMode switch
        {
            "Strength"         => $"+{delta.ToString("0", inv)} kg",
            "AthleticHeight"   => $"+{delta.ToString("0", inv)} cm",
            "AthleticDistance" => $"+{delta.ToString("0", inv)} cm",
            "AthleticIndex"    => $"+{delta.ToString("0.00", inv)}",
            "AthleticTime"     => $"-{delta.ToString("0.00", inv)} s",
            _                  => $"+{delta.ToString("0.##", inv)}"
        };
    }
}
