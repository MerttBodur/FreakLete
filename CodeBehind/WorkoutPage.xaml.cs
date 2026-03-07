namespace GymTracker;

public partial class WorkoutPage : ContentPage
{
	public WorkoutPage()
	{
		InitializeComponent();
	}

	private async void OnOpenNewWorkoutClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(NewWorkoutPage), false);
	}

	private async void OnOpenCalendarPageClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(CalendarPage), false);
	}
}
