using GymTracker.Services;

namespace GymTracker.Core.Tests;

public class CalculationServiceTests
{
	[Fact]
	public void CalculateOneRm_UsesEpleyWithRir()
	{
		double oneRm = CalculationService.CalculateOneRm(100, 5, 1);

		Assert.Equal(120, oneRm, precision: 2);
	}

	[Fact]
	public void CalculateRmFromOneRm_ReturnsExpectedValue()
	{
		double rm = CalculationService.CalculateRmFromOneRm(120, 5);

		Assert.Equal(102.86, rm, precision: 2);
	}

	[Fact]
	public void BuildRmTable_ReturnsEightValuesByDefault()
	{
		IReadOnlyList<double> values = CalculationService.BuildRmTable(120);

		Assert.Equal(8, values.Count);
		Assert.Equal(116.13, values[0], precision: 2);
		Assert.Equal(94.74, values[7], precision: 2);
	}

	[Fact]
	public void CalculateRsi_ReturnsExpectedValue()
	{
		double rsi = CalculationService.CalculateRsi(55, 0.20);

		Assert.Equal(2.75, rsi, precision: 2);
	}

	[Theory]
	[InlineData(0, 5, 1)]
	[InlineData(100, 0, 1)]
	[InlineData(100, 5, -1)]
	public void CalculateOneRm_InvalidInput_Throws(int weightKg, int reps, int rir)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CalculationService.CalculateOneRm(weightKg, reps, rir));
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(120, 0)]
	public void CalculateRmFromOneRm_InvalidInput_Throws(double oneRm, int targetRm)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CalculationService.CalculateRmFromOneRm(oneRm, targetRm));
	}

	[Theory]
	[InlineData(0, 0.20)]
	[InlineData(55, 0)]
	public void CalculateRsi_InvalidInput_Throws(double jumpHeightCm, double gctSeconds)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CalculationService.CalculateRsi(jumpHeightCm, gctSeconds));
	}
}
