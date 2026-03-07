using System.Text;
using System.Text.Json;

namespace GymTracker;

public partial class OneRmPage : ContentPage
{
	private const string SavedPrKey = "saved_pr_entries_v1";
	private readonly List<string> _savedPrEntries = new();

	public OneRmPage()
	{
		InitializeComponent();
		LoadSavedPrEntries();
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

	private void OnSaveExerciseClicked(object? sender, EventArgs e)
	{
		ClearError();

		if (!TryGetInputs(out int weightKg, out int reps, out int rir))
		{
			return;
		}

		string entry = $"{weightKg} x {reps} RIR{rir}";
		_savedPrEntries.Insert(0, entry);
		SavePrEntries();
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

	private void LoadSavedPrEntries()
	{
		string? json = Preferences.Default.Get(SavedPrKey, string.Empty);
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		try
		{
			var items = JsonSerializer.Deserialize<List<string>>(json);
			if (items is not null)
			{
				_savedPrEntries.Clear();
				_savedPrEntries.AddRange(items);
			}
		}
		catch
		{
			_savedPrEntries.Clear();
		}
	}

	private void SavePrEntries()
	{
		string json = JsonSerializer.Serialize(_savedPrEntries);
		Preferences.Default.Set(SavedPrKey, json);
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
