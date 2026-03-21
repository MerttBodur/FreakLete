using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Security;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class LoginPage : ContentPage
{
	private readonly AppDatabase _database;
	private readonly UserSession _session;

	public LoginPage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		ClearError();

		string email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
		string password = PasswordEntry.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			ShowError("Please enter your email and password.");
			return;
		}

		User? user = await _database.GetUserByEmailAsync(email);
		if (user is null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
		{
			ShowError("Invalid email or password.");
			return;
		}

		_session.SignIn(user.Id);

		Window? window = Application.Current?.Windows.FirstOrDefault();
		if (window is not null)
		{
			window.Page = new NavigationPage(new HomePage());
		}
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		ClearError();
		await Navigation.PushAsync(new RegisterPage(), false);
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
