using FreakLete.Services;
using static FreakLete.Services.ProfileStateManager;

namespace FreakLete.Core.Tests;

public class ProfileStateManagerTests
{
	// ── Validation ────────────────────────────────────────

	[Fact]
	public void ValidateAthleteFields_ValidValues_ReturnsValid()
	{
		var (isValid, error) = ValidateAthleteFields("80", "15");
		Assert.True(isValid);
		Assert.Null(error);
	}

	[Fact]
	public void ValidateAthleteFields_EmptyFields_ReturnsValid()
	{
		var (isValid, _) = ValidateAthleteFields("", "");
		Assert.True(isValid);
	}

	[Fact]
	public void ValidateAthleteFields_NullFields_ReturnsValid()
	{
		var (isValid, _) = ValidateAthleteFields(null, null);
		Assert.True(isValid);
	}

	[Theory]
	[InlineData("abc", "15", "Weight must be a valid number.")]
	[InlineData("80", "xyz", "Body fat must be a valid number.")]
	[InlineData("10", "15", "Weight must be between 20 and 400 kg.")]
	[InlineData("500", "15", "Weight must be between 20 and 400 kg.")]
	[InlineData("80", "-1", "Body fat must be between 0 and 100.")]
	[InlineData("80", "101", "Body fat must be between 0 and 100.")]
	public void ValidateAthleteFields_InvalidValues_ReturnsError(
		string weight, string bodyFat, string expectedError)
	{
		var (isValid, error) = ValidateAthleteFields(weight, bodyFat);
		Assert.False(isValid);
		Assert.Equal(expectedError, error);
	}

	[Fact]
	public void ValidateAthleteFields_BoundaryValues_AreValid()
	{
		Assert.True(ValidateAthleteFields("20", "0").IsValid);
		Assert.True(ValidateAthleteFields("400", "100").IsValid);
	}

	// ── Athlete payload builder ───────────────────────────

	[Fact]
	public void BuildAthletePayload_IncludesWeightAndBodyFat()
	{
		var payload = BuildAthletePayload(
			"80.5", "15.2",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal(80.5, payload["weightKg"]);
		Assert.Equal(15.2, payload["bodyFatPercentage"]);
		Assert.False(payload.ContainsKey("dateOfBirth"));
	}

	[Fact]
	public void BuildAthletePayload_EmptyWeight_SendsClearSentinel()
	{
		var payload = BuildAthletePayload(
			"", "",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		// Empty text → 0.0 sentinel tells API to clear the value
		Assert.Equal(0.0, payload["weightKg"]);
		Assert.Equal(0.0, payload["bodyFatPercentage"]);
	}

	[Fact]
	public void BuildAthletePayload_DateOfBirthChanged_IncludesDate()
	{
		var dob = new DateTime(1995, 6, 15);
		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: true,
			selectedDateOfBirth: dob,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal(new DateOnly(1995, 6, 15), payload["dateOfBirth"]);
	}

	[Fact]
	public void BuildAthletePayload_DateOfBirthNotChanged_ExcludesDate()
	{
		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.False(payload.ContainsKey("dateOfBirth"));
	}

	[Fact]
	public void BuildAthletePayload_SportWithPosition_IncludesBoth()
	{
		var sport = new SportInfo
		{
			Name = "Basketball",
			HasPositions = true,
			Positions = ["PG", "SG", "SF", "PF", "C"]
		};

		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: sport,
			selectedPosition: "PG",
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal("Basketball", payload["sportName"]);
		Assert.Equal("PG", payload["position"]);
	}

	[Fact]
	public void BuildAthletePayload_SportWithoutPositions_ClearsPosition()
	{
		var sport = new SportInfo
		{
			Name = "Swimming",
			HasPositions = false,
			Positions = []
		};

		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: sport,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: "PG");

		Assert.Equal("Swimming", payload["sportName"]);
		// Must explicitly send empty position to clear server-side value
		Assert.True(payload.ContainsKey("position"));
		Assert.Equal("", payload["position"]);
	}

	[Fact]
	public void BuildAthletePayload_SportWithPositions_NoneSelected_DoesNotSendPosition()
	{
		var sport = new SportInfo
		{
			Name = "Basketball",
			HasPositions = true,
			Positions = ["PG", "SG"]
		};

		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: sport,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal("Basketball", payload["sportName"]);
		Assert.False(payload.ContainsKey("position"));
	}

	[Fact]
	public void BuildAthletePayload_GymExperience_Included()
	{
		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: "3-4 years",
			previousPosition: null);

		Assert.Equal("3-4 years", payload["gymExperienceLevel"]);
	}

	[Fact]
	public void BuildAthletePayload_NoSport_NoGymExperience_MinimalPayload()
	{
		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal(2, payload.Count); // only weightKg and bodyFatPercentage
	}

	// ── Coach payload builder ─────────────────────────────

