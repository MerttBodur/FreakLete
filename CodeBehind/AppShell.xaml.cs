namespace GymTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(NewWorkoutPage), typeof(NewWorkoutPage));
		Routing.RegisterRoute(nameof(WorkoutPage), typeof(WorkoutPage));
		Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
		Routing.RegisterRoute(nameof(OneRmPage), typeof(OneRmPage));
	}
}
