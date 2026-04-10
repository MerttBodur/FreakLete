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
			BuildQuickWorkoutCards(quickCards.Take(6).ToList());
	}

	private void OnChartRangeChanged(object? sender, ChartDataHelper.ChartRange range)
	{
		_selectedRange = range;
		UpdateComparisonChart();
	}

	private void UpdateComparisonChart()
	{
		var today      = DateTime.Now.Date;
		var dayAbbr    = AppLanguage.HomeDayAbbreviations;
		var monthAbbr  = AppLanguage.ChartMonthAbbreviations;

		var (data1, labels1) = ChartDataHelper.BuildBuckets(
			_exercise1Name, _selectedRange, today,
			_allWorkouts, _allPrEntries, _allAthleticEntries,
			dayAbbr, monthAbbr);

		var (data2, _) = ChartDataHelper.BuildBuckets(
			_exercise2Name, _selectedRange, today,
			_allWorkouts, _allPrEntries, _allAthleticEntries,
			dayAbbr, monthAbbr);

		string unit1 = ChartDataHelper.UnitForExercise(_exercise1Name, _allPrEntries, _allAthleticEntries, _allWorkouts);
		string unit2 = ChartDataHelper.UnitForExercise(_exercise2Name, _allPrEntries, _allAthleticEntries, _allWorkouts);

		ComparisonChart.Exercise1Name  = _exercise1Name;
		ComparisonChart.Exercise2Name  = _exercise2Name;
		ComparisonChart.Exercise1Data  = data1;
		ComparisonChart.Exercise2Data  = data2;
		ComparisonChart.AxisLabels     = labels1;
		ComparisonChart.Exercise1Unit  = unit1;
		ComparisonChart.Exercise2Unit  = unit2;
		ComparisonChart.Exercise1Delta = CalculateDelta(data1, unit1);
		ComparisonChart.Exercise2Delta = CalculateDelta(data2, unit2);
		ComparisonChart.SelectedRange  = _selectedRange;
	}

	private static string CalculateDelta(List<float> data, string unit)
	{
		float firstNonZero = data.FirstOrDefault(v => v > 0);
		float last = data.LastOrDefault(v => v > 0);
		if (firstNonZero <= 0 || last <= 0) return "-";
		float diff = last - firstNonZero;
		return diff >= 0 ? $"+{diff:0}{unit}" : $"{diff:0}{unit}";
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
			var card = new Border
			{
				WidthRequest = 180,
				StrokeShape = new RoundRectangle { CornerRadius = 18 },
				Stroke = (Color)Application.Current!.Resources["SurfaceBorder"],
				StrokeThickness = 1,
				Padding = 0,
				Background = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(1, 1),
					GradientStops =
					{
						new GradientStop { Color = (Color)Application.Current.Resources["SurfaceRaised"], Offset = 0 },
						new GradientStop { Color = (Color)Application.Current.Resources["Surface"], Offset = 1 }
					}
				}
			};

			var stack = new VerticalStackLayout { Spacing = 0 };

			// Image area with overlay
			var imageArea = new Grid { HeightRequest = 90, WidthRequest = 180 };
			imageArea.Clip = new RoundRectangleGeometry(new CornerRadius(18, 18, 0, 0),
				new Rect(0, 0, 180, 90));

			var imageName = WorkoutImageResolver.GetImageForProgram(program.Name);
			if (imageName is not null)
			{
				imageArea.Children.Add(new Image
				{
					Source = imageName,
					Aspect = Aspect.AspectFill,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Fill
				});
				imageArea.Children.Add(new BoxView
				{
					Color = Colors.Black,
					Opacity = 0.35,
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Fill
				});
			}
			else
			{
				var fallbackHex = WorkoutImageResolver.GetFallbackColor(program.Name);
				imageArea.Children.Add(new BoxView
				{
					BackgroundColor = Color.FromArgb(fallbackHex),
					HorizontalOptions = LayoutOptions.Fill,
					VerticalOptions = LayoutOptions.Fill
				});
				imageArea.Children.Add(new Label
				{
					Text = program.Name.Length > 0 ? program.Name[..1] : "W",
					FontSize = 30,
					FontFamily = "OpenSansSemibold",
					TextColor = Colors.White,
					Opacity = 0.6,
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				});
			}
			stack.Children.Add(imageArea);

			var textStack = new VerticalStackLayout
			{
				Padding = new Thickness(14, 10, 14, 14),
				Spacing = 4
			};

			textStack.Children.Add(new Label
			{
				Text = program.Name,
				FontSize = 14,
				FontFamily = "OpenSansSemibold",
				TextColor = (Color)Application.Current.Resources["TextPrimary"],
				LineBreakMode = LineBreakMode.TailTruncation,
				MaxLines = 1
			});

			textStack.Children.Add(new Label
			{
				Text = AppLanguage.FormatDaysPerWeek(program.DaysPerWeek),
				FontSize = 11,
				FontFamily = "OpenSansRegular",
				TextColor = (Color)Application.Current.Resources["TextSecondary"]
			});

			if (!string.IsNullOrEmpty(program.Goal))
			{
				textStack.Children.Add(new Label
				{
					Text = program.Goal,
					FontSize = 11,
					FontFamily = "OpenSansRegular",
					TextColor = (Color)Application.Current.Resources["TextMuted"],
					LineBreakMode = LineBreakMode.TailTruncation,
					MaxLines = 1
				});
			}

			stack.Children.Add(textStack);
			card.Content = stack;

			var tap = new TapGestureRecognizer();
			int programId = program.Id;
			bool cardIsStarter = isStarter;
			tap.Tapped += async (s, e) => await OnQuickWorkoutTapped(programId, cardIsStarter);
			card.GestureRecognizers.Add(tap);

			QuickWorkoutsLayout.Children.Add(card);
		}
	}

	private async Task OnQuickWorkoutTapped(int programId, bool isStarterTemplate)
	{
		await Navigation.PushAsync(new ProgramDetailPage(programId, isStarterTemplate), true);
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new StartWorkoutSessionPage(), true);
	}
}
