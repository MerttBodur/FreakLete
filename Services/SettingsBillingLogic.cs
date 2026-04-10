namespace FreakLete.Services;

/// <summary>
/// Testable, MAUI-free logic for Settings billing flow outcomes.
/// Separates Play purchase result from backend sync/verification result.
/// </summary>
public static class SettingsBillingLogic
{
    // ── Sync outcome classification ──────────────────────────────────────────

    public enum SyncOutcome
    {
        /// <summary>Backend confirmed the purchase and granted entitlement.</summary>
        Verified,
        /// <summary>Backend responded but verification/state was not confirmed.</summary>
        VerificationFailed,
        /// <summary>API call failed (network, config, 500, etc.).</summary>
        SyncFailed,
    }

    /// <summary>
    /// Interprets the ApiResult from SyncGooglePlayPurchaseAsync for subscriptions.
    /// Rules:
    ///   - API call failed (Success=false) → SyncFailed
    ///   - State is "verified" or "active" → Verified
    ///   - Any other state, or Data is null → VerificationFailed
    /// </summary>
    public static SyncOutcome ClassifySyncResult(ApiResult<GooglePlaySyncResponse> result)
    {
        if (!result.Success || result.Data is null)
            return SyncOutcome.SyncFailed;

        var state = result.Data.State?.Trim().ToLowerInvariant() ?? "";
        return state is "verified" or "active"
            ? SyncOutcome.Verified
            : SyncOutcome.VerificationFailed;
    }

    /// <summary>
    /// Interprets the ApiResult from SyncGooglePlayPurchaseAsync for donations.
    /// Donations use "completed" state (verified + consumed flow), not "active"/"verified".
    /// Rules:
    ///   - API call failed (Success=false) → SyncFailed
    ///   - State is "completed" → Verified
    ///   - Any other state, or Data is null → VerificationFailed
    /// Subscribe security model is NOT relaxed: this method is donate-only.
    /// </summary>
    public static SyncOutcome ClassifyDonateSyncResult(ApiResult<GooglePlaySyncResponse> result)
    {
        if (!result.Success || result.Data is null)
            return SyncOutcome.SyncFailed;

        var state = result.Data.State?.Trim().ToLowerInvariant() ?? "";
        return state == "completed"
            ? SyncOutcome.Verified
            : SyncOutcome.VerificationFailed;
    }

    // ── Restore scenario classification ──────────────────────────────────────

    public enum RestoreOutcome
    {
        /// <summary>No purchases found in Play inventory.</summary>
        NoPurchases,
        /// <summary>Purchases found but none synced successfully.</summary>
        AllSyncFailed,
        /// <summary>Some synced, some failed.</summary>
        PartialSuccess,
        /// <summary>All synced successfully (all verified or active).</summary>
        FullSuccess,
    }

    /// <summary>
    /// Given per-purchase sync outcomes, determines the overall restore scenario.
    /// </summary>
    public static RestoreOutcome ClassifyRestoreOutcome(IReadOnlyList<SyncOutcome> outcomes)
    {
        if (outcomes.Count == 0) return RestoreOutcome.NoPurchases;

        int verified = outcomes.Count(o => o == SyncOutcome.Verified);

        if (verified == 0)           return RestoreOutcome.AllSyncFailed;
        if (verified == outcomes.Count) return RestoreOutcome.FullSuccess;
        return RestoreOutcome.PartialSuccess;
    }

    // ── Message selection ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the user-facing toast message for a donate purchase + sync outcome.
    /// Never shows success if sync was not verified.
    /// </summary>
    public static string DonateToastMessage(BillingPurchaseStatus purchaseStatus, SyncOutcome? syncOutcome)
    {
        return purchaseStatus switch
        {
            BillingPurchaseStatus.Cancelled    => AppLanguage.SettingsPurchaseCancelled,
            BillingPurchaseStatus.AlreadyOwned => AppLanguage.SettingsPurchaseAlreadyOwned,
            BillingPurchaseStatus.Unavailable  => AppLanguage.SettingsBillingUnavailable,
            BillingPurchaseStatus.Error        => AppLanguage.SettingsPurchaseError,
            BillingPurchaseStatus.Success      => syncOutcome switch
            {
                SyncOutcome.Verified           => AppLanguage.SettingsDonateSuccess,
                SyncOutcome.VerificationFailed => AppLanguage.SettingsSyncVerificationFailed,
                SyncOutcome.SyncFailed         => AppLanguage.SettingsSyncFailed,
                _                              => AppLanguage.SettingsSyncFailed
            },
            _ => AppLanguage.SettingsPurchaseError
        };
    }

    /// <summary>
    /// Returns the user-facing toast message for a subscribe purchase + sync outcome.
    /// Never shows success or activates premium if sync was not verified.
    /// </summary>
    public static string SubscribeToastMessage(BillingPurchaseStatus purchaseStatus, SyncOutcome? syncOutcome)
    {
        return purchaseStatus switch
        {
            BillingPurchaseStatus.Cancelled    => AppLanguage.SettingsPurchaseCancelled,
            BillingPurchaseStatus.AlreadyOwned => AppLanguage.SettingsPurchaseAlreadyOwned,
            BillingPurchaseStatus.Unavailable  => AppLanguage.SettingsSubscriptionUnavailable,
            BillingPurchaseStatus.Error        => AppLanguage.SettingsPurchaseError,
            BillingPurchaseStatus.Success      => syncOutcome switch
            {
                SyncOutcome.Verified           => AppLanguage.SettingsSubscribeSuccess,
                SyncOutcome.VerificationFailed => AppLanguage.SettingsSyncVerificationFailed,
                SyncOutcome.SyncFailed         => AppLanguage.SettingsSyncFailed,
                _                              => AppLanguage.SettingsSyncFailed
            },
            _ => AppLanguage.SettingsPurchaseError
        };
    }

    /// <summary>
    /// Returns true only when a subscribe flow completed AND backend verified it.
    /// Used to gate the billing status refresh and premium UI update.
    /// </summary>
    public static bool ShouldRefreshAfterSubscribe(BillingPurchaseStatus purchaseStatus, SyncOutcome syncOutcome)
        => purchaseStatus == BillingPurchaseStatus.Success && syncOutcome == SyncOutcome.Verified;

    /// <summary>
    /// Returns the user-facing toast for a restore flow.
    /// </summary>
    public static string RestoreToastMessage(RestoreOutcome outcome)
        => outcome switch
        {
            RestoreOutcome.NoPurchases   => AppLanguage.SettingsRestoreEmpty,
            RestoreOutcome.AllSyncFailed => AppLanguage.SettingsRestoreAllFailed,
            RestoreOutcome.PartialSuccess => AppLanguage.SettingsRestorePartial,
            RestoreOutcome.FullSuccess   => AppLanguage.SettingsRestoreSuccess,
            _                            => AppLanguage.SettingsRestoreAllFailed
        };

    /// <summary>
    /// Returns true if the restore outcome warrants a billing status refresh.
    /// Refresh only when at least one purchase was verified.
    /// </summary>
    public static bool ShouldRefreshAfterRestore(RestoreOutcome outcome)
        => outcome is RestoreOutcome.FullSuccess or RestoreOutcome.PartialSuccess;
}
