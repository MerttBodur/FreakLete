namespace GymTracker;

public partial class HomePage : ContentPage
{
	private bool _isBackView;

	public HomePage()
	{
		InitializeComponent();
	}

	private async void OnAddNewWorkoutClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(WorkoutPage), false);
	}

	private async void OnOneRmCalculatorClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(OneRmPage), false);
	}

	private void OnRotateModelClicked(object? sender, EventArgs e)
	{
		_isBackView = !_isBackView;
		MuscleModelImage.Source = _isBackView ? "backview.PNG" : "frontview.PNG";
	}
}
