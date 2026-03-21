using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class NewWorkoutPage : ContentPage
{
	private readonly List<ExerciseEntry> _exercises = new();
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

	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private readonly int? _editingWorkoutId;
	private string? _selectedExerciseName;

	public NewWorkoutPage() : this(null)
	{
	}

	public NewWorkoutPage(int? workoutId)
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		_editingWorkoutId = workoutId;
		ExerciseOptionsView.ItemsSource = _exerciseOptions;
		ConfigurePageMode();
		RefreshExercisesList();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_editingWorkoutId.HasValue)
		{
			await LoadWorkoutForEditAsync();
		}
	}

	private void ConfigurePageMode()
	{
		bool isEditing = _editingWorkoutId.HasValue;
		PageTitleLabel.Text = isEditing ? "Edit Workout" : "Add New Workout";
		PageHelperLabel.Text = isEditing
			? "Update the workout details, adjust exercises, and save the new session version."
			: "Set the date, name your workout, then add exercises to the session.";
		Title = isEditing ? "Edit Workout" : "Add New Workout";
		ConfirmWorkoutButton.Text = isEditing ? "Save Workout Changes" : "Confirm Add New Workout";
	}

	private async Task LoadWorkoutForEditAsync()
	{
		Workout? workout = await _database.GetWorkoutByIdAsync(_editingWorkoutId!.Value);
		if (workout is null)
		{
			ShowError("Workout could not be loaded.");
			return;
		}

		List<ExerciseEntry> exercises = await _database.GetExercisesByWorkoutIdAsync(workout.Id);

		WorkoutNameEntry.Text = workout.WorkoutName;
		WorkoutDatePicker.Date = workout.WorkoutDate;
		ExercisesSection.IsVisible = true;

		_exercises.Clear();
		foreach (ExerciseEntry exercise in exercises)
		{
			_exercises.Add(new ExerciseEntry
			{
				ExerciseName = exercise.ExerciseName,
				Sets = exercise.Sets,
				Reps = exercise.Reps,
				RIR = exercise.RIR,
				RestSeconds = exercise.RestSeconds
			});
		}

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
		int? rir = null;

		if (!string.IsNullOrWhiteSpace(RirEntry.Text))
		{
			if (!int.TryParse(RirEntry.Text, out int parsedRir) || parsedRir < 0 || parsedRir > 5)
			{
				ShowError("RIR must be between 0 - 5.");
				return;
			}

			rir = parsedRir;
		}

		if (!string.IsNullOrWhiteSpace(RestSecondsEntry.Text))
		{
			if (!int.TryParse(RestSecondsEntry.Text, out int parsedRest) || parsedRest <= 0)
			{
				ShowError("Rest seconds must be a positive number.");
				return;
			}

			restSeconds = parsedRest;
		}

		_exercises.Add(new ExerciseEntry
		{
			ExerciseName = _selectedExerciseName,
			Sets = setCount,
			Reps = repCount,
			RIR = rir,
			RestSeconds = restSeconds
		});

		ClearExerciseInputs();
		RefreshExercisesList();
	}

	private async void OnConfirmWorkoutClicked(object? sender, EventArgs e)
	{
		ClearError();

		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			ShowError("Please log in again.");
			return;
		}

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

		Workout workout = new()
		{
			Id = _editingWorkoutId.GetValueOrDefault(),
			UserId = currentUserId.Value,
			WorkoutDate = WorkoutDatePicker.Date.GetValueOrDefault().Date,
			WorkoutName = WorkoutNameEntry.Text.Trim()
		};

		List<ExerciseEntry> exerciseCopies = _exercises.Select(exercise => new ExerciseEntry
		{
			ExerciseName = exercise.ExerciseName,
			Sets = exercise.Sets,
			Reps = exercise.Reps,
			RIR = exercise.RIR,
			RestSeconds = exercise.RestSeconds
		}).ToList();

		if (_editingWorkoutId.HasValue)
		{
			await _database.UpdateWorkoutAsync(workout, exerciseCopies);
		}
		else
		{
			await _database.SaveWorkoutAsync(workout, exerciseCopies);
		}

		await Navigation.PopAsync(false);
	}

	private void RefreshExercisesList()
	{
		var items = _exercises
			.Select((x, index) => new ExerciseListItem
			{
				Index = index,
				ExerciseName = x.ExerciseName,
				SetRepText = x.RIR.HasValue
					? $"Set x Rep: {x.Sets} x {x.Reps} (RIR{x.RIR.Value})"
					: $"Set x Rep: {x.Sets} x {x.Reps}",
				RestText = x.RestSeconds.HasValue ? $"Rest: {x.RestSeconds.Value} sec" : "Rest: -"
			})
			.ToList();

		ExercisesCollectionView.ItemsSource = items;
		ExerciseCountLabel.Text = _exercises.Count == 1 ? "1 item" : $"{_exercises.Count} items";
	}

	private void OnExerciseSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		var selected = e.CurrentSelection.FirstOrDefault() as ExerciseOption;
		_selectedExerciseName = selected?.Name;
		SelectedExerciseLabel.Text = $"Selected: {_selectedExerciseName ?? "-"}";
	}

	private void OnDeleteExerciseInvoked(object? sender, EventArgs e)
	{
		if (sender is not SwipeItem swipeItem || swipeItem.BindingContext is not ExerciseListItem item)
		{
			return;
		}

		if (item.Index < 0 || item.Index >= _exercises.Count)
		{
			return;
		}

		_exercises.RemoveAt(item.Index);
		RefreshExercisesList();
	}

	private void ClearExerciseInputs()
	{
		SetCountEntry.Text = string.Empty;
		RepCountEntry.Text = string.Empty;
		RirEntry.Text = string.Empty;
		RestSecondsEntry.Text = string.Empty;
		_selectedExerciseName = null;
		ExerciseOptionsView.SelectedItem = null;
		SelectedExerciseLabel.Text = "Selected: -";
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

	private async void OnHeaderBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(false);
	}

	private sealed class ExerciseListItem
	{
		public int Index { get; set; }
		public string ExerciseName { get; set; } = string.Empty;
		public string SetRepText { get; set; } = string.Empty;
		public string RestText { get; set; } = string.Empty;
	}

	private sealed class ExerciseOption
	{
		public string Name { get; set; } = string.Empty;
	}
}