	[Fact]
	public void BuildCoachPayload_AllFieldsSet_IncludesAll()
	{
		var payload = BuildCoachPayload(
			"5", "60",
			"Dumbbells, Barbell",
			"Bad knee",
			"ACL tear 2020",
			"Lower back pain",
			"Strength",
			"Hypertrophy",
			"High Protein");

		Assert.Equal(5, payload["trainingDaysPerWeek"]);
		Assert.Equal(60, payload["preferredSessionDurationMinutes"]);
		Assert.Equal("Dumbbells, Barbell", payload["availableEquipment"]);
		Assert.Equal("Bad knee", payload["physicalLimitations"]);
		Assert.Equal("ACL tear 2020", payload["injuryHistory"]);
		Assert.Equal("Lower back pain", payload["currentPainPoints"]);
		Assert.Equal("Strength", payload["primaryTrainingGoal"]);
		Assert.Equal("Hypertrophy", payload["secondaryTrainingGoal"]);
		Assert.Equal("High Protein", payload["dietaryPreference"]);
	}

	[Fact]
	public void BuildCoachPayload_EmptyFields_Excluded()
	{
		var payload = BuildCoachPayload(
			null, null,
			"", "", "", "",
			null, null, null);

		Assert.Empty(payload);
	}

	[Fact]
	public void BuildCoachPayload_PartialFields_OnlyIncludesNonEmpty()
	{
		var payload = BuildCoachPayload(
			"3", null,
			"", "",
			"Shoulder surgery", "",
			"Fat Loss", null, null);

		Assert.Equal(3, payload["trainingDaysPerWeek"]);
		Assert.Equal("Shoulder surgery", payload["injuryHistory"]);
		Assert.Equal("Fat Loss", payload["primaryTrainingGoal"]);
		Assert.Equal(3, payload.Count);
	}

	// ── Sport / position coherence ────────────────────────

	[Fact]
	public void ResolvePositionForSport_NullSport_HidesSelector()
	{
		var (position, show) = ResolvePositionForSport(null, "PG");
		Assert.Null(position);
		Assert.False(show);
	}

	[Fact]
	public void ResolvePositionForSport_SportWithPositions_MatchesCurrent()
	{
		var sport = new SportInfo
		{
			Name = "Basketball",
			HasPositions = true,
			Positions = ["PG", "SG", "SF", "PF", "C"]
		};

		var (position, show) = ResolvePositionForSport(sport, "SG");
		Assert.Equal("SG", position);
		Assert.True(show);
	}

	[Fact]
	public void ResolvePositionForSport_SportWithPositions_CaseInsensitiveMatch()
	{
		var sport = new SportInfo
		{
			Name = "Basketball",
			HasPositions = true,
			Positions = ["PG", "SG"]
		};

		var (position, show) = ResolvePositionForSport(sport, "pg");
		Assert.Equal("PG", position);
		Assert.True(show);
	}

	[Fact]
	public void ResolvePositionForSport_SportWithPositions_NoMatch_ReturnsNull()
	{
		var sport = new SportInfo
		{
			Name = "Basketball",
			HasPositions = true,
			Positions = ["PG", "SG"]
		};

		var (position, show) = ResolvePositionForSport(sport, "Goalkeeper");
		Assert.Null(position);
		Assert.True(show);
	}

	[Fact]
	public void ResolvePositionForSport_SportWithPositions_NullCurrent_ReturnsNull()
	{
		var sport = new SportInfo
		{
			Name = "Basketball",
			HasPositions = true,
			Positions = ["PG", "SG"]
		};

		var (position, show) = ResolvePositionForSport(sport, null);
		Assert.Null(position);
		Assert.True(show);
	}

	[Fact]
	public void ResolvePositionForSport_SportWithoutPositions_ClearsAndHides()
	{
		var sport = new SportInfo
		{
			Name = "Swimming",
			HasPositions = false,
			Positions = []
		};

		var (position, show) = ResolvePositionForSport(sport, "PG");
		Assert.Null(position);
		Assert.False(show);
	}

	[Fact]
	public void ResolvePositionForSport_HasPositionsTrue_ButEmptyList_HidesSelector()
	{
		var sport = new SportInfo
		{
			Name = "Track",
			HasPositions = true,
			Positions = []
		};

		var (position, show) = ResolvePositionForSport(sport, "PG");
		Assert.Null(position);
		Assert.False(show);
	}

	// ── Sport change → save → no stale position scenario ──

	[Fact]
	public void SportChange_FromPositionedToPositionless_PayloadClearsPosition()
	{
		// User had Basketball/PG, switches to Swimming (no positions)
		var swimming = new SportInfo
		{
			Name = "Swimming",
			HasPositions = false,
			Positions = []
		};

		// After sport change, UI sets _selectedPosition = null via ResolvePositionForSport
		var (resolvedPosition, _) = ResolvePositionForSport(swimming, null);
		Assert.Null(resolvedPosition);

		// Build payload — must explicitly clear position
		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: swimming,
			selectedPosition: resolvedPosition,
			selectedGymExperience: null,
			previousPosition: "PG");

