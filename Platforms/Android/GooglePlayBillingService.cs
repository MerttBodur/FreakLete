using Android.App;
using Android.BillingClient.Api;
using Plugin.InAppBilling;
using PluginPurchaseState = Plugin.InAppBilling.PurchaseState;

namespace FreakLete.Services;

/// <summary>
/// Android Google Play Billing implementation.
/// Subscriptions: native BillingClient with offer-token support for monthly/annual base plan selection.
/// Donations: Plugin.InAppBilling (one-time consumable purchases).
/// </summary>
public class GooglePlayBillingService : IBillingService
{
    private BillingClient? _billingClient;
    private bool _connected;

    private TaskCompletionSource<BillingPurchaseResult>? _pendingSubscriptionTcs;
    private string? _pendingBasePlanId;

    public bool IsAvailable => _connected;

    // ── Connect ─────────────────────────────────────────────

    public async Task<bool> ConnectAsync()
    {
        try
        {
            bool pluginConnected = await CrossInAppBilling.Current.ConnectAsync();

            var activity = Platform.CurrentActivity as Activity;
            if (activity is null)
            {
                _connected = pluginConnected;
                return pluginConnected;
            }

            var tcs = new TaskCompletionSource<bool>();

            _billingClient = BillingClient.NewBuilder(activity)
                .SetListener(new PurchasesUpdatedListener(this))
                .EnablePendingPurchases()
                .Build();

            _billingClient.StartConnection(new BillingStateListener(
                onConnected: () => tcs.TrySetResult(true),
                onDisconnected: () => tcs.TrySetResult(false)));

            bool nativeConnected = await tcs.Task;
            _connected = pluginConnected || nativeConnected;
            return _connected;
        }
        catch (Exception)
        {
            _connected = false;
            return false;
        }
    }

