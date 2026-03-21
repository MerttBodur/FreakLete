using FreakLete.Data;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		MainPage = new NavigationPage(BuildStartPage());
	}

	private static Page BuildStartPage()
	{
		AppDatabase database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		UserSession session = MauiProgram.Services.GetRequiredService<UserSession>();
		Page startPage = new LoginPage();

		if (session.IsLoggedIn())
		{
			int? currentUserId = session.GetCurrentUserId();
			if (currentUserId.HasValue)
			{
				try
				{
					User? existingUser = database.GetUserByIdAsync(currentUserId.Value).GetAwaiter().GetResult();
					if (existingUser is not null)
					{
						startPage = new HomePage();
					}
					else
					{
						session.SignOut();
					}
				}
				catch
				{
					session.SignOut();
				}
			}
			else
			{
				session.SignOut();
			}
		}

		return startPage;
	}
}
