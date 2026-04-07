using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace FreakLete;

public partial class RegisterPage : ContentPage
{
	private readonly ApiClient _api;

	public RegisterPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		ApplyLanguage();
	}

	private void ApplyLanguage()
	{
		TopHeader.Title = AppLanguage.RegisterTitle;
		EyebrowLabel.Text = AppLanguage.RegisterEyebrow;
		HeadlineLabel.Text = AppLanguage.RegisterHeadline;
		SubtitleLabel.Text = AppLanguage.RegisterSubtitle;
		FirstNameLabel.Text = AppLanguage.RegisterFirstName;
		FirstNameEntry.Placeholder = AppLanguage.RegisterFirstNamePlaceholder;
		LastNameLabel.Text = AppLanguage.RegisterLastName;
		LastNameEntry.Placeholder = AppLanguage.RegisterLastNamePlaceholder;
		RegEmailLabel.Text = AppLanguage.LoginEmail;
		EmailEntry.Placeholder = AppLanguage.LoginEmailPlaceholder;
		RegPasswordLabel.Text = AppLanguage.LoginPassword;
		PasswordEntry.Placeholder = AppLanguage.RegisterPasswordPlaceholder;
		ConfirmPasswordLabel.Text = AppLanguage.RegisterConfirmPassword;
		ConfirmPasswordEntry.Placeholder = AppLanguage.RegisterConfirmPasswordPlaceholder;
		PasswordRulesLabel.Text = AppLanguage.RegisterPasswordRules;
		SignUpBtn.Text = AppLanguage.RegisterButton;
	}

	private async void OnCreateAccountClicked(object? sender, EventArgs e)
	{
		ClearError();

		string firstName = FirstNameEntry.Text?.Trim() ?? string.Empty;
		string lastName = LastNameEntry.Text?.Trim() ?? string.Empty;
		string email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
		string password = PasswordEntry.Text?.Trim() ?? string.Empty;
		string confirmPassword = ConfirmPasswordEntry.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(firstName) ||
			string.IsNullOrWhiteSpace(lastName) ||
			string.IsNullOrWhiteSpace(email) ||
			string.IsNullOrWhiteSpace(password) ||
			string.IsNullOrWhiteSpace(confirmPassword))
		{
			ShowError(AppLanguage.RegisterErrorEmpty);
			return;
		}

		if (!email.Contains('@') || !email.Contains('.'))
		{
			ShowError(AppLanguage.RegisterErrorEmail);
			return;
		}

		if (password.Length < 8)
		{
			ShowError(AppLanguage.RegisterErrorPasswordLength);
			return;
		}

		if (!password.Any(char.IsUpper))
		{
			ShowError(AppLanguage.RegisterErrorPasswordUpper);
			return;
		}

		if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
		{
			ShowError(AppLanguage.RegisterErrorPasswordSpecial);
			return;
		}

		if (password != confirmPassword)
		{
			ShowError(AppLanguage.RegisterErrorPasswordMismatch);
			return;
		}

		var result = await _api.RegisterAsync(firstName, lastName, email, password);

		if (result.Success)
		{
			await MessageDialogPage.ShowAsync(
				Navigation,
				AppLanguage.RegisterSuccessTitle,
				AppLanguage.RegisterSuccessMessage,
				buttonText: AppLanguage.RegisterSuccessButton);
			await Navigation.PopAsync(true);
		}
		else
		{
			ShowError(result.Error ?? AppLanguage.RegisterErrorFailed);
		}
	}

	private void ShowError(string message)
	{
		ErrorLabel.Text = message;
		ErrorLabel.IsVisible = true;
	}

	private void ClearError()
	{
		ErrorLabel.Text = string.Empty;
		ErrorLabel.IsVisible = false;
	}

	private async void OnHeaderBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}
}
