using FreakLete.Models;

namespace FreakLete.Services;

public static class CalculationsPageLogic
{
	public static OneRmInputParseResult ParseOneRmInputs(
		string? weightText,
		string? repsText,
		string? rirText,
		string? concentricTimeText)
	{
		if (!int.TryParse(weightText, out int weightKg) ||
			!int.TryParse(repsText, out int reps) ||
			!int.TryParse(rirText, out int rir))
		{
			return OneRmInputParseResult.Failure("Please enter numbers only.");
		}

		if (weightKg < 40 || weightKg > 250)
		{
			return OneRmInputParseResult.Failure("Weight must be between 40 kg - 250 kg.");
		}

		if (reps < 1 || reps > 8)
		{
			return OneRmInputParseResult.Failure("Reps must be between 1 - 8.");
		}

		if (rir < 0 || rir > 5)
		{
			return OneRmInputParseResult.Failure("RIR must be between 0 - 5.");
		}

		double? concentricTime = null;
		if (!string.IsNullOrWhiteSpace(concentricTimeText))
		{
			if (!MetricInput.TryParseFlexibleDouble(concentricTimeText, out double parsedTime) || parsedTime <= 0)
			{
				return OneRmInputParseResult.Failure("Concentric time must be a positive number.");
			}

			concentricTime = parsedTime;
		}

		return OneRmInputParseResult.Success(weightKg, reps, rir, concentricTime);
	}

	public static PrEntryBuildResult BuildPrEntry(PrEntryBuildRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.ExerciseName))
		{
			return PrEntryBuildResult.Failure("Choose a movement before saving.");
		}

		if (request.TrackingMode == ExerciseTrackingMode.Strength)
		{
			if (!int.TryParse(request.WeightText, out int weightKg) || weightKg <= 0)
			{
				return PrEntryBuildResult.Failure("Weight must be a positive number.");
			}

			if (!int.TryParse(request.RepsText, out int reps) || reps <= 0)
			{
				return PrEntryBuildResult.Failure("Reps must be a positive number.");
			}

			int? rir = null;
			if (!string.IsNullOrWhiteSpace(request.RirText))
			{
				if (!int.TryParse(request.RirText, out int parsedRir) || parsedRir < 0 || parsedRir > 5)
				{
					return PrEntryBuildResult.Failure("RIR must be between 0 - 5.");
				}

				rir = parsedRir;
			}

			double? concentricTime = null;
			if (!string.IsNullOrWhiteSpace(request.ConcentricTimeText))
			{
				if (!MetricInput.TryParseFlexibleDouble(request.ConcentricTimeText, out double parsedTime) || parsedTime <= 0)
				{
					return PrEntryBuildResult.Failure("Concentric time must be a positive number.");
				}

				concentricTime = parsedTime;
			}

			return PrEntryBuildResult.Success(new PrEntry
			{
				UserId = request.UserId,
				ExerciseName = request.ExerciseName,
				ExerciseCategory = request.ExerciseCategory,
				TrackingMode = nameof(ExerciseTrackingMode.Strength),
				Weight = weightKg,
				Reps = reps,
				RIR = rir,
				ConcentricTimeSeconds = concentricTime
			});
		}

		if (!MetricInput.TryParseFlexibleDouble(request.Metric1Text, out double metric1) || metric1 <= 0)
		{
			return PrEntryBuildResult.Failure($"{request.PrimaryLabel} is required.");
		}

		double? metric2 = null;
		if (request.HasSecondaryMetric)
		{
			if (!MetricInput.TryParseFlexibleDouble(request.Metric2Text, out double parsedMetric2) || parsedMetric2 <= 0)
			{
				return PrEntryBuildResult.Failure($"{request.SecondaryLabel} is required.");
			}

			metric2 = parsedMetric2;
		}

		double? gct = null;
		if (request.SupportsGroundContactTime && !string.IsNullOrWhiteSpace(request.GroundContactTimeText))
		{
			if (!MetricInput.TryParseFlexibleDouble(request.GroundContactTimeText, out double parsedGctSeconds) || parsedGctSeconds <= 0)
			{
				return PrEntryBuildResult.Failure("Ground contact time must be a positive number.");
			}

			gct = MetricInput.SecondsToMilliseconds(parsedGctSeconds);
		}

		return PrEntryBuildResult.Success(new PrEntry
		{
			UserId = request.UserId,
			ExerciseName = request.ExerciseName,
			ExerciseCategory = request.ExerciseCategory,
			TrackingMode = nameof(ExerciseTrackingMode.Custom),
			Metric1Value = metric1,
			Metric1Unit = request.PrimaryUnit,
			Metric2Value = metric2,
			Metric2Unit = request.SecondaryUnit,
			GroundContactTimeMs = gct
		});
	}
}

public sealed record OneRmInputParseResult(
	bool IsValid,
	string ErrorMessage,
	int WeightKg,
	int Reps,
	int Rir,
	double? ConcentricTimeSeconds)
{
	public static OneRmInputParseResult Success(int weightKg, int reps, int rir, double? concentricTimeSeconds)
	{
		return new OneRmInputParseResult(true, string.Empty, weightKg, reps, rir, concentricTimeSeconds);
	}

	public static OneRmInputParseResult Failure(string errorMessage)
	{
		return new OneRmInputParseResult(false, errorMessage, 0, 0, 0, null);
	}
}

public sealed record PrEntryBuildRequest
{
	public int UserId { get; init; }
	public string ExerciseName { get; init; } = string.Empty;
	public string ExerciseCategory { get; init; } = string.Empty;
	public ExerciseTrackingMode TrackingMode { get; init; } = ExerciseTrackingMode.Strength;
	public string PrimaryLabel { get; init; } = string.Empty;
	public string PrimaryUnit { get; init; } = string.Empty;
	public string SecondaryLabel { get; init; } = string.Empty;
	public string SecondaryUnit { get; init; } = string.Empty;
	public bool HasSecondaryMetric { get; init; }
	public bool SupportsGroundContactTime { get; init; }
	public string? WeightText { get; init; }
	public string? RepsText { get; init; }
	public string? RirText { get; init; }
	public string? ConcentricTimeText { get; init; }
	public string? Metric1Text { get; init; }
	public string? Metric2Text { get; init; }
	public string? GroundContactTimeText { get; init; }
}

public sealed record PrEntryBuildResult(bool IsValid, string ErrorMessage, PrEntry? Entry)
{
	public static PrEntryBuildResult Success(PrEntry entry)
	{
		return new PrEntryBuildResult(true, string.Empty, entry);
	}

	public static PrEntryBuildResult Failure(string errorMessage)
	{
		return new PrEntryBuildResult(false, errorMessage, null);
	}
}
