using System.Globalization;

namespace GymTracker.Services;

public static class MetricInput
{
	public static bool TryParseFlexibleDouble(string? text, out double value)
	{
		value = 0;

		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		string candidate = text.Trim();

		return
			double.TryParse(candidate, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value) ||
			double.TryParse(candidate, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
			double.TryParse(candidate.Replace(',', '.'), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
			double.TryParse(candidate.Replace('.', ','), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("tr-TR"), out value);
	}

	public static double SecondsToMilliseconds(double seconds)
	{
		return seconds * 1000d;
	}

	public static double MillisecondsToSeconds(double milliseconds)
	{
		return milliseconds / 1000d;
	}

	public static string FormatSecondsFromMilliseconds(double milliseconds)
	{
		return $"{MillisecondsToSeconds(milliseconds):0.##} s";
	}
}
