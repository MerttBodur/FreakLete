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
	private string? _sportCatalogLoadError;

	// Static cache so sport catalog survives page re-creation within the same app session
	private static List<SportDefinitionResponse>? _cachedSportCatalog;

	// Selection state for custom pickers
	private DateTime _selectedDateOfBirth = DateTime.Today.AddYears(-18);
	private bool _dateOfBirthChanged;
	private string? _selectedPosition;
	private string? _selectedGymExperience;
	private string? _selectedTrainingDays;
	private string? _selectedSessionDuration;
	private string? _selectedPrimaryGoal;
	private string? _selectedSecondaryGoal;
	private string? _selectedDietaryPreference;

	public ProfilePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
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

		_selectedDateOfBirth = _profile.DateOfBirth?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today.AddYears(-18);
		UpdateDateOfBirthLabel(hasValue: _profile.DateOfBirth.HasValue);
		UpdateAgeLabel(_profile.DateOfBirth);

		WeightEntry.Text = _profile.WeightKg?.ToString("0.##") ?? string.Empty;
		BodyFatEntry.Text = _profile.BodyFatPercentage?.ToString("0.##") ?? string.Empty;

		await LoadSportCatalogAsync();
		SetSportSelection(_profile.SportName, _profile.Position);

		SetSelectorValue(GymExperienceLabel, _profile.GymExperienceLevel, "Select experience level");
		_selectedGymExperience = string.IsNullOrWhiteSpace(_profile.GymExperienceLevel) ? null : _profile.GymExperienceLevel;

		WorkoutCountLabel.Text = _profile.TotalWorkouts.ToString();
		OneRmPrCountLabel.Text = _profile.TotalPrs.ToString();

		// Coach profile fields
		SetSelectorValue(TrainingDaysLabel, _profile.TrainingDaysPerWeek?.ToString(), "Select days per week");
		_selectedTrainingDays = _profile.TrainingDaysPerWeek?.ToString();

		SetSelectorValue(SessionDurationLabel, _profile.PreferredSessionDurationMinutes?.ToString(), "Select session duration");
		_selectedSessionDuration = _profile.PreferredSessionDurationMinutes?.ToString();

		SetSelectorValue(PrimaryGoalLabel, _profile.PrimaryTrainingGoal, "Select your primary goal");
		_selectedPrimaryGoal = string.IsNullOrWhiteSpace(_profile.PrimaryTrainingGoal) ? null : _profile.PrimaryTrainingGoal;

		SetSelectorValue(SecondaryGoalLabel, _profile.SecondaryTrainingGoal, "Select secondary goal");
		_selectedSecondaryGoal = string.IsNullOrWhiteSpace(_profile.SecondaryTrainingGoal) ? null : _profile.SecondaryTrainingGoal;

		EquipmentEditor.Text = _profile.AvailableEquipment;
		InjuryHistoryEditor.Text = _profile.InjuryHistory;
		CurrentPainEditor.Text = _profile.CurrentPainPoints;
		PhysicalLimitationsEditor.Text = _profile.PhysicalLimitations;

		SetSelectorValue(DietaryPreferenceLabel, _profile.DietaryPreference, "Select dietary preference");
		_selectedDietaryPreference = string.IsNullOrWhiteSpace(_profile.DietaryPreference) ? null : _profile.DietaryPreference;

		await LoadAthleticPerformancesAsync();
		await LoadMovementGoalsAsync();
	}

	private static void SetSelectorValue(Label label, string? value, string placeholder)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			label.Text = placeholder;
			label.TextColor = Color.FromArgb("#B3B2C5"); // TextSecondary
		}
		else
		{
			label.Text = value;
			label.TextColor = Color.FromArgb("#F7F7FB"); // TextPrimary
		}
	}

	private void UpdateDateOfBirthLabel(bool hasValue = true)
	{
		if (hasValue)
		{
			DateOfBirthLabel.Text = _selectedDateOfBirth.ToString("dd MMMM yyyy");
			DateOfBirthLabel.TextColor = Color.FromArgb("#F7F7FB");
		}
		else
		{
			DateOfBirthLabel.Text = "Select date of birth";
			DateOfBirthLabel.TextColor = Color.FromArgb("#B3B2C5");
		}
	}

	// ── Custom selector tap handlers ──────────────────────────────────

	private async void OnDateOfBirthTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new DateSelectorPage(_selectedDateOfBirth, date =>
			{
				_selectedDateOfBirth = date;
				_dateOfBirthChanged = true;
				UpdateDateOfBirthLabel();
				UpdateAgeLabel(DateOnly.FromDateTime(date));
			}), true);
	}

	private async void OnSportSelectorTapped(object? sender, TappedEventArgs e)
	{
		// Build items from cache or show loading
		var items = BuildSportPickerItems();
		var categories = BuildSportCategories();

		var picker = new OptionPickerPage(
			"Select Sport",
			items,
			_selectedSport?.Name,
			name =>
			{
				var sport = _sportCatalog.FirstOrDefault(s =>
					string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
				if (sport is not null)
				{
					_selectedSport = sport;
					SetSelectorValue(SportLabel, sport.Name, "Select your sport");
					UpdatePositionUI(sport, null);
				}
			},
			categories,
			async () => await ReloadSportCatalogIntoPickerAsync());

		await Navigation.PushAsync(picker, true);

		// If catalog wasn't loaded yet, load it now into the picker
		if (_sportCatalog.Count == 0)
		{
			picker.ShowLoading();
			await LoadSportCatalogAsync(forceReload: true);

			if (_sportCatalog.Count == 0)
			{
				picker.ShowError(_sportCatalogLoadError ?? "Sport list could not be loaded.");
			}
			else
			{
				picker.SetItems(BuildSportPickerItems(), BuildSportCategories());
			}
		}
	}

	private List<OptionPickerPage.OptionItem> BuildSportPickerItems()
	{
		return _sportCatalog.Select(s => new OptionPickerPage.OptionItem
		{
			Text = s.Name,
			GroupName = s.Category
		}).ToList();
	}

	private List<string> BuildSportCategories()
	{
		return _sportCatalog
			.Select(s => s.Category)
			.Where(c => !string.IsNullOrWhiteSpace(c))
			.Distinct()
			.ToList();
	}

	private async Task ReloadSportCatalogIntoPickerAsync()
	{
		await LoadSportCatalogAsync(forceReload: true);
		// The picker's OnRetryClicked calls this, then we update via navigation re-open
		// But since we navigate to the picker, we need to find the current page and update it
		if (Navigation.NavigationStack.LastOrDefault() is OptionPickerPage activePicker)
		{
			if (_sportCatalog.Count == 0)
			{
				activePicker.ShowError(_sportCatalogLoadError ?? "Sport list could not be loaded.");
			}
			else
			{
				activePicker.SetItems(BuildSportPickerItems(), BuildSportCategories());
			}
		}
	}

	private async void OnPositionSelectorTapped(object? sender, TappedEventArgs e)
	{
		if (_selectedSport is null || !_selectedSport.HasPositions || _selectedSport.Positions.Count == 0) return;

		await Navigation.PushAsync(
			new OptionPickerPage("Select Position", _selectedSport.Positions, _selectedPosition, pos =>
			{
				_selectedPosition = pos;
				SetSelectorValue(PositionLabel, pos, "Select your position");
			}), true);
	}

	private async void OnGymExperienceTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Gym Experience", ExperienceLevels, _selectedGymExperience, val =>
			{
				_selectedGymExperience = val;
				SetSelectorValue(GymExperienceLabel, val, "Select experience level");
			}), true);
	}

	private async void OnTrainingDaysTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Training Days / Week", TrainingDaysOptions, _selectedTrainingDays, val =>
			{
				_selectedTrainingDays = val;
				SetSelectorValue(TrainingDaysLabel, val, "Select days per week");
			}), true);
	}

	private async void OnSessionDurationTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Session Duration", SessionDurationOptions, _selectedSessionDuration, val =>
			{
				_selectedSessionDuration = val;
				SetSelectorValue(SessionDurationLabel, val, "Select session duration");
			}), true);
	}

	private async void OnPrimaryGoalTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Primary Goal", TrainingGoalOptions, _selectedPrimaryGoal, val =>
			{
				_selectedPrimaryGoal = val;
				SetSelectorValue(PrimaryGoalLabel, val, "Select your primary goal");
			}), true);
	}

	private async void OnSecondaryGoalTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Secondary Goal", TrainingGoalOptions, _selectedSecondaryGoal, val =>
			{
				_selectedSecondaryGoal = val;
				SetSelectorValue(SecondaryGoalLabel, val, "Select secondary goal");
			}), true);
	}

	private async void OnDietaryPreferenceTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Dietary Preference", DietaryPreferenceOptions, _selectedDietaryPreference, val =>
			{
				_selectedDietaryPreference = val;
				SetSelectorValue(DietaryPreferenceLabel, val, "Select dietary preference");
			}), true);
	}

	// ── Data loading ──────────────────────────────────────────────────

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

	// ── Save handlers ─────────────────────────────────────────────────

	private async void OnSaveProfileClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_profile is null)
		{
			return;
		}

		var (isValid, error) = ProfileStateManager.ValidateAthleteFields(
			WeightEntry.Text, BodyFatEntry.Text);
		if (!isValid)
		{
			ShowError(error!);
			return;
		}

		var sportInfo = _selectedSport is not null
			? new ProfileStateManager.SportInfo
			{
				Name = _selectedSport.Name,
				HasPositions = _selectedSport.HasPositions,
				Positions = _selectedSport.Positions
			}
			: null;

		var profileData = ProfileStateManager.BuildAthletePayload(
			WeightEntry.Text,
			BodyFatEntry.Text,
			_dateOfBirthChanged,
			_selectedDateOfBirth,
			sportInfo,
			_selectedPosition,
			_selectedGymExperience,
			_profile.Position);

		var result = await _api.UpdateProfileAsync(profileData);
		if (result.Success)
		{
			_dateOfBirthChanged = false;
			ShowSuccess("Profile saved.");
			// Re-fetch from server so UI reflects actual persisted state
			await LoadProfileAsync();
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

		var coachData = ProfileStateManager.BuildCoachPayload(
			_selectedTrainingDays,
			_selectedSessionDuration,
			EquipmentEditor.Text?.Trim(),
			PhysicalLimitationsEditor.Text?.Trim(),
			InjuryHistoryEditor.Text?.Trim(),
			CurrentPainEditor.Text?.Trim(),
			_selectedPrimaryGoal,
			_selectedSecondaryGoal,
			_selectedDietaryPreference);

		var result = await _api.UpdateProfileAsync(coachData);
		if (result.Success)
		{
			ShowSuccess("Coach profile saved.");
			// Re-fetch from server so UI reflects actual persisted state
			await LoadProfileAsync();
		}
		else
		{
			ShowError(result.Error ?? "Failed to save coach profile.");
		}
	}

	// ── Athletic Performance CRUD ─────────────────────────────────────

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

	// ── Movement Goals CRUD ───────────────────────────────────────────

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

	// ── Account actions ───────────────────────────────────────────────

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

	// ── Helpers ───────────────────────────────────────────────────────

	private static TItem? GetBindingContext<TItem>(object? sender) where TItem : class
	{
		return sender switch
		{
			BindableObject bindable when bindable.BindingContext is TItem item => item,
			_ => null
		};
	}

	private void UpdateAgeLabel(DateOnly? dateOfBirth)
	{
		var age = ProfileStateManager.CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.Today));
		AgeLabel.Text = age.HasValue ? $"Age: {age}" : "Age: -";
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

	private async Task LoadSportCatalogAsync(bool forceReload = false)
	{
		if (!forceReload && _sportCatalog.Count > 0) return;

		// Use static cache if available
		if (!forceReload && _cachedSportCatalog is not null && _cachedSportCatalog.Count > 0)
		{
			_sportCatalog = _cachedSportCatalog;
			_sportCatalogLoadError = null;
			return;
		}

		var result = await _api.GetSportCatalogAsync();
		if (result.Success && result.Data is not null)
		{
			_sportCatalog = result.Data;
			_cachedSportCatalog = result.Data;
			_sportCatalogLoadError = null;
		}
		else
		{
			_sportCatalog = [];
			_sportCatalogLoadError = result.Error ?? "Sport catalog request failed.";
		}
	}

	private void SetSportSelection(string sportName, string position)
	{
		if (string.IsNullOrWhiteSpace(sportName) || _sportCatalog.Count == 0)
		{
			SetSelectorValue(SportLabel, null, "Select your sport");
			return;
		}

		var sport = _sportCatalog.FirstOrDefault(s =>
			string.Equals(s.Name, sportName, StringComparison.OrdinalIgnoreCase));

		if (sport is not null)
		{
			_selectedSport = sport;
			SetSelectorValue(SportLabel, sport.Name, "Select your sport");
			UpdatePositionUI(sport, position);
		}
		else
		{
			SetSelectorValue(SportLabel, null, "Select your sport");
		}
	}

	private void UpdatePositionUI(SportDefinitionResponse sport, string? currentPosition)
	{
		var sportInfo = new ProfileStateManager.SportInfo
		{
			Name = sport.Name,
			HasPositions = sport.HasPositions,
			Positions = sport.Positions
		};
		var (position, showSelector) = ProfileStateManager.ResolvePositionForSport(sportInfo, currentPosition);

		PositionContainer.IsVisible = showSelector;
		_selectedPosition = position;
		SetSelectorValue(PositionLabel, _selectedPosition, "Select your position");
	}

	// ── Inner model classes ───────────────────────────────────────────

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
