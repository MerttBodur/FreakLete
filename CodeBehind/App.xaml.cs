using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		AppLanguage.Initialize();
		MainPage = new NavigationPage(BuildStartPage());
	}

	private static Page BuildStartPage()
	{
		UserSession session = MauiProgram.Services.GetRequiredService<UserSession>();

		if (session.IsLoggedIn())
		{
			// Token var, kullanıcıyı direkt HomePage'e yönlendir
			// Token expired ise profile çağrısında 401 alır, o zaman login'e yönlendiririz
			return new HomePage();
		}

		return new LoginPage();
	}
}
