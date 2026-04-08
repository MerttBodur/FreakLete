namespace FreakLete.Api.Entities;

public class BillingPurchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Platform { get; set; } = string.Empty;       // "android", "ios"
    public string Kind { get; set; } = string.Empty;           // "subscription", "donation"
    public string ProductId { get; set; } = string.Empty;      // e.g. "freaklete_premium"
    public string BasePlanId { get; set; } = string.Empty;     // e.g. "monthly", "annual"
    public string PurchaseToken { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;          // "active", "expired", "cancelled", "pending"
    public DateTime EntitlementStartsAtUtc { get; set; }
    public DateTime EntitlementEndsAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? LastVerifiedAtUtc { get; set; }
    public string? RawPayloadJson { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
