using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("grant-premium")]
    public async Task<IActionResult> GrantPremium([FromBody] GrantPremiumRequest request)
    {
        var expectedKey = _config["Admin:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
            return StatusCode(503, new { message = "Admin API key not configured." });

        if (!Request.Headers.TryGetValue("X-Admin-Key", out var providedKey) ||
            providedKey != expectedKey)
            return Unauthorized(new { message = "Invalid or missing X-Admin-Key header." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return NotFound(new { message = $"User '{request.Email}' not found." });

        var existing = await _db.BillingPurchases.FirstOrDefaultAsync(p =>
            p.UserId == user.Id && p.Kind == "subscription" && p.State == "active");

        if (existing is not null)
        {
            existing.EntitlementEndsAtUtc = DateTime.UtcNow.AddYears(10);
            existing.LastVerifiedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _db.BillingPurchases.Add(new BillingPurchase
            {
                UserId = user.Id,
                Platform = "admin",
                Kind = "subscription",
                ProductId = "freaklete_premium",
                BasePlanId = "admin",
                PurchaseToken = "admin-grant",
                OrderId = $"admin-{user.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                State = "active",
                EntitlementStartsAtUtc = DateTime.UtcNow,
                EntitlementEndsAtUtc = DateTime.UtcNow.AddYears(10),
                AcknowledgedAtUtc = DateTime.UtcNow,
                LastVerifiedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Premium granted to '{request.Email}' until {DateTime.UtcNow.AddYears(10):yyyy-MM-dd}." });
    }
}

public record GrantPremiumRequest(string Email);
