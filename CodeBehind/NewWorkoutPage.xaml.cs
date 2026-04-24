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
		ApplyLanguage();
		ConfigurePageMode();
		UpdateExerciseSelectionUI();
		RefreshExercisesList();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;
		if (_editingWorkoutId.HasValue)
		{
			await LoadWorkoutForEditAsync();
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged()
	{
		ApplyLanguage();
		ConfigurePageMode();
		UpdateExerciseSelectionUI();
		RefreshExercisesList();
	}

	private void ApplyLanguage()
	{
		Header.Title = _editingWorkoutId.HasValue ? AppLanguage.NewWorkoutEditTitle : AppLanguage.NewWorkoutTitle;
		SessionSetupBadge.Text = AppLanguage.NewWorkoutSessionSetup;
		WorkoutDetailsLabel.Text = AppLanguage.NewWorkoutDetails;
		DateLabel.Text = AppLanguage.NewWorkoutDate;
		WorkoutNameLabel2.Text = AppLanguage.NewWorkoutName;
		WorkoutNameEntry.Placeholder = AppLanguage.NewWorkoutNamePlaceholder;
		ContinueButton.Text = AppLanguage.SharedContinue;
		ExerciseBuilderLabel.Text = AppLanguage.NewWorkoutExerciseBuilder;
		ChooseExerciseLabel.Text = AppLanguage.NewWorkoutChooseExercise;
		BrowseExerciseBtn.Text = AppLanguage.SharedBrowse;
		SetCountLabel.Text = AppLanguage.NewWorkoutSetCount;
		RepCountLabel.Text = AppLanguage.NewWorkoutRepCount;
		RirLabel.Text = AppLanguage.NewWorkoutRir;
		RestSecondsLabel.Text = AppLanguage.NewWorkoutRestSeconds;
		ConcentricTimeLabel.Text = AppLanguage.NewWorkoutConcentricTime;
		GctLabel.Text = AppLanguage.NewWorkoutGctLabel;
		AddExerciseBtn.Text = AppLanguage.NewWorkoutAdd;
		SessionExercisesLabel.Text = AppLanguage.NewWorkoutSessionExercises;
		NoExerciseLabel.Text = AppLanguage.NewWorkoutNoExerciseAdded;
	}

	private void ConfigurePageMode()
	{
		bool isEditing = _editingWorkoutId.HasValue;
		PageTitleLabel.Text = isEditing ? AppLanguage.NewWorkoutEditTitle : AppLanguage.NewWorkoutTitle;
		PageHelperLabel.Text = isEditing ? AppLanguage.NewWorkoutEditDesc : AppLanguage.NewWorkoutAddDesc;
		Title = isEditing ? AppLanguage.NewWorkoutEditTitle : AppLanguage.NewWorkoutTitle;
		ConfirmWorkoutButton.Text = isEditing ? AppLanguage.NewWorkoutSaveChanges : AppLanguage.NewWorkoutSave;
	}

	private async Task LoadWorkoutForEditAsync()
	{
		var result = await _api.GetWorkoutByIdAsync(_editingWorkoutId!.Value);
		if (!result.Success || result.Data is null)
		{
			ShowError(AppLanguage.NewWorkoutCouldNotLoad);
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
			ShowError(AppLanguage.NewWorkoutNameRequired);
			return;
		}

		ExercisesSection.IsVisible = true;
	}

	private async void OnAddExerciseClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (_selectedExerciseItem is null)
		{
			ShowError(AppLanguage.NewWorkoutChooseFirst);
			return;
		}

		List<SetDetail>? setDetails = null;

		if (_selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
		{
			if (!int.TryParse(SetCountEntry.Text, out int setCount) || setCount <= 0)
			{
				ShowError(AppLanguage.NewWorkoutSetError);
				return;
			}

			int? defaultReps = int.TryParse(RepCountEntry.Text, out int r) && r > 0 ? r : null;
			setDetails = await Xaml.Controls.SetDetailsPopup.ShowAsync(this, setCount, defaultReps);
			if (setDetails is null)
				return;
		}

		ExerciseEntry? entry = BuildExerciseEntryFromInputs(setDetails);
		if (entry is null)
			return;

		_exercises.Add(entry);

		ClearExerciseInputs();
		RefreshExercisesList();
	}

	private async void OnConfirmWorkoutClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (!_session.IsLoggedIn())
		{
			ShowError(AppLanguage.SharedPleaseLogin);
			return;
		}

		if (string.IsNullOrWhiteSpace(WorkoutNameEntry.Text))
		{
			ShowError(AppLanguage.NewWorkoutWorkoutNameRequired);
			return;
		}

		if (_exercises.Count == 0)
		{
			ShowError(AppLanguage.NewWorkoutAddAtLeastOne);
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
				ShowError(result.Error ?? AppLanguage.NewWorkoutFailedUpdate);
				return;
			}
		}
		else
		{
			var result = await _api.CreateWorkoutAsync(workoutData);
			if (!result.Success)
			{
				ShowError(result.Error ?? AppLanguage.NewWorkoutFailedSave);
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
		ExerciseCountLabel.Text = AppLanguage.FormatItemCount(_exercises.Count);
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
				AppLanguage.NewWorkoutChooseExerciseTitle,
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
			SelectedExerciseLabel.Text = AppLanguage.NewWorkoutNoExercise;
			SelectedExerciseHintLabel.Text = AppLanguage.NewWorkoutExerciseHint;
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

	private ExerciseEntry? BuildExerciseEntryFromInputs(List<SetDetail>? setDetails = null)
	{
		if (_selectedExerciseItem is null)
		{
			ShowError(AppLanguage.NewWorkoutChooseFirst);
			return null;
		}

		if (_selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
		{
			if (setDetails is null || setDetails.Count == 0)
			{
				ShowError(AppLanguage.NewWorkoutSetError);
				return null;
			}

			int? restSeconds = null;
			int? rir = null;

			if (!string.IsNullOrWhiteSpace(RirEntry.Text))
			{
				if (!int.TryParse(RirEntry.Text, out int parsedRir) || parsedRir < 0 || parsedRir > 5)
				{
					ShowError(AppLanguage.NewWorkoutRirError);
					return null;
				}

				rir = parsedRir;
			}

			if (!string.IsNullOrWhiteSpace(RestSecondsEntry.Text))
			{
				if (!int.TryParse(RestSecondsEntry.Text, out int parsedRest) || parsedRest <= 0)
				{
					ShowError(AppLanguage.NewWorkoutRestError);
					return null;
				}

				restSeconds = parsedRest;
			}

			double? concentricTime = null;
			if (!string.IsNullOrWhiteSpace(ConcentricTimeEntry.Text))
			{
				if (!MetricInput.TryParseFlexibleDouble(ConcentricTimeEntry.Text, out double parsedTime) || parsedTime <= 0)
				{
					ShowError(AppLanguage.NewWorkoutConcentricError);
					return null;
				}

				concentricTime = parsedTime;
			}

			var agg = SetDetailsAggregator.Aggregate(setDetails);

			return new ExerciseEntry
			{
				ExerciseName = _selectedExerciseItem.Name,
				ExerciseCategory = _selectedExerciseItem.Category,
				TrackingMode = nameof(ExerciseTrackingMode.Strength),
				Sets = agg.Sets,
				Reps = agg.Reps,
				RIR = rir,
				RestSeconds = restSeconds,
				ConcentricTimeSeconds = concentricTime,
				Metric1Value = agg.MaxWeight,
				Metric1Unit = agg.MaxWeight is null ? string.Empty : "kg"
			};
		}

		if (!MetricInput.TryParseFlexibleDouble(Metric1Entry.Text, out double metric1) || metric1 <= 0)
		{
			ShowError(AppLanguage.FormatMustBePositive(_selectedExerciseItem.PrimaryLabel));
			return null;
		}

		double? metric2 = null;
		if (_selectedExerciseItem.HasSecondaryMetric)
		{
			if (!MetricInput.TryParseFlexibleDouble(Metric2Entry.Text, out double parsedMetric2) || parsedMetric2 <= 0)
			{
				ShowError(AppLanguage.FormatMustBePositive(_selectedExerciseItem.SecondaryLabel));
				return null;
			}

			metric2 = parsedMetric2;
		}

		double? gct = null;
		if (_selectedExerciseItem.SupportsGroundContactTime && !string.IsNullOrWhiteSpace(GroundContactTimeEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(GroundContactTimeEntry.Text, out double parsedGctSeconds) || parsedGctSeconds <= 0)
			{
				ShowError(AppLanguage.NewWorkoutGctError);
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
