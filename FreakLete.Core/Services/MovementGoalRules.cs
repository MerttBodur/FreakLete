namespace FreakLete.Services;

public static class MovementGoalRules
{
	public const string SprintCategory = "Sprint";

	public static bool MatchesCategory(string? existingCategory, string? requestedCategory)
	{
		return string.Equals(existingCategory, requestedCategory, StringComparison.OrdinalIgnoreCase) ||
			   string.IsNullOrWhiteSpace(existingCategory);
	}

	public static string ResolveGoalLabel(
		string category,
		bool hasSecondaryMetric,
		string primaryLabel,
		string secondaryLabel)
	{
		return category == SprintCategory && hasSecondaryMetric
			? secondaryLabel
			: primaryLabel;
	}

	public static string ResolveGoalUnit(
		string category,
		bool hasSecondaryMetric,
		string primaryUnit,
		string secondaryUnit)
	{
		return category == SprintCategory && hasSecondaryMetric
			? secondaryUnit
			: primaryUnit;
	}
}
