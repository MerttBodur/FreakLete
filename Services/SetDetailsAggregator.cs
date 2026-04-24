using FreakLete.Models;

namespace FreakLete.Services;

public static class SetDetailsAggregator
{
    public readonly record struct AggregatedResult(int Sets, int Reps, double? MaxWeight);

    public static AggregatedResult Aggregate(IReadOnlyList<SetDetail> sets)
    {
        if (sets.Count == 0)
            throw new ArgumentException("Set list must not be empty.", nameof(sets));

        int lastReps = sets[^1].Reps;

        double? maxWeight = null;
        foreach (var s in sets)
        {
            if (s.Weight.HasValue && (maxWeight is null || s.Weight.Value > maxWeight.Value))
                maxWeight = s.Weight.Value;
        }

        return new AggregatedResult(sets.Count, lastReps, maxWeight);
    }
}
