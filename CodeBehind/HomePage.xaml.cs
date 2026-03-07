namespace GymTracker;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

	private async void OnAddNewWorkoutClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(NewWorkoutPage));
	}

	private async void OnOneRmCalculatorClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(OneRmPage));
	}
}
