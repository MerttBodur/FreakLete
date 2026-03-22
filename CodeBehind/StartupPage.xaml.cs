using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class StartupPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly UserSession _session;
	private bool _hasValidatedSession;

	public StartupPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
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

		if (_session.IsLoggedIn())
		{
			var profileResult = await _api.GetProfileAsync();
			if (profileResult.Success && profileResult.Data is not null)
			{
				nextPage = new HomePage();
			}
			else
			{
				_session.SignOut();
			}
		}

		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			await Navigation.PushAsync(nextPage, false);
			Navigation.RemovePage(this);
		});
	}
}
