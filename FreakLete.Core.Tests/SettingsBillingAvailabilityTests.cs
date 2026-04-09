using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class SettingsBillingAvailabilityTests
{
    [Fact]
    public void DonatePicker_Uses_Donation_Readiness_Only()
    {
        var billing = new FakeBillingService(canPurchaseDonations: true, canPurchaseSubscriptions: false);

        Assert.True(SettingsBillingAvailability.CanOpenDonatePicker(billing));
        Assert.False(SettingsBillingAvailability.CanOpenSubscribePicker(billing));
    }

    [Fact]
    public void SubscribePicker_Stays_Closed_When_Only_Donations_Are_Ready()
    {
        var billing = new FakeBillingService(canPurchaseDonations: true, canPurchaseSubscriptions: false);

        Assert.False(SettingsBillingAvailability.CanOpenSubscribePicker(billing));
        Assert.Equal("Subscriptions are currently unavailable.", SettingsBillingAvailability.GetSubscribeUnavailableMessage());
    }

    [Fact]
    public void DonateUnavailableMessage_Is_Specific()
    {
        Assert.Equal("Donations are currently unavailable.", SettingsBillingAvailability.GetDonateUnavailableMessage());
    }

    private sealed class FakeBillingService(bool canPurchaseDonations, bool canPurchaseSubscriptions) : IBillingService
    {
        public bool IsAvailable => CanPurchaseDonations || CanPurchaseSubscriptions;
        public bool CanPurchaseDonations { get; } = canPurchaseDonations;
        public bool CanPurchaseSubscriptions { get; } = canPurchaseSubscriptions;

        public Task<bool> ConnectAsync() => Task.FromResult(IsAvailable);

        public Task<IReadOnlyList<BillingProduct>> GetProductsAsync(IEnumerable<string> productIds, BillingProductType type)
            => Task.FromResult<IReadOnlyList<BillingProduct>>([]);

        public Task<BillingPurchaseResult> PurchaseSubscriptionAsync(string productId, string basePlanId)
            => Task.FromResult(new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable });

        public Task<BillingPurchaseResult> PurchaseDonationAsync(string productId)
            => Task.FromResult(new BillingPurchaseResult { Status = BillingPurchaseStatus.Unavailable });

        public Task<IReadOnlyList<BillingPurchaseRecord>> RestorePurchasesAsync()
            => Task.FromResult<IReadOnlyList<BillingPurchaseRecord>>([]);

        public void Disconnect()
        {
        }
    }
}
