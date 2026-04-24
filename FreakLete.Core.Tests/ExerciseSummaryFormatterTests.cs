using FreakLete.Helpers;
using FreakLete.Models;

namespace FreakLete.Core.Tests;

public class ExerciseSummaryFormatterTests
{
	[Fact]
	public void Strength_WithVaryingWeights_ListsAllSets()
	{
		var entry = new ExerciseEntry
		{
			SetsCount = 3,
			Reps = 5,
			Sets =
			[
				new SetDetail { SetNumber = 1, Reps = 5, Weight = 90 },
				new SetDetail { SetNumber = 2, Reps = 5, Weight = 110 },
				new SetDetail { SetNumber = 3, Reps = 5, Weight = 120 }
			]
		};

		Assert.Equal("90×5  110×5  120×5", ExerciseSummaryFormatter.FormatStrength(entry));
	}

	[Fact]
	public void Strength_WithRir_AppendsRir()
	{
		var entry = new ExerciseEntry
		{
			Sets = [new SetDetail { SetNumber = 1, Reps = 5, Weight = 90 }],
			RIR = 2
		};

		Assert.Equal("90×5 (RIR2)", ExerciseSummaryFormatter.FormatStrength(entry));
	}

	[Fact]
	public void Strength_LegacyEntryWithoutSets_FallsBackToMetric()
	{
		var entry = new ExerciseEntry
		{
			Sets = [],
			SetsCount = 4,
			Reps = 5,
			Metric1Value = 90,
			Metric1Unit = "kg"
		};

		Assert.Equal("4 x 5 @ 90 kg", ExerciseSummaryFormatter.FormatStrength(entry));
	}
}
