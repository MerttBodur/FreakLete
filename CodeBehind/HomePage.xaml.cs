using FreakLete.Data;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

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
			_session.SignOut();
			return;
		}

		User? user = await _database.GetUserByIdAsync(currentUserId.Value);
		if (user is null)
		{
			_session.SignOut();
			return;
		}

		WelcomeLabel.Text = $"WELCOME, {user.FirstName.ToUpperInvariant()}";

		int workoutCount = await _database.GetWorkoutCountByUserAsync(currentUserId.Value);
		List<AthleticPerformanceEntry> athleticEntries = await _database.GetAthleticPerformanceEntriesByUserAsync(currentUserId.Value);
		List<PrEntry> prEntries = await _database.GetPrEntriesByUserAsync(currentUserId.Value);

		WorkoutTotalLabel.Text = workoutCount.ToString();
		AthleticCountLabel.Text = athleticEntries.Count.ToString();
		LatestPrLabel.Text = prEntries.Count > 0
			? FormatLatestPr(prEntries[0])
			: "No PR saved yet.";
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new WorkoutPage(), true);
	}

	private async void OnOpenCalculationsClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CalculationsPage(), true);
	}

	private static string FormatLatestPr(PrEntry entry)
	{
		if (entry.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			string text = $"{entry.ExerciseName}: {entry.Metric1Value:0.##} {entry.Metric1Unit}";
			if (entry.Metric2Value.HasValue && !string.IsNullOrWhiteSpace(entry.Metric2Unit))
			{
				text += $" | {entry.Metric2Value:0.##} {entry.Metric2Unit}";
			}

			return text;
		}

		return entry.RIR.HasValue
			? $"{entry.ExerciseName}: {entry.Weight} x {entry.Reps} RIR{entry.RIR.Value}"
			: $"{entry.ExerciseName}: {entry.Weight} x {entry.Reps}";
	}
}
