namespace GymTracker;

public partial class MessageDialogPage : ContentPage
{
	private readonly TaskCompletionSource<bool> _resultSource = new();

	public MessageDialogPage(string badge, string title, string message, string buttonText)
	{
		InitializeComponent();
		BadgeLabel.Text = badge;
		TitleLabel.Text = title;
		MessageLabel.Text = message;
		CloseButton.Text = buttonText;
	}

	public static async Task ShowAsync(
		INavigation navigation,
		string title,
		string message,
		string buttonText = "Continue",
		string badge = "SUCCESS")
	{
		MessageDialogPage page = new(badge, title, message, buttonText);
		await navigation.PushModalAsync(page, false);
		await page._resultSource.Task;
	}

	private async void OnCloseClicked(object? sender, EventArgs e)
	{
		if (_resultSource.Task.IsCompleted)
		{
			return;
		}

		_resultSource.SetResult(true);
		await Navigation.PopModalAsync(false);
	}
}
