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
		LblRestore.Text = AppLanguage.SettingsRestorePurchases;
		LblRestoreDesc.Text = AppLanguage.SettingsRestorePurchasesDesc;
		LblManageSub.Text = AppLanguage.SettingsManageSubscription;
		LblManageSubDesc.Text = AppLanguage.SettingsManageSubscriptionDesc;
		DonatePickerTitle.Text = AppLanguage.SettingsDonateChooseAmount;
		ToastText.Text = AppLanguage.SettingsComingSoonToast;
		BtnDonateCancel.Text = AppLanguage.SettingsCancel;
	}

	private async Task InitBillingStateAsync()
	{
		// Check premium status from API
		var status = await _api.GetBillingStatusAsync();
		if (status.Success && status.Data is not null)
		{
			_isPremium = status.Data.IsPremiumActive;
			UpdatePremiumUI();
		}

		// Connect billing client
		await _billing.ConnectAsync();
	}

	private void UpdatePremiumUI()
	{
		if (_isPremium)
		{
			LblSubscribeBadge.Text = AppLanguage.SettingsPremiumActive;
			ManageSubCard.IsVisible = true;
		}
		else
		{
			LblSubscribeBadge.Text = "";
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
		if (!_billing.IsAvailable)
		{
			await ShowToast(AppLanguage.SettingsBillingUnavailable);
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

	private async void OnDonate1Clicked(object? sender, EventArgs e) => await ExecuteDonateAsync("donate_1");
	private async void OnDonate5Clicked(object? sender, EventArgs e) => await ExecuteDonateAsync("donate_5");
	private async void OnDonate10Clicked(object? sender, EventArgs e) => await ExecuteDonateAsync("donate_10");
	private async void OnDonate20Clicked(object? sender, EventArgs e) => await ExecuteDonateAsync("donate_20");

	private async Task ExecuteDonateAsync(string productId)
	{
		await DonateOverlay.FadeTo(0, 200, Easing.CubicIn);
		DonateOverlay.IsVisible = false;

		var result = await _billing.PurchaseDonationAsync(productId);
		if (result.Status == BillingPurchaseStatus.Success && result.Purchase is not null)
		{
			await SyncPurchaseAsync(result.Purchase);
			await ShowToast(AppLanguage.SettingsPurchaseSuccess);
		}
		else if (result.Status == BillingPurchaseStatus.Cancelled)
		{
			await ShowToast(AppLanguage.SettingsPurchaseCancelled);
		}
		else if (result.Status == BillingPurchaseStatus.Unavailable)
		{
			await ShowToast(AppLanguage.SettingsBillingUnavailable);
		}
		else
		{
			await ShowToast(AppLanguage.SettingsPurchaseError);
		}
	}

	// ── Subscribe ───────────────────────────────────────────

	private async void OnSubscribeClicked(object? sender, TappedEventArgs e)
	{
		if (_isPremium)
		{
			OnManageSubscriptionClicked(sender, e);
			return;
		}

		if (!_billing.IsAvailable)
		{
			await ShowToast(AppLanguage.SettingsBillingUnavailable);
			return;
		}

		// Plugin.InAppBilling cannot select base plans/offers yet.
		// Use a single honest subscription flow until real plan selection is supported.
		await ExecuteSubscribeAsync("freaklete_premium");
	}

	private async Task ExecuteSubscribeAsync(string productId)
	{
		var result = await _billing.PurchaseSubscriptionAsync(productId, "");
		if (result.Status == BillingPurchaseStatus.Success && result.Purchase is not null)
		{
			await SyncPurchaseAsync(result.Purchase);
			await RefreshBillingStatusAsync();
			await ShowToast(AppLanguage.SettingsPurchaseSuccess);
		}
		else if (result.Status == BillingPurchaseStatus.Cancelled)
		{
			await ShowToast(AppLanguage.SettingsPurchaseCancelled);
		}
		else if (result.Status == BillingPurchaseStatus.Unavailable)
		{
			await ShowToast(AppLanguage.SettingsBillingUnavailable);
		}
		else
		{
			await ShowToast(AppLanguage.SettingsPurchaseError);
		}
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

		foreach (var purchase in purchases)
		{
			await SyncPurchaseAsync(purchase);
		}

		await RefreshBillingStatusAsync();
		await ShowToast(AppLanguage.SettingsRestoreSuccess);
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

	// ── Sync + Refresh ──────────────────────────────────────

	private async Task SyncPurchaseAsync(BillingPurchaseRecord purchase)
	{
		await _api.SyncGooglePlayPurchaseAsync(new GooglePlaySyncRequest
		{
			ProductId = purchase.ProductId,
			BasePlanId = purchase.BasePlanId,
			PurchaseToken = purchase.PurchaseToken,
			OrderId = purchase.OrderId,
			PurchaseState = purchase.PurchaseState,
			IsAcknowledged = purchase.IsAcknowledged,
			RawPayloadJson = purchase.RawJson
		});
	}

	private async Task RefreshBillingStatusAsync()
	{
		var status = await _api.GetBillingStatusAsync();
		if (status.Success && status.Data is not null)
		{
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
