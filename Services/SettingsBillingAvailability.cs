namespace FreakLete.Services;

/// <summary>
/// Centralizes Settings billing gating so donate and subscription readiness stay distinct.
/// </summary>
public static class SettingsBillingAvailability
{
    public static bool CanOpenDonatePicker(IBillingService billing)
        => billing.CanPurchaseDonations;

    public static bool CanOpenSubscribePicker(IBillingService billing)
        => billing.CanPurchaseSubscriptions;

    public static string GetDonateUnavailableMessage()
        => AppLanguage.SettingsDonationUnavailable;

    public static string GetSubscribeUnavailableMessage()
        => AppLanguage.SettingsSubscriptionUnavailable;
}
