using FreakLete.Services;

namespace FreakLete;

public partial class MessageDialogPage : ContentPage
{
	private readonly TaskCompletionSource<bool> _resultSource = new();
	private readonly bool _usesDefaultBadge;
	private readonly bool _usesDefaultButton;

	public MessageDialogPage(string badge, string title, string message, string buttonText,
		bool usesDefaultBadge = false, bool usesDefaultButton = false)
	{
		InitializeComponent();
		_usesDefaultBadge = usesDefaultBadge;
		_usesDefaultButton = usesDefaultButton;
		BadgeLabel.Text = badge;
		TitleLabel.Text = title;
		MessageLabel.Text = message;
		CloseButton.Text = buttonText;
	}

	public static async Task ShowAsync(
		INavigation navigation,
		string title,
		string message,
		string? buttonText = null,
		string? badge = null)
	{
		MessageDialogPage page = new(
			badge ?? AppLanguage.DialogSuccess,
			title,
			message,
			buttonText ?? AppLanguage.DialogContinue,
			usesDefaultBadge: badge is null,
			usesDefaultButton: buttonText is null);
		await navigation.PushModalAsync(page, false);
		await page._resultSource.Task;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged()
	{
		if (_usesDefaultBadge)
			BadgeLabel.Text = AppLanguage.DialogSuccess;
		if (_usesDefaultButton)
			CloseButton.Text = AppLanguage.DialogContinue;
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
