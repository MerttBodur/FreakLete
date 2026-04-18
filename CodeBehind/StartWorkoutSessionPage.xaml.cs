using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete;

public partial class StartWorkoutSessionPage : ContentPage
{
	private readonly WorkoutSessionState _sessionState;
	private readonly ProgramSessionResponse? _templateSession;
	private readonly List<ExerciseInputRowBuilder.ExerciseRowData> _rowData = [];

	private IDispatcherTimer? _workoutTimer;
	private IDispatcherTimer? _restTimer;
	private int _restSecondsElapsed;
	private bool _timerStarted;
	private bool _pickingExercise;

	private ExerciseInputRowBuilder.SetData? _activeSet;

	/// <summary>
	/// Template mode — pre-loads exercises from a program session.
	/// </summary>
	public StartWorkoutSessionPage(string programName, string sessionDisplayName, ProgramSessionResponse session)
	{
		InitializeComponent();
		_templateSession = session;

		var workoutName = $"{programName} - {sessionDisplayName}";
		var exercises = ProgramExerciseConverter.ConvertAll(session.Exercises ?? []);

		_sessionState = WorkoutSessionState.FromTemplate(workoutName, exercises);
		WorkoutNameLabel.Text = _sessionState.WorkoutName;
		ApplyLanguage();
		BuildExerciseRows();
	}

