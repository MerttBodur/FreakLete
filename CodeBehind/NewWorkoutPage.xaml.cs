using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class NewWorkoutPage : ContentPage
{
	private readonly List<ExerciseEntry> _exercises = new();
	private readonly ApiClient _api;
	private readonly UserSession _session;
	private readonly int? _editingWorkoutId;
	private ExerciseCatalogItem? _selectedExerciseItem;

	public NewWorkoutPage() : this(null)
	{
	}

	public NewWorkoutPage(int? workoutId)
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		_editingWorkoutId = workoutId;
		ConfigurePageMode();
		UpdateExerciseSelectionUI();
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
		ConfirmWorkoutButton.Text = isEditing ? "Save Changes" : "Save Workout";
	}

	private async Task LoadWorkoutForEditAsync()
	{
		var result = await _api.GetWorkoutByIdAsync(_editingWorkoutId!.Value);
		if (!result.Success || result.Data is null)
		{
			ShowError("Workout could not be loaded.");
			return;
		}

		var workout = result.Data;
		WorkoutNameEntry.Text = workout.WorkoutName;
		WorkoutDatePicker.Date = workout.WorkoutDate;
		ExercisesSection.IsVisible = true;

		_exercises.Clear();
		foreach (var ex in workout.Exercises)
		{
			_exercises.Add(new ExerciseEntry
			{
				ExerciseName = ex.ExerciseName,
				ExerciseCategory = ex.ExerciseCategory,
				TrackingMode = ex.TrackingMode,
				Sets = ex.Sets,
				Reps = ex.Reps,
				RIR = ex.RIR,
				RestSeconds = ex.RestSeconds,
				GroundContactTimeMs = ex.GroundContactTimeMs,
				ConcentricTimeSeconds = ex.ConcentricTimeSeconds,
				Metric1Value = ex.Metric1Value,
				Metric1Unit = ex.Metric1Unit,
				Metric2Value = ex.Metric2Value,
				Metric2Unit = ex.Metric2Unit
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

		if (_selectedExerciseItem is null)
		{
			ShowError("Please choose an exercise first.");
			return;
		}

		ExerciseEntry? entry = BuildExerciseEntryFromInputs();
		if (entry is null)
		{
			return;
		}

		_exercises.Add(entry);

		ClearExerciseInputs();
		RefreshExercisesList();
	}

	private async void OnConfirmWorkoutClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (!_session.IsLoggedIn())
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

		string dateStr = $"{WorkoutDatePicker.Date:yyyy-MM-dd}";
		var exercises = _exercises.Select(ex => new
		{
			exerciseName = ex.ExerciseName,
			exerciseCategory = ex.ExerciseCategory,
			trackingMode = ex.TrackingMode,
			sets = ex.Sets,
			reps = ex.Reps,
			rir = ex.RIR,
			restSeconds = ex.RestSeconds,
			groundContactTimeMs = ex.GroundContactTimeMs,
			concentricTimeSeconds = ex.ConcentricTimeSeconds,
			metric1Value = ex.Metric1Value,
			metric1Unit = ex.Metric1Unit,
			metric2Value = ex.Metric2Value,
			metric2Unit = ex.Metric2Unit
		}).ToList();

		var workoutData = new
		{
			workoutName = WorkoutNameEntry.Text.Trim(),
			workoutDate = dateStr,
			exercises
		};

		if (_editingWorkoutId.HasValue)
		{
			var result = await _api.UpdateWorkoutAsync(_editingWorkoutId.Value, workoutData);
			if (!result.Success)
			{
				ShowError(result.Error ?? "Failed to update workout.");
				return;
			}
		}
		else
		{
			var result = await _api.CreateWorkoutAsync(workoutData);
			if (!result.Success)
			{
				ShowError(result.Error ?? "Failed to save workout.");
				return;
			}
		}

		await Navigation.PopAsync(true);
	}

	private void RefreshExercisesList()
	{
		var items = _exercises
			.Select((x, index) => new ExerciseListItem
			{
				Index = index,
				ExerciseName = x.ExerciseName,
				SetRepText = FormatPrimarySummary(x),
				RestText = FormatSecondarySummary(x)
			})
			.ToList();

		ExercisesCollectionView.ItemsSource = items;
		ExerciseCountLabel.Text = _exercises.Count == 1 ? "1 item" : $"{_exercises.Count} items";
	}

	private void OnDeleteExerciseInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<ExerciseListItem>(sender) is not ExerciseListItem item)
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
		ConcentricTimeEntry.Text = string.Empty;
		Metric1Entry.Text = string.Empty;
		Metric2Entry.Text = string.Empty;
		GroundContactTimeEntry.Text = string.Empty;
		_selectedExerciseItem = null;
		UpdateExerciseSelectionUI();
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
		await Navigation.PopAsync(true);
	}

	private static TItem? GetBindingContext<TItem>(object? sender) where TItem : class
	{
		return sender switch
		{
			BindableObject bindable when bindable.BindingContext is TItem item => item,
			_ => null
		};
	}

	private sealed class ExerciseListItem
	{
		public int Index { get; set; }
		public string ExerciseName { get; set; } = string.Empty;
		public string SetRepText { get; set; } = string.Empty;
		public string RestText { get; set; } = string.Empty;
	}

	private async void OnChooseExerciseClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				"Choose Exercise",
				ExerciseCatalog.Categories,
				OnExerciseSelected),
			true);
	}

	private void OnExerciseSelected(ExerciseCatalogItem item)
	{
		_selectedExerciseItem = item;
		UpdateExerciseSelectionUI();
	}

	private void UpdateExerciseSelectionUI()
	{
		if (_selectedExerciseItem is null)
		{
			SelectedExerciseLabel.Text = "No exercise selected";
			SelectedExerciseHintLabel.Text = "Tap browse to open your recommended movement library.";
			StrengthInputsSection.IsVisible = false;
			CustomInputsSection.IsVisible = false;
			Metric2Container.IsVisible = false;
			StrengthTimingContainer.IsVisible = false;
			GroundContactTimeContainer.IsVisible = false;
			return;
		}

		SelectedExerciseLabel.Text = _selectedExerciseItem.Name;
		SelectedExerciseHintLabel.Text = _selectedExerciseItem.SelectionHintText;

		bool isStrength = _selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength;
		StrengthInputsSection.IsVisible = isStrength;
		CustomInputsSection.IsVisible = !isStrength;
		StrengthTimingContainer.IsVisible = isStrength && _selectedExerciseItem.SupportsConcentricTime;
		GroundContactTimeContainer.IsVisible = !isStrength && _selectedExerciseItem.SupportsGroundContactTime;

		if (!isStrength)
		{
			Metric1Label.Text = $"{_selectedExerciseItem.PrimaryLabel} ({_selectedExerciseItem.PrimaryUnit})";
			Metric1Entry.Placeholder = $"Enter {_selectedExerciseItem.PrimaryLabel.ToLowerInvariant()}";
			Metric2Container.IsVisible = _selectedExerciseItem.HasSecondaryMetric;
			Metric2Label.Text = $"{_selectedExerciseItem.SecondaryLabel} ({_selectedExerciseItem.SecondaryUnit})";
			Metric2Entry.Placeholder = $"Enter {_selectedExerciseItem.SecondaryLabel.ToLowerInvariant()}";
		}
	}

	private ExerciseEntry? BuildExerciseEntryFromInputs()
	{
		if (_selectedExerciseItem is null)
		{
			ShowError("Please choose an exercise first.");
			return null;
		}

		if (_selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
		{
			if (!int.TryParse(SetCountEntry.Text, out int setCount) || setCount <= 0)
			{
				ShowError("Set count is required and must be a positive number.");
				return null;
			}

			if (!int.TryParse(RepCountEntry.Text, out int repCount) || repCount <= 0)
			{
				ShowError("Rep count is required and must be a positive number.");
				return null;
			}

			int? restSeconds = null;
			int? rir = null;

			if (!string.IsNullOrWhiteSpace(RirEntry.Text))
			{
				if (!int.TryParse(RirEntry.Text, out int parsedRir) || parsedRir < 0 || parsedRir > 5)
				{
					ShowError("RIR must be between 0 - 5.");
					return null;
				}

				rir = parsedRir;
			}

			if (!string.IsNullOrWhiteSpace(RestSecondsEntry.Text))
			{
				if (!int.TryParse(RestSecondsEntry.Text, out int parsedRest) || parsedRest <= 0)
				{
					ShowError("Rest seconds must be a positive number.");
					return null;
				}

				restSeconds = parsedRest;
			}

			double? concentricTime = null;
			if (!string.IsNullOrWhiteSpace(ConcentricTimeEntry.Text))
			{
				if (!MetricInput.TryParseFlexibleDouble(ConcentricTimeEntry.Text, out double parsedTime) || parsedTime <= 0)
				{
					ShowError("Concentric time must be a positive number.");
					return null;
				}

				concentricTime = parsedTime;
			}

			return new ExerciseEntry
			{
				ExerciseName = _selectedExerciseItem.Name,
				ExerciseCategory = _selectedExerciseItem.Category,
				TrackingMode = nameof(ExerciseTrackingMode.Strength),
				Sets = setCount,
				Reps = repCount,
				RIR = rir,
				RestSeconds = restSeconds,
				ConcentricTimeSeconds = concentricTime
			};
		}

		if (!MetricInput.TryParseFlexibleDouble(Metric1Entry.Text, out double metric1) || metric1 <= 0)
		{
			ShowError($"{_selectedExerciseItem.PrimaryLabel} must be a positive number.");
			return null;
		}

		double? metric2 = null;
		if (_selectedExerciseItem.HasSecondaryMetric)
		{
			if (!MetricInput.TryParseFlexibleDouble(Metric2Entry.Text, out double parsedMetric2) || parsedMetric2 <= 0)
			{
				ShowError($"{_selectedExerciseItem.SecondaryLabel} must be a positive number.");
				return null;
			}

			metric2 = parsedMetric2;
		}

		double? gct = null;
		if (_selectedExerciseItem.SupportsGroundContactTime && !string.IsNullOrWhiteSpace(GroundContactTimeEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(GroundContactTimeEntry.Text, out double parsedGctSeconds) || parsedGctSeconds <= 0)
			{
				ShowError("Ground contact time must be a positive number.");
				return null;
			}

			gct = MetricInput.SecondsToMilliseconds(parsedGctSeconds);
		}

		return new ExerciseEntry
		{
			ExerciseName = _selectedExerciseItem.Name,
			ExerciseCategory = _selectedExerciseItem.Category,
			TrackingMode = nameof(ExerciseTrackingMode.Custom),
			Metric1Value = metric1,
			Metric1Unit = _selectedExerciseItem.PrimaryUnit,
			Metric2Value = metric2,
			Metric2Unit = _selectedExerciseItem.SecondaryUnit,
			GroundContactTimeMs = gct
		};
	}

	private static string FormatPrimarySummary(ExerciseEntry entry)
	{
		if (entry.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory(entry.ExerciseName, entry.ExerciseCategory);
			if (item is not null)
			{
				return $"{item.PrimaryLabel}: {entry.Metric1Value:0.##} {entry.Metric1Unit}";
			}
		}

		return entry.RIR.HasValue
			? $"Sets x Reps: {entry.Sets} x {entry.Reps} (RIR{entry.RIR.Value})"
			: $"Sets x Reps: {entry.Sets} x {entry.Reps}";
	}

	private static string FormatSecondarySummary(ExerciseEntry entry)
	{
		if (entry.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory(entry.ExerciseName, entry.ExerciseCategory);
			if (item is not null && item.HasSecondaryMetric && entry.Metric2Value.HasValue)
			{
				string baseText = $"{item.SecondaryLabel}: {entry.Metric2Value:0.##} {entry.Metric2Unit}";
				return entry.GroundContactTimeMs.HasValue
					? $"{baseText} | GCT: {MetricInput.FormatSecondsFromMilliseconds(entry.GroundContactTimeMs.Value)}"
					: baseText;
			}

			return entry.GroundContactTimeMs.HasValue
				? $"GCT: {MetricInput.FormatSecondsFromMilliseconds(entry.GroundContactTimeMs.Value)}"
				: $"Category: {entry.ExerciseCategory}";
		}

		List<string> details = [];
		if (entry.RestSeconds.HasValue)
		{
			details.Add($"Rest: {entry.RestSeconds.Value} sec");
		}

		if (entry.ConcentricTimeSeconds.HasValue)
		{
			details.Add($"Concentric: {entry.ConcentricTimeSeconds.Value:0.##} s");
		}

		return details.Count > 0
			? string.Join(" | ", details)
			: $"Category: {entry.ExerciseCategory}";
	}
}
