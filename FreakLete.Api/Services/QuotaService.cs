using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

/// <summary>
/// Enforces FreakAI usage quotas per plan and intent.
/// Backend is source of truth — checked before every Gemini call.
/// </summary>
public class QuotaService
{
    private readonly AppDbContext _db;
    private readonly EntitlementService _entitlement;

    public QuotaService(AppDbContext db, EntitlementService entitlement)
    {
        _db = db;
        _entitlement = entitlement;
    }

    /// <summary>
    /// Checks if the user can proceed with the given intent.
    /// Returns null if allowed, or a QuotaDenied result if blocked.
    /// </summary>
    public async Task<QuotaDenied?> CheckAsync(int userId, string intent, CancellationToken ct = default)
    {
        var plan = await _entitlement.ResolvePlanAsync(userId, ct);
        var now = DateTime.UtcNow;

        var limits = GetLimits(plan, intent);
        if (limits is null)
            return null; // no quota for this intent (e.g. program_view)

        foreach (var limit in limits)
        {
            var windowStart = GetWindowStart(now, limit.Window);
            var count = await CountUsageAsync(userId, intent, windowStart, now, ct);

            if (count >= limit.Max)
            {
                var resetAt = GetWindowEnd(now, limit.Window);
                return new QuotaDenied(plan, intent, limit.Window, limit.Max, count, resetAt);
            }
        }

        return null;
    }

