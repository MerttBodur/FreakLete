using FreakLete.Models;
using FreakLete.Services;
using FreakLete.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

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

	private static readonly string[] SexOptions = ["Male", "Female"];
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
	private static readonly string[] EquipmentAccessOptions =
	[
		"Home",
		"Local Gym",
		"Commercial Gym",
		"Powerlifting Gym",
		"CrossFit Gym",
		"Weightlifting Gym",
		"Athlete Performance Gym"
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
	private int _lastAthleticCount;
	private int _lastGoalCount;

	// Static cache so sport catalog survives page re-creation within the same app session
	private static List<SportDefinitionResponse>? _cachedSportCatalog;

	// Athlete profile ViewModel — owns draft state and save logic
	internal AthleteProfileViewModel? _athleteVm;

	// Coach profile ViewModel — owns draft state and save logic
	internal CoachProfileViewModel? _coachVm;

	// Guard to prevent reload-overwrite bug: when returning from selector pages,
	// skip the next OnAppearing()->LoadProfileAsync() to preserve local selections
	private bool _skipNextProfileReload;

	public ProfilePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		ApplyLanguage();
		UpdatePerformanceSelectionUI();
		UpdateGoalSelectionUI();
	}

	private void ApplyLanguage()
	{
		WorkoutsSubLabel.Text = AppLanguage.ProfileWorkouts;
		SavedPrsSubLabel.Text = AppLanguage.ProfileSavedPrs;
		RecordsSubLabel.Text = AppLanguage.ProfileRecords;
		HighlightsLabel.Text = AppLanguage.ProfileHighlights;
		ProfileDetailsTitle.Text = AppLanguage.ProfileDetails;
		DobLabel.Text = AppLanguage.ProfileDateOfBirth;
		DateOfBirthLabel.Text = AppLanguage.ProfileSelectDob;
		AgeLabel.Text = AppLanguage.ProfileAge;
		WeightKgLabel.Text = AppLanguage.ProfileWeightKg;
		BodyFatLabel.Text = AppLanguage.ProfileBodyFat;
		HeightCmLabel.Text = AppLanguage.ProfileHeightCm;
		SexTitleLabel.Text = AppLanguage.ProfileSex;
		SexLabel.Text = AppLanguage.ProfileSelectSex;
		SportTitleLabel.Text = AppLanguage.ProfileSport;
		SportLabel.Text = AppLanguage.ProfileSelectSport;
		PositionTitleLabel.Text = AppLanguage.ProfilePosition;
		PositionLabel.Text = AppLanguage.ProfileSelectPosition;
		GymExpTitleLabel.Text = AppLanguage.ProfileGymExperience;
		GymExperienceLabel.Text = AppLanguage.ProfileSelectExperience;
		CoachProfileTitle.Text = AppLanguage.ProfileCoachProfile;
		TrainingDaysTitleLabel.Text = AppLanguage.ProfileTrainingDays;
		TrainingDaysLabel.Text = AppLanguage.ProfileSelectDays;
		SessionDurationTitleLabel.Text = AppLanguage.ProfileSessionDuration;
		SessionDurationLabel.Text = AppLanguage.ProfileSelectDuration;
		PrimaryGoalTitleLabel.Text = AppLanguage.ProfilePrimaryGoal;
		PrimaryGoalLabel.Text = AppLanguage.ProfileSelectPrimaryGoal;
		SecondaryGoalTitleLabel.Text = AppLanguage.ProfileSecondaryGoal;
		SecondaryGoalLabel.Text = AppLanguage.ProfileSelectSecondaryGoal;
		EquipmentTitleLabel.Text = AppLanguage.ProfileEquipment;
		EquipmentLabel.Text = AppLanguage.ProfileSelectEquipment;
		InjuryHistoryTitleLabel.Text = AppLanguage.ProfileInjuryHistory;
		InjuryHistoryEditor.Placeholder = AppLanguage.ProfileInjuryPlaceholder;
		CurrentPainTitleLabel.Text = AppLanguage.ProfileCurrentPain;
		CurrentPainEditor.Placeholder = AppLanguage.ProfilePainPlaceholder;
		PhysicalLimitsTitleLabel.Text = AppLanguage.ProfileLimitations;
		PhysicalLimitationsEditor.Placeholder = AppLanguage.ProfileLimitationsPlaceholder;
		DietaryPrefTitleLabel.Text = AppLanguage.ProfileDietaryPreference;
		DietaryPreferenceLabel.Text = AppLanguage.ProfileSelectDietary;
		AthleticPerfTitle.Text = AppLanguage.ProfileAthleticPerformance;
		AthleticMovementLabel.Text = AppLanguage.CalcMovement;
		BrowsePerformanceBtn.Text = AppLanguage.SharedBrowse;
		AthleticPerformanceEmptyLabel.Text = AppLanguage.ProfileNoPerformance;
		PerformanceActionButton.Text = AppLanguage.SharedSave;
		PerformanceCancelButton.Text = AppLanguage.SharedCancel;
		MovementGoalsTitle.Text = AppLanguage.ProfileMovementGoals;
		GoalMovementLabel.Text = AppLanguage.ProfileGoalMovement;
		BrowseGoalBtn.Text = AppLanguage.SharedBrowse;
		GoalActionButton.Text = AppLanguage.SharedSave;
		GoalCancelButton.Text = AppLanguage.SharedCancel;
		MovementGoalsEmptyLabel.Text = AppLanguage.ProfileNoGoals;
		ChangePhotoLabel.Text = AppLanguage.ProfileChangePhoto;
		SettingsBtn.Text = AppLanguage.ProfileSettings;
		LogoutBtn.Text = AppLanguage.ProfileLogout;
		DeleteAccountBtn.Text = AppLanguage.ProfileDeleteAccount;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged() => ApplyLanguage();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;

		// Skip if returning from a selector page (athlete auto-save handles persistence)
		if (_skipNextProfileReload)
		{
			_skipNextProfileReload = false;
			return;
		}

		await LoadProfileAsync();
	}

	public async Task LoadProfileAsync()
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
			ShowError(result.Error ?? AppLanguage.ProfileFailedLoad);
			return;
		}

		_profile = result.Data;

		FullNameLabel.Text = $"{_profile.FirstName} {_profile.LastName}";
		EmailLabel.Text = _profile.Email;

		// Initials avatar
		var fi = string.IsNullOrWhiteSpace(_profile.FirstName) ? "" : _profile.FirstName[..1];
		var li = string.IsNullOrWhiteSpace(_profile.LastName) ? "" : _profile.LastName[..1];
		InitialsLabel.Text = $"{fi}{li}".ToUpperInvariant();

		// Status chip — derived from real gym experience or sport context
		string? chipText = !string.IsNullOrWhiteSpace(_profile.GymExperienceLevel)
			? _profile.GymExperienceLevel.ToUpperInvariant()
			: !string.IsNullOrWhiteSpace(_profile.SportName)
				? _profile.SportName.ToUpperInvariant()
				: null;

		if (chipText is not null)
		{
			StatusChip.IsVisible = true;
			StatusLabel.Text = chipText;
			StatusLabel.IsVisible = true;
		}
		else
		{
			StatusChip.IsVisible = false;
		}

		await LoadSportCatalogAsync();

		// Create or rehydrate the athlete ViewModel
		_athleteVm = new AthleteProfileViewModel(_api.SaveAthleteProfileAsync, _sportCatalog);
		_athleteVm.HydrateFromProfile(_profile);

		// Sync UI from ViewModel state
		SyncDateOfBirthUI();

		WeightEntry.TextChanged -= OnWeightTextChanged;
		BodyFatEntry.TextChanged -= OnBodyFatTextChanged;
		HeightEntry.TextChanged -= OnHeightTextChanged;
		WeightEntry.Text = _athleteVm.WeightText;
		BodyFatEntry.Text = _athleteVm.BodyFatText;
		HeightEntry.Text = _athleteVm.HeightText;
		WeightEntry.TextChanged += OnWeightTextChanged;
		BodyFatEntry.TextChanged += OnBodyFatTextChanged;
		HeightEntry.TextChanged += OnHeightTextChanged;

		SetSelectorValue(SportLabel, _athleteVm.SelectedSport?.Name, AppLanguage.ProfileSelectSport);
		SetSelectorValue(SexLabel, _athleteVm.SelectedSex, AppLanguage.ProfileSelectSex);
		SyncPositionUI();

		SetSelectorValue(GymExperienceLabel, _athleteVm.SelectedGymExperience, AppLanguage.ProfileSelectExperience);

		WorkoutCountLabel.Text = _profile.TotalWorkouts.ToString();
		OneRmPrCountLabel.Text = _profile.TotalPrs.ToString();

		// Load profile photo in background; don't block profile load
		_ = LoadProfilePhotoAsync();

		// Create or rehydrate the coach ViewModel
		_coachVm = new CoachProfileViewModel(_api.SaveCoachProfileAsync);
		_coachVm.HydrateFromProfile(_profile);
		SyncCoachUI();

		// Load athletic performances and goals in parallel, capture counts
		var perfResultTask = _api.GetAthleticPerformancesAsync();
		var goalsResultTask = _api.GetMovementGoalsAsync();
		await Task.WhenAll(perfResultTask, goalsResultTask);

		var perfResult = perfResultTask.Result;
		var goalsResult = goalsResultTask.Result;

		// Populate lists
		if (perfResult.Success && perfResult.Data is not null)
		{
			var items = perfResult.Data.Select(entry => new AthleticPerformanceListItem
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
		else
		{
			AthleticPerformanceEmptyLabel.IsVisible = true;
		}

		if (goalsResult.Success && goalsResult.Data is not null)
		{
			var items = goalsResult.Data.Select(goal => new MovementGoalListItem
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
		else
		{
			MovementGoalsEmptyLabel.IsVisible = true;
		}

		_lastAthleticCount = perfResult.Success && perfResult.Data is not null ? perfResult.Data.Count : 0;
		_lastGoalCount = goalsResult.Success && goalsResult.Data is not null ? goalsResult.Data.Count : 0;

		AthleticRecordsCountLabel.Text = _lastAthleticCount.ToString();

		BuildHighlights(_profile.TotalWorkouts, _profile.TotalPrs, _lastAthleticCount, _lastGoalCount);
	}

	private void BuildHighlights(int totalWorkouts, int totalPrs, int athleticCount, int goalCount)
	{
		HighlightsContainer.Children.Clear();

		var milestones = new List<(string title, string subtitle, bool achieved)>
		{
			(AppLanguage.ProfileHighlightFirstWorkout, AppLanguage.ProfileHighlightFirstWorkoutDesc, totalWorkouts >= 1),
			(AppLanguage.ProfileHighlightFirstPr, AppLanguage.ProfileHighlightFirstPrDesc, totalPrs >= 1),
			(AppLanguage.ProfileHighlightConsistent, AppLanguage.ProfileHighlightConsistentDesc, totalWorkouts >= 10),
			(AppLanguage.ProfileHighlightPerformance, AppLanguage.ProfileHighlightPerformanceDesc, athleticCount >= 1),
			(AppLanguage.ProfileHighlightGoalSetter, AppLanguage.ProfileHighlightGoalSetterDesc, goalCount >= 1)
		};

		var achieved = milestones.Where(m => m.achieved).ToList();

		if (achieved.Count == 0)
		{
			var emptyCard = new Border
			{
				StrokeShape = new RoundRectangle { CornerRadius = 16 },
				BackgroundColor = GetProfileColor("SurfaceRaised", "#1D1828"),
				Stroke = new SolidColorBrush(GetProfileColor("SurfaceBorder", "#342D46")),
				StrokeThickness = 1,
				Padding = new Thickness(20, 16)
			};
			emptyCard.Content = new Label
			{
				Text = AppLanguage.ProfileNoHighlights,
				FontSize = 13,
				FontFamily = "OpenSansRegular",
				TextColor = GetProfileColor("TextSecondary", "#B3B2C5"),
				HorizontalTextAlignment = TextAlignment.Center
			};
			HighlightsContainer.Children.Add(emptyCard);
			return;
		}

		foreach (var (title, subtitle, _) in achieved)
		{
			var card = new Border
			{
				StrokeShape = new RoundRectangle { CornerRadius = 18 },
				Stroke = new SolidColorBrush(GetProfileColor("AccentSoft", "#2F2346")),
				StrokeThickness = 1,
				Padding = new Thickness(18, 14),
				Background = new LinearGradientBrush(
				[
					new GradientStop(GetProfileColor("AccentSoft", "#2F2346"), 0.0f),
					new GradientStop(GetProfileColor("Surface", "#100D1A"), 1.0f)
				], new Point(0, 0), new Point(1, 1))
			};

			var row = new HorizontalStackLayout { Spacing = 14 };

			var checkBorder = new Border
			{
				StrokeShape = new RoundRectangle { CornerRadius = 16 },
				BackgroundColor = GetProfileColor("Accent", "#8B5CF6").WithAlpha(0.2f),
				Stroke = new SolidColorBrush(GetProfileColor("Accent", "#8B5CF6").WithAlpha(0.4f)),
				StrokeThickness = 1,
				WidthRequest = 32,
				HeightRequest = 32,
				VerticalOptions = LayoutOptions.Center
			};
			checkBorder.Content = new Label
			{
				Text = "\u2713",
				FontSize = 15,
				FontFamily = "OpenSansSemibold",
				TextColor = GetProfileColor("AccentGlow", "#A78BFA"),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center
			};

			var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
			textStack.Children.Add(new Label
			{
				Text = title,
				FontSize = 15,
				FontFamily = "OpenSansSemibold",
				TextColor = GetProfileColor("TextPrimary", "#F7F7FB")
			});
			textStack.Children.Add(new Label
			{
				Text = subtitle,
				FontSize = 12,
				FontFamily = "OpenSansRegular",
				TextColor = GetProfileColor("TextSecondary", "#B3B2C5")
			});

			row.Children.Add(checkBorder);
			row.Children.Add(textStack);
			card.Content = row;

			HighlightsContainer.Children.Add(card);
		}
	}

	private void RefreshTopSummary()
	{
		AthleticRecordsCountLabel.Text = _lastAthleticCount.ToString();

		if (_profile is not null)
			BuildHighlights(_profile.TotalWorkouts, _profile.TotalPrs, _lastAthleticCount, _lastGoalCount);
	}

	private static Color GetProfileColor(string key, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
			return color;
		return Color.FromArgb(fallback);
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
			new DateSelectorPage(initialDate, async date =>
			{
				if (_athleteVm is not null)
					_athleteVm.DateOfBirth = DateOnly.FromDateTime(date);
				SyncDateOfBirthUI();
				
				// Autosave the change
				var success = await SaveAthleteFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnSportSelectorTapped(object? sender, TappedEventArgs e)
	{
		// Build items from cache or show loading
		var items = BuildSportPickerItems();
		var categories = BuildSportCategories();

		var picker = new OptionPickerPage(
			AppLanguage.ProfileSelectSportTitle,
			items,
			_athleteVm?.SelectedSport?.Name,
			async name =>
			{
				var sport = _sportCatalog.FirstOrDefault(s =>
					string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
				if (sport is not null)
				{
					if (_athleteVm is not null)
						_athleteVm.SelectedSport = sport;
					SetSelectorValue(SportLabel, sport.Name, AppLanguage.ProfileSelectSport);
					SyncPositionUI();
					
					// Autosave the change
					var success = await SaveAthleteFieldAsync();
					
					// Prevent OnAppearing from reloading only if save succeeded
					if (success)
						_skipNextProfileReload = true;
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
				picker.ShowError(_sportCatalogLoadError ?? AppLanguage.SportCatalogLoadError);
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
				activePicker.ShowError(_sportCatalogLoadError ?? AppLanguage.SportCatalogLoadError);
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
			new OptionPickerPage(AppLanguage.ProfileSelectPositionTitle, sport.Positions, currentPos, async pos =>
			{
				if (_athleteVm is not null)
					_athleteVm.SelectedPosition = pos;
				SetSelectorValue(PositionLabel, pos, AppLanguage.ProfileSelectPosition);
				
				// Autosave the change
				var success = await SaveAthleteFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnGymExperienceTapped(object? sender, TappedEventArgs e)
	{
		var current = _athleteVm?.SelectedGymExperience;
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileGymExperienceTitle, ExperienceLevels, current, async val =>
			{
				if (_athleteVm is not null)
					_athleteVm.SelectedGymExperience = val;
				SetSelectorValue(GymExperienceLabel, val, AppLanguage.ProfileSelectExperience);

				var success = await SaveAthleteFieldAsync();

				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					// Prevent OnAppearing from reloading and overwriting our change
				_skipNextProfileReload = true;
			}), true);
	}

	private async void OnSexSelectorTapped(object? sender, TappedEventArgs e)
	{
		var current = _athleteVm?.SelectedSex;
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileSexTitle, SexOptions, current, async val =>
			{
				if (_athleteVm is not null)
					_athleteVm.SelectedSex = val;
				SetSelectorValue(SexLabel, val, AppLanguage.ProfileSelectSex);

				var success = await SaveAthleteFieldAsync();

				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnTrainingDaysTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileTrainingDaysTitle, TrainingDaysOptions, _coachVm?.SelectedTrainingDays, async val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedTrainingDays = val;
				SetSelectorValue(TrainingDaysLabel, val, AppLanguage.ProfileSelectDays);
				
				// Autosave the change
				var success = await SaveCoachFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnSessionDurationTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileSessionDurationTitle, SessionDurationOptions, _coachVm?.SelectedSessionDuration, async val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedSessionDuration = val;
				SetSelectorValue(SessionDurationLabel, val, AppLanguage.ProfileSelectDuration);
				
				// Autosave the change
				var success = await SaveCoachFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnPrimaryGoalTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfilePrimaryGoalTitle, TrainingGoalOptions, _coachVm?.SelectedPrimaryGoal, async val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedPrimaryGoal = val;
				SetSelectorValue(PrimaryGoalLabel, val, AppLanguage.ProfileSelectPrimaryGoal);
				
				// Autosave the change
				var success = await SaveCoachFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnSecondaryGoalTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileSecondaryGoalTitle, TrainingGoalOptions, _coachVm?.SelectedSecondaryGoal, async val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedSecondaryGoal = val;
				SetSelectorValue(SecondaryGoalLabel, val, AppLanguage.ProfileSelectSecondaryGoal);
				
				// Autosave the change
				var success = await SaveCoachFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnDietaryPreferenceTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileDietaryTitle, DietaryPreferenceOptions, _coachVm?.SelectedDietaryPreference, async val =>
			{
				if (_coachVm is not null)
					_coachVm.SelectedDietaryPreference = val;
				SetSelectorValue(DietaryPreferenceLabel, val, AppLanguage.ProfileSelectDietary);
				
				// Autosave the change
				var success = await SaveCoachFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	private async void OnEquipmentSelectorTapped(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(
			new OptionPickerPage(AppLanguage.ProfileEquipmentTitle, EquipmentAccessOptions, _coachVm?.EquipmentText, async val =>
			{
				if (_coachVm is not null)
					_coachVm.EquipmentText = val;
				SetSelectorValue(EquipmentLabel, val, AppLanguage.ProfileSelectEquipment);
				
				// Autosave the change
				var success = await SaveCoachFieldAsync();
				
				// Prevent OnAppearing from reloading only if save succeeded
				if (success)
					_skipNextProfileReload = true;
			}), true);
	}

	// ── Data loading ──────────────────────────────────────────────────

	private async Task LoadAthleticPerformancesAsync()
	{
		var result = await _api.GetAthleticPerformancesAsync();
		if (!result.Success || result.Data is null)
		{
			AthleticPerformanceEmptyLabel.IsVisible = true;
			_lastAthleticCount = 0;
			RefreshTopSummary();
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
		_lastAthleticCount = items.Count;
		RefreshTopSummary();
	}

	private async Task LoadMovementGoalsAsync()
	{
		var result = await _api.GetMovementGoalsAsync();
		if (!result.Success || result.Data is null)
		{
			MovementGoalsEmptyLabel.IsVisible = true;
			_lastGoalCount = 0;
			RefreshTopSummary();
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
		_lastGoalCount = items.Count;
		RefreshTopSummary();
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

	private void OnHeightTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_athleteVm is not null)
			_athleteVm.HeightText = e.NewTextValue ?? "";
	}

	private async void OnWeightUnfocused(object? sender, FocusEventArgs e)
	{
		// Autosave when user leaves the weight field
		await SaveAthleteFieldAsync();
	}

	private async void OnBodyFatUnfocused(object? sender, FocusEventArgs e)
	{
		// Autosave when user leaves the body fat field
		await SaveAthleteFieldAsync();
	}

	private async void OnHeightUnfocused(object? sender, FocusEventArgs e)
	{
		// Autosave when user leaves the height field
		await SaveAthleteFieldAsync();
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
			HeightEntry.TextChanged -= OnHeightTextChanged;
			WeightEntry.Text = _athleteVm.WeightText;
			BodyFatEntry.Text = _athleteVm.BodyFatText;
			HeightEntry.Text = _athleteVm.HeightText;
			WeightEntry.TextChanged += OnWeightTextChanged;
			BodyFatEntry.TextChanged += OnBodyFatTextChanged;
			HeightEntry.TextChanged += OnHeightTextChanged;
			SetSelectorValue(SportLabel, _athleteVm.SelectedSport?.Name, AppLanguage.ProfileSelectSport);
			SetSelectorValue(SexLabel, _athleteVm.SelectedSex, AppLanguage.ProfileSelectSex);
			SyncPositionUI();
			SetSelectorValue(GymExperienceLabel, _athleteVm.SelectedGymExperience, AppLanguage.ProfileSelectExperience);
			ShowSuccess(AppLanguage.ProfileSaved);
		}
		else
		{
			ShowError(_athleteVm.SaveError ?? AppLanguage.ProfileFailedSave);
		}
	}

	/// <summary>
	/// Autosave for athlete profile changes. Shows errors but not success spam.
	/// Returns true if save succeeded (for callers that need to know).
	/// </summary>
	private async Task<bool> SaveAthleteFieldAsync()
	{
		if (_athleteVm is null) return false;

		var success = await _athleteVm.SaveAsync();
		if (!success)
		{
			ShowError(_athleteVm.SaveError ?? AppLanguage.ProfileAthleteFailedSave);
		}
		return success;
	}

	/// <summary>
	/// Autosave for coach profile changes. Shows errors but not success spam.
	/// Returns true if save succeeded (for callers that need to know).
	/// </summary>
	private async Task<bool> SaveCoachFieldAsync()
	{
		if (_coachVm is null) return false;

		var success = await _coachVm.SaveAsync();
		if (!success)
		{
			ShowError(_coachVm.SaveError ?? AppLanguage.ProfileCoachFailedSave);
		}
		return success;
	}

	internal async void OnSaveCoachProfileClicked(object? sender, EventArgs e)
	{
		ClearStatus();

		if (_profile is null || _coachVm is null) return;

		// INTENTIONAL: Editor controls use save-time push (not live TextChanged sync).
		// Editors contain multi-line free text — pushing on every keystroke would
		// trigger VM dirty tracking and validation noise. The VM sees editor values
		// only when the user explicitly taps Save.
		// Tested by: ProfilePageTests.RealPage_CoachEditors_PushedAtSaveTime_NotBefore
		// Equipment is now a selector (autosaves automatically) — no manual push needed
		_coachVm.LimitationsText = PhysicalLimitationsEditor.Text?.Trim() ?? "";
		_coachVm.InjuryHistoryText = InjuryHistoryEditor.Text?.Trim() ?? "";
		_coachVm.PainPointsText = CurrentPainEditor.Text?.Trim() ?? "";

		var success = await _coachVm.SaveAsync();

		if (success)
		{
			SyncCoachUI();
			ShowSuccess(AppLanguage.ProfileCoachSaved);
		}
		else
		{
			ShowError(_coachVm.SaveError ?? AppLanguage.ProfileCoachFailedSave);
		}
	}

	private async void OnPhysicalLimitationsEditorUnfocused(object? sender, FocusEventArgs e)
	{
		if (_coachVm is null) return;
		_coachVm.LimitationsText = PhysicalLimitationsEditor.Text?.Trim() ?? "";
		await SaveCoachFieldAsync();
	}

	private async void OnInjuryHistoryEditorUnfocused(object? sender, FocusEventArgs e)
	{
		if (_coachVm is null) return;
		_coachVm.InjuryHistoryText = InjuryHistoryEditor.Text?.Trim() ?? "";
		await SaveCoachFieldAsync();
	}

	private async void OnCurrentPainEditorUnfocused(object? sender, FocusEventArgs e)
	{
		if (_coachVm is null) return;
		_coachVm.PainPointsText = CurrentPainEditor.Text?.Trim() ?? "";
		await SaveCoachFieldAsync();
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
			ShowError(AppLanguage.ProfileChooseMovementError);
			return;
		}

		bool parsed = MetricInput.TryParseFlexibleDouble(PerformanceValueEntry.Text, out double value);
		if (!parsed || value <= 0)
		{
			ShowError(AppLanguage.FormatMustBePositive(_selectedPerformanceItem.PrimaryLabel));
			return;
		}

		double? secondaryValue = null;
		if (_selectedPerformanceItem.HasSecondaryMetric)
		{
			if (!MetricInput.TryParseFlexibleDouble(PerformanceSecondaryValueEntry.Text, out double parsedSecondary) || parsedSecondary <= 0)
			{
				ShowError(AppLanguage.FormatMustBePositive(_selectedPerformanceItem.SecondaryLabel));
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
				ShowError(AppLanguage.ProfileGctError);
				return;
			}

			groundContactTime = MetricInput.SecondsToMilliseconds(parsedGctSeconds);
		}

		if (_selectedPerformanceItem.SupportsConcentricTime && !string.IsNullOrWhiteSpace(PerformanceTimingEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(PerformanceTimingEntry.Text, out double parsedTime) || parsedTime <= 0)
			{
				ShowError(AppLanguage.ProfileConcentricError);
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
				ShowSuccess(AppLanguage.ProfilePerformanceUpdated);
			else
			{
				ShowError(result.Error ?? AppLanguage.ProfileFailedUpdate);
				return;
			}
		}
		else
		{
			var result = await _api.CreateAthleticPerformanceAsync(data);
			if (result.Success)
				ShowSuccess(AppLanguage.ProfilePerformanceAdded);
			else
			{
				ShowError(result.Error ?? AppLanguage.ProfileFailedGeneric);
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
			AppLanguage.ProfileDeleteEntryTitle,
			AppLanguage.FormatDeleteConfirm(item.Text),
			AppLanguage.SharedDelete,
			AppLanguage.SharedCancel);
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
			ShowSuccess(AppLanguage.ProfilePerformanceDeleted);
		}
		else
		{
			ShowError(result.Error ?? AppLanguage.ProfileFailedDelete);
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
		PerformanceActionButton.Text = AppLanguage.SharedUpdate;
		PerformanceCancelButton.IsVisible = true;
		ShowSuccess(AppLanguage.FormatEditing(item.Text));
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
			ShowError(AppLanguage.SharedChooseMovement);
			return;
		}

		string movementName = _selectedGoalItem.Name;
		string unit = ResolveGoalUnit(_selectedGoalItem);
		bool parsed = MetricInput.TryParseFlexibleDouble(GoalTargetValueEntry.Text, out double targetValue);

		if (string.IsNullOrWhiteSpace(movementName) || string.IsNullOrWhiteSpace(unit) || !parsed || targetValue <= 0)
		{
			ShowError(AppLanguage.ProfileGoalError);
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
				ShowSuccess(AppLanguage.ProfileGoalUpdated);
			else
			{
				ShowError(result.Error ?? AppLanguage.ProfileFailedUpdate);
				return;
			}
		}
		else
		{
			var result = await _api.CreateMovementGoalAsync(data);
			if (result.Success)
				ShowSuccess(AppLanguage.ProfileGoalSaved);
			else
			{
				ShowError(result.Error ?? AppLanguage.ProfileFailedGeneric);
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
			AppLanguage.ProfileDeleteGoalTitle,
			AppLanguage.FormatDeleteConfirm(item.Text),
			AppLanguage.SharedDelete,
			AppLanguage.SharedCancel);
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
			ShowSuccess(AppLanguage.ProfileGoalDeleted);
		}
		else
		{
			ShowError(result.Error ?? AppLanguage.ProfileFailedDelete);
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
		GoalActionButton.Text = AppLanguage.SharedUpdate;
		GoalCancelButton.IsVisible = true;
		ShowSuccess(AppLanguage.FormatEditing(item.Text));
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

	// ── Settings ──────────────────────────────────────────────────────

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		_skipNextProfileReload = true;
		await Navigation.PushAsync(new SettingsPage(_profile?.Email), true);
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
			AppLanguage.ProfileDeleteTitle,
			AppLanguage.ProfileDeleteConfirm,
			AppLanguage.SharedDelete,
			AppLanguage.SharedCancel);

		if (!confirmed)
		{
			return;
		}

		var password = await DisplayPromptAsync(
			AppLanguage.ProfileDeleteTitle,
			AppLanguage.ProfileDeletePasswordPrompt,
			keyboard: Keyboard.Default,
			maxLength: 128);

		if (string.IsNullOrEmpty(password))
		{
			return;
		}

		var result = await _api.DeleteAccountAsync(password);
		if (result.Success)
		{
			_session.SignOut();
			GoToLogin();
		}
		else
		{
			ShowError(result.Error ?? AppLanguage.ProfileFailedDelete);
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
		PerformanceActionButton.Text = AppLanguage.SharedSave;
		PerformanceCancelButton.IsVisible = false;
	}

	private async void OnChoosePerformanceMovementClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				AppLanguage.SharedChooseMovement,
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
				AppLanguage.SharedChooseMovement,
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
			SelectedPerformanceLabel.Text = AppLanguage.SharedNoMovementSelected;
			SelectedPerformanceHintLabel.Text = AppLanguage.ProfileBrowsePerformanceHint;
			PerformanceMetric1Label.Text = AppLanguage.ProfileResult;
			PerformanceValueEntry.Placeholder = AppLanguage.ProfileEnterResult;
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
			? AppLanguage.CalcGroundContactTime
			: AppLanguage.CalcConcentricTime;
		PerformanceTimingEntry.Placeholder = AppLanguage.ProfileTimingPlaceholder;
	}

	private void UpdateGoalSelectionUI()
	{
		if (_selectedGoalItem is null)
		{
			SelectedGoalLabel.Text = AppLanguage.SharedNoMovementSelected;
			SelectedGoalHintLabel.Text = AppLanguage.ProfileGoalHint;
			GoalUnitLabel.Text = "-";
			GoalTargetValueEntry.Placeholder = AppLanguage.ProfileTargetValue;
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
		GoalActionButton.Text = AppLanguage.SharedSave;
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
			_sportCatalogLoadError = result.Error ?? AppLanguage.SportCatalogRequestFailed;
		}
	}

	/// <summary>
	/// Syncs position UI from the athlete ViewModel's state.
	/// </summary>
	private void SyncPositionUI()
	{
		if (_athleteVm is null) return;
		PositionContainer.IsVisible = _athleteVm.ShowPositionSelector;
		SetSelectorValue(PositionLabel, _athleteVm.SelectedPosition, AppLanguage.ProfileSelectPosition);
	}

	/// <summary>
	/// Syncs all coach profile UI from the coach ViewModel's state.
	/// </summary>
	private void SyncCoachUI()
	{
		if (_coachVm is null) return;
		SetSelectorValue(TrainingDaysLabel, _coachVm.SelectedTrainingDays, AppLanguage.ProfileSelectDays);
		SetSelectorValue(SessionDurationLabel, _coachVm.SelectedSessionDuration, AppLanguage.ProfileSelectDuration);
		SetSelectorValue(PrimaryGoalLabel, _coachVm.SelectedPrimaryGoal, AppLanguage.ProfileSelectPrimaryGoal);
		SetSelectorValue(SecondaryGoalLabel, _coachVm.SelectedSecondaryGoal, AppLanguage.ProfileSelectSecondaryGoal);
		SetSelectorValue(DietaryPreferenceLabel, _coachVm.SelectedDietaryPreference, AppLanguage.ProfileSelectDietary);
		SetSelectorValue(EquipmentLabel, _coachVm.EquipmentText, AppLanguage.ProfileSelectEquipment);
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

	// ── Profile Photo ────────────────────────────────────────────

	private async Task LoadProfilePhotoAsync()
	{
		var result = await _api.GetProfilePhotoAsync();
		if (result.Success && result.Data is { Length: > 0 })
		{
			var bytes = result.Data;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ProfilePhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
				ProfilePhotoImage.IsVisible = true;
				InitialsLabel.IsVisible = false;
			});
		}
		else
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ProfilePhotoImage.IsVisible = false;
				InitialsLabel.IsVisible = true;
			});
		}
	}

	private async void OnAvatarTapped(object? sender, TappedEventArgs e)
	{
		bool hasPhoto = ProfilePhotoImage.IsVisible;
		string[] buttons = hasPhoto
			? [AppLanguage.ProfileChoosePhoto, AppLanguage.ProfileRemovePhoto]
			: [AppLanguage.ProfileChoosePhoto];

		var action = await DisplayActionSheet(
			AppLanguage.ProfileChangePhoto,
			AppLanguage.SharedCancel,
			null,
			buttons);

		if (action == AppLanguage.ProfileChoosePhoto)
			await PickAndUploadPhotoAsync();
		else if (action == AppLanguage.ProfileRemovePhoto)
			await RemovePhotoAsync();
	}

	private async Task PickAndUploadPhotoAsync()
	{
		try
		{
			var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
			{
				Title = AppLanguage.ProfileChoosePhoto
			});

			if (photo is null) return;

			await using var stream = await photo.OpenReadAsync();

			if (stream.Length > 2 * 1024 * 1024)
			{
				await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProfilePhotoTooLarge, AppLanguage.SharedOk);
				return;
			}

			var contentType = photo.ContentType ?? "image/jpeg";
			var result = await _api.UploadProfilePhotoAsync(stream, contentType, photo.FileName);

			if (result.Success)
			{
				await LoadProfilePhotoAsync();
				ShowSuccess(AppLanguage.ProfilePhotoUpdated);
			}
			else if (result.StatusCode == 400)
			{
				var errorMsg = result.Error ?? string.Empty;
				var msg = (errorMsg.Contains("büyük") || errorMsg.Contains("large"))
					? AppLanguage.ProfilePhotoTooLarge
					: (errorMsg.Contains("tür") || errorMsg.Contains("type"))
						? AppLanguage.ProfilePhotoUnsupportedType
						: errorMsg.Length > 0 ? errorMsg : AppLanguage.ProfilePhotoUploadFailed;
				await DisplayAlert(AppLanguage.SharedError, msg, AppLanguage.SharedOk);
			}
			else
			{
				await DisplayAlert(AppLanguage.SharedError,
					result.Error ?? AppLanguage.ProfilePhotoUploadFailed,
					AppLanguage.SharedOk);
			}
		}
		catch (Exception ex) when (ex is FeatureNotSupportedException or PermissionException)
		{
			await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProfilePhotoUploadFailed, AppLanguage.SharedOk);
		}
		catch (Exception)
		{
			await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProfilePhotoUploadFailed, AppLanguage.SharedOk);
		}
	}

	private async Task RemovePhotoAsync()
	{
		var result = await _api.DeleteProfilePhotoAsync();
		if (result.Success)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ProfilePhotoImage.IsVisible = false;
				InitialsLabel.IsVisible = true;
			});
			ShowSuccess(AppLanguage.ProfilePhotoRemoved);
		}
		else
		{
			await DisplayAlert(AppLanguage.SharedError,
				result.Error ?? AppLanguage.ProfilePhotoUploadFailed,
				AppLanguage.SharedOk);
		}
	}
}
