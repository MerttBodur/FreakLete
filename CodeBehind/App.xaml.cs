using GymTracker.Data;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		UserSession session = MauiProgram.Services.GetRequiredService<UserSession>();

		Page startPage = session.IsLoggedIn()
			? new HomePage()
			: new LoginPage();

		return new Window(new NavigationPage(startPage));
	}
}
