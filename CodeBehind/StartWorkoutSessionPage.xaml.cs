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
	private int _restSecondsRemaining;
	private bool _timerStarted;
	private bool _pickingExercise;

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
		// No exercises to build — user adds via "Egzersiz Ekle"
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// If returning from exercise picker, don't restart timer
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
		// Don't stop workout timer — it should keep counting when navigating to preview
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
					var (view, data) = ExerciseInputRowBuilder.Build(pe, prefilled);
					_rowData.Add(data);
					ExercisesContainer.Children.Add(view);
				}
				catch
				{
					// Skip exercises that fail to convert rather than crashing
				}
			}
		}

		// Also add any exercises that were added dynamically (empty mode or mid-session adds)
		// These are already in _rowData and ExercisesContainer from AddExerciseRow
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
		var (view, data) = ExerciseInputRowBuilder.Build(pe, prefilled);
		_rowData.Add(data);
		ExercisesContainer.Children.Add(view);

		// Also track in session state
		_sessionState.Exercises.Add(prefilled);
	}

	private async void OnAddExerciseClicked(object? sender, EventArgs e)
	{
		_pickingExercise = true;
		await Navigation.PushAsync(
			new ExercisePickerPage("Egzersiz Ekle", ExerciseCatalog.Categories, OnExercisePicked), true);
	}

	private void OnExercisePicked(ExerciseCatalogItem item)
	{
		MainThread.BeginInvokeOnMainThread(() => AddExerciseRow(item));
	}

	private void OnRestClicked(object? sender, EventArgs e)
	{
		if (_restTimer is not null && _restTimer.IsRunning)
		{
			_restTimer.Stop();
			RestTimerLabel.IsVisible = false;
			RestButton.Text = "Dinlenme Başlat";
			return;
		}

		int restSeconds = 60;
		foreach (var row in _rowData)
		{
			if (row.TemplateExercise.RestSeconds is > 0)
			{
				restSeconds = row.TemplateExercise.RestSeconds.Value;
				break;
			}
		}

		_restSecondsRemaining = restSeconds;
		RestTimerLabel.Text = $"Rest: {_restSecondsRemaining}s";
		RestTimerLabel.IsVisible = true;
		RestButton.Text = "Dinlenme Durdur";

		_restTimer = Dispatcher.CreateTimer();
		_restTimer.Interval = TimeSpan.FromSeconds(1);
		_restTimer.Tick += OnRestTimerTick;
		_restTimer.Start();
	}

	private void OnRestTimerTick(object? sender, EventArgs e)
	{
		_restSecondsRemaining--;
		if (_restSecondsRemaining <= 0)
		{
			_restTimer?.Stop();
			RestTimerLabel.Text = "Rest: Done!";
			RestButton.Text = "Dinlenme Başlat";

			try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300)); }
			catch { /* ignore if not supported */ }

			Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), () =>
			{
				if (RestTimerLabel.Text == "Rest: Done!")
					RestTimerLabel.IsVisible = false;
			});
			return;
		}

		RestTimerLabel.Text = $"Rest: {_restSecondsRemaining}s";
	}

	private async void OnFinishClicked(object? sender, EventArgs e)
	{
		ErrorLabel.IsVisible = false;

		// Read current values from all input rows
		var exercises = new List<ExerciseEntry>();
		foreach (var row in _rowData)
		{
			var entry = ExerciseInputRowBuilder.ReadValues(row);
			if (entry.Sets <= 0)
			{
				ErrorLabel.Text = $"{entry.ExerciseName}: Set sayısı gerekli.";
				ErrorLabel.IsVisible = true;
				return;
			}
			exercises.Add(entry);
		}

		if (exercises.Count == 0)
		{
			ErrorLabel.Text = "En az bir egzersiz gerekli.";
			ErrorLabel.IsVisible = true;
			return;
		}

		// Capture current state — session stays active so user can go back
		_sessionState.Exercises = exercises;

		var duration = DateTime.Now - _sessionState.StartedAt;
		await Navigation.PushAsync(
			new WorkoutPreviewPage(_sessionState.WorkoutName, _sessionState.StartedAt, duration, exercises, this), true);
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		bool confirm = await DisplayAlert("Antrenmanı İptal Et",
			"Devam eden antrenmanı iptal etmek istediğinize emin misiniz?", "Evet", "Hayır");
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
