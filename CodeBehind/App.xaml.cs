using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		AppLanguage.Initialize();

		UserSession session = MauiProgram.Services.GetRequiredService<UserSession>();

		// SecureStorage (Android Keystore) ilk erişimde yavaş olabilir;
		// UI thread'i bloklamadan token cache'ini önceden ısıt.
		_ = session.PreloadTokenAsync();

		MainPage = new NavigationPage(BuildStartPage(session));
	}

	private static Page BuildStartPage(UserSession session)
	{
		if (session.GetCurrentUserId().HasValue)
		{
			// UserId var, kullanıcıyı direkt HomePage'e yönlendir
			// Token eksik/expired ise profile çağrısında 401 alır, o zaman login'e yönlendiririz
			return new HomePage();
		}

		return new LoginPage();
	}
}
