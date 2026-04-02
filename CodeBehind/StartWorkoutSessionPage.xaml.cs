using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete;

public partial class StartWorkoutSessionPage : ContentPage
{
	private readonly string _workoutName;
	private readonly string _sessionDisplayName;
	private readonly ProgramSessionResponse _session;
	private readonly List<ExerciseInputRowBuilder.ExerciseRowData> _rowData = [];

	private DateTime _startTime;
	private IDispatcherTimer? _workoutTimer;
	private IDispatcherTimer? _restTimer;
	private int _restSecondsRemaining;
	private bool _timerStarted;

	public StartWorkoutSessionPage(string programName, string sessionDisplayName, ProgramSessionResponse session)
	{
		InitializeComponent();
		_session = session;
		_sessionDisplayName = sessionDisplayName;
		_workoutName = $"{programName} - {sessionDisplayName}";
		WorkoutNameLabel.Text = _workoutName;
		BuildExerciseRows();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (!_timerStarted)
		{
			_startTime = DateTime.Now;
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
		var elapsed = DateTime.Now - _startTime;
		TimerLabel.Text = elapsed.TotalHours >= 1
			? elapsed.ToString(@"h\:mm\:ss")
			: elapsed.ToString(@"mm\:ss");
	}

	private void BuildExerciseRows()
	{
		ExercisesContainer.Children.Clear();
		_rowData.Clear();

		foreach (var pe in _session.Exercises.OrderBy(e => e.Order))
		{
			var prefilled = ProgramExerciseConverter.Convert(pe);
			var (view, data) = ExerciseInputRowBuilder.Build(pe, prefilled);
			_rowData.Add(data);
			ExercisesContainer.Children.Add(view);
		}
	}

	private void OnRestClicked(object? sender, EventArgs e)
	{
		if (_restTimer is not null && _restTimer.IsRunning)
		{
			// Stop rest timer
			_restTimer.Stop();
			RestTimerLabel.IsVisible = false;
			RestButton.Text = "Dinlenme Başlat";
			return;
		}

		// Determine rest duration — use first exercise's RestSeconds or default 60
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

			// Vibrate if supported
			try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300)); }
			catch { /* ignore if not supported */ }

			// Hide after 3 seconds
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

		var duration = DateTime.Now - _startTime;
		await Navigation.PushAsync(
			new WorkoutPreviewPage(_workoutName, DateTime.Now, duration, exercises, this), true);
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		bool confirm = await DisplayAlert("Antrenmanı İptal Et",
			"Devam eden antrenmanı iptal etmek istediğinize emin misiniz?", "Evet", "Hayır");
		if (confirm)
		{
			StopAllTimers();
			await Navigation.PopAsync(true);
		}
	}

	public void StopAllTimers()
	{
		_workoutTimer?.Stop();
		_restTimer?.Stop();
	}
}
