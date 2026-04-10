using FreakLete.Services;
using static FreakLete.Services.SettingsBillingLogic;

namespace FreakLete.Core.Tests;

public class SettingsBillingLogicTests
{
    // ── ClassifySyncResult ───────────────────────────────────────────────────

    [Fact]
    public void ClassifySyncResult_ApiFailure_ReturnsSyncFailed()
    {
        var result = ApiResult<GooglePlaySyncResponse>.Fail("network error");
        Assert.Equal(SyncOutcome.SyncFailed, ClassifySyncResult(result));
    }

    [Fact]
    public void ClassifySyncResult_NullData_ReturnsSyncFailed()
    {
        var result = new ApiResult<GooglePlaySyncResponse> { Success = true, Data = null };
        Assert.Equal(SyncOutcome.SyncFailed, ClassifySyncResult(result));
    }

    [Theory]
    [InlineData("verified")]
    [InlineData("VERIFIED")]
    [InlineData("active")]
    [InlineData("Active")]
    public void ClassifySyncResult_VerifiedOrActiveState_ReturnsVerified(string state)
    {
        var result = ApiResult<GooglePlaySyncResponse>.Ok(new GooglePlaySyncResponse { State = state });
        Assert.Equal(SyncOutcome.Verified, ClassifySyncResult(result));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("expired")]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("unknown_state")]
    public void ClassifySyncResult_UnknownState_ReturnsVerificationFailed(string state)
    {
        var result = ApiResult<GooglePlaySyncResponse>.Ok(new GooglePlaySyncResponse { State = state });
        Assert.Equal(SyncOutcome.VerificationFailed, ClassifySyncResult(result));
    }

    // ── ClassifyDonateSyncResult ─────────────────────────────────────────────

    [Fact]
    public void ClassifyDonateSyncResult_ApiFailure_ReturnsSyncFailed()
    {
        var result = ApiResult<GooglePlaySyncResponse>.Fail("network error");
        Assert.Equal(SyncOutcome.SyncFailed, ClassifyDonateSyncResult(result));
    }

    [Fact]
    public void ClassifyDonateSyncResult_NullData_ReturnsSyncFailed()
    {
        var result = new ApiResult<GooglePlaySyncResponse> { Success = true, Data = null };
        Assert.Equal(SyncOutcome.SyncFailed, ClassifyDonateSyncResult(result));
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("COMPLETED")]
    public void ClassifyDonateSyncResult_CompletedState_ReturnsVerified(string state)
    {
        var result = ApiResult<GooglePlaySyncResponse>.Ok(new GooglePlaySyncResponse { State = state });
        Assert.Equal(SyncOutcome.Verified, ClassifyDonateSyncResult(result));
    }

    [Theory]
    [InlineData("verified")]
    [InlineData("active")]
    [InlineData("pending")]
    [InlineData("")]
    public void ClassifyDonateSyncResult_NonCompletedState_ReturnsVerificationFailed(string state)
    {
        // Donate classifier only accepts "completed". "verified"/"active" are subscription states.
        var result = ApiResult<GooglePlaySyncResponse>.Ok(new GooglePlaySyncResponse { State = state });
        Assert.Equal(SyncOutcome.VerificationFailed, ClassifyDonateSyncResult(result));
    }

    [Fact]
    public void ClassifySyncResult_CompletedState_ReturnsVerificationFailed()
    {
        // Subscribe classifier must NOT accept "completed" — donate-only state.
        var result = ApiResult<GooglePlaySyncResponse>.Ok(new GooglePlaySyncResponse { State = "completed" });
        Assert.Equal(SyncOutcome.VerificationFailed, ClassifySyncResult(result));
    }

    // ── ClassifyRestoreOutcome ───────────────────────────────────────────────

    [Fact]
    public void ClassifyRestoreOutcome_EmptyList_ReturnsNoPurchases()
    {
        var outcomes = new List<SyncOutcome>();
        Assert.Equal(RestoreOutcome.NoPurchases, ClassifyRestoreOutcome(outcomes));
    }

    [Fact]
    public void ClassifyRestoreOutcome_AllFailed_ReturnsAllSyncFailed()
    {
        var outcomes = new List<SyncOutcome>
        {
            SyncOutcome.SyncFailed, SyncOutcome.VerificationFailed
        };
        Assert.Equal(RestoreOutcome.AllSyncFailed, ClassifyRestoreOutcome(outcomes));
    }

    [Fact]
    public void ClassifyRestoreOutcome_AllVerified_ReturnsFullSuccess()
    {
        var outcomes = new List<SyncOutcome>
        {
            SyncOutcome.Verified, SyncOutcome.Verified
        };
        Assert.Equal(RestoreOutcome.FullSuccess, ClassifyRestoreOutcome(outcomes));
    }

    [Fact]
    public void ClassifyRestoreOutcome_MixedResults_ReturnsPartialSuccess()
    {
        var outcomes = new List<SyncOutcome>
        {
            SyncOutcome.Verified, SyncOutcome.SyncFailed
        };
        Assert.Equal(RestoreOutcome.PartialSuccess, ClassifyRestoreOutcome(outcomes));
    }

    [Fact]
    public void ClassifyRestoreOutcome_OneVerifiedRestFailed_ReturnsPartialSuccess()
    {
        var outcomes = new List<SyncOutcome>
        {
            SyncOutcome.Verified, SyncOutcome.VerificationFailed, SyncOutcome.SyncFailed
        };
        Assert.Equal(RestoreOutcome.PartialSuccess, ClassifyRestoreOutcome(outcomes));
    }

    // ── DonateToastMessage ───────────────────────────────────────────────────

    [Fact]
    public void DonateToastMessage_Cancelled_ReturnsCancelledString()
    {
        var msg = DonateToastMessage(BillingPurchaseStatus.Cancelled, null);
        Assert.Equal(AppLanguage.SettingsPurchaseCancelled, msg);
    }

    [Fact]
    public void DonateToastMessage_AlreadyOwned_ReturnsAlreadyOwnedString()
    {
        var msg = DonateToastMessage(BillingPurchaseStatus.AlreadyOwned, null);
        Assert.Equal(AppLanguage.SettingsPurchaseAlreadyOwned, msg);
    }

    [Fact]
    public void DonateToastMessage_SuccessVerified_ReturnsDonateSuccess()
    {
        var msg = DonateToastMessage(BillingPurchaseStatus.Success, SyncOutcome.Verified);
        Assert.Equal(AppLanguage.SettingsDonateSuccess, msg);
    }

    [Fact]
    public void DonateToastMessage_SuccessVerificationFailed_ReturnsVerificationFailed()
    {
        var msg = DonateToastMessage(BillingPurchaseStatus.Success, SyncOutcome.VerificationFailed);
        Assert.Equal(AppLanguage.SettingsSyncVerificationFailed, msg);
    }

    [Fact]
    public void DonateToastMessage_SuccessSyncFailed_ReturnsSyncFailed()
    {
        var msg = DonateToastMessage(BillingPurchaseStatus.Success, SyncOutcome.SyncFailed);
        Assert.Equal(AppLanguage.SettingsSyncFailed, msg);
    }

    [Fact]
    public void DonateToastMessage_Unavailable_ReturnsUnavailableString()
    {
        var msg = DonateToastMessage(BillingPurchaseStatus.Unavailable, null);
        Assert.Equal(AppLanguage.SettingsBillingUnavailable, msg);
    }

    // ── SubscribeToastMessage ────────────────────────────────────────────────

    [Fact]
    public void SubscribeToastMessage_SuccessVerified_ReturnsSubscribeSuccess()
    {
        var msg = SubscribeToastMessage(BillingPurchaseStatus.Success, SyncOutcome.Verified);
        Assert.Equal(AppLanguage.SettingsSubscribeSuccess, msg);
    }

    [Fact]
    public void SubscribeToastMessage_SuccessSyncFailed_ReturnsSyncFailed()
    {
        var msg = SubscribeToastMessage(BillingPurchaseStatus.Success, SyncOutcome.SyncFailed);
        Assert.Equal(AppLanguage.SettingsSyncFailed, msg);
    }

    [Fact]
    public void SubscribeToastMessage_SuccessVerificationFailed_ReturnsVerificationFailed()
    {
        var msg = SubscribeToastMessage(BillingPurchaseStatus.Success, SyncOutcome.VerificationFailed);
        Assert.Equal(AppLanguage.SettingsSyncVerificationFailed, msg);
    }

    [Fact]
    public void SubscribeToastMessage_Cancelled_ReturnsCancelledString()
    {
        var msg = SubscribeToastMessage(BillingPurchaseStatus.Cancelled, SyncOutcome.SyncFailed);
        Assert.Equal(AppLanguage.SettingsPurchaseCancelled, msg);
    }

    [Fact]
    public void SubscribeToastMessage_AlreadyOwned_ReturnsAlreadyOwnedString()
    {
        var msg = SubscribeToastMessage(BillingPurchaseStatus.AlreadyOwned, SyncOutcome.SyncFailed);
        Assert.Equal(AppLanguage.SettingsPurchaseAlreadyOwned, msg);
    }

    [Fact]
    public void SubscribeToastMessage_Unavailable_ReturnsSubscriptionUnavailableString()
    {
        var msg = SubscribeToastMessage(BillingPurchaseStatus.Unavailable, SyncOutcome.SyncFailed);
        Assert.Equal(AppLanguage.SettingsSubscriptionUnavailable, msg);
    }

    // ── ShouldRefreshAfterSubscribe ──────────────────────────────────────────

    [Fact]
    public void ShouldRefreshAfterSubscribe_SuccessVerified_ReturnsTrue()
    {
        Assert.True(ShouldRefreshAfterSubscribe(BillingPurchaseStatus.Success, SyncOutcome.Verified));
    }

    [Fact]
    public void ShouldRefreshAfterSubscribe_SuccessSyncFailed_ReturnsFalse()
    {
        Assert.False(ShouldRefreshAfterSubscribe(BillingPurchaseStatus.Success, SyncOutcome.SyncFailed));
    }

    [Fact]
    public void ShouldRefreshAfterSubscribe_SuccessVerificationFailed_ReturnsFalse()
    {
        Assert.False(ShouldRefreshAfterSubscribe(BillingPurchaseStatus.Success, SyncOutcome.VerificationFailed));
    }

    [Fact]
    public void ShouldRefreshAfterSubscribe_CancelledVerified_ReturnsFalse()
    {
        Assert.False(ShouldRefreshAfterSubscribe(BillingPurchaseStatus.Cancelled, SyncOutcome.Verified));
    }

    // ── RestoreToastMessage ──────────────────────────────────────────────────

    [Fact]
    public void RestoreToastMessage_NoPurchases_ReturnsEmpty()
    {
        Assert.Equal(AppLanguage.SettingsRestoreEmpty, RestoreToastMessage(RestoreOutcome.NoPurchases));
    }

    [Fact]
    public void RestoreToastMessage_AllFailed_ReturnsAllFailed()
    {
        Assert.Equal(AppLanguage.SettingsRestoreAllFailed, RestoreToastMessage(RestoreOutcome.AllSyncFailed));
    }

    [Fact]
    public void RestoreToastMessage_Partial_ReturnsPartial()
    {
        Assert.Equal(AppLanguage.SettingsRestorePartial, RestoreToastMessage(RestoreOutcome.PartialSuccess));
    }

    [Fact]
    public void RestoreToastMessage_FullSuccess_ReturnsSuccess()
    {
        Assert.Equal(AppLanguage.SettingsRestoreSuccess, RestoreToastMessage(RestoreOutcome.FullSuccess));
    }

    [Fact]
    public void ClassifyRestoreOutcome_MixedDonateAndSubscribeVerified_ReturnsFullSuccess()
    {
        // Restore list may contain donate (Verified via completed) and subscribe (Verified via active).
        // Both map to SyncOutcome.Verified before ClassifyRestoreOutcome is called.
        var outcomes = new List<SyncOutcome>
        {
            SyncOutcome.Verified, SyncOutcome.Verified
        };
        Assert.Equal(RestoreOutcome.FullSuccess, ClassifyRestoreOutcome(outcomes));
    }

    [Fact]
    public void ClassifyRestoreOutcome_DonateVerifiedSubscribeFailed_ReturnsPartialSuccess()
    {
        var outcomes = new List<SyncOutcome>
        {
            SyncOutcome.Verified, SyncOutcome.VerificationFailed
        };
        Assert.Equal(RestoreOutcome.PartialSuccess, ClassifyRestoreOutcome(outcomes));
    }

    // ── ShouldRefreshAfterRestore ────────────────────────────────────────────

    [Theory]
    [InlineData(RestoreOutcome.FullSuccess,   true)]
    [InlineData(RestoreOutcome.PartialSuccess, true)]
    [InlineData(RestoreOutcome.AllSyncFailed,  false)]
    [InlineData(RestoreOutcome.NoPurchases,    false)]
    public void ShouldRefreshAfterRestore_ExpectedBehavior(RestoreOutcome outcome, bool expected)
    {
        Assert.Equal(expected, ShouldRefreshAfterRestore(outcome));
    }

    // ── Key invariant: fake-success impossible ───────────────────────────────

    [Fact]
    public void DonateToastMessage_SuccessWithNullSyncOutcome_ReturnsSyncFailed()
    {
        // If Play says success but we somehow have no sync outcome, show failure not success.
        var msg = DonateToastMessage(BillingPurchaseStatus.Success, null);
        Assert.NotEqual(AppLanguage.SettingsDonateSuccess, msg);
        Assert.NotEqual(AppLanguage.SettingsPurchaseSuccess, msg);
    }
}
