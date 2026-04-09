namespace FreakLete.Services;

/// <summary>
/// Platform-abstracted billing service for purchases and subscriptions.
/// Real implementation on Android; no-op fallback on other platforms.
/// </summary>
public interface IBillingService
{
    /// <summary>Whether the billing client is connected and ready.</summary>
    bool IsAvailable { get; }

    /// <summary>Connect to the billing backend. Call once on app start.</summary>
    Task<bool> ConnectAsync();

    /// <summary>Fetch product details for the given SKUs.</summary>
    Task<IReadOnlyList<BillingProduct>> GetProductsAsync(IEnumerable<string> productIds, BillingProductType type);

    /// <summary>Launch a subscription purchase flow.</summary>
    Task<BillingPurchaseResult> PurchaseSubscriptionAsync(string productId, string basePlanId);

    /// <summary>Launch a one-time (donate) purchase flow.</summary>
    Task<BillingPurchaseResult> PurchaseDonationAsync(string productId);

    /// <summary>Restore previous purchases (query owned items).</summary>
    Task<IReadOnlyList<BillingPurchaseRecord>> RestorePurchasesAsync();

    /// <summary>Disconnect from billing backend.</summary>
    void Disconnect();
}

public enum BillingProductType
{
    Subscription,
    InApp // one-time / consumable
}

public class BillingProduct
{
    public string ProductId { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string FormattedPrice { get; init; } = "";
    public string? BasePlanId { get; init; }
}

public enum BillingPurchaseStatus
{
    Success,
    Cancelled,
    AlreadyOwned,
    Error,
    Unavailable
}

public class BillingPurchaseResult
{
    public BillingPurchaseStatus Status { get; init; }
    public BillingPurchaseRecord? Purchase { get; init; }
    public string? ErrorMessage { get; init; }
}

public class BillingPurchaseRecord
{
    public string ProductId { get; init; } = "";
    public string? BasePlanId { get; init; }
    public string PurchaseToken { get; init; } = "";
    public string OrderId { get; init; } = "";
    public int PurchaseState { get; init; }
    public bool IsAcknowledged { get; init; }
    public bool IsAutoRenewing { get; init; }
    public string RawJson { get; init; } = "";
}