		Assert.Equal("Swimming", payload["sportName"]);
		Assert.Equal("", payload["position"]); // cleared, not omitted
	}

	[Fact]
	public void SportChange_FromPositionedToPositioned_PayloadDoesNotClearPosition()
	{
		// User had Basketball/PG, switches to Football, selects Midfielder
		var football = new SportInfo
		{
			Name = "Football",
			HasPositions = true,
			Positions = ["Goalkeeper", "Defender", "Midfielder", "Forward"]
		};

		var (resolvedPosition, _) = ResolvePositionForSport(football, null);
		Assert.Null(resolvedPosition); // no match for old position

		// User picks Midfielder
		var payload = BuildAthletePayload(
			"80", "15",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: football,
			selectedPosition: "Midfielder",
			selectedGymExperience: null,
			previousPosition: "PG");

		Assert.Equal("Football", payload["sportName"]);
		Assert.Equal("Midfielder", payload["position"]);
	}

	// ── Save failure must not alter state ─────────────────

	[Fact]
	public void BuildAthletePayload_ProducesNewDictionary_EachCall()
	{
		// Ensure repeated calls don't share mutable state
		var payload1 = BuildAthletePayload("80", "15", false, DateTime.Today,
			null, null, null, null);
		var payload2 = BuildAthletePayload("90", "20", false, DateTime.Today,
			null, null, null, null);

		Assert.Equal(80.0, payload1["weightKg"]);
		Assert.Equal(90.0, payload2["weightKg"]);
	}

	// ── ParseNullableDouble ───────────────────────────────

	[Theory]
	[InlineData("80.5", 80.5)]
	[InlineData("80,5", 80.5)]
	[InlineData("  80  ", 80.0)]
	[InlineData("0", 0.0)]
	public void ParseNullableDouble_ValidInput_ReturnsValue(string input, double expected)
	{
		Assert.Equal(expected, ProfileStateManager.ParseNullableDouble(input));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ParseNullableDouble_EmptyOrNull_ReturnsNull(string? input)
	{
		Assert.Null(ProfileStateManager.ParseNullableDouble(input));
	}

	[Fact]
	public void ParseNullableDouble_InvalidText_ReturnsNull()
	{
		Assert.Null(ProfileStateManager.ParseNullableDouble("abc"));
	}

	// ── Date of birth display ────────────────────────────

	[Fact]
	public void FormatDateOfBirth_NullDob_ReturnsNull()
	{
		Assert.Null(ProfileStateManager.FormatDateOfBirth(null));
	}

	[Fact]
	public void FormatDateOfBirth_ValidDob_ReturnsFormattedString()
	{
		var dob = new DateOnly(1995, 6, 15);
		var result = ProfileStateManager.FormatDateOfBirth(dob);
		Assert.NotNull(result);
		Assert.Contains("1995", result);
	}

	[Fact]
	public void CalculateAge_NullDob_ReturnsNull()
	{
		Assert.Null(ProfileStateManager.CalculateAge(null, new DateOnly(2026, 3, 25)));
	}

	[Fact]
	public void CalculateAge_ValidDob_ReturnsCorrectAge()
	{
		var dob = new DateOnly(2000, 6, 15);
		var today = new DateOnly(2026, 3, 25);
		Assert.Equal(25, ProfileStateManager.CalculateAge(dob, today));
	}

	[Fact]
	public void CalculateAge_BirthdayNotYetThisYear_SubtractsOne()
	{
		var dob = new DateOnly(2000, 12, 25);
		var today = new DateOnly(2026, 3, 25);
		Assert.Equal(25, ProfileStateManager.CalculateAge(dob, today));
	}

	[Fact]
	public void CalculateAge_ExactBirthday_FullAge()
	{
		var dob = new DateOnly(2000, 3, 25);
		var today = new DateOnly(2026, 3, 25);
		Assert.Equal(26, ProfileStateManager.CalculateAge(dob, today));
	}

	// ── Weight/BodyFat clear sentinel ────────────────────

	[Fact]
	public void BuildAthletePayload_ValidWeight_SendsValue()
	{
		var payload = BuildAthletePayload(
			"80.5", "15.2",
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal(80.5, payload["weightKg"]);
		Assert.Equal(15.2, payload["bodyFatPercentage"]);
	}

	[Fact]
	public void BuildAthletePayload_NullWeight_SendsZeroSentinel()
	{
		var payload = BuildAthletePayload(
			null, null,
			dateOfBirthChanged: false,
			selectedDateOfBirth: DateTime.Today,
			selectedSport: null,
			selectedPosition: null,
			selectedGymExperience: null,
			previousPosition: null);

		Assert.Equal(0.0, payload["weightKg"]);
		Assert.Equal(0.0, payload["bodyFatPercentage"]);
	}
}
