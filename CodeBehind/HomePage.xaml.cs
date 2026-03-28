using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class HomePage : ContentPage
{
	private readonly ApiClient _api;
	private readonly UserSession _session;
	private WorkoutResponse? _latestWorkout;

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

		// Load profile
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

		// Load PR entries for latest PR stat
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

		// Build weekly activity chart data
		await LoadWeeklyActivityAsync();

		// Load and display featured workout
		await LoadFeaturedWorkoutAsync();
	}

	private async Task LoadWeeklyActivityAsync()
	{
		try
		{
			var weeklyItems = new List<ChartItem>();
			var today = DateTime.Now.Date;
			double maxActivityValue = 0;

			// Get workouts for past 7 days
			var workoutCounts = new Dictionary<string, int>();
			string[] dayLabels = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

			for (int i = 6; i >= 0; i--)
			{
				var targetDate = today.AddDays(-i);
				var dayLabel = dayLabels[(int)targetDate.DayOfWeek == 0 ? 6 : ((int)targetDate.DayOfWeek - 1)];

				var workoutResult = await _api.GetWorkoutsByDateAsync(targetDate);
				int count = 0;

				if (workoutResult.Success && workoutResult.Data is not null)
				{
					count = workoutResult.Data.Count;
				}

				weeklyItems.Add(new ChartItem
				{
					Label = dayLabel,
					Value = count
				});

				if (count > maxActivityValue)
					maxActivityValue = count;
			}

			// Set chart properties with real data only
			WeeklyActivityCard.MaxValue = maxActivityValue > 0 ? maxActivityValue : 1;
			WeeklyActivityCard.Items = weeklyItems;

			// Show honest summary: zero workouts or actual count
			int totalWorkouts = weeklyItems.Sum(x => (int)x.Value);
			if (totalWorkouts == 0)
			{
				WeeklyActivityCard.SummaryText = "No workouts this week yet";
			}
			else
			{
				WeeklyActivityCard.SummaryText = $"{totalWorkouts} workout{(totalWorkouts != 1 ? "s" : "")} this week";
			}
		}
		catch
		{
			// Show empty state on error
			WeeklyActivityCard.Items = new List<ChartItem>();
			WeeklyActivityCard.MaxValue = 1;
			WeeklyActivityCard.SummaryText = "Unable to load activity";
		}
	}

	private async Task LoadFeaturedWorkoutAsync()
	{
		try
		{
			// Get all workouts, sort by date descending
			var workoutResult = await _api.GetWorkoutsAsync();

			if (workoutResult.Success && workoutResult.Data is not null && workoutResult.Data.Count > 0)
			{
				// Get the most recent workout
				_latestWorkout = workoutResult.Data
					.OrderByDescending(w => w.WorkoutDate)
					.FirstOrDefault();

				if (_latestWorkout is not null)
				{
					// Update featured section
					FeaturedWorkoutNameLabel.Text = _latestWorkout.WorkoutName ?? "Untitled Workout";
					FeaturedDateLabel.Text = _latestWorkout.WorkoutDate.ToString("MMM dd, yyyy");
					FeaturedExerciseCountLabel.Text = _latestWorkout.Exercises?.Count.ToString() ?? "0";
					FeaturedViewButton.IsVisible = true;
					
					return;
				}
			}

			// No workouts found
			FeaturedWorkoutNameLabel.Text = "No recent workouts";
			FeaturedDateLabel.Text = "-";
			FeaturedExerciseCountLabel.Text = "-";
			FeaturedViewButton.IsVisible = false;
		}
		catch
		{
			// Show placeholder on error
			FeaturedWorkoutNameLabel.Text = "Unable to load";
			FeaturedDateLabel.Text = "-";
			FeaturedExerciseCountLabel.Text = "-";
			FeaturedViewButton.IsVisible = false;
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

	private async void OnViewFeaturedWorkoutClicked(object? sender, EventArgs e)
	{
		if (_latestWorkout is not null)
		{
			await Navigation.PushAsync(new WorkoutPage(), true);
		}
	}
}
