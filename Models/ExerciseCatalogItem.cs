using System.Text.Json.Serialization;

namespace FreakLete.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExerciseTrackingMode
{
	Strength,
	Custom
}

public sealed class ExerciseCatalogItem
{
	public string Id { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public string TurkishName { get; init; } = string.Empty;

	public string EnglishName { get; init; } = string.Empty;

	[JsonPropertyName("appCategory")]
	public string Category { get; init; } = string.Empty;

	[JsonPropertyName("category")]
	public string SourceSection { get; init; } = string.Empty;

	public string Force { get; init; } = string.Empty;

	public string Level { get; init; } = string.Empty;

	public string Mechanic { get; init; } = string.Empty;

	public string Equipment { get; init; } = string.Empty;

	public List<string> PrimaryMuscles { get; init; } = [];

	public List<string> SecondaryMuscles { get; init; } = [];

	public List<string> Instructions { get; init; } = [];

	public ExerciseTrackingMode TrackingMode { get; init; } = ExerciseTrackingMode.Strength;

	public string PrimaryLabel { get; init; } = string.Empty;

	public string PrimaryUnit { get; init; } = string.Empty;

	public string SecondaryLabel { get; init; } = string.Empty;

	public string SecondaryUnit { get; init; } = string.Empty;

	public bool SupportsGroundContactTime { get; init; }

	public bool SupportsConcentricTime { get; init; }

	public string MovementPattern { get; init; } = string.Empty;

	public string AthleticQuality { get; init; } = string.Empty;

	public string SportRelevance { get; init; } = string.Empty;

	public string NervousSystemLoad { get; init; } = string.Empty;

	public string GctProfile { get; init; } = string.Empty;

	public string LoadPrescription { get; init; } = string.Empty;

	public string CommonMistakes { get; init; } = string.Empty;

	public string Progression { get; init; } = string.Empty;

	public string Regression { get; init; } = string.Empty;

	public int RecommendedRank { get; init; }

	public string? MediaUrl { get; init; }

	public string? ThumbnailUrl { get; init; }

	public bool HasMedia => !string.IsNullOrWhiteSpace(MediaUrl);

	public bool HasSecondaryMetric => !string.IsNullOrWhiteSpace(SecondaryLabel);

	public string HintText =>
		TrackingMode == ExerciseTrackingMode.Strength
			? "Sets, reps, RIR, rest"
			: HasSecondaryMetric
				? $"{PrimaryLabel} ({PrimaryUnit}) + {SecondaryLabel} ({SecondaryUnit})"
				: $"{PrimaryLabel} ({PrimaryUnit})";

	public string MuscleSummary =>
		PrimaryMuscles.Count > 0
			? string.Join(", ", PrimaryMuscles.Take(3))
			: "General athletic output";

	public string DetailSummary
	{
		get
		{
			List<string> parts = [$"Primary: {MuscleSummary}"];

			if (!string.IsNullOrWhiteSpace(NervousSystemLoad))
			{
				parts.Add($"CNS: {NervousSystemLoad}");
			}

			if (!string.IsNullOrWhiteSpace(Equipment))
			{
				parts.Add(Equipment);
			}

			return string.Join(" • ", parts);
		}
	}

	public string SelectionHintText
	{
		get
		{
			List<string> parts = [$"{Category} | {HintText}"];

			if (!string.IsNullOrWhiteSpace(MuscleSummary))
			{
				parts.Add($"Muscles: {MuscleSummary}");
			}

			if (!string.IsNullOrWhiteSpace(NervousSystemLoad))
			{
				parts.Add($"CNS: {NervousSystemLoad}");
			}

			return string.Join(" | ", parts);
		}
	}
}
