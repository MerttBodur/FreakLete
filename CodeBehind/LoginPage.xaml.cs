using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class LoginPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly UserSession _session;

	public LoginPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		ApplyLanguage();
	}

	private void ApplyLanguage()
	{
		WelcomeLabel.Text = AppLanguage.LoginWelcome;
		SubtitleLabel.Text = AppLanguage.LoginSubtitle;
		EmailLabel.Text = AppLanguage.LoginEmail;
		EmailEntry.Placeholder = AppLanguage.LoginEmailPlaceholder;
		PasswordLabel.Text = AppLanguage.LoginPassword;
		PasswordEntry.Placeholder = AppLanguage.LoginPasswordPlaceholder;
		LoginBtn.Text = AppLanguage.LoginButton;
		SignUpBtn.Text = AppLanguage.LoginSignUp;
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		ClearError();

		string email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
		string password = PasswordEntry.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			ShowError(AppLanguage.LoginErrorEmpty);
			return;
		}

		var result = await _api.LoginAsync(email, password);

		if (result.Success && result.Data is not null)
		{
			_session.SignIn(result.Data.UserId, result.Data.Token, result.Data.Email, result.Data.FirstName);
			await TabNavigationHelper.ResetToRootAsync(Navigation, () => new HomePage(), false);
		}
		else
		{
			ShowError(result.Error ?? AppLanguage.LoginErrorFailed);
		}
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		ClearError();
		await Navigation.PushAsync(new RegisterPage(), true);
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
}
