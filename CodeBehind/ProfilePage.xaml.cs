using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class ProfilePage : ContentPage
{
	private static readonly string[] ExperienceLevels =
	[
		"< 1 year",
		"1-2 years",
		"3-4 years",
		"5+ years"
	];

	private static readonly string[] AthleticMovements =
	[
		"Single Broad Jump",
		"Vertical Jump",
		"40y Sprint Dash"
	];

	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private User? _currentUser;

	public ProfilePage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();

		GymExperiencePicker.ItemsSource = ExperienceLevels;
		PerformanceMovementPicker.ItemsSource = AthleticMovements;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProfileAsync();
	}

	private async Task LoadProfileAsync()
	{
		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			GoToLogin();
			return;
		}

		_currentUser = await _database.GetUserByIdAsync(currentUserId.Value);
		if (_currentUser is null)
		{
			_session.SignOut();
			GoToLogin();
			return;
		}

		FullNameLabel.Text = $"{_currentUser.FirstName} {_currentUser.LastName}";
		EmailLabel.Text = _currentUser.Email;

		DateTime dateOfBirth = _currentUser.DateOfBirth?.Date ?? DateTime.Today.AddYears(-18);
		DateOfBirthPicker.Date = dateOfBirth;
		UpdateAgeLabel(_currentUser.DateOfBirth);

		WeightEntry.Text = _currentUser.WeightKg?.ToString("0.##") ?? string.Empty;
		BodyFatEntry.Text = _currentUser.BodyFatPercentage?.ToString("0.##") ?? string.Empty;
		SportEntry.Text = _currentUser.SportName;

		if (!string.IsNullOrWhiteSpace(_currentUser.GymExperienceLevel))
		{
			GymExperiencePicker.SelectedItem = _currentUser.GymExperienceLevel;
		}
		else
		{
			GymExperiencePicker.SelectedIndex = -1;
		}

		await LoadStatsAsync(_currentUser.Id);
		await LoadAthleticPerformancesAsync(_currentUser.Id);
		await LoadMovementGoalsAsync(_currentUser.Id);
		await LoadProfilePrsAsync(_currentUser.Id);
	}

	private async Task LoadStatsAsync(int userId)
	{
		int workoutCount = await _database.GetWorkoutCountByUserAsync(userId);
		int oneRmPrCount = await _database.GetPrCountByUserAsync(userId);
		int profilePrCount = await _database.GetProfilePrCountByUserAsync(userId);

		WorkoutCountLabel.Text = $"Total Workouts: {workoutCount}";
		OneRmPrCountLabel.Text = $"1RM Saved Entries: {oneRmPrCount}";
		ProfilePrCountLabel.Text = $"Profile PR Entries: {profilePrCount}";
	}

	private async Task LoadAthleticPerformancesAsync(int userId)
	{
		List<AthleticPerformanceEntry> entries = await _database.GetAthleticPerformanceEntriesByUserAsync(userId);
		if (entries.Count == 0)
		{
			AthleticPerformanceLabel.Text = "No athletic performance records yet.";
			return;
		}

		AthleticPerformanceLabel.Text = string.Join(
			Environment.NewLine,
			entries.Select(entry => $"{entry.MovementName}: {entry.Value:0.##} {entry.Unit} ({entry.RecordedAt:dd.MM.yyyy})"));
	}

	private async Task LoadMovementGoalsAsync(int userId)
	{
		List<MovementGoal> goals = await _database.GetMovementGoalsByUserAsync(userId);
		if (goals.Count == 0)
		{
			MovementGoalsLabel.Text = "No movement goals set yet.";
			return;
		}

		MovementGoalsLabel.Text = string.Join(
			Environment.NewLine,
			goals.Select(goal => $"{goal.MovementName}: {goal.TargetValue:0.##} {goal.Unit}"));
	}

	private async Task LoadProfilePrsAsync(int userId)
	{
		List<ProfilePrEntry> entries = await _database.GetProfilePrEntriesByUserAsync(userId);
		if (entries.Count == 0)
		{
			ProfilePrLabel.Text = "No profile PR records yet.";
			return;
		}

		ProfilePrLabel.Text = string.Join(
			Environment.NewLine,
			entries.Select(entry => $"{entry.MovementName}: {entry.Value:0.##} {entry.Unit} ({entry.RecordedAt:dd.MM.yyyy})"));
	}

	private async void OnSaveProfileClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_currentUser is null)
		{
			return;
		}

		double? weight = ParseNullableDouble(WeightEntry.Text);
		double? bodyFat = ParseNullableDouble(BodyFatEntry.Text);

		if (WeightEntry.Text?.Length > 0 && !weight.HasValue)
		{
			ShowError("Weight must be a valid number.");
			return;
		}

		if (BodyFatEntry.Text?.Length > 0 && !bodyFat.HasValue)
		{
			ShowError("Body fat must be a valid number.");
			return;
		}

		if (bodyFat.HasValue && (bodyFat.Value < 0 || bodyFat.Value > 100))
		{
			ShowError("Body fat must be between 0 and 100.");
			return;
		}

		_currentUser.DateOfBirth = DateOfBirthPicker.Date;
		_currentUser.WeightKg = weight;
		_currentUser.BodyFatPercentage = bodyFat;
		_currentUser.SportName = SportEntry.Text?.Trim() ?? string.Empty;
		_currentUser.GymExperienceLevel = GymExperiencePicker.SelectedItem?.ToString() ?? string.Empty;

		await _database.UpdateUserAsync(_currentUser);
		UpdateAgeLabel(_currentUser.DateOfBirth);
		ShowSuccess("Profile saved.");
	}

	private async void OnAddPerformanceClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_currentUser is null)
		{
			return;
		}

		string movement = PerformanceMovementPicker.SelectedItem?.ToString() ?? string.Empty;
		bool parsed = double.TryParse(PerformanceValueEntry.Text, out double value);

		if (string.IsNullOrWhiteSpace(movement) || !parsed)
		{
			ShowError("Select a movement and enter a valid result.");
			return;
		}

		AthleticPerformanceEntry entry = new()
		{
			UserId = _currentUser.Id,
			MovementName = movement,
			Value = value,
			Unit = GetAthleticUnit(movement)
		};

		await _database.SaveAthleticPerformanceEntryAsync(entry);
		PerformanceValueEntry.Text = string.Empty;
		await LoadAthleticPerformancesAsync(_currentUser.Id);
		ShowSuccess("Athletic performance added.");
	}

	private async void OnSaveGoalClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_currentUser is null)
		{
			return;
		}

		string movementName = GoalMovementEntry.Text?.Trim() ?? string.Empty;
		string unit = GoalUnitEntry.Text?.Trim() ?? string.Empty;
		bool parsed = double.TryParse(GoalTargetValueEntry.Text, out double targetValue);

		if (string.IsNullOrWhiteSpace(movementName) || string.IsNullOrWhiteSpace(unit) || !parsed)
		{
			ShowError("Goal movement, target value, and unit are required.");
			return;
		}

		MovementGoal goal = new()
		{
			UserId = _currentUser.Id,
			MovementName = movementName,
			TargetValue = targetValue,
			Unit = unit
		};

		await _database.SaveMovementGoalAsync(goal);
		GoalMovementEntry.Text = string.Empty;
		GoalTargetValueEntry.Text = string.Empty;
		GoalUnitEntry.Text = string.Empty;
		await LoadMovementGoalsAsync(_currentUser.Id);
		ShowSuccess("Movement goal saved.");
	}

	private async void OnAddProfilePrClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_currentUser is null)
		{
			return;
		}

		string movementName = ProfilePrMovementEntry.Text?.Trim() ?? string.Empty;
		string unit = ProfilePrUnitEntry.Text?.Trim() ?? string.Empty;
		bool parsed = double.TryParse(ProfilePrValueEntry.Text, out double value);

		if (string.IsNullOrWhiteSpace(movementName) || string.IsNullOrWhiteSpace(unit) || !parsed)
		{
			ShowError("PR movement, value, and unit are required.");
			return;
		}

		ProfilePrEntry entry = new()
		{
			UserId = _currentUser.Id,
			MovementName = movementName,
			Value = value,
			Unit = unit
		};

		await _database.SaveProfilePrEntryAsync(entry);
		ProfilePrMovementEntry.Text = string.Empty;
		ProfilePrValueEntry.Text = string.Empty;
		ProfilePrUnitEntry.Text = string.Empty;
		await LoadProfilePrsAsync(_currentUser.Id);
		await LoadStatsAsync(_currentUser.Id);
		ShowSuccess("Profile PR added.");
	}

	private void OnDateOfBirthChanged(object? sender, DateChangedEventArgs e)
	{
		UpdateAgeLabel(e.NewDate);
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		_session.SignOut();
		GoToLogin();
	}

	private void GoToLogin()
	{
		Window? window = Application.Current?.Windows.FirstOrDefault();
		if (window is not null)
		{
			window.Page = new NavigationPage(new LoginPage());
		}
	}

	private static double? ParseNullableDouble(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		return double.TryParse(text, out double value) ? value : null;
	}

	private static string GetAthleticUnit(string movementName)
	{
		return movementName switch
		{
			"Single Broad Jump" => "cm",
			"Vertical Jump" => "cm",
			"40y Sprint Dash" => "s",
			_ => string.Empty
		};
	}

	private void UpdateAgeLabel(DateTime? dateOfBirth)
	{
		if (!dateOfBirth.HasValue)
		{
			AgeLabel.Text = "Age: -";
			return;
		}

		DateTime today = DateTime.Today;
		int age = today.Year - dateOfBirth.Value.Year;
		if (dateOfBirth.Value.Date > today.AddYears(-age))
		{
			age--;
		}

		AgeLabel.Text = $"Age: {age}";
	}

	private void ShowError(string message)
	{
		StatusLabel.TextColor = Colors.Red;
		StatusLabel.Text = message;
		StatusLabel.IsVisible = true;
	}

	private void ShowSuccess(string message)
	{
		StatusLabel.TextColor = Colors.LightGreen;
		StatusLabel.Text = message;
		StatusLabel.IsVisible = true;
	}

	private void ClearStatus()
	{
		StatusLabel.Text = string.Empty;
		StatusLabel.IsVisible = false;
	}
}
