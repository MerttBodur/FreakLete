using System.Globalization;
using System.Text;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace FreakLete.Api.Services.Rag;

public sealed class ContextBuilder : IContextBuilder
{
    private readonly AppDbContext _db;
    private readonly GeminiClient _gemini;
    private readonly ILogger<ContextBuilder> _logger;

    public ContextBuilder(AppDbContext db, GeminiClient gemini, ILogger<ContextBuilder> logger)
    {
        _db = db;
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<FreakAiContext?> BuildAsync(
        int userId,
        string intent,
        string userMessage,
        CancellationToken ct = default)
    {
        try
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
            {
                return null;
            }

            return intent switch
            {
                FreakAiUsageIntent.ProgramGenerate => await BuildProgramGenerateAsync(user, ct),
                FreakAiUsageIntent.ProgramAnalyze => await BuildProgramAnalyzeAsync(user, userMessage, ct),
                FreakAiUsageIntent.NutritionGuidance => BuildNutritionGuidance(user),
                _ => BuildGeneralChat(user)
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ContextBuilder failed for user {UserId}, intent {Intent}; falling back to static prompt",
                userId,
                intent);
            return null;
        }
    }

    private async Task<FreakAiContext> BuildProgramGenerateAsync(User user, CancellationToken ct)
    {
        var program = await GetActiveProgramAsync(user.Id, ct);
        var prSummary = await GetRecentPrSummaryAsync(user.Id, ct);
        var snapshot = await GetUserSnapshotContextAsync(user.Id, ct);

        return new FreakAiContext
        {
            UserProfile = FormatProfile(user, includeBody: true),
            Goals = FormatGoals(user),
            Equipment = NullIfEmpty(user.AvailableEquipment),
            PhysicalLimitations = FormatLimitations(user),
            CurrentProgram = program,
            RecentPrSummary = prSummary,
            UserSnapshotContext = snapshot
        };
    }

    private async Task<FreakAiContext> BuildProgramAnalyzeAsync(
        User user,
        string userMessage,
        CancellationToken ct)
    {
        var program = await GetActiveProgramAsync(user.Id, ct);
        var prSummary = await GetRecentPrSummaryAsync(user.Id, ct);
        var similar = await GetSimilarWorkoutsAsync(user.Id, userMessage, ct);

        return new FreakAiContext
        {
            UserProfile = FormatProfile(user, includeBody: false),
            Goals = FormatGoals(user),
            CurrentProgram = program,
            RecentPrSummary = prSummary,
            SimilarWorkouts = similar
        };
    }

    private static FreakAiContext BuildNutritionGuidance(User user) => new()
    {
        UserProfile = FormatProfileMinimalNutrition(user),
        Goals = FormatGoals(user)
    };

    private static FreakAiContext BuildGeneralChat(User user) => new()
    {
        UserProfile = FormatProfileMinimal(user),
        Goals = FormatGoals(user)
    };

    private async Task<List<string>> GetSimilarWorkoutsAsync(
        int userId,
        string userMessage,
        CancellationToken ct)
    {
        var floats = await _gemini.EmbedAsync(userMessage, ct);
        if (floats is null || floats.Length != 768)
        {
            return [];
        }

        var query = new Vector(floats);

        try
        {
            return await _db.WorkoutEmbeddings
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.Embedding.CosineDistance(query))
                .Take(3)
                .Select(e => e.TextSnapshot)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workout similarity query failed for user {UserId}", userId);
            return [];
        }
    }

    private async Task<string?> GetUserSnapshotContextAsync(int userId, CancellationToken ct)
    {
        try
        {
            var snapshot = await _db.UserSnapshotEmbeddings
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => e.TextSnapshot)
                .FirstOrDefaultAsync(ct);
            return NullIfEmpty(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "User snapshot context fetch failed for user {UserId}", userId);
            return null;
        }
    }

    private async Task<string?> GetActiveProgramAsync(int userId, CancellationToken ct)
    {
        var program = await _db.TrainingPrograms
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == "active")
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new { p.Name, p.Goal, p.DaysPerWeek })
            .FirstOrDefaultAsync(ct);

        return program is null
            ? null
            : $"{program.Name} | Goal: {program.Goal} | {program.DaysPerWeek.ToString(CultureInfo.InvariantCulture)} days/week";
    }

    private async Task<string?> GetRecentPrSummaryAsync(int userId, CancellationToken ct)
    {
        var prs = await _db.PrEntries
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new { p.ExerciseName, p.Weight, p.Reps })
            .ToListAsync(ct);

        return prs.Count == 0
            ? null
            : string.Join(", ", prs.Select(p =>
                $"{p.ExerciseName} {p.Weight.ToString(CultureInfo.InvariantCulture)}kg x{p.Reps.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static string FormatProfile(User user, bool includeBody)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(user.SportName, "Unknown"));
        sb.Append(" | Position: ").Append(NonEmpty(user.Position, "Unknown"));
        sb.Append(" | Experience: ").Append(NonEmpty(user.GymExperienceLevel, "Unknown"));

        if (includeBody)
        {
            if (user.WeightKg.HasValue)
            {
                sb.Append(" | Weight: ").Append(user.WeightKg.Value.ToString("0.#", CultureInfo.InvariantCulture)).Append("kg");
            }

            if (user.HeightCm.HasValue)
            {
                sb.Append(" | Height: ").Append(user.HeightCm.Value.ToString("0.#", CultureInfo.InvariantCulture)).Append("cm");
            }

            if (user.BodyFatPercentage.HasValue)
            {
                sb.Append(" | Body Fat: ").Append(user.BodyFatPercentage.Value.ToString("0.#", CultureInfo.InvariantCulture)).Append('%');
            }
        }

        return sb.ToString();
    }

    private static string FormatProfileMinimal(User user)
        => $"Sport: {NonEmpty(user.SportName, "Unknown")} | Position: {NonEmpty(user.Position, "Unknown")}";

    private static string FormatProfileMinimalNutrition(User user)
    {
        var sb = new StringBuilder();
        sb.Append("Sport: ").Append(NonEmpty(user.SportName, "Unknown"));

        if (user.WeightKg.HasValue)
        {
            sb.Append(" | Weight: ").Append(user.WeightKg.Value.ToString("0.#", CultureInfo.InvariantCulture)).Append("kg");
        }

        if (user.BodyFatPercentage.HasValue)
        {
            sb.Append(" | Body Fat: ").Append(user.BodyFatPercentage.Value.ToString("0.#", CultureInfo.InvariantCulture)).Append('%');
        }

        if (!string.IsNullOrWhiteSpace(user.DietaryPreference))
        {
            sb.Append(" | Diet: ").Append(user.DietaryPreference);
        }

        return sb.ToString();
    }

    private static string FormatGoals(User user)
    {
        var primary = NonEmpty(user.PrimaryTrainingGoal, "Not set");
        var secondary = NonEmpty(user.SecondaryTrainingGoal, "None");
        return $"Primary: {primary} | Secondary: {secondary}";
    }

    private static string? FormatLimitations(User user)
    {
        var limitations = NullIfEmpty(user.PhysicalLimitations);
        var pain = NullIfEmpty(user.CurrentPainPoints);
        var injury = NullIfEmpty(user.InjuryHistory);

        if (limitations is null && pain is null && injury is null)
        {
            return null;
        }

        return $"Limitations: {limitations ?? "None"} | Current pain: {pain ?? "None"} | Injury history: {injury ?? "None"}";
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string NonEmpty(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
