namespace GymTracker;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//HomePage", false);
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await DisplayAlertAsync("Info", "Register page is not ready yet.", "OK");
	}
}
