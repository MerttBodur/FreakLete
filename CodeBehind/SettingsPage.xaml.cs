using FreakLete.Services;

namespace FreakLete;

public partial class SettingsPage : ContentPage
{
	private readonly string? _userEmail;
	private readonly IBillingService _billing;
	private readonly ApiClient _api;
	private bool _isPremium;

	public SettingsPage(string? userEmail = null)
	{
		InitializeComponent();
		_userEmail = userEmail;
		_billing = MauiProgram.Services.GetRequiredService<IBillingService>();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		ApplyLanguage();
		_ = InitBillingStateAsync();
	}

	private void ApplyLanguage()
	{
		PageTitle.Text = AppLanguage.SettingsTitle;
		LblChangePassword.Text = AppLanguage.SettingsChangePassword;
		LblChangePasswordDesc.Text = AppLanguage.SettingsChangePasswordDesc;
		LblLanguage.Text = AppLanguage.SettingsLanguage;
		LblLanguageDesc.Text = AppLanguage.SettingsLanguageDesc;
		LblLanguageCurrent.Text = AppLanguage.SettingsLanguageCurrent;
		LblReview.Text = AppLanguage.SettingsLeaveReview;
		LblReviewDesc.Text = AppLanguage.SettingsLeaveReviewDesc;
		LblReviewBadge.Text = AppLanguage.SettingsComingSoon;
		LblDonate.Text = AppLanguage.SettingsDonate;
		LblDonateDesc.Text = AppLanguage.SettingsDonateDesc;
		LblSubscribe.Text = AppLanguage.SettingsSubscribe;
		LblSubscribeDesc.Text = AppLanguage.SettingsSubscribeDesc;
		SubscribePickerTitle.Text = AppLanguage.SettingsChoosePlan;
		BtnSubscribeMonthly.Text = AppLanguage.SettingsPlanMonthly;
		BtnSubscribeAnnual.Text = AppLanguage.SettingsPlanAnnual;
		LblRestore.Text = AppLanguage.SettingsRestorePurchases;
		LblRestoreDesc.Text = AppLanguage.SettingsRestorePurchasesDesc;
		LblManageSub.Text = AppLanguage.SettingsManageSubscription;
		LblManageSubDesc.Text = AppLanguage.SettingsManageSubscriptionDesc;
		LblCurrentPlan.Text = AppLanguage.SettingsCurrentPlan;
		DonatePickerTitle.Text = AppLanguage.SettingsDonateChooseAmount;
		ToastText.Text = AppLanguage.SettingsComingSoonToast;
		BtnDonateCancel.Text = AppLanguage.SettingsCancel;
	}

	private BillingStatusResponse? _billingData;

	private async Task InitBillingStateAsync()
	{
		var status = await _api.GetBillingStatusAsync();
		if (status.Success && status.Data is not null)
		{
			_billingData = status.Data;
			_isPremium = status.Data.IsPremiumActive;
			UpdatePremiumUI();
		}

		await _billing.ConnectAsync();
	}

	private void UpdatePremiumUI()
	{
		CurrentPlanCard.IsVisible = true;
		LblCurrentPlan.Text = AppLanguage.SettingsCurrentPlan;

		if (_isPremium)
		{
			LblSubscribeBadge.Text = AppLanguage.SettingsPremiumActive;
			LblCurrentPlanBadge.Text = AppLanguage.SettingsPlanPremium;
			ManageSubCard.IsVisible = true;

			if (_billingData?.SubscriptionEndsAtUtc is { } endDate)
				LblCurrentPlanDesc.Text = AppLanguage.FormatRenewalDate(endDate);
			else
				LblCurrentPlanDesc.Text = AppLanguage.SettingsPlanPremium;
		}
		else
		{
			LblSubscribeBadge.Text = "";
			LblCurrentPlanBadge.Text = AppLanguage.SettingsPlanFree;
			LblCurrentPlanDesc.Text = AppLanguage.SettingsCurrentPlanDesc;
			ManageSubCard.IsVisible = false;
		}
	}

