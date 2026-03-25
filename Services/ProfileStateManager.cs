namespace FreakLete.Services;

/// <summary>
/// Testable profile save/state logic extracted from ProfilePage.
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

	// ── Payload builders ──────────────────────────────────

	public static Dictionary<string, object?> BuildAthletePayload(
		string? weightText,
		string? bodyFatText,
		bool dateOfBirthChanged,
		DateTime selectedDateOfBirth,
		SportInfo? selectedSport,
		string? selectedPosition,
		string? selectedGymExperience,
		string? previousPosition)
	{
		var data = new Dictionary<string, object?>();

		if (dateOfBirthChanged)
			data["dateOfBirth"] = DateOnly.FromDateTime(selectedDateOfBirth);

		data["weightKg"] = ParseNullableDouble(weightText);
		data["bodyFatPercentage"] = ParseNullableDouble(bodyFatText);

		if (selectedSport is not null)
		{
			data["sportName"] = selectedSport.Name;

			// When sport has no positions, explicitly clear server-side position
			if (!selectedSport.HasPositions)
			{
				data["position"] = "";
			}
			else if (selectedPosition is not null)
			{
				data["position"] = selectedPosition;
			}
			// If sport has positions but none selected yet, don't send position
			// (preserve whatever the server has)
		}
		else if (selectedPosition is not null)
		{
			data["position"] = selectedPosition;
		}

		if (selectedGymExperience is not null)
			data["gymExperienceLevel"] = selectedGymExperience;

		return data;
	}

	public static Dictionary<string, object?> BuildCoachPayload(
		string? selectedTrainingDays,
		string? selectedSessionDuration,
		string? equipmentText,
		string? limitationsText,
		string? injuryText,
		string? painText,
		string? selectedPrimaryGoal,
		string? selectedSecondaryGoal,
		string? selectedDietaryPreference)
	{
		var data = new Dictionary<string, object?>();

		if (selectedTrainingDays is not null)
			data["trainingDaysPerWeek"] = int.Parse(selectedTrainingDays);

		if (selectedSessionDuration is not null)
			data["preferredSessionDurationMinutes"] = int.Parse(selectedSessionDuration);

		if (!string.IsNullOrEmpty(equipmentText))
			data["availableEquipment"] = equipmentText;

		if (!string.IsNullOrEmpty(limitationsText))
			data["physicalLimitations"] = limitationsText;

		if (!string.IsNullOrEmpty(injuryText))
			data["injuryHistory"] = injuryText;

		if (!string.IsNullOrEmpty(painText))
			data["currentPainPoints"] = painText;

		if (selectedPrimaryGoal is not null)
			data["primaryTrainingGoal"] = selectedPrimaryGoal;

		if (selectedSecondaryGoal is not null)
			data["secondaryTrainingGoal"] = selectedSecondaryGoal;

		if (selectedDietaryPreference is not null)
			data["dietaryPreference"] = selectedDietaryPreference;

		return data;
	}

	// ── Sport / position coherence ────────────────────────

	/// <summary>
	/// Resolves position state when sport changes.
	/// Returns (selectedPosition, positionSelectorVisible).
	/// </summary>
	public static (string? Position, bool ShowPositionSelector) ResolvePositionForSport(
		SportInfo? sport, string? currentPosition)
	{
		if (sport is null)
			return (null, false);

		if (sport.HasPositions && sport.Positions.Count > 0)
		{
			string? matched = null;
			if (!string.IsNullOrWhiteSpace(currentPosition))
			{
				matched = sport.Positions.FirstOrDefault(p =>
					string.Equals(p, currentPosition, StringComparison.OrdinalIgnoreCase));
			}
			return (matched, true);
		}

		return (null, false);
	}

	// ── Helpers ───────────────────────────────────────────

	public static double? ParseNullableDouble(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return null;
		return MetricInput.TryParseFlexibleDouble(text, out var v) ? v : null;
	}

	// ── Sport info (lightweight, no MAUI dependency) ──────

	public class SportInfo
	{
		public required string Name { get; init; }
		public bool HasPositions { get; init; }
		public List<string> Positions { get; init; } = [];
	}
}
