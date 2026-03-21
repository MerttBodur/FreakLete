using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace GymTracker;

public partial class RegisterPage : ContentPage
{
	private readonly AppDatabase _database;

	public RegisterPage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
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
			ShowError("Please fill in all fields.");
			return;
		}

		if (!email.Contains('@') || !email.Contains('.'))
		{
			ShowError("Please enter a valid email address.");
			return;
		}

		if (password.Length < 8)
		{
			ShowError("Password must be at least 8 characters.");
			return;
		}

		if (!password.Any(char.IsUpper))
		{
			ShowError("Password must include at least 1 uppercase letter.");
			return;
		}

		if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
		{
			ShowError("Password must include at least 1 special character.");
			return;
		}

		if (password != confirmPassword)
		{
			ShowError("Passwords do not match.");
			return;
		}

		bool emailExists = await _database.EmailExistsAsync(email);
		if (emailExists)
		{
			ShowError("This email is already registered.");
			return;
		}

		User user = new()
		{
			FirstName = firstName,
			LastName = lastName,
			Email = email,
			PasswordHash = PasswordHasher.HashPassword(password)
		};

		await _database.CreateUserAsync(user);
		await DisplayAlertAsync("Success", "Account created. You can now log in.", "OK");
		await Navigation.PopAsync(false);
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
