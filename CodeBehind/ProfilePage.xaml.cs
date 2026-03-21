using FreakLete.Data;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

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
	private ExerciseCatalogItem? _selectedPerformanceItem;
	private ExerciseCatalogItem? _selectedGoalItem;

	public ProfilePage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();

		GymExperiencePicker.ItemsSource = ExperienceLevels;
		UpdatePerformanceSelectionUI();
		UpdateGoalSelectionUI();
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
	}

	private async Task LoadStatsAsync(int userId)
	{
		int workoutCount = await _database.GetWorkoutCountByUserAsync(userId);
		int oneRmPrCount = await _database.GetPrCountByUserAsync(userId);

		WorkoutCountLabel.Text = workoutCount.ToString();
		OneRmPrCountLabel.Text = oneRmPrCount.ToString();
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
			GroundContactTimeMs = entry.GroundContactTimeMs,
			ConcentricTimeSeconds = entry.ConcentricTimeSeconds,
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
			MovementCategory = goal.MovementCategory,
			GoalMetricLabel = goal.GoalMetricLabel,
			TargetValue = goal.TargetValue,
			Unit = goal.Unit,
			Text = string.IsNullOrWhiteSpace(goal.GoalMetricLabel)
				? $"{goal.MovementName}: {goal.TargetValue:0.##} {goal.Unit}"
				: $"{goal.MovementName}: {goal.GoalMetricLabel} {goal.TargetValue:0.##} {goal.Unit}"
		}).ToList();

		BindableLayout.SetItemsSource(MovementGoalsList, items);
		MovementGoalsEmptyLabel.IsVisible = items.Count == 0;
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

		if (weight.HasValue && (weight.Value < 20 || weight.Value > 400))
		{
			ShowError("Weight must be between 20 and 400 kg.");
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

		bool parsed = MetricInput.TryParseFlexibleDouble(PerformanceValueEntry.Text, out double value);
		if (!parsed || value <= 0)
		{
			ShowError($"{_selectedPerformanceItem.PrimaryLabel} must be a positive number.");
			return;
		}

		double? secondaryValue = null;
		if (_selectedPerformanceItem.HasSecondaryMetric)
		{
			if (!MetricInput.TryParseFlexibleDouble(PerformanceSecondaryValueEntry.Text, out double parsedSecondary) || parsedSecondary <= 0)
			{
				ShowError($"{_selectedPerformanceItem.SecondaryLabel} must be a positive number.");
				return;
			}

			secondaryValue = parsedSecondary;
		}

		double? groundContactTime = null;
		double? concentricTime = null;
		if (_selectedPerformanceItem.SupportsGroundContactTime && !string.IsNullOrWhiteSpace(PerformanceTimingEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(PerformanceTimingEntry.Text, out double parsedGctSeconds) || parsedGctSeconds <= 0)
			{
				ShowError("Ground contact time must be a positive number.");
				return;
			}

			groundContactTime = MetricInput.SecondsToMilliseconds(parsedGctSeconds);
		}

		if (_selectedPerformanceItem.SupportsConcentricTime && !string.IsNullOrWhiteSpace(PerformanceTimingEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(PerformanceTimingEntry.Text, out double parsedTime) || parsedTime <= 0)
			{
				ShowError("Concentric time must be a positive number.");
				return;
			}

			concentricTime = parsedTime;
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
			SecondaryUnit = _selectedPerformanceItem.SecondaryUnit,
			GroundContactTimeMs = groundContactTime,
			ConcentricTimeSeconds = concentricTime
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

		bool confirmed = await ConfirmDialogPage.ShowAsync(
			Navigation,
			"Delete Entry",
			$"Delete '{item.Text}'?",
			"Delete",
			"Cancel");
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
		_selectedPerformanceItem = ExerciseCatalog.GetByNameAndCategory(item.MovementName, item.MovementCategory);
		UpdatePerformanceSelectionUI();
		PerformanceValueEntry.Text = item.Value.ToString("0.##");
		PerformanceSecondaryValueEntry.Text = item.SecondaryValue?.ToString("0.##") ?? string.Empty;
		PerformanceTimingEntry.Text = item.GroundContactTimeMs.HasValue
			? MetricInput.MillisecondsToSeconds(item.GroundContactTimeMs.Value).ToString("0.##")
			: item.ConcentricTimeSeconds?.ToString("0.##") ?? string.Empty;
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

		if (_selectedGoalItem is null)
		{
			ShowError("Choose a movement before saving a goal.");
			return;
		}

		string movementName = _selectedGoalItem.Name;
		string unit = ResolveGoalUnit(_selectedGoalItem);
		bool parsed = MetricInput.TryParseFlexibleDouble(GoalTargetValueEntry.Text, out double targetValue);

		if (string.IsNullOrWhiteSpace(movementName) || string.IsNullOrWhiteSpace(unit) || !parsed || targetValue <= 0)
		{
			ShowError("Goal movement and target value are required, and target must be positive.");
			return;
		}

		MovementGoal goal = new()
		{
			Id = _editingGoalId.GetValueOrDefault(),
			UserId = _currentUser.Id,
			MovementName = movementName,
			MovementCategory = _selectedGoalItem.Category,
			GoalMetricLabel = ResolveGoalLabel(_selectedGoalItem),
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

		bool confirmed = await ConfirmDialogPage.ShowAsync(
			Navigation,
			"Delete Goal",
			$"Delete '{item.Text}'?",
			"Delete",
			"Cancel");
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
		_selectedGoalItem = ExerciseCatalog.GetByNameAndCategory(item.MovementName, item.MovementCategory);
		UpdateGoalSelectionUI();
		GoalTargetValueEntry.Text = item.TargetValue.ToString("0.##");
		GoalActionButton.Text = "Update";
		GoalCancelButton.IsVisible = true;
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

		bool confirmed = await ConfirmDialogPage.ShowAsync(
			Navigation,
			"Delete Account",
			"This will permanently delete your profile, workouts, PRs, goals, and athletic performance records.",
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
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await TabNavigationHelper.ResetToRootAsync(Navigation, () => new LoginPage(), false);
		});
	}

	private static double? ParseNullableDouble(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		return MetricInput.TryParseFlexibleDouble(text, out double value) ? value : null;
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
		PerformanceTimingEntry.Text = string.Empty;
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

	private async void OnChooseGoalMovementClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				"Choose Goal Movement",
				ExerciseCatalog.Categories,
				OnGoalMovementSelected),
			true);
	}

	private void OnGoalMovementSelected(ExerciseCatalogItem item)
	{
		_selectedGoalItem = item;
		UpdateGoalSelectionUI();
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
			PerformanceTimingContainer.IsVisible = false;
			return;
		}

		SelectedPerformanceLabel.Text = _selectedPerformanceItem.Name;
		SelectedPerformanceHintLabel.Text = _selectedPerformanceItem.SelectionHintText;
		PerformanceMetric1Label.Text = $"{_selectedPerformanceItem.PrimaryLabel} ({_selectedPerformanceItem.PrimaryUnit})";
		PerformanceValueEntry.Placeholder = $"Enter {_selectedPerformanceItem.PrimaryLabel.ToLowerInvariant()}";
		PerformanceMetric2Container.IsVisible = _selectedPerformanceItem.HasSecondaryMetric;
		PerformanceMetric2Label.Text = $"{_selectedPerformanceItem.SecondaryLabel} ({_selectedPerformanceItem.SecondaryUnit})";
		PerformanceSecondaryValueEntry.Placeholder = $"Enter {_selectedPerformanceItem.SecondaryLabel.ToLowerInvariant()}";
		PerformanceTimingContainer.IsVisible = _selectedPerformanceItem.SupportsGroundContactTime || _selectedPerformanceItem.SupportsConcentricTime;
		PerformanceTimingLabel.Text = _selectedPerformanceItem.SupportsGroundContactTime
			? "Ground Contact Time (s)"
			: "Concentric Time (s)";
		PerformanceTimingEntry.Placeholder = "Optional";
	}

	private void UpdateGoalSelectionUI()
	{
		if (_selectedGoalItem is null)
		{
			SelectedGoalLabel.Text = "No movement selected";
			SelectedGoalHintLabel.Text = "Browse the exercise catalog and set a target on the movement's main metric.";
			GoalUnitLabel.Text = "-";
			GoalTargetValueEntry.Placeholder = "Target value";
			return;
		}

		SelectedGoalLabel.Text = _selectedGoalItem.Name;
		SelectedGoalHintLabel.Text = $"{_selectedGoalItem.SelectionHintText} | Goal metric: {ResolveGoalLabel(_selectedGoalItem)}";
		GoalUnitLabel.Text = ResolveGoalUnit(_selectedGoalItem);
		GoalTargetValueEntry.Placeholder = $"Target {ResolveGoalLabel(_selectedGoalItem).ToLowerInvariant()}";
	}

	private static string ResolveGoalLabel(ExerciseCatalogItem item)
	{
		return MovementGoalRules.ResolveGoalLabel(
			item.Category,
			item.HasSecondaryMetric,
			item.PrimaryLabel,
			item.SecondaryLabel);
	}

	private static string ResolveGoalUnit(ExerciseCatalogItem item)
	{
		return MovementGoalRules.ResolveGoalUnit(
			item.Category,
			item.HasSecondaryMetric,
			item.PrimaryUnit,
			item.SecondaryUnit);
	}

	private static string FormatAthleticPerformanceText(AthleticPerformanceEntry entry)
	{
		ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory(entry.MovementName, entry.MovementCategory);
		string primary = item is not null
			? $"{item.PrimaryLabel}: {entry.Value:0.##} {entry.Unit}"
			: $"{entry.Value:0.##} {entry.Unit}";

		if (item is not null && item.HasSecondaryMetric && entry.SecondaryValue.HasValue)
		{
			string text = $"{entry.MovementName}: {primary} | {item.SecondaryLabel}: {entry.SecondaryValue:0.##} {entry.SecondaryUnit}";
			if (entry.GroundContactTimeMs.HasValue)
			{
				text += $" | GCT: {MetricInput.FormatSecondsFromMilliseconds(entry.GroundContactTimeMs.Value)}";
			}

			if (entry.ConcentricTimeSeconds.HasValue)
			{
				text += $" | Concentric: {entry.ConcentricTimeSeconds.Value:0.##} s";
			}

			return $"{text} ({entry.RecordedAt:dd.MM.yyyy})";
		}

		string singleMetricText = $"{entry.MovementName}: {primary}";
		if (entry.GroundContactTimeMs.HasValue)
		{
			singleMetricText += $" | GCT: {MetricInput.FormatSecondsFromMilliseconds(entry.GroundContactTimeMs.Value)}";
		}

		if (entry.ConcentricTimeSeconds.HasValue)
		{
			singleMetricText += $" | Concentric: {entry.ConcentricTimeSeconds.Value:0.##} s";
		}

		return $"{singleMetricText} ({entry.RecordedAt:dd.MM.yyyy})";
	}

	private void ResetGoalForm()
	{
		_editingGoalId = null;
		_selectedGoalItem = null;
		UpdateGoalSelectionUI();
		GoalTargetValueEntry.Text = string.Empty;
		GoalActionButton.Text = "Save";
		GoalCancelButton.IsVisible = false;
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
		public double? GroundContactTimeMs { get; set; }
		public double? ConcentricTimeSeconds { get; set; }
		public DateTime RecordedAt { get; set; }
	}

	private sealed class MovementGoalListItem : TextListItem
	{
		public string MovementName { get; set; } = string.Empty;
		public string MovementCategory { get; set; } = string.Empty;
		public string GoalMetricLabel { get; set; } = string.Empty;
		public double TargetValue { get; set; }
		public string Unit { get; set; } = string.Empty;
	}
}
