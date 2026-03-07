using System.Text;

namespace GymTracker;

public partial class OneRmPage : ContentPage
{
	public OneRmPage()
	{
		InitializeComponent();
	}

	private async void OnCalculateClicked(object? sender, EventArgs e)
	{
		ResultsLabel.Text = string.Empty;

		if (!int.TryParse(WeightEntry.Text, out int weightKg) ||
			!int.TryParse(RepsEntry.Text, out int reps))
		{
			await DisplayAlertAsync("Invalid Input", "Please enter numbers only.", "OK");
			return;
		}

		if (weightKg < 40 || weightKg > 250)
		{
			await DisplayAlertAsync("Out of Range", "Weight must be between 40 kg - 250 kg.", "OK");
			return;
		}

		if (reps < 1 || reps > 8)
		{
			await DisplayAlertAsync("Out of Range", "Reps must be between 1 - 8 rep.", "OK");
			return;
		}

		double oneRm = CalculateOneRm(weightKg, reps);
		var output = new StringBuilder();

		for (int rm = 1; rm <= 8; rm++)
		{
			double rmWeight = oneRm / (1 + (rm / 30.0));
			output.AppendLine($"{rm}RM: {Math.Round(rmWeight, 1)} kg");
		}

		ResultsLabel.Text = output.ToString();
	}

	private static double CalculateOneRm(int weightKg, int reps)
	{
		return weightKg * (1 + (reps / 30.0));
	}
}
