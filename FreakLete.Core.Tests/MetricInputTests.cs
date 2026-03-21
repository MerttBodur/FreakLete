using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class MetricInputTests
{
	[Theory]
	[InlineData("1.96", 1.96)]
	[InlineData("1,96", 1.96)]
	[InlineData(" 2.5 ", 2.5)]
	[InlineData("1,234.56", 1234.56)]
	[InlineData("1.234,56", 1234.56)]
	public void TryParseFlexibleDouble_ValidInputs_ReturnsExpectedValue(string text, double expected)
	{
		bool parsed = MetricInput.TryParseFlexibleDouble(text, out double value);

		Assert.True(parsed);
		Assert.Equal(expected, value, precision: 2);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("abc")]
	public void TryParseFlexibleDouble_InvalidInputs_ReturnsFalse(string text)
	{
		bool parsed = MetricInput.TryParseFlexibleDouble(text, out double value);

		Assert.False(parsed);
		Assert.Equal(0, value);
	}

	[Fact]
	public void SecondsToMilliseconds_ConvertsCorrectly()
	{
		double milliseconds = MetricInput.SecondsToMilliseconds(1.96);

		Assert.Equal(1960, milliseconds, precision: 2);
	}

	[Fact]
	public void MillisecondsToSeconds_ConvertsCorrectly()
	{
		double seconds = MetricInput.MillisecondsToSeconds(1960);

		Assert.Equal(1.96, seconds, precision: 2);
	}

	[Fact]
	public void FormatSecondsFromMilliseconds_FormatsExpectedText()
	{
		string result = MetricInput.FormatSecondsFromMilliseconds(1960);

		Assert.Equal("1.96 s", result);
	}
}
