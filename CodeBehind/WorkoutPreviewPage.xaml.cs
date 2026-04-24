using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class WorkoutPreviewPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly string _workoutName;
	private readonly DateTime _workoutDate;
	private readonly TimeSpan _duration;
	private readonly List<ExerciseEntry> _exercises;
	private readonly StartWorkoutSessionPage _sessionPage;
	private bool _saved;

	public WorkoutPreviewPage(
		string workoutName,
		DateTime workoutDate,
		TimeSpan duration,
		List<ExerciseEntry> exercises,
		StartWorkoutSessionPage sessionPage)
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_workoutName = workoutName;
		_workoutDate = workoutDate;
		_duration = duration;
		_exercises = exercises;
		_sessionPage = sessionPage;

		ApplyLanguage();
		BuildSummary();
		BuildExerciseList();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged()
	{
		ApplyLanguage();
		BuildSummary();
	}

	private void ApplyLanguage()
	{
		PageTitleLabel.Text = AppLanguage.PreviewTitle;
		GoBackButton.Text = AppLanguage.PreviewGoBack;
		SaveButton.Text = AppLanguage.PreviewSave;
	}

	/// <summary>
	/// Whether this preview resulted in a successful save.
	/// </summary>
	public bool Saved => _saved;

	private void BuildSummary()
	{
		SummaryNameLabel.Text = _workoutName;
		SummaryDateLabel.Text = _workoutDate.ToString("dd MMM yyyy");
		var formatted = _duration.TotalHours >= 1
			? _duration.ToString(@"h\:mm\:ss")
			: _duration.ToString(@"mm\:ss");
		SummaryDurationLabel.Text = AppLanguage.FormatDuration(formatted);
	}

	private void BuildExerciseList()
	{
		ExercisesContainer.Children.Clear();

		foreach (var entry in _exercises)
		{
			var card = new Border
			{
				StrokeShape = new RoundRectangle { CornerRadius = 14 },
				BackgroundColor = ColorResources.GetColor("SurfaceRaised", "#1D1828"),
				Stroke = new SolidColorBrush(ColorResources.GetColor("SurfaceBorder", "#342D46")),
				StrokeThickness = 1,
				Padding = new Thickness(16, 12)
			};

			var stack = new VerticalStackLayout { Spacing = 4 };

			stack.Children.Add(new Label
			{
				Text = entry.ExerciseName,
				FontSize = 14,
				FontFamily = "OpenSansSemibold",
				TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB")
			});

			var details = new List<string> { ExerciseSummaryFormatter.FormatStrength(entry) };
			if (entry.RestSeconds.HasValue) details.Add($"Rest: {entry.RestSeconds.Value}s");

			stack.Children.Add(new Label
			{
				Text = string.Join(" | ", details),
				FontSize = 12,
				FontFamily = "OpenSansRegular",
				TextColor = ColorResources.GetColor("TextSecondary", "#B3B2C5")
			});

			card.Content = stack;
			ExercisesContainer.Children.Add(card);
		}
	}

	private async void OnGoBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		ErrorLabel.IsVisible = false;
		SaveButton.IsEnabled = false;
		SaveButton.Text = AppLanguage.PreviewSaving;

		var exercises = _exercises.Select(entry => new
		{
			exerciseName = entry.ExerciseName,
			exerciseCategory = entry.ExerciseCategory,
			trackingMode = entry.TrackingMode,
			setsCount = entry.SetsCount,
			sets = entry.Sets.Select(s => new
			{
				setNumber = s.SetNumber,
				reps = s.Reps,
				weight = s.Weight
			}).ToList(),
			reps = entry.Reps,
			rir = entry.RIR,
			restSeconds = entry.RestSeconds,
			groundContactTimeMs = entry.GroundContactTimeMs,
			concentricTimeSeconds = entry.ConcentricTimeSeconds,
			metric1Value = entry.Metric1Value,
			metric1Unit = entry.Metric1Unit,
			metric2Value = entry.Metric2Value,
			metric2Unit = entry.Metric2Unit
		}).ToList();

		var workoutData = new
		{
			workoutName = _workoutName,
			workoutDate = $"{_workoutDate:yyyy-MM-dd}",
			exercises
		};

		var result = await _api.CreateWorkoutAsync(workoutData);
		if (result.Success)
		{
			_saved = true;
			_sessionPage.StopAllTimers();
			// Remove the live session page from the stack, then pop this page → lands on ProgramDetail
			Navigation.RemovePage(_sessionPage);
			await Navigation.PopAsync(true);
		}
		else
		{
			SaveButton.IsEnabled = true;
			SaveButton.Text = AppLanguage.PreviewSave;
			ErrorLabel.Text = result.Error ?? AppLanguage.PreviewFailed;
			ErrorLabel.IsVisible = true;
		}
	}

}
