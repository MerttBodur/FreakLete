using Plugin.InAppBilling;

namespace FreakLete.Services;

/// <summary>
/// Android Google Play Billing implementation using Plugin.InAppBilling.
/// Handles subscriptions (freaklete_premium) and one-time donations (donate_*).
/// </summary>
public class GooglePlayBillingService : IBillingService
{
    private bool _connected;

    public bool IsAvailable => _connected;

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _connected = await CrossInAppBilling.Current.ConnectAsync();
            return _connected;
        }
        catch (Exception)
        {
            _connected = false;
            return false;
        }
    }

    public async Task<IReadOnlyList<BillingProduct>> GetProductsAsync(
        IEnumerable<string> productIds, BillingProductType type)
    {
        if (!_connected) return [];

        try
        {
            var itemType = type == BillingProductType.Subscription
                ? ItemType.Subscription
                : ItemType.InAppPurchase;

            var products = await CrossInAppBilling.Current
                .GetProductInfoAsync(itemType, productIds.ToArray());

            if (products is null) return [];

            return products.Select(p => new BillingProduct
            {
                ProductId = p.ProductId,
                Title = p.Name,
                Description = p.Description,
                FormattedPrice = p.LocalizedPrice,
                BasePlanId = null // base plan resolved at purchase time
            }).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<BillingPurchaseResult> PurchaseSubscriptionAsync(string productId, string basePlanId)
    {
        if (!_connected)
            return new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable };

        try
        {
            // KNOWN LIMITATION (Phase 4 improvement):
            // Plugin.InAppBilling does not support base plan / offer selection.
            // When launching the purchase flow below, `basePlanId` is NOT sent to Google Play.
            // The user will always see Google Play's default offer.
            // This parameter is stored but backend verification corrects entitlement end dates
            // based on the actual plan purchased from Google Play.
            
            var purchase = await CrossInAppBilling.Current.PurchaseAsync(
                productId, ItemType.Subscription, obfuscatedAccountId: null);

            if (purchase is null)
                return new BillingPurchaseResult { Status = BillingPurchaseStatus.Cancelled };

            return new BillingPurchaseResult
            {
                Status = MapState(purchase.State),
                Purchase = MapPurchase(purchase, basePlanId)
            };
        }
        catch (InAppBillingPurchaseException ex) when (ex.PurchaseError == PurchaseError.UserCancelled)
        {
            return new BillingPurchaseResult { Status = BillingPurchaseStatus.Cancelled };
        }
        catch (InAppBillingPurchaseException ex) when (ex.PurchaseError == PurchaseError.AlreadyOwned)
        {
            return new BillingPurchaseResult { Status = BillingPurchaseStatus.AlreadyOwned };
        }
        catch (Exception ex)
        {
            return new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<BillingPurchaseResult> PurchaseDonationAsync(string productId)
    {
        if (!_connected)
            return new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable };

        try
        {
            var purchase = await CrossInAppBilling.Current.PurchaseAsync(
                productId, ItemType.InAppPurchase, obfuscatedAccountId: null);

            if (purchase is null)
                return new BillingPurchaseResult { Status = BillingPurchaseStatus.Cancelled };

            // NOTE: Do NOT consume on client-side. Server consumes after verification succeeds.
            // This ensures ledger durability: if server sync fails, purchase can be retried.

            return new BillingPurchaseResult
            {
                Status = MapState(purchase.State),
                Purchase = MapPurchase(purchase, null)
            };
        }
        catch (InAppBillingPurchaseException ex) when (ex.PurchaseError == PurchaseError.UserCancelled)
        {
            return new BillingPurchaseResult { Status = BillingPurchaseStatus.Cancelled };
        }
        catch (Exception ex)
        {
            return new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<BillingPurchaseRecord>> RestorePurchasesAsync()
    {
        if (!_connected) return [];

        try
        {
            var results = new List<BillingPurchaseRecord>();

            // Restore subscriptions
            var subs = await CrossInAppBilling.Current
                .GetPurchasesAsync(ItemType.Subscription);
            if (subs is not null)
                results.AddRange(subs.Select(p => MapPurchase(p, null)));

            // Restore in-app (donations are consumed, so this may be empty)
            var inApp = await CrossInAppBilling.Current
                .GetPurchasesAsync(ItemType.InAppPurchase);
            if (inApp is not null)
                results.AddRange(inApp.Select(p => MapPurchase(p, null)));

            return results;
        }
        catch (Exception)
        {
            return [];
        }
    }

    public void Disconnect()
    {
        try
        {
            _ = CrossInAppBilling.Current.DisconnectAsync();
        }
        catch { }
        _connected = false;
    }

    private static BillingPurchaseStatus MapState(PurchaseState state) => state switch
    {
        PurchaseState.Purchased => BillingPurchaseStatus.Success,
        PurchaseState.PaymentPending => BillingPurchaseStatus.Success, // treat pending as success, backend will track state
        _ => BillingPurchaseStatus.Error
    };

    private static BillingPurchaseRecord MapPurchase(InAppBillingPurchase p, string? basePlanId) => new()
    {
        ProductId = p.ProductId,
        BasePlanId = basePlanId,
        PurchaseToken = p.PurchaseToken,
        OrderId = p.Id ?? "",
        PurchaseState = (int)p.State,
        IsAcknowledged = p.IsAcknowledged ?? false,
        IsAutoRenewing = p.AutoRenewing,
        RawJson = p.OriginalJson ?? ""
    };
}
