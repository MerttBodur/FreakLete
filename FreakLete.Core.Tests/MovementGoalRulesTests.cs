using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class MovementGoalRulesTests
{
	[Theory]
	[InlineData("Sprint", "Sprint", true)]
	[InlineData("sprint", "Sprint", true)]
	[InlineData("", "Sprint", true)]
	[InlineData(null, "Sprint", true)]
	[InlineData("Jumps", "Sprint", false)]
	public void MatchesCategory_ReturnsExpectedResult(string? existingCategory, string requestedCategory, bool expected)
	{
		bool result = MovementGoalRules.MatchesCategory(existingCategory, requestedCategory);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ResolveGoalLabel_SprintWithSecondaryMetric_UsesSecondaryLabel()
	{
		string label = MovementGoalRules.ResolveGoalLabel("Sprint", true, "Distance", "Time");

		Assert.Equal("Time", label);
	}

	[Fact]
	public void ResolveGoalLabel_NonSprint_UsesPrimaryLabel()
	{
		string label = MovementGoalRules.ResolveGoalLabel("Push", true, "Load", "Time");

		Assert.Equal("Load", label);
	}

	[Fact]
	public void ResolveGoalUnit_SprintWithSecondaryMetric_UsesSecondaryUnit()
	{
		string unit = MovementGoalRules.ResolveGoalUnit("Sprint", true, "m", "s");

		Assert.Equal("s", unit);
	}

	[Fact]
	public void ResolveGoalUnit_NonSprint_UsesPrimaryUnit()
	{
		string unit = MovementGoalRules.ResolveGoalUnit("Olympic Lifts", false, "kg", "");

		Assert.Equal("kg", unit);
	}
}
