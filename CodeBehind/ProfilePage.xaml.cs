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

	private static readonly string[] TrainingDaysOptions = ["1", "2", "3", "4", "5", "6", "7"];
	private static readonly string[] SessionDurationOptions = ["30", "45", "60", "75", "90", "120"];
	private static readonly string[] TrainingGoalOptions =
	[
		"Strength", "Hypertrophy", "Athletic Performance", "Fat Loss",
		"General Fitness", "Sport-Specific", "Powerlifting", "Olympic Weightlifting",
		"Rehab / Return to Training", "Body Recomposition"
	];
	private static readonly string[] DietaryPreferenceOptions =
	[
		"No preference", "Standard / Balanced", "High Protein", "Vegetarian",
		"Vegan", "Pescatarian", "Keto / Low Carb", "Mediterranean",
		"Intermittent Fasting", "Halal", "Kosher"
	];

	private static readonly string[] AthleticCategories =
	[
		ExerciseCatalog.Sprint,
		ExerciseCatalog.Jumps,
		ExerciseCatalog.Plyometrics,
		ExerciseCatalog.OlympicLifts
	];

	private readonly ApiClient _api;
	private readonly UserSession _session;
	private UserProfileResponse? _profile;
	private int? _editingPerformanceId;
	private int? _editingGoalId;
	private ExerciseCatalogItem? _selectedPerformanceItem;
	private ExerciseCatalogItem? _selectedGoalItem;
	private List<SportDefinitionResponse> _sportCatalog = [];
	private SportDefinitionResponse? _selectedSport;

	public ProfilePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();

		GymExperiencePicker.ItemsSource = ExperienceLevels;
		TrainingDaysPicker.ItemsSource = TrainingDaysOptions;
		SessionDurationPicker.ItemsSource = SessionDurationOptions;
		PrimaryGoalPicker.ItemsSource = TrainingGoalOptions;
		SecondaryGoalPicker.ItemsSource = TrainingGoalOptions;
		DietaryPreferencePicker.ItemsSource = DietaryPreferenceOptions;
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
		if (!_session.IsLoggedIn())
		{
			GoToLogin();
			return;
		}

		var result = await _api.GetProfileAsync();
		if (!result.Success || result.Data is null)
		{
			if (result.StatusCode == 401)
			{
				_session.SignOut();
				GoToLogin();
				return;
			}
			ShowError(result.Error ?? "Failed to load profile.");
			return;
		}

		_profile = result.Data;

		FullNameLabel.Text = $"{_profile.FirstName} {_profile.LastName}";
		EmailLabel.Text = _profile.Email;

		DateTime dateOfBirth = _profile.DateOfBirth?.Date ?? DateTime.Today.AddYears(-18);
		DateOfBirthPicker.Date = dateOfBirth;
		UpdateAgeLabel(_profile.DateOfBirth);

		WeightEntry.Text = _profile.WeightKg?.ToString("0.##") ?? string.Empty;
		BodyFatEntry.Text = _profile.BodyFatPercentage?.ToString("0.##") ?? string.Empty;

		await LoadSportCatalogAsync();
		SetSportPickerSelection(_profile.SportName, _profile.Position);

		if (!string.IsNullOrWhiteSpace(_profile.GymExperienceLevel))
		{
			GymExperiencePicker.SelectedItem = _profile.GymExperienceLevel;
		}
		else
		{
			GymExperiencePicker.SelectedIndex = -1;
		}

		WorkoutCountLabel.Text = _profile.TotalWorkouts.ToString();
		OneRmPrCountLabel.Text = _profile.TotalPrs.ToString();

		// Coach profile fields
		if (_profile.TrainingDaysPerWeek.HasValue)
			TrainingDaysPicker.SelectedItem = _profile.TrainingDaysPerWeek.Value.ToString();
		if (_profile.PreferredSessionDurationMinutes.HasValue)
			SessionDurationPicker.SelectedItem = _profile.PreferredSessionDurationMinutes.Value.ToString();
		if (!string.IsNullOrWhiteSpace(_profile.PrimaryTrainingGoal))
			PrimaryGoalPicker.SelectedItem = _profile.PrimaryTrainingGoal;
		if (!string.IsNullOrWhiteSpace(_profile.SecondaryTrainingGoal))
			SecondaryGoalPicker.SelectedItem = _profile.SecondaryTrainingGoal;
		EquipmentEditor.Text = _profile.AvailableEquipment;
		InjuryHistoryEditor.Text = _profile.InjuryHistory;
		CurrentPainEditor.Text = _profile.CurrentPainPoints;
		PhysicalLimitationsEditor.Text = _profile.PhysicalLimitations;
		if (!string.IsNullOrWhiteSpace(_profile.DietaryPreference))
			DietaryPreferencePicker.SelectedItem = _profile.DietaryPreference;

		await LoadAthleticPerformancesAsync();
		await LoadMovementGoalsAsync();
	}

	private async Task LoadAthleticPerformancesAsync()
	{
		var result = await _api.GetAthleticPerformancesAsync();
		if (!result.Success || result.Data is null)
		{
			AthleticPerformanceEmptyLabel.IsVisible = true;
			return;
		}

		List<AthleticPerformanceListItem> items = result.Data.Select(entry => new AthleticPerformanceListItem
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

	private async Task LoadMovementGoalsAsync()
	{
		var result = await _api.GetMovementGoalsAsync();
		if (!result.Success || result.Data is null)
		{
			MovementGoalsEmptyLabel.IsVisible = true;
			return;
		}

		List<MovementGoalListItem> items = result.Data.Select(goal => new MovementGoalListItem
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

		if (_profile is null)
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

		string dateStr = $"{DateOfBirthPicker.Date:yyyy-MM-dd}";
		string sportName = _selectedSport?.Name ?? string.Empty;
		string position = PositionPicker.SelectedItem?.ToString() ?? string.Empty;

		var profileData = new
		{
			dateOfBirth = dateStr,
			weightKg = weight,
			bodyFatPercentage = bodyFat,
			sportName,
			position,
			gymExperienceLevel = GymExperiencePicker.SelectedItem?.ToString() ?? string.Empty
		};

		var result = await _api.UpdateProfileAsync(profileData);
		if (result.Success)
		{
			UpdateAgeLabel(DateOfBirthPicker.Date);
			ShowSuccess("Profile saved.");
		}
		else
		{
			ShowError(result.Error ?? "Failed to save profile.");
		}
	}

	private async void OnSaveCoachProfileClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_profile is null) return;

		int? trainingDays = TrainingDaysPicker.SelectedItem is string td ? int.Parse(td) : null;
		int? sessionDuration = SessionDurationPicker.SelectedItem is string sd ? int.Parse(sd) : null;

		var coachData = new
		{
			trainingDaysPerWeek = trainingDays,
			preferredSessionDurationMinutes = sessionDuration,
			availableEquipment = EquipmentEditor.Text?.Trim() ?? "",
			physicalLimitations = PhysicalLimitationsEditor.Text?.Trim() ?? "",
			injuryHistory = InjuryHistoryEditor.Text?.Trim() ?? "",
			currentPainPoints = CurrentPainEditor.Text?.Trim() ?? "",
			primaryTrainingGoal = PrimaryGoalPicker.SelectedItem?.ToString() ?? "",
			secondaryTrainingGoal = SecondaryGoalPicker.SelectedItem?.ToString() ?? "",
			dietaryPreference = DietaryPreferencePicker.SelectedItem?.ToString() ?? ""
		};

		var result = await _api.UpdateProfileAsync(coachData);
		if (result.Success)
			ShowSuccess("Coach profile saved.");
		else
			ShowError(result.Error ?? "Failed to save coach profile.");
	}

	private async void OnAddPerformanceClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_profile is null)
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

		var data = new
		{
			movementName = _selectedPerformanceItem.Name,
			movementCategory = _selectedPerformanceItem.Category,
			value,
			unit = _selectedPerformanceItem.PrimaryUnit,
			secondaryValue,
			secondaryUnit = _selectedPerformanceItem.SecondaryUnit ?? string.Empty,
			groundContactTimeMs = groundContactTime,
			concentricTimeSeconds = concentricTime
		};

		if (_editingPerformanceId.HasValue)
		{
			var result = await _api.UpdateAthleticPerformanceAsync(_editingPerformanceId.Value, data);
			if (result.Success)
				ShowSuccess("Athletic performance updated.");
			else
			{
				ShowError(result.Error ?? "Failed to update.");
				return;
			}
		}
		else
		{
			var result = await _api.CreateAthleticPerformanceAsync(data);
			if (result.Success)
				ShowSuccess("Athletic performance added.");
			else
			{
				ShowError(result.Error ?? "Failed to save.");
				return;
			}
		}

		ResetPerformanceForm();
		await LoadAthleticPerformancesAsync();
	}

	private async void OnDeletePerformanceInvoked(object? sender, EventArgs e)
	{
		if (_profile is null || GetBindingContext<AthleticPerformanceListItem>(sender) is not AthleticPerformanceListItem item)
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

		var result = await _api.DeleteAthleticPerformanceAsync(item.Id);
		if (result.Success)
		{
			if (_editingPerformanceId == item.Id)
			{
				ResetPerformanceForm();
			}
			await LoadAthleticPerformancesAsync();
			ShowSuccess("Athletic performance deleted.");
		}
		else
		{
			ShowError(result.Error ?? "Failed to delete.");
		}
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

		if (_profile is null)
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

		var data = new
		{
			movementName,
			movementCategory = _selectedGoalItem.Category,
			goalMetricLabel = ResolveGoalLabel(_selectedGoalItem),
			targetValue,
			unit
		};

		if (_editingGoalId.HasValue)
		{
			var result = await _api.UpdateMovementGoalAsync(_editingGoalId.Value, data);
			if (result.Success)
				ShowSuccess("Movement goal updated.");
			else
			{
				ShowError(result.Error ?? "Failed to update goal.");
				return;
			}
		}
		else
		{
			var result = await _api.CreateMovementGoalAsync(data);
			if (result.Success)
				ShowSuccess("Movement goal saved.");
			else
			{
				ShowError(result.Error ?? "Failed to save goal.");
				return;
			}
		}

		ResetGoalForm();
		await LoadMovementGoalsAsync();
	}

	private async void OnDeleteGoalInvoked(object? sender, EventArgs e)
	{
		if (_profile is null || GetBindingContext<MovementGoalListItem>(sender) is not MovementGoalListItem item)
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

		var result = await _api.DeleteMovementGoalAsync(item.Id);
		if (result.Success)
		{
			if (_editingGoalId == item.Id)
			{
				ResetGoalForm();
			}
			await LoadMovementGoalsAsync();
			ShowSuccess("Movement goal deleted.");
		}
		else
		{
			ShowError(result.Error ?? "Failed to delete goal.");
		}
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
		if (_profile is null)
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

		var result = await _api.DeleteAccountAsync();
		if (result.Success)
		{
			_session.SignOut();
			GoToLogin();
		}
		else
		{
			ShowError(result.Error ?? "Failed to delete account.");
		}
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

	private static string FormatAthleticPerformanceText(AthleticPerformanceResponse entry)
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

	private async Task LoadSportCatalogAsync()
	{
		if (_sportCatalog.Count > 0) return;

		var result = await _api.GetSportCatalogAsync();
		if (result.Success && result.Data is not null)
		{
			_sportCatalog = result.Data;
			SportPicker.ItemsSource = _sportCatalog.Select(s => s.Name).ToList();
		}
	}

	private void SetSportPickerSelection(string sportName, string position)
	{
		if (string.IsNullOrWhiteSpace(sportName) || _sportCatalog.Count == 0)
		{
			SportPicker.SelectedIndex = -1;
			return;
		}

		var sport = _sportCatalog.FirstOrDefault(s =>
			string.Equals(s.Name, sportName, StringComparison.OrdinalIgnoreCase));

		if (sport is not null)
		{
			_selectedSport = sport;
			SportPicker.SelectedIndex = _sportCatalog.IndexOf(sport);
			UpdatePositionPicker(sport, position);
		}
		else
		{
			SportPicker.SelectedIndex = -1;
		}
	}

	private void OnSportPickerChanged(object? sender, EventArgs e)
	{
		int index = SportPicker.SelectedIndex;
		if (index < 0 || index >= _sportCatalog.Count)
		{
			_selectedSport = null;
			PositionContainer.IsVisible = false;
			return;
		}

		_selectedSport = _sportCatalog[index];
		UpdatePositionPicker(_selectedSport, null);
	}

	private void UpdatePositionPicker(SportDefinitionResponse sport, string? currentPosition)
	{
		if (sport.HasPositions && sport.Positions.Count > 0)
		{
			PositionContainer.IsVisible = true;
			PositionPicker.ItemsSource = sport.Positions;

			if (!string.IsNullOrWhiteSpace(currentPosition))
			{
				int posIndex = sport.Positions.FindIndex(p =>
					string.Equals(p, currentPosition, StringComparison.OrdinalIgnoreCase));
				PositionPicker.SelectedIndex = posIndex >= 0 ? posIndex : -1;
			}
			else
			{
				PositionPicker.SelectedIndex = -1;
			}
		}
		else
		{
			PositionContainer.IsVisible = false;
			PositionPicker.SelectedIndex = -1;
		}
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
