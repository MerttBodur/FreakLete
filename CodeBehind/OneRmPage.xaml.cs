using System.Text;
using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class OneRmPage : ContentPage
{
	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private readonly List<SavedPrItem> _savedPrEntries = new();
	private int? _editingPrEntryId;

	public OneRmPage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private void OnCalculateClicked(object? sender, EventArgs e)
	{
		ResultsLabel.Text = string.Empty;
		ClearError();

		if (!TryGetInputs(out int weightKg, out int reps, out int rir))
		{
			return;
		}

		int estimatedMaxRep = reps + rir;
		double oneRm = CalculateOneRm(weightKg, estimatedMaxRep);
		var output = new StringBuilder();

		for (int rm = 1; rm <= 8; rm++)
		{
			double rmWeight = oneRm / (1 + (rm / 30.0));
			output.AppendLine($"{rm}RM: {Math.Round(rmWeight, 1)} kg");
		}

		ResultsLabel.Text = output.ToString();
	}

	private async void OnSaveExerciseClicked(object? sender, EventArgs e)
	{
		ClearError();

		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			ShowError("Please log in again.");
			return;
		}

		if (!TryGetInputs(out int weightKg, out int reps, out int rir))
		{
			return;
		}

		if (_editingPrEntryId.HasValue)
		{
			PrEntry entry = new()
			{
				Id = _editingPrEntryId.Value,
				UserId = currentUserId.Value,
				ExerciseName = "1RM Saved Entry",
				Weight = weightKg,
				Reps = reps,
				RIR = rir
			};

			await _database.UpdatePrEntryAsync(entry);
			ShowSuccess("Saved PR updated.");
		}
		else
		{
			PrEntry entry = new()
			{
				UserId = currentUserId.Value,
				ExerciseName = "1RM Saved Entry",
				Weight = weightKg,
				Reps = reps,
				RIR = rir
			};

			await _database.SavePrEntryAsync(entry);
			ShowSuccess("Saved PR added.");
		}

		ResetSaveMode();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private bool TryGetInputs(out int weightKg, out int reps, out int rir)
	{
		weightKg = 0;
		reps = 0;
		rir = 0;

		if (!int.TryParse(WeightEntry.Text, out weightKg) ||
			!int.TryParse(RepsEntry.Text, out reps) ||
			!int.TryParse(RirEntry.Text, out rir))
		{
			ShowError("Please enter numbers only.");
			return false;
		}

		if (weightKg < 40 || weightKg > 250)
		{
			ShowError("Weight must be between 40 kg - 250 kg.");
			return false;
		}

		if (reps < 1 || reps > 8)
		{
			ShowError("Reps must be between 1 - 8 rep.");
			return false;
		}

		if (rir < 0 || rir > 5)
		{
			ShowError("RIR must be between 0 - 5.");
			return false;
		}

		return true;
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
			Weight = entry.Weight,
			Reps = entry.Reps,
			Rir = entry.RIR.GetValueOrDefault(),
			Text = $"{entry.Weight} x {entry.Reps} RIR{entry.RIR.GetValueOrDefault()}"
		}));
	}

	private void RefreshSavedPrList()
	{
		SavedPrView.ItemsSource = _savedPrEntries.ToList();
	}

	private async void OnDeleteSavedPrInvoked(object? sender, EventArgs e)
	{
		if (sender is not SwipeItem swipeItem || swipeItem.BindingContext is not SavedPrItem item)
		{
			return;
		}

		bool confirmed = await DisplayAlertAsync("Delete PR", $"Delete '{item.Text}'?", "Delete", "Cancel");
		if (!confirmed)
		{
			return;
		}

		await _database.DeletePrEntryAsync(item.Id);
		if (_editingPrEntryId == item.Id)
		{
			ResetSaveMode();
		}
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
		ShowSuccess("Saved PR deleted.");
	}

	private void OnEditSavedPrInvoked(object? sender, EventArgs e)
	{
		if (sender is not SwipeItem swipeItem || swipeItem.BindingContext is not SavedPrItem item)
		{
			return;
		}

		_editingPrEntryId = item.Id;
		WeightEntry.Text = item.Weight.ToString();
		RepsEntry.Text = item.Reps.ToString();
		RirEntry.Text = item.Rir.ToString();
		SaveExerciseButton.Text = "Update Exercise";
		CancelEditButton.IsVisible = true;
		ErrorLabel.TextColor = Colors.LightGreen;
		ErrorLabel.Text = $"Editing: {item.Text}";
		ErrorLabel.IsVisible = true;
	}

	private void OnCancelEditClicked(object? sender, EventArgs e)
	{
		ResetSaveMode();
		ClearError();
	}

	private void ShowError(string message)
	{
		ErrorLabel.Text = message;
		ErrorLabel.TextColor = Colors.Red;
		ErrorLabel.IsVisible = true;
	}

	private void ShowSuccess(string message)
	{
		ErrorLabel.Text = message;
		ErrorLabel.TextColor = Colors.LightGreen;
		ErrorLabel.IsVisible = true;
	}

	private void ClearError()
	{
		ErrorLabel.Text = string.Empty;
		ErrorLabel.IsVisible = false;
	}

	private void ResetSaveMode()
	{
		_editingPrEntryId = null;
		WeightEntry.Text = string.Empty;
		RepsEntry.Text = string.Empty;
		RirEntry.Text = string.Empty;
		SaveExerciseButton.Text = "Save Exercise";
		CancelEditButton.IsVisible = false;
	}

	private static double CalculateOneRm(int weightKg, int estimatedMaxReps)
	{
		return weightKg * (1 + (estimatedMaxReps / 30.0));
	}

	private sealed class SavedPrItem
	{
		public int Id { get; set; }
		public int Weight { get; set; }
		public int Reps { get; set; }
		public int Rir { get; set; }
		public string Text { get; set; } = string.Empty;
	}
}
