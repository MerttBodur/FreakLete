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
		WelcomeLabel.Text = $"WELCOME, {profile.FirstName.ToUpperInvariant()}";
		WorkoutTotalLabel.Text = profile.TotalWorkouts.ToString();

		var perfResult = await _api.GetAthleticPerformancesAsync();
		AthleticCountLabel.Text = perfResult.Success && perfResult.Data is not null
			? perfResult.Data.Count.ToString()
			: "0";

		LatestPrLabel.Text = "No PR saved yet.";
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
