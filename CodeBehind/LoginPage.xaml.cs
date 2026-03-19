namespace GymTracker;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		var window = Application.Current?.Windows.FirstOrDefault();
		if (window is not null)
		{
			window.Page = new NavigationPage(new HomePage());
		}
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await DisplayAlertAsync("Info", "Register page is not ready yet.", "OK");
	}
}
