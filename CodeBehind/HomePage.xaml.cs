using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class HomePage : ContentPage
{
	private readonly ApiClient _api;
	private readonly UserSession _session;

	private string _exercise1Name = "Bench Press";
	private string _exercise2Name = "Squat";

	private List<WorkoutResponse>? _allWorkouts;
	private List<PrEntryResponse>? _allPrEntries;
	private List<AthleticPerformanceResponse>? _allAthleticEntries;

	private ChartDataHelper.ChartRange _selectedRange = ChartDataHelper.ChartRange.Days14;

	private int _pickingExerciseSlot; // 0 = not picking, 1 = picking ex1, 2 = picking ex2
	private sealed record QuickCard(TrainingProgramListResponse Program, bool IsStarter);

	public HomePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		ApplyLanguage();

		ComparisonChart.RangeChanged += OnChartRangeChanged;
		CalcTile.Command = new Command(async () => await TabNavigationHelper.SwitchToTabAsync(() => new CalculationsPage()));
		CalendarTile.Command = new Command(async () => await Navigation.PushAsync(new CalendarPage(), true));
		AiSendTap.Tapped += OnAiSendTapped;
	}

	private void ApplyLanguage()
	{
		WelcomeLabel.Text = AppLanguage.HomeWelcome;
		StartWorkoutLabel.Text = AppLanguage.HomeStartWorkout;
		StartButton.Text = AppLanguage.HomeStart;
		WorkoutsBadgeLabel.Text = AppLanguage.HomeWorkoutsBadge;
		QuickWorkoutsTitle.Text = AppLanguage.HomeQuickWorkouts;
		QuickWorkoutsDesc.Text = AppLanguage.HomeQuickWorkoutsDesc;
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

		// If returning from exercise picker for exercise 2
		if (_pickingExerciseSlot == 2)
		{
			await Navigation.PushAsync(
				new ExercisePickerPage(AppLanguage.HomePickExercise2, ExerciseCatalog.Categories, OnComparisonExercisePicked), true);
			return;
		}

		_pickingExerciseSlot = 0;
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
		UserNameLabel.Text = profile.FirstName;
		WorkoutCountLabel.Text = profile.TotalWorkouts.ToString();

		// Load all data sources in parallel
		var workoutsTask    = _api.GetWorkoutsAsync();
		var programsTask    = _api.GetTrainingProgramsAsync();
		var prTask          = _api.GetPrEntriesAsync();
		var athleticTask    = _api.GetAthleticPerformancesAsync();

		await Task.WhenAll(workoutsTask, programsTask, prTask, athleticTask);

		// Comparison chart data
		var workoutResult  = workoutsTask.Result;
		var prResult       = prTask.Result;
		var athleticResult = athleticTask.Result;

		_allWorkouts       = workoutResult.Success  && workoutResult.Data  is not null ? workoutResult.Data  : [];
		_allPrEntries      = prResult.Success        && prResult.Data       is not null ? prResult.Data       : [];
		_allAthleticEntries = athleticResult.Success && athleticResult.Data is not null ? athleticResult.Data : [];

		var weekStart = DateTime.Now.Date.AddDays(-6);
		WeekSessionsTile.Value = _allWorkouts.Count(w => w.WorkoutDate.Date >= weekStart).ToString();
		LastOnermTile.Value = ResolveLatestOneRm(_allPrEntries);

		UpdateComparisonChart();

		// Quick workouts
		var programsResult  = programsTask.Result;
		var starterResult   = await _api.GetStarterTemplatesAsync();

		var quickCards = new List<QuickCard>();

		if (programsResult.Success && programsResult.Data is not null)
			foreach (var p in programsResult.Data)
				quickCards.Add(new QuickCard(p, false));

		if (starterResult.Success && starterResult.Data is not null)
		{
			var existingNames = new HashSet<string>(
				quickCards.Select(c => c.Program.Name), StringComparer.OrdinalIgnoreCase);
			foreach (var p in starterResult.Data)
				if (!existingNames.Contains(p.Name))
					quickCards.Add(new QuickCard(p, true));
		}

		if (quickCards.Count > 0)
		{
			// Cap user programs at 3, fill remaining slots with starters that
			// have a mapped image so the rail always shows some photos.
			var userCards = quickCards.Where(c => !c.IsStarter).Take(3).ToList();
			var starterCards = quickCards
				.Where(c => c.IsStarter && WorkoutImageResolver.GetImageForProgram(c.Program.Name, c.Program.Goal) is not null)
				.Take(6 - userCards.Count)
				.ToList();
			BuildQuickWorkoutCards([.. userCards, .. starterCards]);
		}
	}

	private void OnChartRangeChanged(object? sender, ChartDataHelper.ChartRange range)
	{
		_selectedRange = range;
		UpdateComparisonChart();
	}

	private void UpdateComparisonChart()
	{
		var today     = DateTime.Now.Date;
		var monthAbbr = AppLanguage.ChartMonthAbbreviations;

		var (values1, labels1) = ChartDataHelper.BuildSparsePoints(
			_exercise1Name, _selectedRange, today,
			_allWorkouts, _allPrEntries, _allAthleticEntries,
			monthAbbr);

		var (values2, _) = ChartDataHelper.BuildSparsePoints(
			_exercise2Name, _selectedRange, today,
			_allWorkouts, _allPrEntries, _allAthleticEntries,
			monthAbbr);

		string unit1 = ChartDataHelper.UnitForExercise(_exercise1Name, _allPrEntries, _allAthleticEntries, _allWorkouts);
		string unit2 = ChartDataHelper.UnitForExercise(_exercise2Name, _allPrEntries, _allAthleticEntries, _allWorkouts);

		ComparisonChart.Exercise1Name  = _exercise1Name;
		ComparisonChart.Exercise2Name  = _exercise2Name;
		ComparisonChart.Exercise1Data  = values1;
		ComparisonChart.Exercise2Data  = values2;
		ComparisonChart.AxisLabels     = labels1;
		ComparisonChart.Exercise1Unit  = unit1;
		ComparisonChart.Exercise2Unit  = unit2;
		ComparisonChart.Exercise1Delta = FormatDelta(ChartDataHelper.ComputeDelta(values1), unit1);
		ComparisonChart.Exercise2Delta = FormatDelta(ChartDataHelper.ComputeDelta(values2), unit2);
		ComparisonChart.SelectedRange  = _selectedRange;
	}

	private static string FormatDelta(float? delta, string unit)
	{
		if (delta is null) return "-";
		return delta >= 0 ? $"+{delta:0}{unit}" : $"{delta:0}{unit}";
	}

	private async void OnChangeComparisonExercisesClicked(object? sender, EventArgs e)
	{
		_pickingExerciseSlot = 1;
		await Navigation.PushAsync(
			new ExercisePickerPage(AppLanguage.HomePickExercise1, ExerciseCatalog.Categories, OnComparisonExercisePicked), true);
	}

	private void OnComparisonExercisePicked(ExerciseCatalogItem item)
	{
		if (_pickingExerciseSlot == 1)
		{
			_exercise1Name = item.Name;
			_pickingExerciseSlot = 2;
		}
		else if (_pickingExerciseSlot == 2)
		{
			_exercise2Name = item.Name;
			_pickingExerciseSlot = 0;
			MainThread.BeginInvokeOnMainThread(UpdateComparisonChart);
		}
	}

	private void BuildQuickWorkoutCards(List<QuickCard> cards)
	{
		QuickWorkoutsLayout.Children.Clear();

		foreach (var (program, isStarter) in cards)
		{
			QuickWorkoutsLayout.Children.Add(CreateQuickWorkoutCard(program, isStarter));
		}
	}

	private View CreateQuickWorkoutCard(TrainingProgramListResponse program, bool isStarter)
	{
		var card = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 20 },
			Stroke = new SolidColorBrush(ColorResources.GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			BackgroundColor = ColorResources.GetColor("SurfaceRaised", "#1D1828"),
			Padding = new Thickness(18, 16),
			HorizontalOptions = LayoutOptions.Fill
		};

		var content = new VerticalStackLayout { Spacing = 10 };
		var titleRow = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			ColumnSpacing = 10
		};

		titleRow.Add(new Label
		{
			Text = program.Name,
			FontSize = 17,
			FontFamily = "OpenSansSemibold",
			TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB"),
			LineBreakMode = LineBreakMode.TailTruncation,
			MaxLines = 1,
			VerticalOptions = LayoutOptions.Center
		}, 0);

		var badgeText = !string.IsNullOrWhiteSpace(program.Goal)
			? program.Goal
			: program.Status;
		if (!string.IsNullOrWhiteSpace(badgeText))
		{
			titleRow.Add(CreateQuickWorkoutBadge(badgeText), 1);
		}

		content.Children.Add(titleRow);

		var pills = new HorizontalStackLayout { Spacing = 8 };
		if (program.DaysPerWeek > 0)
		{
			pills.Children.Add(CreateQuickWorkoutPill(AppLanguage.FormatXPerWeek(program.DaysPerWeek)));
		}

		if (isStarter)
		{
			pills.Children.Add(CreateQuickWorkoutPill(AppLanguage.ProgramDetailTemplate));
		}
		else if (!string.IsNullOrWhiteSpace(program.Status)
			&& !string.Equals(program.Status, badgeText, StringComparison.OrdinalIgnoreCase))
		{
			pills.Children.Add(CreateQuickWorkoutPill(program.Status));
		}

		if (pills.Children.Count > 0)
		{
			content.Children.Add(pills);
		}

		card.Content = content;

		var programId = program.Id;
		card.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(async () => await OnQuickWorkoutTapped(programId, isStarter))
		});

		return card;
	}

	private static Border CreateQuickWorkoutBadge(string text)
	{
		return new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = ColorResources.GetColor("AccentSoft", "#2F2346"),
			Stroke = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(10, 4),
			VerticalOptions = LayoutOptions.Center,
			Content = new Label
			{
				Text = text,
				FontSize = 10,
				FontFamily = "OpenSansSemibold",
				TextColor = ColorResources.GetColor("AccentGlow", "#A78BFA"),
				LineBreakMode = LineBreakMode.TailTruncation,
				MaxLines = 1
			}
		};
	}

	private static Border CreateQuickWorkoutPill(string text)
	{
		return new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = ColorResources.GetColor("Surface", "#171321"),
			Stroke = new SolidColorBrush(ColorResources.GetColor("SurfaceBorder", "#342D46")),
			Padding = new Thickness(8, 4),
			VerticalOptions = LayoutOptions.Center,
			Content = new Label
			{
				Text = text,
				FontSize = 11,
				FontFamily = "OpenSansSemibold",
				TextColor = ColorResources.GetColor("TextSecondary", "#B3B2C5")
			}
		};
	}

	private async Task OnQuickWorkoutTapped(int programId, bool isStarterTemplate)
	{
		await Navigation.PushAsync(new ProgramDetailPage(programId, isStarterTemplate), true);
	}

	private static string ResolveLatestOneRm(List<PrEntryResponse> entries)
	{
		var latestStrength = entries
			.Where(e => e.Weight > 0 && e.Reps > 0)
			.OrderByDescending(e => e.CreatedAt)
			.FirstOrDefault();

		if (latestStrength is null)
			return "-";

		var oneRm = CalculationService.CalculateOneRm(
			latestStrength.Weight,
			latestStrength.Reps,
			latestStrength.RIR ?? 0);

		return oneRm.ToString("0.#");
	}

	private async void OnAiSendTapped(object? sender, TappedEventArgs e)
	{
		await TabNavigationHelper.SwitchToTabAsync(() => new FreakAiPage());
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new StartWorkoutSessionPage(), true);
	}
}