	// ── Navigation ──────────────────────────────────────────

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	private async void OnChangePasswordClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(new ChangePasswordPage(_userEmail), true);
	}

	// ── Language ────────────────────────────────────────────

	private async void OnLanguageClicked(object? sender, TappedEventArgs e)
	{
		LangPickerTitle.Text = AppLanguage.SettingsSelectLanguage;
		BtnLangCancel.Text = AppLanguage.SettingsCancel;

		bool isTr = AppLanguage.Code == "tr";
		Style secondary = (Style)Application.Current!.Resources["SecondaryButton"];
		BtnLangEnglish.Style = isTr ? null : secondary;
		BtnLangTurkish.Style = isTr ? secondary : null;

		LanguageOverlay.IsVisible = true;
		await LanguageOverlay.FadeTo(1, 200, Easing.CubicOut);
	}

	private async void OnLanguageOverlayDismiss(object? sender, EventArgs e)
	{
		await LanguageOverlay.FadeTo(0, 200, Easing.CubicIn);
		LanguageOverlay.IsVisible = false;
	}

	private async void OnLangEnglishClicked(object? sender, EventArgs e)
	{
		AppLanguage.SetLanguage("en");
		ApplyLanguage();
		await LanguageOverlay.FadeTo(0, 200, Easing.CubicIn);
		LanguageOverlay.IsVisible = false;
	}

	private async void OnLangTurkishClicked(object? sender, EventArgs e)
	{
		AppLanguage.SetLanguage("tr");
		ApplyLanguage();
		await LanguageOverlay.FadeTo(0, 200, Easing.CubicIn);
		LanguageOverlay.IsVisible = false;
	}

	// ── Donate ──────────────────────────────────────────────

	private async void OnDonateClicked(object? sender, TappedEventArgs e)
	{
		if (!SettingsBillingAvailability.CanOpenDonatePicker(_billing))
		{
			await ShowToast(SettingsBillingAvailability.GetDonateUnavailableMessage());
			return;
		}

		DonateOverlay.IsVisible = true;
		await DonateOverlay.FadeTo(1, 200, Easing.CubicOut);
	}

	private async void OnDonateOverlayDismiss(object? sender, EventArgs e)
	{
		await DonateOverlay.FadeTo(0, 200, Easing.CubicIn);
		DonateOverlay.IsVisible = false;
	}

	private async void OnDonate1Clicked(object? sender, EventArgs e)  => await ExecuteDonateAsync("donate_1");
	private async void OnDonate5Clicked(object? sender, EventArgs e)  => await ExecuteDonateAsync("donate_5");
	private async void OnDonate10Clicked(object? sender, EventArgs e) => await ExecuteDonateAsync("donate_10");
	private async void OnDonate20Clicked(object? sender, EventArgs e) => await ExecuteDonateAsync("donate_20");

	private async Task ExecuteDonateAsync(string productId)
	{
		await DonateOverlay.FadeTo(0, 200, Easing.CubicIn);
		DonateOverlay.IsVisible = false;

		var result = await _billing.PurchaseDonationAsync(productId);

		SettingsBillingLogic.SyncOutcome? syncOutcome = null;

		if (result.Status == BillingPurchaseStatus.Success && result.Purchase is not null)
		{
			// Play purchase succeeded — now sync with backend.
			// Sync failure must NOT show success toast.
			var syncResult = await _api.SyncGooglePlayPurchaseAsync(new GooglePlaySyncRequest
			{
				ProductId      = result.Purchase.ProductId,
				BasePlanId     = result.Purchase.BasePlanId,
				PurchaseToken  = result.Purchase.PurchaseToken,
				OrderId        = result.Purchase.OrderId,
				PurchaseState  = result.Purchase.PurchaseState,
				IsAcknowledged = result.Purchase.IsAcknowledged,
				RawPayloadJson = result.Purchase.RawJson
			});

			syncOutcome = SettingsBillingLogic.ClassifyDonateSyncResult(syncResult);
		}

		var message = SettingsBillingLogic.DonateToastMessage(result.Status, syncOutcome);
		await ShowToast(message);
	}

	// ── Subscribe ───────────────────────────────────────────

	private async void OnSubscribeClicked(object? sender, TappedEventArgs e)
	{
		if (_isPremium)
		{
			OnManageSubscriptionClicked(sender, e);
			return;
		}

		if (!SettingsBillingAvailability.CanOpenSubscribePicker(_billing))
		{
			await ShowToast(SettingsBillingAvailability.GetSubscribeUnavailableMessage());
			return;
		}

		SubscribePickerTitle.Text = AppLanguage.SettingsChoosePlan;
		BtnSubscribeMonthly.Text  = AppLanguage.SettingsPlanMonthly;
		BtnSubscribeAnnual.Text   = AppLanguage.SettingsPlanAnnual;
		BtnSubscribeCancel.Text   = AppLanguage.SettingsCancel;

		SubscribeOverlay.IsVisible = true;
		await SubscribeOverlay.FadeTo(1, 200, Easing.CubicOut);
	}

	private async void OnSubscribeOverlayDismiss(object? sender, EventArgs e)
	{
		await SubscribeOverlay.FadeTo(0, 200, Easing.CubicIn);
		SubscribeOverlay.IsVisible = false;
	}

	private async void OnSubscribeMonthlyClicked(object? sender, EventArgs e)
	{
		await SubscribeOverlay.FadeTo(0, 200, Easing.CubicIn);
		SubscribeOverlay.IsVisible = false;
		await ExecuteSubscribeAsync("freaklete_premium", "monthly");
	}

	private async void OnSubscribeAnnualClicked(object? sender, EventArgs e)
	{
		await SubscribeOverlay.FadeTo(0, 200, Easing.CubicIn);
		SubscribeOverlay.IsVisible = false;
		await ExecuteSubscribeAsync("freaklete_premium", "annual");
	}

	private async Task ExecuteSubscribeAsync(string productId, string basePlanId)
	{
		if (!SettingsBillingAvailability.CanOpenSubscribePicker(_billing))
		{
			await ShowToast(SettingsBillingAvailability.GetSubscribeUnavailableMessage());
			return;
		}

		var result = await _billing.PurchaseSubscriptionAsync(productId, basePlanId);

		SettingsBillingLogic.SyncOutcome syncOutcome = SettingsBillingLogic.SyncOutcome.SyncFailed;

		if (result.Status == BillingPurchaseStatus.Success && result.Purchase is not null)
		{
			var syncResult = await _api.SyncGooglePlayPurchaseAsync(new GooglePlaySyncRequest
			{
				ProductId      = result.Purchase.ProductId,
				BasePlanId     = result.Purchase.BasePlanId,
				PurchaseToken  = result.Purchase.PurchaseToken,
				OrderId        = result.Purchase.OrderId,
				PurchaseState  = result.Purchase.PurchaseState,
				IsAcknowledged = result.Purchase.IsAcknowledged,
				RawPayloadJson = result.Purchase.RawJson
			});

			syncOutcome = SettingsBillingLogic.ClassifySyncResult(syncResult);

			// Only refresh premium UI when backend has verified the purchase.
			if (SettingsBillingLogic.ShouldRefreshAfterSubscribe(result.Status, syncOutcome))
				await RefreshBillingStatusAsync();
		}

		var message = SettingsBillingLogic.SubscribeToastMessage(result.Status, syncOutcome);
		await ShowToast(message);
	}

	// ── Restore ─────────────────────────────────────────────

	private async void OnRestorePurchasesClicked(object? sender, TappedEventArgs e)
	{
		if (!_billing.IsAvailable)
		{
			await ShowToast(AppLanguage.SettingsBillingUnavailable);
			return;
		}

		var purchases = await _billing.RestorePurchasesAsync();

		if (purchases.Count == 0)
		{
			await ShowToast(AppLanguage.SettingsRestoreEmpty);
			return;
		}

		// Sync each purchase and track individual outcomes.
		var outcomes = new List<SettingsBillingLogic.SyncOutcome>();

		foreach (var purchase in purchases)
		{
			var syncResult = await _api.SyncGooglePlayPurchaseAsync(new GooglePlaySyncRequest
			{
				ProductId      = purchase.ProductId,
				BasePlanId     = purchase.BasePlanId,
				PurchaseToken  = purchase.PurchaseToken,
				OrderId        = purchase.OrderId,
				PurchaseState  = purchase.PurchaseState,
				IsAcknowledged = purchase.IsAcknowledged,
				RawPayloadJson = purchase.RawJson
			});

			outcomes.Add(SettingsBillingLogic.ClassifySyncResult(syncResult));
		}

		var restoreOutcome = SettingsBillingLogic.ClassifyRestoreOutcome(outcomes);

		// Only refresh premium UI if at least one purchase was verified.
		if (SettingsBillingLogic.ShouldRefreshAfterRestore(restoreOutcome))
			await RefreshBillingStatusAsync();

		await ShowToast(SettingsBillingLogic.RestoreToastMessage(restoreOutcome));
	}

	// ── Manage Subscription ─────────────────────────────────

	private async void OnManageSubscriptionClicked(object? sender, EventArgs e)
	{
		try
		{
			await Browser.Default.OpenAsync(
				"https://play.google.com/store/account/subscriptions",
				BrowserLaunchMode.External);
		}
		catch
		{
			await ShowToast(AppLanguage.SettingsPurchaseError);
		}
	}

	// ── Billing refresh ─────────────────────────────────────

	private async Task RefreshBillingStatusAsync()
	{
		var status = await _api.GetBillingStatusAsync();
		if (status.Success && status.Data is not null)
		{
			_billingData = status.Data;
			_isPremium = status.Data.IsPremiumActive;
			UpdatePremiumUI();
		}
	}

	// ── Coming soon + Toast ─────────────────────────────────

	private bool _toastShowing;

	private async void OnComingSoonClicked(object? sender, TappedEventArgs e)
	{
		await ShowToast(AppLanguage.SettingsComingSoonToast);
	}

	private async Task ShowToast(string message)
	{
		if (_toastShowing) return;
		_toastShowing = true;

		ToastText.Text = message;
		ToastBanner.IsVisible = true;
		await ToastBanner.FadeTo(1, 200, Easing.CubicOut);
		await Task.Delay(1800);
		await ToastBanner.FadeTo(0, 300, Easing.CubicIn);
		ToastBanner.IsVisible = false;

		_toastShowing = false;
	}
}
