using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete.Helpers;

public static class ProgramExerciseConverter
{
	public static ExerciseEntry Convert(ProgramExerciseResponse pe)
	{
		var catalogItem = ExerciseCatalog.GetByName(pe.ExerciseName);
		string category = !string.IsNullOrWhiteSpace(pe.ExerciseCategory)
			? pe.ExerciseCategory
			: catalogItem?.Category ?? "Push";

		string trackingMode = catalogItem is not null
			? catalogItem.TrackingMode.ToString()
			: nameof(ExerciseTrackingMode.Strength);

		int reps = ParseReps(pe.RepsOrDuration);

		return new ExerciseEntry
		{
			ExerciseName = pe.ExerciseName,
			ExerciseCategory = category,
			TrackingMode = trackingMode,
			Sets = pe.Sets,
			Reps = reps,
			RestSeconds = pe.RestSeconds,
			RIR = null
		};
	}

	public static List<ExerciseEntry> ConvertAll(List<ProgramExerciseResponse> exercises)
	{
		return exercises.OrderBy(e => e.Order).Select(Convert).ToList();
	}

	/// <summary>
	/// Parses RepsOrDuration strings like "8-10", "5x5", "30s", "AMRAP", "12".
	/// Returns the lower bound for ranges, 0 for duration/AMRAP.
	/// </summary>
	public static int ParseReps(string? repsOrDuration)
	{
		if (string.IsNullOrWhiteSpace(repsOrDuration)) return 0;

		var text = repsOrDuration.Trim();

		// "AMRAP" or similar
		if (text.Contains("AMRAP", StringComparison.OrdinalIgnoreCase)) return 0;

		// Duration like "30s", "60sec"
		if (text.EndsWith('s') || text.EndsWith("sec", StringComparison.OrdinalIgnoreCase)) return 0;

		// Range like "8-10"
		if (text.Contains('-'))
		{
			var parts = text.Split('-');
			if (int.TryParse(parts[0].Trim(), out int lower)) return lower;
		}

		// "5x5" format
		if (text.Contains('x', StringComparison.OrdinalIgnoreCase))
		{
			var parts = text.Split('x', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int reps)) return reps;
		}

		// Plain number
		if (int.TryParse(text, out int plain)) return plain;

		return 0;
	}

	/// <summary>
	/// Builds a template hint string like "3 x 8-10 @ RPE 7, Rest: 90s"
	/// </summary>
	public static string BuildTemplateHint(ProgramExerciseResponse pe)
	{
		var parts = new List<string>();
		if (pe.Sets > 0 && !string.IsNullOrWhiteSpace(pe.RepsOrDuration))
			parts.Add($"{pe.Sets} x {pe.RepsOrDuration}");

		if (!string.IsNullOrWhiteSpace(pe.IntensityGuidance))
			parts.Add($"@ {pe.IntensityGuidance}");

		if (pe.RestSeconds.HasValue && pe.RestSeconds.Value > 0)
			parts.Add($"Rest: {pe.RestSeconds.Value}s");

		return parts.Count > 0 ? string.Join(", ", parts) : "";
	}
}
