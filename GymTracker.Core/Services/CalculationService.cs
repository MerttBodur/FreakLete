namespace GymTracker.Services;

public static class CalculationService
{
	public static double CalculateOneRm(int weightKg, int reps, int rir)
	{
		if (weightKg <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(weightKg));
		}

		if (reps <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(reps));
		}

		if (rir < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(rir));
		}

		int estimatedMaxReps = reps + rir;
		return weightKg * (1 + (estimatedMaxReps / 30.0));
	}

	public static double CalculateRmFromOneRm(double oneRm, int targetRm)
	{
		if (oneRm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(oneRm));
		}

		if (targetRm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(targetRm));
		}

		return oneRm / (1 + (targetRm / 30.0));
	}

	public static IReadOnlyList<double> BuildRmTable(double oneRm, int maxRm = 8)
	{
		if (maxRm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxRm));
		}

		List<double> values = [];
		for (int rm = 1; rm <= maxRm; rm++)
		{
			values.Add(CalculateRmFromOneRm(oneRm, rm));
		}

		return values;
	}

	public static double CalculateRsi(double jumpHeightCm, double gctSeconds)
	{
		if (jumpHeightCm <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(jumpHeightCm));
		}

		if (gctSeconds <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(gctSeconds));
		}

		return (jumpHeightCm / 100.0) / gctSeconds;
	}
}
