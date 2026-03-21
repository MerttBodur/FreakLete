using FreakLete.Data;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class StartupPage : ContentPage
{
	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private bool _hasValidatedSession;

	public StartupPage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_hasValidatedSession)
		{
			return;
		}

		_hasValidatedSession = true;
		await RouteAsync();
	}

	private async Task RouteAsync()
	{
		Page nextPage = new LoginPage();
		int? currentUserId = _session.GetCurrentUserId();

		if (currentUserId.HasValue)
		{
			User? user = await _database.GetUserByIdAsync(currentUserId.Value);
			if (user is not null)
			{
				nextPage = new HomePage();
			}
			else
			{
				_session.SignOut();
			}
		}

		Window? window = Application.Current?.Windows.FirstOrDefault();
		if (window is not null)
		{
			window.Page = new NavigationPage(nextPage);
		}
	}
}