    /// <summary>
    /// Records a usage event after a successful Gemini call.
    /// </summary>
    public async Task RecordUsageAsync(int userId, string intent, string plan, bool wasBlocked, string? notes = null, CancellationToken ct = default)
    {
        _db.AiUsageRecords.Add(new AiUsageRecord
        {
            UserId = userId,
            Intent = intent,
            OccurredAtUtc = DateTime.UtcNow,
            WasBlocked = wasBlocked,
            PlanAtTime = plan,
            Notes = notes
        });
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Gets remaining quota counts for billing status response.
    /// </summary>
    public async Task<QuotaSnapshot> GetSnapshotAsync(int userId, string plan, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = GetWindowStart(now, "daily");
        var monthStart = GetWindowStart(now, "monthly");
        var rolling14Start = GetWindowStart(now, "rolling_14d");

        var generalChatToday = await CountUsageAsync(userId, FreakAiUsageIntent.GeneralChat, todayStart, now, ct);
        var programGenMonth = await CountUsageAsync(userId, FreakAiUsageIntent.ProgramGenerate, monthStart, now, ct);
        var programAnalyzeMonth = await CountUsageAsync(userId, FreakAiUsageIntent.ProgramAnalyze, monthStart, now, ct);
        var nutritionRolling = await CountUsageAsync(userId, FreakAiUsageIntent.NutritionGuidance, rolling14Start, now, ct);

        if (plan == "free")
        {
            return new QuotaSnapshot
            {
                GeneralChatRemainingToday = Math.Max(0, 3 - generalChatToday),
                ProgramGenerateRemainingThisMonth = Math.Max(0, 1 - programGenMonth),
                ProgramAnalyzeRemainingThisMonth = Math.Max(0, 1 - programAnalyzeMonth),
                NutritionGuidanceNextAvailableAtUtc = nutritionRolling >= 1
                    ? await GetNextAvailableAsync(userId, FreakAiUsageIntent.NutritionGuidance, 14, ct)
                    : null
            };
        }

        // Premium snapshot
        var genToday = await CountUsageAsync(userId, FreakAiUsageIntent.ProgramGenerate, todayStart, now, ct);
        var analyzeToday = await CountUsageAsync(userId, FreakAiUsageIntent.ProgramAnalyze, todayStart, now, ct);
        var nutritionToday = await CountUsageAsync(userId, FreakAiUsageIntent.NutritionGuidance, todayStart, now, ct);
        var nutritionMonth = await CountUsageAsync(userId, FreakAiUsageIntent.NutritionGuidance, monthStart, now, ct);

        return new QuotaSnapshot
        {
            GeneralChatRemainingToday = Math.Max(0, 150 - generalChatToday),
            ProgramGenerateRemainingThisMonth = Math.Max(0, 60 - programGenMonth),
            ProgramAnalyzeRemainingThisMonth = Math.Max(0, 120 - programAnalyzeMonth),
            NutritionGuidanceNextAvailableAtUtc = nutritionToday >= 8 || nutritionMonth >= 60
                ? GetWindowEnd(now, nutritionToday >= 8 ? "daily" : "monthly")
                : null
        };
    }

    // ── Private helpers ────────────────────────────────────

    private async Task<int> CountUsageAsync(int userId, string intent, DateTime from, DateTime to, CancellationToken ct)
    {
        return await _db.AiUsageRecords.CountAsync(r =>
            r.UserId == userId &&
            r.Intent == intent &&
            !r.WasBlocked &&
            r.OccurredAtUtc >= from &&
            r.OccurredAtUtc <= to, ct);
    }

    private async Task<DateTime?> GetNextAvailableAsync(int userId, string intent, int rollingDays, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddDays(-rollingDays);

        // Find the earliest usage within the current rolling window.
        // When this record falls outside the window, the user can use again.
        var earliestInWindow = await _db.AiUsageRecords
            .Where(r => r.UserId == userId && r.Intent == intent && !r.WasBlocked
                        && r.OccurredAtUtc >= windowStart)
            .OrderBy(r => r.OccurredAtUtc)
            .Select(r => r.OccurredAtUtc)
            .FirstOrDefaultAsync(ct);

        if (earliestInWindow == default)
            return null;

        return earliestInWindow.AddDays(rollingDays);
    }

    private static List<QuotaLimit>? GetLimits(string plan, string intent)
    {
        // program_view is free — no quota
        if (intent == FreakAiUsageIntent.ProgramView)
            return null;

        if (plan == "free")
        {
            return intent switch
            {
                FreakAiUsageIntent.ProgramGenerate => [new("monthly", 1)],
                FreakAiUsageIntent.ProgramAnalyze => [new("monthly", 1)],
                FreakAiUsageIntent.NutritionGuidance => [new("rolling_14d", 1)],
                FreakAiUsageIntent.GeneralChat => [new("daily", 3)],
                _ => [new("daily", 3)] // unknown → general_chat limits
            };
        }

        // Premium hidden caps
        return intent switch
        {
            FreakAiUsageIntent.ProgramGenerate => [new("daily", 8), new("monthly", 60)],
            FreakAiUsageIntent.ProgramAnalyze => [new("daily", 12), new("monthly", 120)],
            FreakAiUsageIntent.NutritionGuidance => [new("daily", 8), new("monthly", 60)],
            FreakAiUsageIntent.GeneralChat => [new("daily", 150)],
            _ => [new("daily", 150)]
        };
    }

    private static DateTime GetWindowStart(DateTime now, string window) => window switch
    {
        "daily" => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc),
        "monthly" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        "rolling_14d" => now.AddDays(-14),
        _ => now.AddDays(-1)
    };

    private static DateTime GetWindowEnd(DateTime now, string window) => window switch
    {
        "daily" => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1),
        "monthly" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
        "rolling_14d" => now.AddDays(14),
        _ => now.AddDays(1)
    };
}

public record QuotaLimit(string Window, int Max);

public record QuotaDenied(
    string Plan,
    string Intent,
    string Window,
    int Max,
    int Used,
    DateTime ResetsAtUtc);

public class QuotaSnapshot
{
    public int GeneralChatRemainingToday { get; set; }
    public int ProgramGenerateRemainingThisMonth { get; set; }
    public int ProgramAnalyzeRemainingThisMonth { get; set; }
    public DateTime? NutritionGuidanceNextAvailableAtUtc { get; set; }
}
