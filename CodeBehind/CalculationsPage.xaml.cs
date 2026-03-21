using System.Text;
using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class CalculationsPage : ContentPage
{
	private static readonly string[] StrengthCategories =
	[
		ExerciseCatalog.Push,
		ExerciseCatalog.Pull,
		ExerciseCatalog.SquatVariation,
		ExerciseCatalog.DeadliftVariation,
		ExerciseCatalog.OlympicLifts
	];

	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private readonly List<SavedPrItem> _savedPrEntries = [];
	private ExerciseCatalogItem? _selectedStrengthExerciseItem;
	private ExerciseCatalogItem? _selectedPrExerciseItem;
	private int? _editingPrEntryId;

	public CalculationsPage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		UpdateOneRmSelectionUI();
		UpdatePrSelectionUI();
		UpdateCalculationTabUI(showOneRm: true);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private void OnOneRmTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(showOneRm: true);
	}

	private void OnRsiTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(showOneRm: false);
	}

	private void UpdateCalculationTabUI(bool showOneRm)
	{
		OneRmSection.IsVisible = showOneRm;
		RsiSection.IsVisible = !showOneRm;

		OneRmTabButton.BackgroundColor = showOneRm ? Color.FromArgb("#7C4DFF") : Color.FromArgb("#161322");
		OneRmTabButton.TextColor = showOneRm ? Colors.White : Color.FromArgb("#C9C3DA");
		RsiTabButton.BackgroundColor = showOneRm ? Color.FromArgb("#161322") : Color.FromArgb("#7C4DFF");
		RsiTabButton.TextColor = showOneRm ? Color.FromArgb("#C9C3DA") : Colors.White;
	}

	private async void OnChooseStrengthExerciseClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				"Choose Strength Exercise",
				StrengthCategories,
				OnStrengthExerciseSelected),
			true);
	}

	private void OnStrengthExerciseSelected(ExerciseCatalogItem item)
	{
		_selectedStrengthExerciseItem = item;
		UpdateOneRmSelectionUI();
	}

	private void UpdateOneRmSelectionUI()
	{
		if (_selectedStrengthExerciseItem is null)
		{
			SelectedStrengthExerciseLabel.Text = "No strength movement selected";
			SelectedStrengthExerciseHintLabel.Text = "Browse weighted movements for the 1RM estimate.";
			return;
		}

		SelectedStrengthExerciseLabel.Text = _selectedStrengthExerciseItem.Name;
		SelectedStrengthExerciseHintLabel.Text = _selectedStrengthExerciseItem.SelectionHintText;
	}

	private void OnCalculateOneRmClicked(object? sender, EventArgs e)
	{
		ResultsLabel.Text = string.Empty;
		ClearLabel(OneRmStatusLabel);

		if (!TryGetOneRmInputs(out int weightKg, out int reps, out int rir, out double? concentricTime))
		{
			return;
		}

		int estimatedMaxRep = reps + rir;
		double oneRm = CalculateOneRm(weightKg, estimatedMaxRep);
		var output = new StringBuilder();

		if (_selectedStrengthExerciseItem is not null)
		{
			output.AppendLine($"Movement: {_selectedStrengthExerciseItem.Name}");
		}

		for (int rm = 1; rm <= 8; rm++)
		{
			double rmWeight = oneRm / (1 + (rm / 30.0));
			output.AppendLine($"{rm}RM: {Math.Round(rmWeight, 1)} kg");
		}

		if (concentricTime.HasValue)
		{
			output.AppendLine();
			output.AppendLine($"Concentric Time: {concentricTime.Value:0.##} s");
		}

		ResultsLabel.Text = output.ToString().TrimEnd();
	}

	private bool TryGetOneRmInputs(out int weightKg, out int reps, out int rir, out double? concentricTime)
	{
		weightKg = 0;
		reps = 0;
		rir = 0;
		concentricTime = null;

		if (!int.TryParse(WeightEntry.Text, out weightKg) ||
			!int.TryParse(RepsEntry.Text, out reps) ||
			!int.TryParse(RirEntry.Text, out rir))
		{
			ShowError(OneRmStatusLabel, "Please enter numbers only.");
			return false;
		}

		if (weightKg < 40 || weightKg > 250)
		{
			ShowError(OneRmStatusLabel, "Weight must be between 40 kg - 250 kg.");
			return false;
		}

		if (reps < 1 || reps > 8)
		{
			ShowError(OneRmStatusLabel, "Reps must be between 1 - 8.");
			return false;
		}

		if (rir < 0 || rir > 5)
		{
			ShowError(OneRmStatusLabel, "RIR must be between 0 - 5.");
			return false;
		}

		if (!string.IsNullOrWhiteSpace(ConcentricTimeEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(ConcentricTimeEntry.Text, out double parsedTime) || parsedTime <= 0)
			{
				ShowError(OneRmStatusLabel, "Concentric time must be a positive number.");
				return false;
			}

			concentricTime = parsedTime;
		}

		return true;
	}

	private async void OnChoosePrExerciseClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				"Choose PR Movement",
				ExerciseCatalog.Categories,
				OnPrExerciseSelected),
			true);
	}

	private void OnPrExerciseSelected(ExerciseCatalogItem item)
	{
		_selectedPrExerciseItem = item;
		UpdatePrSelectionUI();
	}

	private void UpdatePrSelectionUI()
	{
		if (_selectedPrExerciseItem is null)
		{
			SelectedPrExerciseLabel.Text = "No PR movement selected";
			SelectedPrExerciseHintLabel.Text = "Browse gym and athletic movements before saving a PR.";
			PrStrengthInputsSection.IsVisible = false;
			PrCustomInputsSection.IsVisible = false;
			PrMetric2Container.IsVisible = false;
			PrGctContainer.IsVisible = false;
			return;
		}

		SelectedPrExerciseLabel.Text = _selectedPrExerciseItem.Name;
		SelectedPrExerciseHintLabel.Text = _selectedPrExerciseItem.SelectionHintText;

		bool isStrength = _selectedPrExerciseItem.TrackingMode == ExerciseTrackingMode.Strength;
		PrStrengthInputsSection.IsVisible = isStrength;
		PrCustomInputsSection.IsVisible = !isStrength;

		if (!isStrength)
		{
			PrMetric1Label.Text = $"{_selectedPrExerciseItem.PrimaryLabel} ({_selectedPrExerciseItem.PrimaryUnit})";
			PrMetric1Entry.Placeholder = $"Enter {_selectedPrExerciseItem.PrimaryLabel.ToLowerInvariant()}";
			PrMetric2Container.IsVisible = _selectedPrExerciseItem.HasSecondaryMetric;
			PrMetric2Label.Text = $"{_selectedPrExerciseItem.SecondaryLabel} ({_selectedPrExerciseItem.SecondaryUnit})";
			PrMetric2Entry.Placeholder = $"Enter {_selectedPrExerciseItem.SecondaryLabel.ToLowerInvariant()}";
			PrGctContainer.IsVisible = _selectedPrExerciseItem.SupportsGroundContactTime;
		}
		else
		{
			PrMetric2Container.IsVisible = false;
			PrGctContainer.IsVisible = false;
		}
	}

	private async void OnSavePrClicked(object? sender, EventArgs e)
	{
		ClearLabel(PrStatusLabel);

		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			ShowError(PrStatusLabel, "Please log in again.");
			return;
		}

		if (_selectedPrExerciseItem is null)
		{
			ShowError(PrStatusLabel, "Choose a movement before saving.");
			return;
		}

		PrEntry? entry = BuildPrEntry(currentUserId.Value);
		if (entry is null)
		{
			return;
		}

		if (_editingPrEntryId.HasValue)
		{
			entry.Id = _editingPrEntryId.Value;
			await _database.UpdatePrEntryAsync(entry);
			ShowSuccess(PrStatusLabel, "Saved PR updated.");
		}
		else
		{
			await _database.SavePrEntryAsync(entry);
			ShowSuccess(PrStatusLabel, "Saved PR added.");
		}

		ResetPrSaveMode();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private PrEntry? BuildPrEntry(int userId)
	{
		if (_selectedPrExerciseItem is null)
		{
			ShowError(PrStatusLabel, "Choose a movement before saving.");
			return null;
		}

		if (_selectedPrExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
		{
			if (!int.TryParse(PrWeightEntry.Text, out int weightKg) || weightKg <= 0)
			{
				ShowError(PrStatusLabel, "Weight must be a positive number.");
				return null;
			}

			if (!int.TryParse(PrRepsEntry.Text, out int reps) || reps <= 0)
			{
				ShowError(PrStatusLabel, "Reps must be a positive number.");
				return null;
			}

			int? rir = null;
			if (!string.IsNullOrWhiteSpace(PrRirEntry.Text))
			{
				if (!int.TryParse(PrRirEntry.Text, out int parsedRir) || parsedRir < 0 || parsedRir > 5)
				{
					ShowError(PrStatusLabel, "RIR must be between 0 - 5.");
					return null;
				}

				rir = parsedRir;
			}

			double? concentricTime = null;
			if (!string.IsNullOrWhiteSpace(PrConcentricTimeEntry.Text))
			{
				if (!MetricInput.TryParseFlexibleDouble(PrConcentricTimeEntry.Text, out double parsedTime) || parsedTime <= 0)
				{
					ShowError(PrStatusLabel, "Concentric time must be a positive number.");
					return null;
				}

				concentricTime = parsedTime;
			}

			return new PrEntry
			{
				UserId = userId,
				ExerciseName = _selectedPrExerciseItem.Name,
				ExerciseCategory = _selectedPrExerciseItem.Category,
				TrackingMode = nameof(ExerciseTrackingMode.Strength),
				Weight = weightKg,
				Reps = reps,
				RIR = rir,
				ConcentricTimeSeconds = concentricTime
			};
		}

		if (!MetricInput.TryParseFlexibleDouble(PrMetric1Entry.Text, out double metric1) || metric1 <= 0)
		{
			ShowError(PrStatusLabel, $"{_selectedPrExerciseItem.PrimaryLabel} is required.");
			return null;
		}

		double? metric2 = null;
		if (_selectedPrExerciseItem.HasSecondaryMetric)
		{
			if (!MetricInput.TryParseFlexibleDouble(PrMetric2Entry.Text, out double parsedMetric2) || parsedMetric2 <= 0)
			{
				ShowError(PrStatusLabel, $"{_selectedPrExerciseItem.SecondaryLabel} is required.");
				return null;
			}

			metric2 = parsedMetric2;
		}

		double? gct = null;
		if (_selectedPrExerciseItem.SupportsGroundContactTime && !string.IsNullOrWhiteSpace(PrGroundContactTimeEntry.Text))
		{
			if (!MetricInput.TryParseFlexibleDouble(PrGroundContactTimeEntry.Text, out double parsedGctSeconds) || parsedGctSeconds <= 0)
			{
				ShowError(PrStatusLabel, "Ground contact time must be a positive number.");
				return null;
			}

			gct = MetricInput.SecondsToMilliseconds(parsedGctSeconds);
		}

		return new PrEntry
		{
			UserId = userId,
			ExerciseName = _selectedPrExerciseItem.Name,
			ExerciseCategory = _selectedPrExerciseItem.Category,
			TrackingMode = nameof(ExerciseTrackingMode.Custom),
			Metric1Value = metric1,
			Metric1Unit = _selectedPrExerciseItem.PrimaryUnit,
			Metric2Value = metric2,
			Metric2Unit = _selectedPrExerciseItem.SecondaryUnit,
			GroundContactTimeMs = gct
		};
	}

	private async Task LoadSavedPrEntriesAsync()
	{
		_savedPrEntries.Clear();

		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			return;
		}

		List<PrEntry> entries = await _database.GetPrEntriesByUserAsync(currentUserId.Value);
		_savedPrEntries.AddRange(entries.Select(entry => new SavedPrItem
		{
			Id = entry.Id,
			ExerciseName = entry.ExerciseName,
			ExerciseCategory = entry.ExerciseCategory,
			TrackingMode = entry.TrackingMode,
			Weight = entry.Weight,
			Reps = entry.Reps,
			Rir = entry.RIR,
			Metric1Value = entry.Metric1Value,
			Metric1Unit = entry.Metric1Unit,
			Metric2Value = entry.Metric2Value,
			Metric2Unit = entry.Metric2Unit,
			GroundContactTimeMs = entry.GroundContactTimeMs,
			ConcentricTimeSeconds = entry.ConcentricTimeSeconds,
			Text = FormatSavedPr(entry)
		}));
	}

	private void RefreshSavedPrList()
	{
		SavedPrView.ItemsSource = _savedPrEntries.ToList();
	}

	private static string FormatSavedPr(PrEntry entry)
	{
		if (entry.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory(entry.ExerciseName, entry.ExerciseCategory);
			string primaryLabel = item?.PrimaryLabel ?? "Value";
			string text = $"{entry.ExerciseName}: {primaryLabel} {entry.Metric1Value:0.##} {entry.Metric1Unit}";

			if (item?.HasSecondaryMetric == true && entry.Metric2Value.HasValue)
			{
				text += $" | {item.SecondaryLabel} {entry.Metric2Value:0.##} {entry.Metric2Unit}";
			}

			if (entry.GroundContactTimeMs.HasValue)
			{
				text += $" | GCT {MetricInput.FormatSecondsFromMilliseconds(entry.GroundContactTimeMs.Value)}";
			}

			return text;
		}

		string strengthText = entry.RIR.HasValue
			? $"{entry.ExerciseName}: {entry.Weight} x {entry.Reps} RIR{entry.RIR.Value}"
			: $"{entry.ExerciseName}: {entry.Weight} x {entry.Reps}";

		if (entry.ConcentricTimeSeconds.HasValue)
		{
			strengthText += $" | Concentric {entry.ConcentricTimeSeconds.Value:0.##} s";
		}

		return strengthText;
	}

	private async void OnDeleteSavedPrInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<SavedPrItem>(sender) is not SavedPrItem item)
		{
			return;
		}

		bool confirmed = await ConfirmDialogPage.ShowAsync(
			Navigation,
			"Delete PR",
			$"Delete '{item.Text}'?",
			"Delete",
			"Cancel");
		if (!confirmed)
		{
			return;
		}

		await _database.DeletePrEntryAsync(item.Id);
		if (_editingPrEntryId == item.Id)
		{
			ResetPrSaveMode();
		}

		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
		ShowSuccess(PrStatusLabel, "Saved PR deleted.");
	}

	private void OnEditSavedPrInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<SavedPrItem>(sender) is not SavedPrItem item)
		{
			return;
		}

		_editingPrEntryId = item.Id;
		_selectedPrExerciseItem = ExerciseCatalog.GetByNameAndCategory(item.ExerciseName, item.ExerciseCategory);
		UpdatePrSelectionUI();

		if (item.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			PrMetric1Entry.Text = item.Metric1Value?.ToString("0.##") ?? string.Empty;
			PrMetric2Entry.Text = item.Metric2Value?.ToString("0.##") ?? string.Empty;
			PrGroundContactTimeEntry.Text = item.GroundContactTimeMs.HasValue
				? MetricInput.MillisecondsToSeconds(item.GroundContactTimeMs.Value).ToString("0.##")
				: string.Empty;
		}
		else
		{
			PrWeightEntry.Text = item.Weight.ToString();
			PrRepsEntry.Text = item.Reps.ToString();
			PrRirEntry.Text = item.Rir?.ToString() ?? string.Empty;
			PrConcentricTimeEntry.Text = item.ConcentricTimeSeconds?.ToString("0.##") ?? string.Empty;
		}

		SavePrButton.Text = "Update PR";
		CancelPrEditButton.IsVisible = true;
		ShowSuccess(PrStatusLabel, $"Editing: {item.Text}");
	}

	private void OnCancelPrEditClicked(object? sender, EventArgs e)
	{
		ResetPrSaveMode();
		ClearLabel(PrStatusLabel);
	}

	private void ResetPrSaveMode()
	{
		_editingPrEntryId = null;
		_selectedPrExerciseItem = null;
		UpdatePrSelectionUI();
		PrWeightEntry.Text = string.Empty;
		PrRepsEntry.Text = string.Empty;
		PrRirEntry.Text = string.Empty;
		PrConcentricTimeEntry.Text = string.Empty;
		PrMetric1Entry.Text = string.Empty;
		PrMetric2Entry.Text = string.Empty;
		PrGroundContactTimeEntry.Text = string.Empty;
		SavePrButton.Text = "Save PR";
		CancelPrEditButton.IsVisible = false;
	}

	private void OnCalculateRsiClicked(object? sender, EventArgs e)
	{
		ClearLabel(RsiStatusLabel);

		if (!MetricInput.TryParseFlexibleDouble(RsiJumpHeightEntry.Text, out double jumpHeightCm) || jumpHeightCm <= 0)
		{
			ShowError(RsiStatusLabel, "Jump height must be a positive number.");
			return;
		}

		if (!MetricInput.TryParseFlexibleDouble(RsiGroundContactTimeEntry.Text, out double gctSeconds) || gctSeconds <= 0)
		{
			ShowError(RsiStatusLabel, "GCT must be a positive number.");
			return;
		}

		double rsi = (jumpHeightCm / 100.0) / gctSeconds;
		RsiResultLabel.Text = $"RSI: {rsi:0.00} | Height {jumpHeightCm:0.##} cm | GCT {gctSeconds:0.##} s";
	}

	private static double CalculateOneRm(int weightKg, int estimatedMaxReps)
	{
		return weightKg * (1 + (estimatedMaxReps / 30.0));
	}

	private static void ShowError(Label label, string message)
	{
		label.Text = message;
		label.TextColor = Colors.Red;
		label.IsVisible = true;
	}

	private static void ShowSuccess(Label label, string message)
	{
		label.Text = message;
		label.TextColor = Colors.LightGreen;
		label.IsVisible = true;
	}

	private static void ClearLabel(Label label)
	{
		label.Text = string.Empty;
		label.IsVisible = false;
	}

	private static TItem? GetBindingContext<TItem>(object? sender) where TItem : class
	{
		return sender switch
		{
			BindableObject bindable when bindable.BindingContext is TItem item => item,
			_ => null
		};
	}

	private sealed class SavedPrItem
	{
		public int Id { get; set; }
		public string ExerciseName { get; set; } = string.Empty;
		public string ExerciseCategory { get; set; } = string.Empty;
		public string TrackingMode { get; set; } = nameof(ExerciseTrackingMode.Strength);
		public int Weight { get; set; }
		public int Reps { get; set; }
		public int? Rir { get; set; }
		public double? Metric1Value { get; set; }
		public string Metric1Unit { get; set; } = string.Empty;
		public double? Metric2Value { get; set; }
		public string Metric2Unit { get; set; } = string.Empty;
		public double? GroundContactTimeMs { get; set; }
		public double? ConcentricTimeSeconds { get; set; }
		public string Text { get; set; } = string.Empty;
	}
}
