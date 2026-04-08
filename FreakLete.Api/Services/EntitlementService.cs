using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

/// <summary>
/// Resolves user plan: "free" or "premium".
/// Premium requires an active subscription with valid entitlement window.
/// </summary>
public class EntitlementService
{
    private readonly AppDbContext _db;

    public EntitlementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> ResolvePlanAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var hasPremium = await _db.BillingPurchases.AnyAsync(p =>
            p.UserId == userId &&
            p.Kind == "subscription" &&
            p.State == "active" &&
            p.EntitlementEndsAtUtc > now, ct);

        return hasPremium ? "premium" : "free";
    }

    public async Task<BillingPurchase?> GetActiveSubscriptionAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _db.BillingPurchases
            .Where(p =>
                p.UserId == userId &&
                p.Kind == "subscription" &&
                p.State == "active" &&
                p.EntitlementEndsAtUtc > now)
            .OrderByDescending(p => p.EntitlementEndsAtUtc)
            .FirstOrDefaultAsync(ct);
    }
}
