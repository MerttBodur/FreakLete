using System.Globalization;
using System.Reflection;
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class CalculationsPageLogicTests
{
	public CalculationsPageLogicTests()
	{
		SetLanguageAndCulture("en");
	}

	[Fact]
	public void ParseOneRmInputs_WithValidValues_ReturnsParsedResult()
	{
		OneRmInputParseResult result = CalculationsPageLogic.ParseOneRmInputs("100", "5", "1", "1.25");

		Assert.True(result.IsValid);
		Assert.Equal(100, result.WeightKg);
		Assert.Equal(5, result.Reps);
		Assert.Equal(1, result.Rir);
		Assert.Equal(1.25, result.ConcentricTimeSeconds);
	}

	[Theory]
	[InlineData("39", "5", "1", "Weight must be between 40 kg - 250 kg.")]
	[InlineData("251", "5", "1", "Weight must be between 40 kg - 250 kg.")]
	[InlineData("100", "0", "1", "Reps must be between 1 - 8.")]
	[InlineData("100", "9", "1", "Reps must be between 1 - 8.")]
	[InlineData("100", "5", "-1", "RIR must be between 0 - 5.")]
	[InlineData("100", "5", "6", "RIR must be between 0 - 5.")]
	public void ParseOneRmInputs_WithOutOfRangeValues_Fails(
		string weightText,
		string repsText,
		string rirText,
		string expectedMessage)
	{
		OneRmInputParseResult result = CalculationsPageLogic.ParseOneRmInputs(weightText, repsText, rirText, null);

		Assert.False(result.IsValid);
		Assert.Equal(expectedMessage, result.ErrorMessage);
	}

	[Fact]
	public void ParseOneRmInputs_WithInvalidConcentricTime_Fails()
	{
		OneRmInputParseResult result = CalculationsPageLogic.ParseOneRmInputs("100", "5", "1", "-0.2");

		Assert.False(result.IsValid);
		Assert.Equal("Concentric time must be a positive number.", result.ErrorMessage);
	}

	[Fact]
	public void BuildPrEntry_ForStrengthMovement_ReturnsStrengthEntry()
	{
		PrEntryBuildResult result = CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = 42,
			ExerciseName = "Bench Press",
			ExerciseCategory = "Push",
			TrackingMode = ExerciseTrackingMode.Strength,
			WeightText = "120",
			RepsText = "3",
			RirText = "1",
			ConcentricTimeText = "1,15"
		});

		Assert.True(result.IsValid);
		Assert.NotNull(result.Entry);
		Assert.Equal(42, result.Entry!.UserId);
		Assert.Equal("Bench Press", result.Entry.ExerciseName);
		Assert.Equal("Push", result.Entry.ExerciseCategory);
		Assert.Equal(nameof(ExerciseTrackingMode.Strength), result.Entry.TrackingMode);
		Assert.Equal(120, result.Entry.Weight);
		Assert.Equal(3, result.Entry.Reps);
		Assert.Equal(1, result.Entry.RIR);
		Assert.NotNull(result.Entry.ConcentricTimeSeconds);
		Assert.Equal(1.15, result.Entry.ConcentricTimeSeconds!.Value, precision: 2);
	}

	[Fact]
	public void BuildPrEntry_ForStrengthMovement_WithInvalidRir_Fails()
	{
		PrEntryBuildResult result = CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = 42,
			ExerciseName = "Bench Press",
			ExerciseCategory = "Push",
			TrackingMode = ExerciseTrackingMode.Strength,
			WeightText = "120",
			RepsText = "3",
			RirText = "8"
		});

		Assert.False(result.IsValid);
		Assert.Equal("RIR must be between 0 - 5.", result.ErrorMessage);
	}

	[Fact]
	public void BuildPrEntry_ForCustomMovement_WithSecondaryMetricAndGct_ConvertsSecondsToMilliseconds()
	{
		PrEntryBuildResult result = CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = 8,
			ExerciseName = "Flying Sprint",
			ExerciseCategory = "Sprint",
			TrackingMode = ExerciseTrackingMode.Custom,
			PrimaryLabel = "Distance",
			PrimaryUnit = "m",
			SecondaryLabel = "Time",
			SecondaryUnit = "s",
			HasSecondaryMetric = true,
			SupportsGroundContactTime = true,
			Metric1Text = "20",
			Metric2Text = "1.96",
			GroundContactTimeText = "0.18"
		});

		Assert.True(result.IsValid);
		Assert.NotNull(result.Entry);
		Assert.Equal(nameof(ExerciseTrackingMode.Custom), result.Entry!.TrackingMode);
		Assert.NotNull(result.Entry.Metric1Value);
		Assert.Equal(20, result.Entry.Metric1Value!.Value, precision: 2);
		Assert.Equal("m", result.Entry.Metric1Unit);
		Assert.NotNull(result.Entry.Metric2Value);
		Assert.Equal(1.96, result.Entry.Metric2Value!.Value, precision: 2);
		Assert.Equal("s", result.Entry.Metric2Unit);
		Assert.NotNull(result.Entry.GroundContactTimeMs);
		Assert.Equal(180, result.Entry.GroundContactTimeMs!.Value, precision: 2);
	}

	[Fact]
	public void BuildPrEntry_ForCustomMovement_WithoutRequiredSecondaryMetric_Fails()
	{
		PrEntryBuildResult result = CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = 8,
			ExerciseName = "Flying Sprint",
			ExerciseCategory = "Sprint",
			TrackingMode = ExerciseTrackingMode.Custom,
			PrimaryLabel = "Distance",
			PrimaryUnit = "m",
			SecondaryLabel = "Time",
			SecondaryUnit = "s",
			HasSecondaryMetric = true,
			Metric1Text = "20",
			Metric2Text = ""
		});

		Assert.False(result.IsValid);
		Assert.Equal("Time is required.", result.ErrorMessage);
	}

	[Fact]
	public void BuildPrEntry_ForCustomMovement_WithInvalidGroundContactTime_Fails()
	{
		PrEntryBuildResult result = CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = 8,
			ExerciseName = "Hurdle Hop",
			ExerciseCategory = "Plyometrics",
			TrackingMode = ExerciseTrackingMode.Custom,
			PrimaryLabel = "Reps",
			PrimaryUnit = "count",
			SupportsGroundContactTime = true,
			Metric1Text = "6",
			GroundContactTimeText = "-0.2"
		});

		Assert.False(result.IsValid);
		Assert.Equal("Ground contact time must be a positive number.", result.ErrorMessage);
	}

	[Fact]
	public void BuildPrEntry_WithoutMovementName_Fails()
	{
		PrEntryBuildResult result = CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = 99,
			ExerciseName = "",
			TrackingMode = ExerciseTrackingMode.Strength
		});

		Assert.False(result.IsValid);
		Assert.Equal("Choose a movement before saving.", result.ErrorMessage);
	}

	[Fact]
	public void BuildOneRmSecondaryText_FormatsRepRangeSummary()
	{
		SetLanguageAndCulture("en-US");

		string result = CalculationsPageLogic.BuildOneRmSecondaryText(120);

		Assert.Equal("2RM 112.5 kg · 5RM 102.9 kg · 8RM 94.7 kg", result);
	}

	[Fact]
	public void BuildRsiSecondaryText_UsesCurrentCultureFormatting()
	{
		SetLanguageAndCulture("tr-TR");

		string result = CalculationsPageLogic.BuildRsiSecondaryText(42.5, 0.21);

		Assert.Equal("Sıçrama Yüksekliği (cm): 42,5 cm · YTS (s): 0,21 s", result);
	}

	[Fact]
	public void BuildFfmiSecondaryText_UsesAdjustedLabels_And_LocalizedNumbers()
	{
		SetLanguageAndCulture("tr-TR");

		string result = CalculationsPageLogic.BuildFfmiSecondaryText(21.46, 72.1);

		Assert.Equal("Ham FFMI: 21,5 · Yağsız Kütle (kg): 72,1 kg", result);
		Assert.Equal("Düzeltilmiş FFMI", AppLanguage.CalcFfmiNormalized);
	}

	private static void SetLanguageAndCulture(string cultureName)
	{
		var culture = new CultureInfo(cultureName);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;

		string code = cultureName.StartsWith("tr", StringComparison.OrdinalIgnoreCase) ? "tr" : "en";
		typeof(AppLanguage)
			.GetProperty(nameof(AppLanguage.Code))!
			.GetSetMethod(true)!
			.Invoke(null, [code]);
	}
}
