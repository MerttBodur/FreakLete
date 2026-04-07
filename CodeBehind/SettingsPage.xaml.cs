using FreakLete.Services;

namespace FreakLete;

public partial class SettingsPage : ContentPage
{
	private readonly string? _userEmail;

	public SettingsPage(string? userEmail = null)
	{
		InitializeComponent();
		_userEmail = userEmail;
		ApplyLanguage();
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
		LblDonateBadge.Text = AppLanguage.SettingsComingSoon;
		LblSubscribe.Text = AppLanguage.SettingsSubscribe;
		LblSubscribeDesc.Text = AppLanguage.SettingsSubscribeDesc;
		LblSubscribeBadge.Text = AppLanguage.SettingsComingSoon;
		ToastText.Text = AppLanguage.SettingsComingSoonToast;
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	private async void OnChangePasswordClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(new ChangePasswordPage(_userEmail), true);
	}

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

	private bool _toastShowing;

	private async void OnComingSoonClicked(object? sender, TappedEventArgs e)
	{
		if (_toastShowing) return;
		_toastShowing = true;

		ToastBanner.IsVisible = true;
		await ToastBanner.FadeTo(1, 200, Easing.CubicOut);
		await Task.Delay(1800);
		await ToastBanner.FadeTo(0, 300, Easing.CubicIn);
		ToastBanner.IsVisible = false;

		_toastShowing = false;
	}
}
