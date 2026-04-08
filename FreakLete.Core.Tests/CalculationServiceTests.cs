using FreakLete.Services;

namespace FreakLete.Core.Tests;

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

	[Fact]
	public void CalculateFfmi_ReturnsExpectedValues()
	{
		// 80 kg, 178 cm, 15% BF
		// LBM = 80 * (1 - 0.15) = 68
		// rawFFMI = 68 / (1.78^2) = 68 / 3.1684 = 21.46
		// normalizedFFMI = 21.46 + 6.1 * (1.8 - 1.78) = 21.46 + 0.122 = 21.58
		var (lbm, raw, normalized) = CalculationService.CalculateFfmi(80, 178, 15);

		Assert.Equal(68.0, lbm, precision: 1);
		Assert.Equal(21.46, raw, precision: 1);
		Assert.Equal(21.58, normalized, precision: 1);
	}

	[Fact]
	public void CalculateFfmi_TallPerson_NormalizationReduces()
	{
		// 90 kg, 195 cm, 12% BF → normalized < raw for tall people
		var (lbm, raw, normalized) = CalculationService.CalculateFfmi(90, 195, 12);

		Assert.True(normalized < raw);
		Assert.Equal(79.2, lbm, precision: 1);
	}

	[Theory]
	[InlineData(0, 178, 15)]
	[InlineData(80, 0, 15)]
	[InlineData(80, 178, -1)]
	[InlineData(80, 178, 100)]
	public void CalculateFfmi_InvalidInput_Throws(double weight, double height, double bodyFat)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => CalculationService.CalculateFfmi(weight, height, bodyFat));
	}
}
