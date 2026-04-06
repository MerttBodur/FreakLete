using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace FreakLete;

public partial class ChangePasswordPage : ContentPage
{
	private readonly ApiClient _api;

	public ChangePasswordPage(string? prefillEmail = null)
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();

		if (!string.IsNullOrWhiteSpace(prefillEmail))
			EmailEntry.Text = prefillEmail;
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
			ShowError("Tüm alanları doldurun.");
			return;
		}

		if (!email.Contains('@') || !email.Contains('.'))
		{
			ShowError("Geçerli bir e-posta adresi girin.");
			return;
		}

		if (newPassword.Length < 8)
		{
			ShowError("Yeni şifre en az 8 karakter olmalıdır.");
			return;
		}

		if (!newPassword.Any(char.IsUpper))
		{
			ShowError("Yeni şifre en az 1 büyük harf içermelidir.");
			return;
		}

		if (!Regex.IsMatch(newPassword, @"[^a-zA-Z0-9]"))
		{
			ShowError("Yeni şifre en az 1 özel karakter içermelidir.");
			return;
		}

		if (newPassword != newPasswordRepeat)
		{
			ShowError("Yeni şifreler eşleşmiyor.");
			return;
		}

		SubmitButton.IsEnabled = false;

		var result = await _api.ChangePasswordAsync(email, currentPassword, newPassword, newPasswordRepeat);

		SubmitButton.IsEnabled = true;

		if (result.Success)
		{
			CurrentPasswordEntry.Text = string.Empty;
			NewPasswordEntry.Text = string.Empty;
			NewPasswordRepeatEntry.Text = string.Empty;
			ShowSuccess("Şifre başarıyla değiştirildi.");
		}
		else
		{
			ShowError(result.Error ?? "Şifre değiştirilemedi.");
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
