using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace FreakLete;

public partial class ChangePasswordPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly UserSession _session;

	public ChangePasswordPage(string? prefillEmail = null)
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();

		if (!string.IsNullOrWhiteSpace(prefillEmail))
			EmailEntry.Text = prefillEmail;

		ApplyLanguage();
	}

	private void ApplyLanguage()
	{
		PageTitle.Text = AppLanguage.ChangePasswordTitle;
		CpEmailLabel.Text = AppLanguage.ChangePasswordEmail;
		EmailEntry.Placeholder = AppLanguage.ChangePasswordEmailPlaceholder;
		CpCurrentLabel.Text = AppLanguage.ChangePasswordCurrent;
		CurrentPasswordEntry.Placeholder = AppLanguage.ChangePasswordCurrentPlaceholder;
		CpNewLabel.Text = AppLanguage.ChangePasswordNew;
		NewPasswordEntry.Placeholder = AppLanguage.ChangePasswordNewPlaceholder;
		CpRepeatLabel.Text = AppLanguage.ChangePasswordRepeat;
		NewPasswordRepeatEntry.Placeholder = AppLanguage.ChangePasswordRepeatPlaceholder;
		CpRulesLabel.Text = AppLanguage.ChangePasswordRules;
		SubmitButton.Text = AppLanguage.ChangePasswordButton;
	}

	private async void OnSubmitClicked(object? sender, EventArgs e)
	{
		ClearMessages();

		string email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
		string currentPassword = CurrentPasswordEntry.Text ?? string.Empty;
		string newPassword = NewPasswordEntry.Text ?? string.Empty;
		string newPasswordRepeat = NewPasswordRepeatEntry.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) ||
			string.IsNullOrWhiteSpace(currentPassword) ||
			string.IsNullOrWhiteSpace(newPassword) ||
			string.IsNullOrWhiteSpace(newPasswordRepeat))
		{
			ShowError(AppLanguage.ChangePasswordErrorEmpty);
			return;
		}

		if (!email.Contains('@') || !email.Contains('.'))
		{
			ShowError(AppLanguage.ChangePasswordErrorEmail);
			return;
		}

		if (newPassword.Length < 8)
		{
			ShowError(AppLanguage.ChangePasswordErrorLength);
			return;
		}

		if (!newPassword.Any(char.IsUpper))
		{
			ShowError(AppLanguage.ChangePasswordErrorUpper);
			return;
		}

		if (!Regex.IsMatch(newPassword, @"[^a-zA-Z0-9]"))
		{
			ShowError(AppLanguage.ChangePasswordErrorSpecial);
			return;
		}

		if (newPassword != newPasswordRepeat)
		{
			ShowError(AppLanguage.ChangePasswordErrorMismatch);
			return;
		}

		SubmitButton.IsEnabled = false;

		var result = await _api.ChangePasswordAsync(email, currentPassword, newPassword, newPasswordRepeat);

		SubmitButton.IsEnabled = true;

		if (result.Success)
		{
			_session.SignOut();
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await TabNavigationHelper.ResetToRootAsync(Navigation, () => new LoginPage(), false);
			});
		}
		else
		{
			ShowError(result.Error ?? AppLanguage.ChangePasswordErrorFailed);
		}
	}

	private void ShowError(string message)
	{
		ErrorLabel.Text = message;
		ErrorLabel.IsVisible = true;
		SuccessLabel.IsVisible = false;
	}

	private void ShowSuccess(string message)
	{
		SuccessLabel.Text = message;
		SuccessLabel.IsVisible = true;
		ErrorLabel.IsVisible = false;
	}

	private void ClearMessages()
	{
		ErrorLabel.IsVisible = false;
		SuccessLabel.IsVisible = false;
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PopAsync(true);
	}
}
