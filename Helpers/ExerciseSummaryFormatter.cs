using FreakLete.Models;

namespace FreakLete.Helpers;

public static class ExerciseSummaryFormatter
{
	public static string FormatStrength(ExerciseEntry entry)
	{
		if (entry.Sets.Count > 0)
		{
			var parts = entry.Sets
				.OrderBy(s => s.SetNumber)
				.Select(s => s.Weight.HasValue ? $"{s.Weight:0.#}×{s.Reps}" : $"{s.Reps}");

			string core = string.Join("  ", parts);
			if (entry.RIR.HasValue)
			{
				core += $" (RIR{entry.RIR.Value})";
			}

			return core;
		}

		string fallback = entry.RIR.HasValue
			? $"{entry.SetsCount} x {entry.Reps} (RIR{entry.RIR.Value})"
			: $"{entry.SetsCount} x {entry.Reps}";

		if (entry.Metric1Value is > 0 && !string.IsNullOrEmpty(entry.Metric1Unit))
		{
			fallback += $" @ {entry.Metric1Value:0.#} {entry.Metric1Unit}";
		}

		return fallback;
	}
}
