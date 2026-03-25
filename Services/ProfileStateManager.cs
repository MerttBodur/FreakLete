namespace FreakLete.Services;

/// <summary>
/// Testable profile helpers extracted from ProfilePage.
/// No MAUI dependencies — operates on plain data.
/// </summary>
public class ProfileStateManager
{
	// ── Validation ────────────────────────────────────────

	public static (bool IsValid, string? Error) ValidateAthleteFields(
		string? weightText, string? bodyFatText)
	{
		double? weight = ParseNullableDouble(weightText);
		double? bodyFat = ParseNullableDouble(bodyFatText);

		if (weightText?.Length > 0 && !weight.HasValue)
			return (false, "Weight must be a valid number.");

		if (bodyFatText?.Length > 0 && !bodyFat.HasValue)
			return (false, "Body fat must be a valid number.");

		if (weight.HasValue && (weight.Value < 20 || weight.Value > 400))
			return (false, "Weight must be between 20 and 400 kg.");

		if (bodyFat.HasValue && (bodyFat.Value < 0 || bodyFat.Value > 100))
			return (false, "Body fat must be between 0 and 100.");

		return (true, null);
	}

	// ── Date of birth display ────────────────────────────

	/// <summary>
	/// Returns the formatted DOB string, or null if dateOfBirth is not set.
	/// </summary>
	public static string? FormatDateOfBirth(DateOnly? dateOfBirth)
	{
		return dateOfBirth?.ToDateTime(TimeOnly.MinValue).ToString("dd MMMM yyyy");
	}

	/// <summary>
	/// Calculates age from DOB. Returns null if DOB is not set.
	/// </summary>
	public static int? CalculateAge(DateOnly? dateOfBirth, DateOnly today)
	{
		if (!dateOfBirth.HasValue) return null;

		int age = today.Year - dateOfBirth.Value.Year;
		if (dateOfBirth.Value > today.AddYears(-age))
			age--;
		return age;
	}

	// ── Helpers ───────────────────────────────────────────

	public static double? ParseNullableDouble(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return null;
		return MetricInput.TryParseFlexibleDouble(text, out var v) ? v : null;
	}
}
