namespace GymTracker;

public partial class WorkoutPage : ContentPage
{
	public WorkoutPage()
	{
		InitializeComponent();
	}

	private async void OnOpenNewWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new NewWorkoutPage(), false);
	}

	private async void OnOpenCalendarPageClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CalendarPage(), false);
	}

	private void OnHeaderCalendarClicked(object? sender, EventArgs e)
	{
		OnOpenCalendarPageClicked(sender, e);
	}
}
