namespace FreakLete;

public partial class PhotoActionSheetPage : ContentPage
{
	public enum PhotoAction { Choose, Remove, Cancel }

	private readonly TaskCompletionSource<PhotoAction> _result = new();

	public PhotoActionSheetPage(bool hasPhoto, string title, string chooseText, string removeText, string cancelText)
	{
		InitializeComponent();
		TitleLabel.Text = title.ToUpperInvariant();
		ChooseLabel.Text = chooseText;
		RemoveLabel.Text = removeText;
		CancelLabel.Text = cancelText;
		RemovePhotoRow.IsVisible = hasPhoto;
	}

	public static async Task<PhotoAction> ShowAsync(
		INavigation navigation,
		bool hasPhoto,
		string title,
		string chooseText,
		string removeText,
		string cancelText)
	{
		var page = new PhotoActionSheetPage(hasPhoto, title, chooseText, removeText, cancelText);
		await navigation.PushModalAsync(page, false);
		return await page._result.Task;
	}

	private void OnSheetTapped(object? sender, TappedEventArgs e)
	{
		// Absorb tap so it doesn't bubble to background dismissal
	}

	private async void OnBackgroundTapped(object? sender, TappedEventArgs e)
		=> await CloseAsync(PhotoAction.Cancel);

	private async void OnChoosePhotoTapped(object? sender, TappedEventArgs e)
		=> await CloseAsync(PhotoAction.Choose);

	private async void OnRemovePhotoTapped(object? sender, TappedEventArgs e)
		=> await CloseAsync(PhotoAction.Remove);

	private async void OnCancelTapped(object? sender, TappedEventArgs e)
		=> await CloseAsync(PhotoAction.Cancel);

	private async Task CloseAsync(PhotoAction action)
	{
		if (_result.Task.IsCompleted) return;
		_result.SetResult(action);
		await Navigation.PopModalAsync(false);
	}
}
