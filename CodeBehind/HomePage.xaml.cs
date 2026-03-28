using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class HomePage : ContentPage
{
	private readonly ApiClient _api;
	private readonly UserSession _session;

	public HomePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadHomeDataAsync();
	}

	private async Task LoadHomeDataAsync()
	{
		if (!_session.IsLoggedIn())
		{
			_session.SignOut();
			return;
		}

		var profileResult = await _api.GetProfileAsync();
		if (!profileResult.Success || profileResult.Data is null)
		{
			if (profileResult.StatusCode == 401)
			{
				_session.SignOut();
				await TabNavigationHelper.ResetToRootAsync(Navigation, () => new LoginPage(), false);
			}
			return;
		}

		var profile = profileResult.Data;
		UserNameLabel.Text = $"{profile.FirstName}";
		
		// Update dashboard stat tiles
		WorkoutCountTile.StatValue = profile.TotalWorkouts.ToString();
		StreakTile.StatValue = "0"; // Placeholder - would need streak endpoint

		// Latest PR tile
		var prResult = await _api.GetPrEntriesAsync();
		if (prResult.Success && prResult.Data is not null && prResult.Data.Count > 0)
		{
			var latest = prResult.Data[0]; // API returns ordered by CreatedAt desc
			LatestPrTile.StatValue = $"{latest.Weight}";
		}
		else
		{
			LatestPrTile.StatValue = "-";
		}
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new WorkoutPage(), true);
	}

	private async void OnOpenCalculationsClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CalculationsPage(), true);
	}
}
