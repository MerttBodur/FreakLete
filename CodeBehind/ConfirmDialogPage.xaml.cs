using FreakLete.Services;

namespace FreakLete;

public partial class ConfirmDialogPage : ContentPage
{
	private readonly TaskCompletionSource<bool> _resultSource = new();

	public ConfirmDialogPage(string title, string message, string confirmText, string cancelText)
	{
		InitializeComponent();
		ActionBadge.Text = AppLanguage.ConfirmAction;
		TitleLabel.Text = title;
		MessageLabel.Text = message;
		ConfirmButton.Text = confirmText;
		CancelButton.Text = cancelText;
	}

	public static async Task<bool> ShowAsync(
		INavigation navigation,
		string title,
		string message,
		string? confirmText = null,
		string? cancelText = null)
	{
		ConfirmDialogPage page = new(title, message, confirmText ?? AppLanguage.SharedDelete, cancelText ?? AppLanguage.SharedCancel);
		await navigation.PushModalAsync(page, false);
		return await page._resultSource.Task;
	}

	private async void OnCancelClicked(object? sender, EventArgs e)
	{
		await CloseAsync(false);
	}

	private async void OnConfirmClicked(object? sender, EventArgs e)
	{
		await CloseAsync(true);
	}

	private async Task CloseAsync(bool result)
	{
		if (_resultSource.Task.IsCompleted)
		{
			return;
		}

		_resultSource.SetResult(result);
		await Navigation.PopModalAsync(false);
	}
}
