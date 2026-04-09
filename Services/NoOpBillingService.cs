namespace FreakLete.Services;

/// <summary>
/// Fallback billing service for non-Android platforms.
/// Reports unavailable; no real purchase flows.
/// </summary>
public class NoOpBillingService : IBillingService
{
    public bool IsAvailable => false;

    public Task<bool> ConnectAsync() => Task.FromResult(false);

    public Task<IReadOnlyList<BillingProduct>> GetProductsAsync(IEnumerable<string> productIds, BillingProductType type)
        => Task.FromResult<IReadOnlyList<BillingProduct>>([]);

    public Task<BillingPurchaseResult> PurchaseSubscriptionAsync(string productId, string basePlanId)
        => Task.FromResult(new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable, ErrorMessage = "Billing is only available on Android." });

    public Task<BillingPurchaseResult> PurchaseDonationAsync(string productId)
        => Task.FromResult(new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable, ErrorMessage = "Billing is only available on Android." });

    public Task<IReadOnlyList<BillingPurchaseRecord>> RestorePurchasesAsync()
        => Task.FromResult<IReadOnlyList<BillingPurchaseRecord>>([]);

    public void Disconnect() { }
}
