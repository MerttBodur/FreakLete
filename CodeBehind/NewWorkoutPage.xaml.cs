namespace GymTracker;

public partial class NewWorkoutPage : ContentPage
{
	private readonly List<ExerciseRecord> _exercises = new();
	private readonly List<ExerciseOption> _exerciseOptions = new()
	{
		new() { Name = "Bench Press" },
		new() { Name = "Incline Dumbbell Press" },
		new() { Name = "Squat" },
		new() { Name = "Deadlift" },
		new() { Name = "Overhead Press" },
		new() { Name = "Barbell Row" },
		new() { Name = "Pull Up" },
		new() { Name = "Lat Pulldown" },
		new() { Name = "Biceps Curl" },
		new() { Name = "Triceps Pushdown" }
	};

	private string? _selectedExerciseName;

	public NewWorkoutPage()
	{
		InitializeComponent();
		ExerciseOptionsView.ItemsSource = _exerciseOptions;
		RefreshExercisesList();
	}

	private void OnAddExercisesClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (string.IsNullOrWhiteSpace(WorkoutNameEntry.Text))
		{
			ShowError("Workout name is required before adding exercises.");
			return;
		}

		ExercisesSection.IsVisible = true;
	}

	private void OnAddExerciseClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (string.IsNullOrWhiteSpace(_selectedExerciseName))
		{
			ShowError("Please select an exercise from the list.");
			return;
		}

		if (!int.TryParse(SetCountEntry.Text, out int setCount) || setCount <= 0)
		{
			ShowError("Set count is required and must be a positive number.");
			return;
		}

		if (!int.TryParse(RepCountEntry.Text, out int repCount) || repCount <= 0)
		{
			ShowError("Rep count is required and must be a positive number.");
			return;
		}

		int? restSeconds = null;
		if (!string.IsNullOrWhiteSpace(RestSecondsEntry.Text))
		{
			if (!int.TryParse(RestSecondsEntry.Text, out int parsedRest) || parsedRest <= 0)
			{
				ShowError("Rest seconds must be a positive number.");
				return;
			}

			restSeconds = parsedRest;
		}

		_exercises.Add(new ExerciseRecord
		{
			ExerciseName = _selectedExerciseName,
			SetCount = setCount,
			RepCount = repCount,
			RestSeconds = restSeconds
		});

		ClearExerciseInputs();
		RefreshExercisesList();
	}

	private async void OnConfirmWorkoutClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (string.IsNullOrWhiteSpace(WorkoutNameEntry.Text))
		{
			ShowError("Workout name is required.");
			return;
		}

		if (_exercises.Count == 0)
		{
			ShowError("Add at least one exercise before confirm.");
			return;
		}

		var allWorkouts = WorkoutStorage.Load();
		allWorkouts.Add(new WorkoutRecord
		{
			Date = WorkoutDatePicker.Date ?? DateTime.Today,
			WorkoutName = WorkoutNameEntry.Text.Trim(),
			Exercises = _exercises.ToList()
		});

		WorkoutStorage.Save(allWorkouts);
		await Shell.Current.GoToAsync("..", false);
	}

	private void RefreshExercisesList()
	{
		var items = _exercises
			.Select(x => new ExerciseListItem
			{
				ExerciseName = x.ExerciseName,
				SetRepText = $"Set x Rep: {x.SetCount} x {x.RepCount}",
				RestText = x.RestSeconds.HasValue ? $"Rest: {x.RestSeconds.Value} sec" : "Rest: -"
			})
			.ToList();

		ExercisesCollectionView.ItemsSource = items;
	}

	private void OnExerciseSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		var selected = e.CurrentSelection.FirstOrDefault() as ExerciseOption;
		_selectedExerciseName = selected?.Name;
		SelectedExerciseLabel.Text = $"Selected: {_selectedExerciseName ?? "-"}";
	}

	private void ClearExerciseInputs()
	{
		SetCountEntry.Text = string.Empty;
		RepCountEntry.Text = string.Empty;
		RestSecondsEntry.Text = string.Empty;
	}

	private void ShowError(string message)
	{
		ErrorLabel.Text = message;
		ErrorLabel.IsVisible = true;
	}

	private void ClearError()
	{
		ErrorLabel.Text = string.Empty;
		ErrorLabel.IsVisible = false;
	}

	private sealed class ExerciseListItem
	{
		public string ExerciseName { get; set; } = string.Empty;
		public string SetRepText { get; set; } = string.Empty;
		public string RestText { get; set; } = string.Empty;
	}

	private sealed class ExerciseOption
	{
		public string Name { get; set; } = string.Empty;
	}
}
