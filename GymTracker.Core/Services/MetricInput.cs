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

		string candidate = text.Trim().Replace(" ", string.Empty);
		string normalized = NormalizeDecimal(candidate);

		return
			double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
			double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
			double.TryParse(candidate, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
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
		return $"{MillisecondsToSeconds(milliseconds).ToString("0.##", CultureInfo.InvariantCulture)} s";
	}

	private static string NormalizeDecimal(string candidate)
	{
		int lastDot = candidate.LastIndexOf('.');
		int lastComma = candidate.LastIndexOf(',');

		if (lastDot == -1 && lastComma == -1)
		{
			return candidate;
		}

		char decimalSeparator = lastDot > lastComma ? '.' : ',';
		string withoutThousands = decimalSeparator == '.'
			? candidate.Replace(",", string.Empty)
			: candidate.Replace(".", string.Empty);

		return withoutThousands.Replace(decimalSeparator, '.');
	}
}
