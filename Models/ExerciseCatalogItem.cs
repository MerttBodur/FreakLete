namespace GymTracker.Models;

public enum ExerciseTrackingMode
{
	Strength,
	Custom
}

public sealed class ExerciseCatalogItem
{
	public string Name { get; init; } = string.Empty;

	public string Category { get; init; } = string.Empty;

	public ExerciseTrackingMode TrackingMode { get; init; } = ExerciseTrackingMode.Strength;

	public string PrimaryLabel { get; init; } = string.Empty;

	public string PrimaryUnit { get; init; } = string.Empty;

	public string SecondaryLabel { get; init; } = string.Empty;

	public string SecondaryUnit { get; init; } = string.Empty;

	public bool HasSecondaryMetric => !string.IsNullOrWhiteSpace(SecondaryLabel);

	public string HintText =>
		TrackingMode == ExerciseTrackingMode.Strength
			? "Sets, reps, RIR, rest"
			: HasSecondaryMetric
				? $"{PrimaryLabel} ({PrimaryUnit}) + {SecondaryLabel} ({SecondaryUnit})"
				: $"{PrimaryLabel} ({PrimaryUnit})";
}
