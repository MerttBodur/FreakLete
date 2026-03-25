using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class ProfileStateManagerTests
{
	// ── Validation ────────────────────────────────────────

	[Fact]
	public void ValidateAthleteFields_ValidValues_ReturnsValid()
	{
		var (isValid, error) = ProfileStateManager.ValidateAthleteFields("80", "15");
		Assert.True(isValid);
		Assert.Null(error);
	}

	[Fact]
	public void ValidateAthleteFields_EmptyFields_ReturnsValid()
	{
		var (isValid, _) = ProfileStateManager.ValidateAthleteFields("", "");
		Assert.True(isValid);
	}

	[Fact]
	public void ValidateAthleteFields_NullFields_ReturnsValid()
	{
		var (isValid, _) = ProfileStateManager.ValidateAthleteFields(null, null);
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
		var (isValid, error) = ProfileStateManager.ValidateAthleteFields(weight, bodyFat);
		Assert.False(isValid);
		Assert.Equal(expectedError, error);
	}

	[Fact]
	public void ValidateAthleteFields_BoundaryValues_AreValid()
	{
		Assert.True(ProfileStateManager.ValidateAthleteFields("20", "0").IsValid);
		Assert.True(ProfileStateManager.ValidateAthleteFields("400", "100").IsValid);
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
}
