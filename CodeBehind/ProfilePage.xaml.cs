using FreakLete.Models;
using FreakLete.Services;
using FreakLete.ViewModels;
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

	private readonly IApiClient _api;
	private readonly ISessionProvider _session;
	private UserProfileResponse? _profile;
	private int? _editingPerformanceId;
	private int? _editingGoalId;
	private ExerciseCatalogItem? _selectedPerformanceItem;
	private ExerciseCatalogItem? _selectedGoalItem;
	private List<SportDefinitionResponse> _sportCatalog = [];
	private string? _sportCatalogLoadError;

	// Static cache so sport catalog survives page re-creation within the same app session
	private static List<SportDefinitionResponse>? _cachedSportCatalog;

	// Athlete profile ViewModel — owns draft state and save logic
	internal AthleteProfileViewModel? _athleteVm;

	// Coach profile ViewModel — owns draft state and save logic
	internal CoachProfileViewModel? _coachVm;

	public ProfilePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		UpdatePerformanceSelectionUI();
		UpdateGoalSelectionUI();
	}

	/// <summary>
	/// Test-only constructor — injects fakes and creates raw MAUI controls
	/// without XAML/WinUI3 so the real page logic can run headless.
	/// </summary>
	internal ProfilePage(IApiClient api, ISessionProvider session, bool headless)
	{
		if (!headless) InitializeComponent();
		else InitializeControlsForTest();
		_api = api;
		_session = session;
		UpdatePerformanceSelectionUI();
		UpdateGoalSelectionUI();
	}

	/// <summary>
	/// Creates the same named controls that XAML source-gen would produce,
	/// but without triggering WinUI3 handler registration.
	/// Every x:Name field from ProfilePage.xaml is assigned here.
	/// </summary>
	private void InitializeControlsForTest()
	{
		// Athlete card
		FullNameLabel = new Label();
		EmailLabel = new Label();
		StatusLabel = new Label { IsVisible = false };

		// Profile details
		DateOfBirthLabel = new Label { Text = "Select date of birth" };
		AgeLabel = new Label { Text = "Age: -" };
		WeightEntry = new Entry();
		BodyFatEntry = new Entry();
		SportLabel = new Label { Text = "Select your sport" };
		PositionContainer = new VerticalStackLayout { IsVisible = false };
		PositionLabel = new Label { Text = "Select your position" };
		GymExperienceLabel = new Label { Text = "Select experience level" };

		// Coach profile
		TrainingDaysLabel = new Label { Text = "Select days per week" };
		SessionDurationLabel = new Label { Text = "Select session duration" };
		PrimaryGoalLabel = new Label { Text = "Select your primary goal" };
		SecondaryGoalLabel = new Label { Text = "Select secondary goal" };
		EquipmentEditor = new Editor();
		InjuryHistoryEditor = new Editor();
		CurrentPainEditor = new Editor();
		PhysicalLimitationsEditor = new Editor();
		DietaryPreferenceLabel = new Label { Text = "Select dietary preference" };

		// Quick stats
		WorkoutCountLabel = new Label { Text = "0" };
		OneRmPrCountLabel = new Label { Text = "0" };

		// Athletic performance
		SelectedPerformanceLabel = new Label();
		SelectedPerformanceHintLabel = new Label();
		PerformanceMetric1Label = new Label();
		PerformanceValueEntry = new Entry();
		PerformanceMetric2Container = new VerticalStackLayout();
		PerformanceMetric2Label = new Label();
		PerformanceSecondaryValueEntry = new Entry();
		PerformanceTimingContainer = new VerticalStackLayout();
		PerformanceTimingLabel = new Label();
		PerformanceTimingEntry = new Entry();
		PerformanceActionButton = new Button();
		PerformanceCancelButton = new Button();
		AthleticPerformanceEmptyLabel = new Label();
		AthleticPerformanceList = new VerticalStackLayout();

		// Movement goals
		SelectedGoalLabel = new Label();
		SelectedGoalHintLabel = new Label();
		GoalUnitLabel = new Label();
		GoalTargetValueEntry = new Entry();
		GoalActionButton = new Button();
		GoalCancelButton = new Button();
		MovementGoalsEmptyLabel = new Label();
		MovementGoalsList = new VerticalStackLayout();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProfileAsync();
	}

	internal async Task LoadProfileAsync()
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

		await LoadSportCatalogAsync();

		// Create or rehydrate the athlete ViewModel
		_athleteVm = new AthleteProfileViewModel(_api.SaveAthleteProfileAsync, _sportCatalog);
		_athleteVm.HydrateFromProfile(_profile);

		// Sync UI from ViewModel state
		SyncDateOfBirthUI();

		WeightEntry.TextChanged -= OnWeightTextChanged;
		BodyFatEntry.TextChanged -= OnBodyFatTextChanged;
		WeightEntry.Text = _athleteVm.WeightText;
		BodyFatEntry.Text = _athleteVm.BodyFatText;
		WeightEntry.TextChanged += OnWeightTextChanged;
		BodyFatEntry.TextChanged += OnBodyFatTextChanged;

		SetSelectorValue(SportLabel, _athleteVm.SelectedSport?.Name, "Select your sport");
		SyncPositionUI();

		SetSelectorValue(GymExperienceLabel, _athleteVm.SelectedGymExperience, "Select experience level");

		WorkoutCountLabel.Text = _profile.TotalWorkouts.ToString();
		OneRmPrCountLabel.Text = _profile.TotalPrs.ToString();

		// Create or rehydrate the coach ViewModel
		_coachVm = new CoachProfileViewModel(_api.SaveCoachProfileAsync);
		_coachVm.HydrateFromProfile(_profile);
		SyncCoachUI();

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

	private void SyncDateOfBirthUI()
	{
		if (_athleteVm is null) return;
		DateOfBirthLabel.Text = _athleteVm.DateOfBirthDisplay;
		DateOfBirthLabel.TextColor = _athleteVm.DateOfBirth.HasValue
			? Color.FromArgb("#F7F7FB")
			: Color.FromArgb("#B3B2C5");
		AgeLabel.Text = _athleteVm.AgeDisplay;
	}

	// ── Custom selector tap handlers ──────────────────────────────────

	private async void OnDateOfBirthTapped(object? sender, TappedEventArgs e)
	{
		var initialDate = _athleteVm?.DateOfBirth?.ToDateTime(TimeOnly.MinValue)
			?? DateTime.Today.AddYears(-18);

		await Navigation.PushAsync(
			new DateSelectorPage(initialDate, date =>
			{
				if (_athleteVm is not null)
					_athleteVm.DateOfBirth = DateOnly.FromDateTime(date);
				SyncDateOfBirthUI();
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
			_athleteVm?.SelectedSport?.Name,
			name =>
			{
				var sport = _sportCatalog.FirstOrDefault(s =>
					string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
				if (sport is not null)
				{
					if (_athleteVm is not null)
						_athleteVm.SelectedSport = sport;
					SetSelectorValue(SportLabel, sport.Name, "Select your sport");
					SyncPositionUI();
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
		var sport = _athleteVm?.SelectedSport;
		if (sport is null || !sport.HasPositions || sport.Positions.Count == 0) return;

		var currentPos = _athleteVm?.SelectedPosition;
		await Navigation.PushAsync(
			new OptionPickerPage("Select Position", sport.Positions, currentPos, pos =>
			{
				if (_athleteVm is not null)
					_athleteVm.SelectedPosition = pos;
				SetSelectorValue(PositionLabel, pos, "Select your position");
			}), true);
	}

	private async void OnGymExperienceTapped(object? sender, TappedEventArgs e)
	{
		var current = _athleteVm?.SelectedGymExperience;
		await Navigation.PushAsync(
			new OptionPickerPage("Gym Experience", ExperienceLevels, current, val =>
			{
				if (_athleteVm is not null)
					_athleteVm.SelectedGymExperience = val;
				SetSelectorValue(GymExperienceLabel, val, "Select experience level");
			}), true);
	}

	private async void OnTrainingDaysTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Training Days / Week", TrainingDaysOptions, _coachVm?.SelectedTrainingDays, val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedTrainingDays = val;
				SetSelectorValue(TrainingDaysLabel, val, "Select days per week");
			}), true);
	}

	private async void OnSessionDurationTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Session Duration", SessionDurationOptions, _coachVm?.SelectedSessionDuration, val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedSessionDuration = val;
				SetSelectorValue(SessionDurationLabel, val, "Select session duration");
			}), true);
	}

	private async void OnPrimaryGoalTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Primary Goal", TrainingGoalOptions, _coachVm?.SelectedPrimaryGoal, val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedPrimaryGoal = val;
				SetSelectorValue(PrimaryGoalLabel, val, "Select your primary goal");
			}), true);
	}

	private async void OnSecondaryGoalTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Secondary Goal", TrainingGoalOptions, _coachVm?.SelectedSecondaryGoal, val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedSecondaryGoal = val;
				SetSelectorValue(SecondaryGoalLabel, val, "Select secondary goal");
			}), true);
	}

	private async void OnDietaryPreferenceTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage("Dietary Preference", DietaryPreferenceOptions, _coachVm?.SelectedDietaryPreference, val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedDietaryPreference = val;
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

	private void OnWeightTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_athleteVm is not null)
			_athleteVm.WeightText = e.NewTextValue ?? "";
	}

	private void OnBodyFatTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_athleteVm is not null)
			_athleteVm.BodyFatText = e.NewTextValue ?? "";
	}

	internal async void OnSaveProfileClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_profile is null || _athleteVm is null)
		{
			return;
		}

		var success = await _athleteVm.SaveAsync();

		if (success)
		{
			// Rehydrate UI from the server-confirmed state in the ViewModel
			SyncDateOfBirthUI();
			WeightEntry.TextChanged -= OnWeightTextChanged;
			BodyFatEntry.TextChanged -= OnBodyFatTextChanged;
			WeightEntry.Text = _athleteVm.WeightText;
			BodyFatEntry.Text = _athleteVm.BodyFatText;
			WeightEntry.TextChanged += OnWeightTextChanged;
			BodyFatEntry.TextChanged += OnBodyFatTextChanged;
			SetSelectorValue(SportLabel, _athleteVm.SelectedSport?.Name, "Select your sport");
			SyncPositionUI();
			SetSelectorValue(GymExperienceLabel, _athleteVm.SelectedGymExperience, "Select experience level");
			ShowSuccess("Profile saved.");
		}
		else
		{
			ShowError(_athleteVm.SaveError ?? "Failed to save profile.");
		}
	}

	internal async void OnSaveCoachProfileClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_profile is null || _coachVm is null) return;

		// Push text editor values into the VM before saving
		_coachVm.EquipmentText = EquipmentEditor.Text?.Trim() ?? "";
		_coachVm.LimitationsText = PhysicalLimitationsEditor.Text?.Trim() ?? "";
		_coachVm.InjuryHistoryText = InjuryHistoryEditor.Text?.Trim() ?? "";
		_coachVm.PainPointsText = CurrentPainEditor.Text?.Trim() ?? "";

		var success = await _coachVm.SaveAsync();

		if (success)
		{
			SyncCoachUI();
			ShowSuccess("Coach profile saved.");
		}
		else
		{
			ShowError(_coachVm.SaveError ?? "Failed to save coach profile.");
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

	/// <summary>
	/// Syncs position UI from the athlete ViewModel's state.
	/// </summary>
	private void SyncPositionUI()
	{
		if (_athleteVm is null) return;
		PositionContainer.IsVisible = _athleteVm.ShowPositionSelector;
		SetSelectorValue(PositionLabel, _athleteVm.SelectedPosition, "Select your position");
	}

	/// <summary>
	/// Syncs all coach profile UI from the coach ViewModel's state.
	/// </summary>
	private void SyncCoachUI()
	{
		if (_coachVm is null) return;
		SetSelectorValue(TrainingDaysLabel, _coachVm.SelectedTrainingDays, "Select days per week");
		SetSelectorValue(SessionDurationLabel, _coachVm.SelectedSessionDuration, "Select session duration");
		SetSelectorValue(PrimaryGoalLabel, _coachVm.SelectedPrimaryGoal, "Select your primary goal");
		SetSelectorValue(SecondaryGoalLabel, _coachVm.SelectedSecondaryGoal, "Select secondary goal");
		SetSelectorValue(DietaryPreferenceLabel, _coachVm.SelectedDietaryPreference, "Select dietary preference");
		EquipmentEditor.Text = _coachVm.EquipmentText;
		InjuryHistoryEditor.Text = _coachVm.InjuryHistoryText;
		CurrentPainEditor.Text = _coachVm.PainPointsText;
		PhysicalLimitationsEditor.Text = _coachVm.LimitationsText;
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
