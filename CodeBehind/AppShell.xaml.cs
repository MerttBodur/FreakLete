namespace GymTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(NewWorkoutPage), typeof(NewWorkoutPage));
		Routing.RegisterRoute(nameof(OneRmPage), typeof(OneRmPage));
	}
}
