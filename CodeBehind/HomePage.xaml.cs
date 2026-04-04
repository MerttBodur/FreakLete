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
	private int _pickingExerciseSlot; // 0 = not picking, 1 = picking ex1, 2 = picking ex2
	private bool _showingStarterTemplates;

	public HomePage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// If returning from exercise picker for exercise 2
		if (_pickingExerciseSlot == 2)
		{
			await Navigation.PushAsync(
				new ExercisePickerPage("Egzersiz 2 Seç", ExerciseCatalog.Categories, OnComparisonExercisePicked), true);
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

		// Load workouts and training programs in parallel
		var workoutsTask = _api.GetWorkoutsAsync();
		var programsTask = _api.GetTrainingProgramsAsync();

		await Task.WhenAll(workoutsTask, programsTask);

		// Process comparison chart
		var workoutResult = workoutsTask.Result;
		if (workoutResult.Success && workoutResult.Data is not null)
		{
			_allWorkouts = workoutResult.Data;
			UpdateComparisonChart();
		}

		// Process quick workouts — fallback to starter templates if user has none
		var programsResult = programsTask.Result;
		if (programsResult.Success && programsResult.Data is not null && programsResult.Data.Count > 0)
		{
			_showingStarterTemplates = false;
			BuildQuickWorkoutCards(programsResult.Data);
		}
		else
		{
			var starterResult = await _api.GetStarterTemplatesAsync();
			if (starterResult.Success && starterResult.Data is not null && starterResult.Data.Count > 0)
			{
				_showingStarterTemplates = true;
				BuildQuickWorkoutCards(starterResult.Data);
			}
		}
	}

	private void UpdateComparisonChart()
	{
		if (_allWorkouts is null || _allWorkouts.Count == 0)
		{
			ComparisonChart.Exercise1Name = _exercise1Name;
			ComparisonChart.Exercise2Name = _exercise2Name;
			return;
		}

		var today = DateTime.Now.Date;
		var exercise1Data = new List<float>();
		var exercise2Data = new List<float>();
		var dayLabels = new List<string>();

		string[] turkishDayAbbr = ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"];

		for (int i = 6; i >= 0; i--)
		{
			var targetDate = today.AddDays(-i);
			dayLabels.Add(turkishDayAbbr[(int)targetDate.DayOfWeek]);

			float max1 = 0, max2 = 0;

			var dayWorkouts = _allWorkouts
				.Where(w => w.WorkoutDate.Date == targetDate);

			foreach (var workout in dayWorkouts)
			{
				if (workout.Exercises is null) continue;
				foreach (var ex in workout.Exercises)
				{
					if (string.Equals(ex.ExerciseName, _exercise1Name, StringComparison.OrdinalIgnoreCase)
						&& ex.Metric1Value.HasValue)
						max1 = Math.Max(max1, (float)ex.Metric1Value.Value);

					if (string.Equals(ex.ExerciseName, _exercise2Name, StringComparison.OrdinalIgnoreCase)
						&& ex.Metric1Value.HasValue)
						max2 = Math.Max(max2, (float)ex.Metric1Value.Value);
				}
			}

			exercise1Data.Add(max1);
			exercise2Data.Add(max2);
		}

		ComparisonChart.Exercise1Name = _exercise1Name;
		ComparisonChart.Exercise2Name = _exercise2Name;
		ComparisonChart.Exercise1Data = exercise1Data;
		ComparisonChart.Exercise2Data = exercise2Data;
		ComparisonChart.Exercise1Delta = CalculateDelta(exercise1Data);
		ComparisonChart.Exercise2Delta = CalculateDelta(exercise2Data);
		ComparisonChart.DayLabels = dayLabels;
	}

	private static string CalculateDelta(List<float> data)
	{
		float firstNonZero = data.FirstOrDefault(v => v > 0);
		float last = data.LastOrDefault(v => v > 0);
		if (firstNonZero <= 0 || last <= 0) return "-";
		float diff = last - firstNonZero;
		return diff >= 0 ? $"+{diff:0}kg" : $"{diff:0}kg";
	}

	private async void OnChangeComparisonExercisesClicked(object? sender, EventArgs e)
	{
		_pickingExerciseSlot = 1;
		await Navigation.PushAsync(
			new ExercisePickerPage("Egzersiz 1 Seç", ExerciseCatalog.Categories, OnComparisonExercisePicked), true);
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

	private void BuildQuickWorkoutCards(List<TrainingProgramListResponse> programs)
	{
		QuickWorkoutsLayout.Children.Clear();

		foreach (var program in programs.Take(6))
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
				Text = $"{program.DaysPerWeek} gün/hafta",
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
			bool isStarter = _showingStarterTemplates;
			tap.Tapped += async (s, e) => await OnQuickWorkoutTapped(programId, isStarter);
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
