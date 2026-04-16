using FreakLete.Api.DTOs.Tier;

namespace FreakLete.Api.Services;

public interface IExerciseTierService
{
    Task<TierResultDto?> RecalculateTierAsync(
        int userId,
        string? catalogId,
        string exerciseName,
        string trackingMode,
        int weight,
        int reps,
        int? rir,
        double? athleticRawValue,
        CancellationToken ct = default);

    Task<List<ExerciseTierDto>> GetTiersForUserAsync(int userId, CancellationToken ct = default);
}
