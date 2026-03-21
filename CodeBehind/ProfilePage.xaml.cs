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

	private static readonly string[] AthleticCategories =
	[
		ExerciseCatalog.Sprint,
		ExerciseCatalog.Jumps,
		ExerciseCatalog.Plyometrics,
		ExerciseCatalog.OlympicLifts
	];

	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private User? _currentUser;
	private int? _editingPerformanceId;
	private int? _editingGoalId;
	private int? _editingProfilePrId;
	private ExerciseCatalogItem? _selectedPerformanceItem;

	public ProfilePage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();

		GymExperiencePicker.ItemsSource = ExperienceLevels;
		UpdatePerformanceSelectionUI();
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

		WorkoutCountLabel.Text = workoutCount.ToString();
		OneRmPrCountLabel.Text = oneRmPrCount.ToString();
		ProfilePrCountLabel.Text = profilePrCount.ToString();
	}

	private async Task LoadAthleticPerformancesAsync(int userId)
	{
		List<AthleticPerformanceEntry> entries = await _database.GetAthleticPerformanceEntriesByUserAsync(userId);
		List<AthleticPerformanceListItem> items = entries.Select(entry => new AthleticPerformanceListItem
		{
			Id = entry.Id,
			MovementName = entry.MovementName,
			MovementCategory = entry.MovementCategory,
			Value = entry.Value,
			Unit = entry.Unit,
			SecondaryValue = entry.SecondaryValue,
			SecondaryUnit = entry.SecondaryUnit,
			RecordedAt = entry.RecordedAt,
			Text = FormatAthleticPerformanceText(entry)
		}).ToList();

		BindableLayout.SetItemsSource(AthleticPerformanceList, items);
		AthleticPerformanceEmptyLabel.IsVisible = items.Count == 0;
	}

	private async Task LoadMovementGoalsAsync(int userId)
	{
		List<MovementGoal> goals = await _database.GetMovementGoalsByUserAsync(userId);
		List<MovementGoalListItem> items = goals.Select(goal => new MovementGoalListItem
		{
			Id = goal.Id,
			MovementName = goal.MovementName,
			TargetValue = goal.TargetValue,
			Unit = goal.Unit,
			Text = $"{goal.MovementName}: {goal.TargetValue:0.##} {goal.Unit}"
		}).ToList();

		BindableLayout.SetItemsSource(MovementGoalsList, items);
		MovementGoalsEmptyLabel.IsVisible = items.Count == 0;
	}

	private async Task LoadProfilePrsAsync(int userId)
	{
		List<ProfilePrEntry> entries = await _database.GetProfilePrEntriesByUserAsync(userId);
		List<ProfilePrListItem> items = entries.Select(entry => new ProfilePrListItem
		{
			Id = entry.Id,
			MovementName = entry.MovementName,
			Value = entry.Value,
			Unit = entry.Unit,
			RecordedAt = entry.RecordedAt,
			Text = $"{entry.MovementName}: {entry.Value:0.##} {entry.Unit} ({entry.RecordedAt:dd.MM.yyyy})"
		}).ToList();

		BindableLayout.SetItemsSource(ProfilePrList, items);
		ProfilePrEmptyLabel.IsVisible = items.Count == 0;
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

		if (_selectedPerformanceItem is null)
		{
			ShowError("Choose a movement and enter a valid result.");
			return;
		}

		bool parsed = double.TryParse(PerformanceValueEntry.Text, out double value);
		if (!parsed)
		{
			ShowError($"{_selectedPerformanceItem.PrimaryLabel} is required.");
			return;
		}

		double? secondaryValue = null;
		if (_selectedPerformanceItem.HasSecondaryMetric)
		{
			if (!double.TryParse(PerformanceSecondaryValueEntry.Text, out double parsedSecondary))
			{
				ShowError($"{_selectedPerformanceItem.SecondaryLabel} is required.");
				return;
			}

			secondaryValue = parsedSecondary;
		}

		AthleticPerformanceEntry entry = new()
		{
			Id = _editingPerformanceId.GetValueOrDefault(),
			UserId = _currentUser.Id,
			MovementName = _selectedPerformanceItem.Name,
			MovementCategory = _selectedPerformanceItem.Category,
			Value = value,
			Unit = _selectedPerformanceItem.PrimaryUnit,
			SecondaryValue = secondaryValue,
			SecondaryUnit = _selectedPerformanceItem.SecondaryUnit
		};

		if (_editingPerformanceId.HasValue)
		{
			await _database.UpdateAthleticPerformanceEntryAsync(entry);
			ShowSuccess("Athletic performance updated.");
		}
		else
		{
			await _database.SaveAthleticPerformanceEntryAsync(entry);
			ShowSuccess("Athletic performance added.");
		}

		ResetPerformanceForm();
		await LoadAthleticPerformancesAsync(_currentUser.Id);
	}

	private async void OnDeletePerformanceInvoked(object? sender, EventArgs e)
	{
		if (_currentUser is null || GetBindingContext<AthleticPerformanceListItem>(sender) is not AthleticPerformanceListItem item)
		{
			return;
		}

		bool confirmed = await DisplayAlertAsync("Delete Entry", $"Delete '{item.Text}'?", "Delete", "Cancel");
		if (!confirmed)
		{
			return;
		}

		await _database.DeleteAthleticPerformanceEntryAsync(item.Id);
		if (_editingPerformanceId == item.Id)
		{
			ResetPerformanceForm();
		}
		await LoadAthleticPerformancesAsync(_currentUser.Id);
		ShowSuccess("Athletic performance deleted.");
	}

	private void OnEditPerformanceInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<AthleticPerformanceListItem>(sender) is not AthleticPerformanceListItem item)
		{
			return;
		}

		_editingPerformanceId = item.Id;
		_selectedPerformanceItem = ExerciseCatalog.GetByName(item.MovementName);
		UpdatePerformanceSelectionUI();
		PerformanceValueEntry.Text = item.Value.ToString("0.##");
		PerformanceSecondaryValueEntry.Text = item.SecondaryValue?.ToString("0.##") ?? string.Empty;
		PerformanceActionButton.Text = "Update";
		PerformanceCancelButton.IsVisible = true;
		ShowSuccess($"Editing: {item.Text}");
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
			Id = _editingGoalId.GetValueOrDefault(),
			UserId = _currentUser.Id,
			MovementName = movementName,
			TargetValue = targetValue,
			Unit = unit
		};

		if (_editingGoalId.HasValue)
		{
			await _database.UpdateMovementGoalAsync(goal);
			ShowSuccess("Movement goal updated.");
		}
		else
		{
			await _database.SaveMovementGoalAsync(goal);
			ShowSuccess("Movement goal saved.");
		}

		ResetGoalForm();
		await LoadMovementGoalsAsync(_currentUser.Id);
	}

	private async void OnDeleteGoalInvoked(object? sender, EventArgs e)
	{
		if (_currentUser is null || GetBindingContext<MovementGoalListItem>(sender) is not MovementGoalListItem item)
		{
			return;
		}

		bool confirmed = await DisplayAlertAsync("Delete Goal", $"Delete '{item.Text}'?", "Delete", "Cancel");
		if (!confirmed)
		{
			return;
		}

		await _database.DeleteMovementGoalAsync(item.Id);
		if (_editingGoalId == item.Id)
		{
			ResetGoalForm();
		}
		await LoadMovementGoalsAsync(_currentUser.Id);
		ShowSuccess("Movement goal deleted.");
	}

	private void OnEditGoalInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<MovementGoalListItem>(sender) is not MovementGoalListItem item)
		{
			return;
		}

		_editingGoalId = item.Id;
		GoalMovementEntry.Text = item.MovementName;
		GoalTargetValueEntry.Text = item.TargetValue.ToString("0.##");
		GoalUnitEntry.Text = item.Unit;
		GoalActionButton.Text = "Update";
		GoalCancelButton.IsVisible = true;
		ShowSuccess($"Editing: {item.Text}");
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
			Id = _editingProfilePrId.GetValueOrDefault(),
			UserId = _currentUser.Id,
			MovementName = movementName,
			Value = value,
			Unit = unit
		};

		if (_editingProfilePrId.HasValue)
		{
			await _database.UpdateProfilePrEntryAsync(entry);
			ShowSuccess("Profile PR updated.");
		}
		else
		{
			await _database.SaveProfilePrEntryAsync(entry);
			ShowSuccess("Profile PR added.");
		}

		ResetProfilePrForm();
		await LoadProfilePrsAsync(_currentUser.Id);
		await LoadStatsAsync(_currentUser.Id);
	}

	private async void OnDeleteProfilePrInvoked(object? sender, EventArgs e)
	{
		if (_currentUser is null || GetBindingContext<ProfilePrListItem>(sender) is not ProfilePrListItem item)
		{
			return;
		}

		bool confirmed = await DisplayAlertAsync("Delete PR", $"Delete '{item.Text}'?", "Delete", "Cancel");
		if (!confirmed)
		{
			return;
		}

		await _database.DeleteProfilePrEntryAsync(item.Id);
		if (_editingProfilePrId == item.Id)
		{
			ResetProfilePrForm();
		}
		await LoadProfilePrsAsync(_currentUser.Id);
		await LoadStatsAsync(_currentUser.Id);
		ShowSuccess("Profile PR deleted.");
	}

	private void OnEditProfilePrInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<ProfilePrListItem>(sender) is not ProfilePrListItem item)
		{
			return;
		}

		_editingProfilePrId = item.Id;
		ProfilePrMovementEntry.Text = item.MovementName;
		ProfilePrValueEntry.Text = item.Value.ToString("0.##");
		ProfilePrUnitEntry.Text = item.Unit;
		ProfilePrActionButton.Text = "Update";
		ProfilePrCancelButton.IsVisible = true;
		ShowSuccess($"Editing: {item.Text}");
	}

	private void OnCancelPerformanceEditClicked(object? sender, EventArgs e)
	{
		ResetPerformanceForm();
		ClearStatus();
	}

	private void OnCancelGoalEditClicked(object? sender, EventArgs e)
	{
		ResetGoalForm();
		ClearStatus();
	}

	private void OnCancelProfilePrEditClicked(object? sender, EventArgs e)
	{
		ResetProfilePrForm();
		ClearStatus();
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

	private async void OnDeleteAccountClicked(object? sender, EventArgs e)
	{
		if (_currentUser is null)
		{
			return;
		}

		bool confirmed = await DisplayAlertAsync(
			"Delete Account",
			"This will permanently delete your profile, workouts, PRs, and athletic performance records.",
			"Delete",
			"Cancel");

		if (!confirmed)
		{
			return;
		}

		await _database.DeleteUserAsync(_currentUser.Id);
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

	private static TItem? GetBindingContext<TItem>(object? sender) where TItem : class
	{
		return sender switch
		{
			BindableObject bindable when bindable.BindingContext is TItem item => item,
			_ => null
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

	private void ResetPerformanceForm()
	{
		_editingPerformanceId = null;
		_selectedPerformanceItem = null;
		UpdatePerformanceSelectionUI();
		PerformanceValueEntry.Text = string.Empty;
		PerformanceSecondaryValueEntry.Text = string.Empty;
		PerformanceActionButton.Text = "Save";
		PerformanceCancelButton.IsVisible = false;
	}

	private async void OnChoosePerformanceMovementClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				"Choose Movement",
				AthleticCategories,
				OnPerformanceMovementSelected),
			true);
	}

	private void OnPerformanceMovementSelected(ExerciseCatalogItem item)
	{
		_selectedPerformanceItem = item;
		UpdatePerformanceSelectionUI();
	}

	private void UpdatePerformanceSelectionUI()
	{
		if (_selectedPerformanceItem is null)
		{
			SelectedPerformanceLabel.Text = "No movement selected";
			SelectedPerformanceHintLabel.Text = "Browse sprint, jump, plyo, and Olympic lift movements.";
			PerformanceMetric1Label.Text = "Result";
			PerformanceValueEntry.Placeholder = "Enter result";
			PerformanceMetric2Container.IsVisible = false;
			return;
		}

		SelectedPerformanceLabel.Text = _selectedPerformanceItem.Name;
		SelectedPerformanceHintLabel.Text = $"{_selectedPerformanceItem.Category} | {_selectedPerformanceItem.HintText}";
		PerformanceMetric1Label.Text = $"{_selectedPerformanceItem.PrimaryLabel} ({_selectedPerformanceItem.PrimaryUnit})";
		PerformanceValueEntry.Placeholder = $"Enter {_selectedPerformanceItem.PrimaryLabel.ToLowerInvariant()}";
		PerformanceMetric2Container.IsVisible = _selectedPerformanceItem.HasSecondaryMetric;
		PerformanceMetric2Label.Text = $"{_selectedPerformanceItem.SecondaryLabel} ({_selectedPerformanceItem.SecondaryUnit})";
		PerformanceSecondaryValueEntry.Placeholder = $"Enter {_selectedPerformanceItem.SecondaryLabel.ToLowerInvariant()}";
	}

	private static string FormatAthleticPerformanceText(AthleticPerformanceEntry entry)
	{
		ExerciseCatalogItem? item = ExerciseCatalog.GetByName(entry.MovementName);
		string primary = item is not null
			? $"{item.PrimaryLabel}: {entry.Value:0.##} {entry.Unit}"
			: $"{entry.Value:0.##} {entry.Unit}";

		if (item is not null && item.HasSecondaryMetric && entry.SecondaryValue.HasValue)
		{
			return $"{entry.MovementName}: {primary} | {item.SecondaryLabel}: {entry.SecondaryValue:0.##} {entry.SecondaryUnit} ({entry.RecordedAt:dd.MM.yyyy})";
		}

		return $"{entry.MovementName}: {primary} ({entry.RecordedAt:dd.MM.yyyy})";
	}

	private void ResetGoalForm()
	{
		_editingGoalId = null;
		GoalMovementEntry.Text = string.Empty;
		GoalTargetValueEntry.Text = string.Empty;
		GoalUnitEntry.Text = string.Empty;
		GoalActionButton.Text = "Save";
		GoalCancelButton.IsVisible = false;
	}

	private void ResetProfilePrForm()
	{
		_editingProfilePrId = null;
		ProfilePrMovementEntry.Text = string.Empty;
		ProfilePrValueEntry.Text = string.Empty;
		ProfilePrUnitEntry.Text = string.Empty;
		ProfilePrActionButton.Text = "Save PR";
		ProfilePrCancelButton.IsVisible = false;
	}

	private class TextListItem
	{
		public int Id { get; set; }
		public string Text { get; set; } = string.Empty;
	}

	private sealed class AthleticPerformanceListItem : TextListItem
	{
		public string MovementName { get; set; } = string.Empty;
		public string MovementCategory { get; set; } = string.Empty;
		public double Value { get; set; }
		public string Unit { get; set; } = string.Empty;
		public double? SecondaryValue { get; set; }
		public string SecondaryUnit { get; set; } = string.Empty;
		public DateTime RecordedAt { get; set; }
	}

	private sealed class MovementGoalListItem : TextListItem
	{
		public string MovementName { get; set; } = string.Empty;
		public double TargetValue { get; set; }
		public string Unit { get; set; } = string.Empty;
	}

	private sealed class ProfilePrListItem : TextListItem
	{
		public string MovementName { get; set; } = string.Empty;
		public double Value { get; set; }
		public string Unit { get; set; } = string.Empty;
		public DateTime RecordedAt { get; set; }
	}
}