    // ── GetProductsAsync ─────────────────────────────────────

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
                BasePlanId = null
            }).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    // ── PurchaseSubscriptionAsync ────────────────────────────

    public async Task<BillingPurchaseResult> PurchaseSubscriptionAsync(string productId, string basePlanId)
    {
        if (_billingClient is null || !_billingClient.IsReady)
            return new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable };

        try
        {
            // 1. Query product details to get the offer token for the requested base plan.
            var queryParams = QueryProductDetailsParams.NewBuilder()
                .SetProductList(
                [
                    QueryProductDetailsParams.Product.NewBuilder()
                        .SetProductId(productId)
                        .SetProductType(BillingClient.ProductType.Subs)
                        .Build()
                ])
                .Build();

            var queryResult = await _billingClient.QueryProductDetailsAsync(queryParams);

            if (queryResult.Result.ResponseCode != BillingResponseCode.Ok
                || queryResult.ProductDetails is null or { Count: 0 })
            {
                return new BillingPurchaseResult
                {
                    Status = BillingPurchaseStatus.Error,
                    ErrorMessage = $"Product query failed: {queryResult.Result.DebugMessage}"
                };
            }

            var productDetails = queryResult.ProductDetails[0];

            // 2. Find the offer token for the requested basePlanId.
            var offerDetails = productDetails.GetSubscriptionOfferDetails();
            if (offerDetails is null or { Count: 0 })
            {
                return new BillingPurchaseResult
                {
                    Status = BillingPurchaseStatus.Error,
                    ErrorMessage = "No subscription offers found for product."
                };
            }

            ProductDetails.SubscriptionOfferDetails? matchedOffer = null;
            foreach (var offer in offerDetails)
            {
                if (string.Equals(offer.BasePlanId, basePlanId, StringComparison.OrdinalIgnoreCase))
                {
                    matchedOffer = offer;
                    break;
                }
            }

            if (matchedOffer is null && !string.IsNullOrEmpty(basePlanId))
            {
                return new BillingPurchaseResult
                {
                    Status = BillingPurchaseStatus.Error,
                    ErrorMessage = $"Base plan '{basePlanId}' not found for product '{productId}'."
                };
            }

            matchedOffer ??= offerDetails[0];
            string offerToken = matchedOffer.OfferToken;

            // 3. Build BillingFlowParams with the offer token.
            var productDetailsParams = BillingFlowParams.ProductDetailsParams.NewBuilder()
                .SetProductDetails(productDetails)
                .SetOfferToken(offerToken)
                .Build();

            var billingFlowParams = BillingFlowParams.NewBuilder()
                .SetProductDetailsParamsList([productDetailsParams])
                .Build();

            // 4. Set up TCS to receive the result from OnPurchasesUpdated.
            _pendingSubscriptionTcs = new TaskCompletionSource<BillingPurchaseResult>();
            _pendingBasePlanId = basePlanId;

            var activity = Platform.CurrentActivity as Activity;
            if (activity is null)
            {
                _pendingSubscriptionTcs = null;
                _pendingBasePlanId = null;
                return new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable };
            }

            var launchResult = _billingClient.LaunchBillingFlow(activity, billingFlowParams);
            if (launchResult.ResponseCode != BillingResponseCode.Ok)
            {
                _pendingSubscriptionTcs = null;
                _pendingBasePlanId = null;
                return new BillingPurchaseResult
                {
                    Status = BillingPurchaseStatus.Error,
                    ErrorMessage = launchResult.DebugMessage
                };
            }

            return await _pendingSubscriptionTcs.Task;
        }
        catch (Exception ex)
        {
            _pendingSubscriptionTcs = null;
            _pendingBasePlanId = null;
            return new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    // Called by PurchasesUpdatedListener when the billing flow completes.
    internal void HandlePurchasesUpdated(BillingResult billingResult, IList<Purchase>? purchases)
    {
        if (_pendingSubscriptionTcs is null) return;

        if (billingResult.ResponseCode == BillingResponseCode.Ok && purchases is { Count: > 0 })
        {
            var p = purchases[0];
            _pendingSubscriptionTcs.TrySetResult(new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.Success,
                Purchase = new BillingPurchaseRecord
                {
                    ProductId = p.Products.FirstOrDefault() ?? "",
                    BasePlanId = _pendingBasePlanId,
                    PurchaseToken = p.PurchaseToken,
                    OrderId = p.OrderId ?? "",
                    PurchaseState = (int)p.PurchaseState,
                    IsAcknowledged = p.IsAcknowledged,
                    IsAutoRenewing = p.IsAutoRenewing,
                    RawJson = p.OriginalJson ?? ""
                }
            });
        }
        else if (billingResult.ResponseCode == BillingResponseCode.UserCancelled)
        {
            _pendingSubscriptionTcs.TrySetResult(new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.Cancelled
            });
        }
        else if (billingResult.ResponseCode == BillingResponseCode.ItemAlreadyOwned)
        {
            _pendingSubscriptionTcs.TrySetResult(new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.AlreadyOwned
            });
        }
        else
        {
            _pendingSubscriptionTcs.TrySetResult(new BillingPurchaseResult
            {
                Status = BillingPurchaseStatus.Error,
                ErrorMessage = billingResult.DebugMessage
            });
        }

        _pendingSubscriptionTcs = null;
        _pendingBasePlanId = null;
    }

    // ── PurchaseDonationAsync ────────────────────────────────

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

            // Do NOT consume on client-side. Server consumes after verification succeeds.
            return new BillingPurchaseResult
            {
                Status = MapPluginState(purchase.State),
                Purchase = MapPluginPurchase(purchase, null)
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

    // ── RestorePurchasesAsync ────────────────────────────────

    public async Task<IReadOnlyList<BillingPurchaseRecord>> RestorePurchasesAsync()
    {
        if (!_connected) return [];

        try
        {
            var results = new List<BillingPurchaseRecord>();

            var subs = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.Subscription);
            if (subs is not null)
                results.AddRange(subs.Select(p => MapPluginPurchase(p, null)));

            var inApp = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
            if (inApp is not null)
                results.AddRange(inApp.Select(p => MapPluginPurchase(p, null)));

            return results;
        }
        catch (Exception)
        {
            return [];
        }
    }

    // ── Disconnect ───────────────────────────────────────────

    public void Disconnect()
    {
        try { _ = CrossInAppBilling.Current.DisconnectAsync(); } catch { }
        try { _billingClient?.EndConnection(); } catch { }
        _connected = false;
    }

    // ── Helpers ──────────────────────────────────────────────

    private static BillingPurchaseStatus MapPluginState(PluginPurchaseState state) => state switch
    {
        PluginPurchaseState.Purchased => BillingPurchaseStatus.Success,
        PluginPurchaseState.PaymentPending => BillingPurchaseStatus.Success,
        _ => BillingPurchaseStatus.Error
    };

    private static BillingPurchaseRecord MapPluginPurchase(InAppBillingPurchase p, string? basePlanId) => new()
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

    // ── Inner Java listener classes ───────────────────────────

    private sealed class PurchasesUpdatedListener(GooglePlayBillingService service)
        : Java.Lang.Object, IPurchasesUpdatedListener
    {
        public void OnPurchasesUpdated(BillingResult billingResult, IList<Purchase>? purchases)
            => service.HandlePurchasesUpdated(billingResult, purchases);
    }

    private sealed class BillingStateListener : Java.Lang.Object, IBillingClientStateListener
    {
        private readonly Action _onConnected;
        private readonly Action _onDisconnected;

        public BillingStateListener(Action onConnected, Action onDisconnected)
        {
            _onConnected = onConnected;
            _onDisconnected = onDisconnected;
        }

        public void OnBillingSetupFinished(BillingResult billingResult)
        {
            if (billingResult.ResponseCode == BillingResponseCode.Ok)
                _onConnected();
            else
                _onDisconnected();
        }

        public void OnBillingServiceDisconnected() => _onDisconnected();
    }
}
