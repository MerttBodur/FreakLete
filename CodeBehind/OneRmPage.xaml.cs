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
	private readonly List<string> _savedPrEntries = new();

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

		PrEntry entry = new()
		{
			UserId = currentUserId.Value,
			ExerciseName = "1RM Saved Entry",
			Weight = weightKg,
			Reps = reps,
			RIR = rir
		};

		await _database.SavePrEntryAsync(entry);
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
		_savedPrEntries.AddRange(entries.Select(entry =>
			$"{entry.Weight} x {entry.Reps} RIR{entry.RIR.GetValueOrDefault()}"));
	}

	private void RefreshSavedPrList()
	{
		SavedPrView.ItemsSource = _savedPrEntries.ToList();
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

	private static double CalculateOneRm(int weightKg, int estimatedMaxReps)
	{
		return weightKg * (1 + (estimatedMaxReps / 30.0));
	}
}
