using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Billing;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly EntitlementService _entitlement;
    private readonly QuotaService _quota;
    private readonly AppDbContext _db;
    private readonly GooglePlayVerificationService _playVerify;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        EntitlementService entitlement,
        QuotaService quota,
        AppDbContext db,
        GooglePlayVerificationService playVerify,
        ILogger<BillingController> logger)
    {
        _entitlement = entitlement;
        _quota = quota;
        _db = db;
        _playVerify = playVerify;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<BillingStatusResponse>> GetStatus()
    {
        var userId = User.GetUserId();
        var ct = HttpContext.RequestAborted;

        var plan = await _entitlement.ResolvePlanAsync(userId, ct);
        var subscription = await _entitlement.GetActiveSubscriptionAsync(userId, ct);
        var snapshot = await _quota.GetSnapshotAsync(userId, plan, ct);

        return Ok(new BillingStatusResponse
        {
            Plan = plan,
            IsPremiumActive = plan == "premium",
            SubscriptionEndsAtUtc = subscription?.EntitlementEndsAtUtc,
            GeneralChatRemainingToday = snapshot.GeneralChatRemainingToday,
            ProgramGenerateRemainingThisMonth = snapshot.ProgramGenerateRemainingThisMonth,
            ProgramAnalyzeRemainingThisMonth = snapshot.ProgramAnalyzeRemainingThisMonth,
            NutritionGuidanceNextAvailableAtUtc = snapshot.NutritionGuidanceNextAvailableAtUtc
        });
    }

    /// <summary>
    /// Idempotent sync of a Google Play purchase.
    /// Upserts BillingPurchase, verifies with Google, updates entitlement.
    /// </summary>
    [HttpPost("googleplay/sync")]
    public async Task<IActionResult> SyncGooglePlayPurchase(GooglePlayPurchaseSyncRequest request)
    {
        var userId = User.GetUserId();
        var ct = HttpContext.RequestAborted;

        var kind = IsDonateProduct(request.ProductId) ? "donation" : "subscription";

        // Upsert: find existing by platform + purchaseToken
        var existing = await _db.BillingPurchases.FirstOrDefaultAsync(
            p => p.Platform == "android" && p.PurchaseToken == request.PurchaseToken, ct);

        if (existing is not null)
        {
            // Idempotent update
            existing.OrderId = request.OrderId ?? existing.OrderId;
            existing.State = NormalizePurchaseState(request.PurchaseState);
            existing.RawPayloadJson = request.RawPayloadJson;
            existing.LastVerifiedAtUtc = DateTime.UtcNow;
        }
        else
        {
            existing = new BillingPurchase
            {
                UserId = userId,
                Platform = "android",
                Kind = kind,
                ProductId = request.ProductId,
                BasePlanId = request.BasePlanId ?? "",
                PurchaseToken = request.PurchaseToken,
                OrderId = request.OrderId ?? "",
                State = NormalizePurchaseState(request.PurchaseState),
                EntitlementStartsAtUtc = DateTime.UtcNow,
                // TEMPORARY: Set based on client-reported basePlanId; verification will correct this.
                // (See VerifyAndUpdateSubscriptionAsync for server-side correction using actual Google Play data.)
                EntitlementEndsAtUtc = kind == "subscription"
                    ? DateTime.UtcNow.AddMonths(request.BasePlanId == "annual" ? 12 : 1)
                    : DateTime.UtcNow,
                RawPayloadJson = request.RawPayloadJson,
                LastVerifiedAtUtc = DateTime.UtcNow
            };
            _db.BillingPurchases.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        // ── Server-side verification ───────────────────────────
        if (kind == "subscription")
            await VerifyAndUpdateSubscriptionAsync(existing, ct);
        else
            await VerifyAndConsumeDonationAsync(existing, ct);

        _logger.LogInformation(
            "Google Play sync: user={UserId}, product={Product}, kind={Kind}, state={State}",
            userId, request.ProductId, kind, existing.State);

        return Ok(new { existing.State, existing.EntitlementEndsAtUtc, kind });
    }

    // ── Private helpers ──────────────────────────────────────

    private async Task VerifyAndUpdateSubscriptionAsync(BillingPurchase purchase, CancellationToken ct)
    {
        var result = await _playVerify.VerifySubscriptionAsync(
            purchase.PurchaseToken, purchase.ProductId, ct);

        // SECURITY: If verification unavailable/fails, mark as unverified.
        // Do NOT grant premium for unverified subscriptions.
        if (result is null)
        {
            purchase.State = "verification_failed";
            purchase.LastVerifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        purchase.State = result.State;
        purchase.EntitlementStartsAtUtc = result.EntitlementStartsAtUtc;
        purchase.EntitlementEndsAtUtc = result.EntitlementEndsAtUtc;
        purchase.LastVerifiedAtUtc = DateTime.UtcNow;

        if (result.NeedsAcknowledge)
        {
            var acked = await _playVerify.AcknowledgeSubscriptionAsync(
                purchase.PurchaseToken, purchase.ProductId, ct);
            if (acked)
                purchase.AcknowledgedAtUtc = DateTime.UtcNow;
        }
        else if (result.IsAcknowledged)
        {
            purchase.AcknowledgedAtUtc ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task VerifyAndConsumeDonationAsync(BillingPurchase purchase, CancellationToken ct)
    {
        // Already consumed — skip
        if (purchase.ConsumedAtUtc.HasValue) return;

        var result = await _playVerify.VerifyDonationAsync(
            purchase.PurchaseToken, purchase.ProductId, ct);

        if (result is null) return;

        if (result.IsPurchased)
            purchase.State = "completed";

        if (result.NeedsConsume)
        {
            var consumed = await _playVerify.ConsumeDonationAsync(
                purchase.PurchaseToken, purchase.ProductId, ct);
            if (consumed)
                purchase.ConsumedAtUtc = DateTime.UtcNow;
        }
        else if (result.IsConsumed)
        {
            purchase.ConsumedAtUtc ??= DateTime.UtcNow;
        }

        purchase.LastVerifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static bool IsDonateProduct(string productId) =>
        productId.StartsWith("donate_", StringComparison.Ordinal);

    private static string NormalizePurchaseState(int state) => state switch
    {
        0 => "active",   // PURCHASED
        1 => "cancelled", // CANCELED
        2 => "pending",   // PENDING
        _ => "pending"
    };
}
