using System.Text;

namespace GymTracker;

public partial class OneRmPage : ContentPage
{
	public OneRmPage()
	{
		InitializeComponent();
	}

	private void OnCalculateClicked(object? sender, EventArgs e)
	{
		ResultsLabel.Text = string.Empty;
		ClearError();

		if (!int.TryParse(WeightEntry.Text, out int weightKg) ||
			!int.TryParse(RepsEntry.Text, out int reps))
		{
			ShowError("Please enter numbers only.");
			return;
		}

		if (weightKg < 40 || weightKg > 250)
		{
			ShowError("Weight must be between 40 kg - 250 kg.");
			return;
		}

		if (reps < 1 || reps > 8)
		{
			ShowError("Reps must be between 1 - 8 rep.");
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

	private static double CalculateOneRm(int weightKg, int reps)
	{
		return weightKg * (1 + (reps / 30.0));
	}
}