	/// <summary>
	/// Empty mode — starts a free-form workout with no pre-loaded exercises.
	/// </summary>
	public StartWorkoutSessionPage()
	{
		InitializeComponent();
		_templateSession = null;
		_sessionState = WorkoutSessionState.Empty();
		WorkoutNameLabel.Text = _sessionState.WorkoutName;
		ApplyLanguage();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;

		if (_pickingExercise)
		{
			_pickingExercise = false;
			return;
		}

		if (!_timerStarted)
		{
			_sessionState.StartedAt = DateTime.Now;
			_sessionState.IsActive = true;
			_timerStarted = true;
			_workoutTimer = Dispatcher.CreateTimer();
			_workoutTimer.Interval = TimeSpan.FromSeconds(1);
			_workoutTimer.Tick += OnWorkoutTimerTick;
			_workoutTimer.Start();
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged() => ApplyLanguage();

	private void ApplyLanguage()
	{
		AddExerciseButton.Text = AppLanguage.LiveWorkoutAddExercise;
		RestButton.Text = (_restTimer is not null && _restTimer.IsRunning)
			? AppLanguage.LiveWorkoutRestEnd
			: AppLanguage.LiveWorkoutRestStart;
		FinishButton.Text = AppLanguage.LiveWorkoutFinish;
	}

	private void OnWorkoutTimerTick(object? sender, EventArgs e)
	{
		var elapsed = _sessionState.Elapsed;
		TimerLabel.Text = elapsed.TotalHours >= 1
			? elapsed.ToString(@"h\:mm\:ss")
			: elapsed.ToString(@"mm\:ss");
	}

	private void BuildExerciseRows()
	{
		ExercisesContainer.Children.Clear();
		_rowData.Clear();

		if (_templateSession is not null)
		{
			var exercises = _templateSession.Exercises ?? [];
			foreach (var pe in exercises.OrderBy(e => e.Order))
			{
				try
				{
					var prefilled = ProgramExerciseConverter.Convert(pe);
					var (view, data) = ExerciseInputRowBuilder.BuildLive(pe, prefilled, OnSetSelected);
					_rowData.Add(data);
					ExercisesContainer.Children.Add(view);
				}
				catch
				{
					// Skip exercises that fail to convert
				}
			}
		}
	}

	private void AddExerciseRow(ExerciseCatalogItem catalogItem)
	{
		var pe = new ProgramExerciseResponse
		{
			ExerciseName = catalogItem.Name,
			ExerciseCategory = catalogItem.Category,
			Sets = 3,
			RepsOrDuration = "10",
			RestSeconds = 60,
			Order = _rowData.Count + 1
		};

		var prefilled = ProgramExerciseConverter.Convert(pe);
		var (view, data) = ExerciseInputRowBuilder.BuildLive(pe, prefilled, OnSetSelected);
		_rowData.Add(data);
		ExercisesContainer.Children.Add(view);

		_sessionState.Exercises.Add(prefilled);
	}

	private void OnSetSelected(ExerciseInputRowBuilder.SetData set)
	{
		// Deselect previous
		if (_activeSet is not null)
		{
			_activeSet.Row.Stroke = new SolidColorBrush(ColorResources.GetColor("SurfaceBorder", "#342D46"));
			_activeSet.Row.StrokeThickness = 1;
		}

		_activeSet = set;
		_activeSet.Row.Stroke = new SolidColorBrush(ColorResources.GetColor("AccentGlow", "#A78BFA"));
		_activeSet.Row.StrokeThickness = 2;
	}

	private async void OnAddExerciseClicked(object? sender, EventArgs e)
	{
		_pickingExercise = true;
		await Navigation.PushAsync(
			new ExercisePickerPage(AppLanguage.LiveWorkoutAddExerciseTitle, ExerciseCatalog.Categories, OnExercisePicked), true);
	}

	private void OnExercisePicked(ExerciseCatalogItem item)
	{
		MainThread.BeginInvokeOnMainThread(() => AddExerciseRow(item));
	}

	private void OnRestClicked(object? sender, EventArgs e)
	{
		if (_restTimer is not null && _restTimer.IsRunning)
		{
			// Stop — write elapsed time to active set
			_restTimer.Stop();

			if (_activeSet is not null)
			{
				_activeSet.RestSeconds = _restSecondsElapsed;
				int mins = _restSecondsElapsed / 60;
				int secs = _restSecondsElapsed % 60;
				_activeSet.RestLabel.Text = mins > 0 ? $"{mins}:{secs:D2}" : $"{secs}s";
			}

			RestTimerLabel.IsVisible = false;
			RestButton.Text = AppLanguage.LiveWorkoutRestStart;
			return;
		}

		// Start count-up timer from 0
		_restSecondsElapsed = 0;
		RestTimerLabel.Text = "0:00";
		RestTimerLabel.IsVisible = true;
		RestButton.Text = AppLanguage.LiveWorkoutRestEnd;

		_restTimer = Dispatcher.CreateTimer();
		_restTimer.Interval = TimeSpan.FromSeconds(1);
		_restTimer.Tick += OnRestTimerTick;
		_restTimer.Start();
	}

	private void OnRestTimerTick(object? sender, EventArgs e)
	{
		_restSecondsElapsed++;
		int mins = _restSecondsElapsed / 60;
		int secs = _restSecondsElapsed % 60;
		RestTimerLabel.Text = $"{mins}:{secs:D2}";
	}

	private async void OnFinishClicked(object? sender, EventArgs e)
	{
		ErrorLabel.IsVisible = false;

		var exercises = new List<ExerciseEntry>();
		foreach (var row in _rowData)
		{
			var entry = ExerciseInputRowBuilder.ReadValues(row);
			if (entry.Sets <= 0)
			{
				ErrorLabel.Text = $"{entry.ExerciseName}: {AppLanguage.LiveWorkoutSetRequired}";
				ErrorLabel.IsVisible = true;
				return;
			}
			exercises.Add(entry);
		}

		if (exercises.Count == 0)
		{
			ErrorLabel.Text = AppLanguage.LiveWorkoutNeedExercise;
			ErrorLabel.IsVisible = true;
			return;
		}

		_sessionState.Exercises = exercises;

		var duration = DateTime.Now - _sessionState.StartedAt;
		await Navigation.PushAsync(
			new WorkoutPreviewPage(_sessionState.WorkoutName, _sessionState.StartedAt, duration, exercises, this), true);
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		bool confirm = await DisplayAlert(AppLanguage.LiveWorkoutCancelTitle,
			AppLanguage.LiveWorkoutCancelConfirm, AppLanguage.LiveWorkoutYes, AppLanguage.LiveWorkoutNo);
		if (confirm)
		{
			StopAllTimers();
			_sessionState.Stop();
			await Navigation.PopAsync(true);
		}
	}

	public void StopAllTimers()
	{
		_workoutTimer?.Stop();
		_restTimer?.Stop();
	}

}
