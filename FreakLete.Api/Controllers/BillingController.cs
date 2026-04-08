using FreakLete.Api.DTOs.Billing;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly EntitlementService _entitlement;
    private readonly QuotaService _quota;

    public BillingController(EntitlementService entitlement, QuotaService quota)
    {
        _entitlement = entitlement;
        _quota = quota;
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
}
