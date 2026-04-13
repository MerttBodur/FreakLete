using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly IConfiguration _config;

    private const string ExpectedPackageName = "com.mert.freaklete";

    public BillingController(
        EntitlementService entitlement,
        QuotaService quota,
        AppDbContext db,
        GooglePlayVerificationService playVerify,
        ILogger<BillingController> logger,
        IConfiguration config)
    {
        _entitlement = entitlement;
        _quota = quota;
        _db = db;
        _playVerify = playVerify;
        _logger = logger;
        _config = config;
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
    // ── Allowlists ───────────────────────────────────────────
    private static readonly HashSet<string> AllowedSubscriptionProductIds =
        new(StringComparer.Ordinal) { "freaklete_premium" };

    private static readonly HashSet<string> AllowedSubscriptionBasePlanIds =
        new(StringComparer.Ordinal) { "monthly", "annual" };

    private static readonly HashSet<string> AllowedDonationProductIds =
        new(StringComparer.Ordinal) { "donate_1", "donate_5", "donate_10", "donate_20" };

    [HttpPost("googleplay/sync")]
    public async Task<IActionResult> SyncGooglePlayPurchase(GooglePlayPurchaseSyncRequest request)
    {
        var userId = User.GetUserId();
        var ct = HttpContext.RequestAborted;

        // ── Product allowlist validation ───────────────────────
        // Determine kind by allowlist membership — not by prefix heuristic.
        bool isDonation = AllowedDonationProductIds.Contains(request.ProductId);
        bool isSubscription = AllowedSubscriptionProductIds.Contains(request.ProductId);

        if (!isDonation && !isSubscription)
            return BadRequest(new { message = "Geçersiz ürün." });

        if (isSubscription)
        {
            if (string.IsNullOrEmpty(request.BasePlanId))
                return BadRequest(new { message = "Abonelik için basePlanId zorunludur." });

            if (!AllowedSubscriptionBasePlanIds.Contains(request.BasePlanId))
                return BadRequest(new { message = "Geçersiz abonelik planı." });
        }

        var kind = isDonation ? "donation" : "subscription";

        // ── Upsert: find existing by platform + purchaseToken ──
        var existing = await _db.BillingPurchases.FirstOrDefaultAsync(
            p => p.Platform == "android" && p.PurchaseToken == request.PurchaseToken, ct);

        if (existing is not null)
        {
            // ── Purchase token ownership protection ──────────────
            if (existing.UserId != userId)
                return Conflict(new { message = "Satın alma işlemi doğrulanamadı." });

            // Idempotent update (same user, same token)
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
            "Google Play sync: userId={UserId}, productId={ProductId}, kind={Kind}, state={State}",
            userId, request.ProductId, kind, existing.State);

        return Ok(new { existing.State, existing.EntitlementEndsAtUtc, kind });
    }

    // ── RTDN (Real-Time Developer Notifications) ─────────────

    /// <summary>
    /// Google Pub/Sub push endpoint for subscription lifecycle events.
    /// Protected by shared secret header; not user-authenticated.
    /// </summary>
    [HttpPost("googleplay/rtdn")]
    [AllowAnonymous]
    public async Task<IActionResult> RtdnPush([FromBody] PubSubPushBody body)
    {
        var configuredSecret = _config["GooglePlay:RealTimeDeveloperNotificationSecret"];
        if (string.IsNullOrEmpty(configuredSecret))
            return StatusCode(503, new { message = "RTDN not configured." });

        var providedSecret = Request.Headers["X-FreakLete-RTDN-Secret"].FirstOrDefault();
        // Query param fallback: Google Pub/Sub cannot inject custom headers into push requests.
        // Configure the push endpoint URL as: .../rtdn?secret=<RTDN_SECRET>
        // NOTE: query params may appear in access logs — prefer a Cloud Run proxy (header injection) in production.
        if (string.IsNullOrEmpty(providedSecret))
            providedSecret = Request.Query["secret"].FirstOrDefault();

        if (!string.Equals(providedSecret, configuredSecret, StringComparison.Ordinal))
            return Unauthorized();

        if (body.Message?.Data is null)
            return BadRequest();

        string rtdnJson;
        try
        {
            var bytes = Convert.FromBase64String(body.Message.Data);
            rtdnJson = Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return BadRequest();
        }

        JsonElement rtdn;
        try
        {
            rtdn = JsonSerializer.Deserialize<JsonElement>(rtdnJson);
        }
        catch
        {
            return BadRequest();
        }

        if (rtdn.TryGetProperty("packageName", out var pkgEl))
        {
            var pkg = pkgEl.GetString();
            if (pkg is not null && pkg != ExpectedPackageName)
            {
                _logger.LogWarning("RTDN: Package name mismatch, got {PackageName}", pkg);
                return BadRequest(new { message = "Package name mismatch." });
            }
        }

        var messageId = body.Message.MessageId ?? Guid.NewGuid().ToString();

        if (rtdn.TryGetProperty("oneTimeProductNotification", out _))
        {
            _logger.LogInformation("RTDN: oneTimeProductNotification ignored, messageId={MessageId}", messageId);
            return Ok(new { status = "ignored" });
        }

        if (!rtdn.TryGetProperty("subscriptionNotification", out var subNotifEl))
        {
            _logger.LogInformation("RTDN: No subscriptionNotification, messageId={MessageId}", messageId);
            return Ok(new { status = "ignored" });
        }

        if (!subNotifEl.TryGetProperty("purchaseToken", out var tokenEl) ||
            !subNotifEl.TryGetProperty("subscriptionId", out var subIdEl) ||
            !subNotifEl.TryGetProperty("notificationType", out var ntEl))
            return BadRequest();

        var purchaseToken = tokenEl.GetString() ?? string.Empty;
        var productId = subIdEl.GetString() ?? string.Empty;
        var notificationType = ntEl.GetInt32();

        var ct = HttpContext.RequestAborted;

        var duplicate = await _db.GooglePlayRtdnEvents
            .AnyAsync(e => e.MessageId == messageId, ct);
        if (duplicate)
        {
            _logger.LogInformation("RTDN: Duplicate messageId={MessageId}, skipping", messageId);
            return Ok(new { status = "duplicate" });
        }

        var tokenFingerprint = ComputeSha256Hex(purchaseToken);

        var rtdnEvent = new GooglePlayRtdnEvent
        {
            MessageId = messageId,
            PurchaseTokenFingerprint = tokenFingerprint,
            ProductId = productId,
            NotificationType = notificationType,
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessingState = "processing"
        };
        _db.GooglePlayRtdnEvents.Add(rtdnEvent);
        await _db.SaveChangesAsync(ct);

        var purchase = await _db.BillingPurchases.FirstOrDefaultAsync(
            p => p.Platform == "android" && p.PurchaseToken == purchaseToken && p.Kind == "subscription", ct);

        if (purchase is null)
        {
            rtdnEvent.ProcessingState = "ignored_unknown_token";
            rtdnEvent.ProcessedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "RTDN: Unknown token, messageId={MessageId}, notificationType={Type}, productId={ProductId}",
                messageId, notificationType, productId);
            return Ok(new { status = "ignored" });
        }

        var result = await _playVerify.VerifySubscriptionAsync(purchase.PurchaseToken, purchase.ProductId, ct);

        if (result is null)
        {
            purchase.State = "verification_failed";
            purchase.LastVerifiedAtUtc = DateTime.UtcNow;
            rtdnEvent.ProcessingState = "verification_failed";
        }
        else
        {
            purchase.State = result.State;
            purchase.EntitlementStartsAtUtc = result.EntitlementStartsAtUtc;
            purchase.EntitlementEndsAtUtc = result.EntitlementEndsAtUtc;
            purchase.LastVerifiedAtUtc = DateTime.UtcNow;
            rtdnEvent.ProcessingState = "processed";
        }

        rtdnEvent.ProcessedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "RTDN: messageId={MessageId}, notificationType={Type}, productId={ProductId}, state={State}",
            messageId, notificationType, productId, purchase.State);

        return Ok(new { status = rtdnEvent.ProcessingState });
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

        if (result is null)
        {
            purchase.State = "verification_failed";
            purchase.LastVerifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

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

    private static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizePurchaseState(int state) => state switch
    {
        0 => "active",   // PURCHASED
        1 => "cancelled", // CANCELED
        2 => "pending",   // PENDING
        _ => "pending"
    };
}
