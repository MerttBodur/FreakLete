using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class HomePage : ContentPage
{
	private readonly AppDatabase _database;
	private readonly UserSession _session;

	public HomePage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadHomeDataAsync();
	}

	private async Task LoadHomeDataAsync()
	{
		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			WelcomeLabel.Text = "WELCOME";
			WorkoutTotalLabel.Text = "0";
			AthleticCountLabel.Text = "0";
			LatestPrLabel.Text = "No PR saved yet.";
			return;
		}

		User? user = await _database.GetUserByIdAsync(currentUserId.Value);
		if (user is not null)
		{
			WelcomeLabel.Text = $"WELCOME, {user.FirstName.ToUpperInvariant()}";
		}

		int workoutCount = await _database.GetWorkoutCountByUserAsync(currentUserId.Value);
		List<AthleticPerformanceEntry> athleticEntries = await _database.GetAthleticPerformanceEntriesByUserAsync(currentUserId.Value);
		List<PrEntry> prEntries = await _database.GetPrEntriesByUserAsync(currentUserId.Value);

		WorkoutTotalLabel.Text = workoutCount.ToString();
		AthleticCountLabel.Text = athleticEntries.Count.ToString();
		LatestPrLabel.Text = prEntries.Count > 0
			? $"{prEntries[0].Weight} x {prEntries[0].Reps} RIR{prEntries[0].RIR.GetValueOrDefault()}"
			: "No PR saved yet.";
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new WorkoutPage(), true);
	}

	private async void OnOpenOneRmClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new OneRmPage(), true);
	}
}
