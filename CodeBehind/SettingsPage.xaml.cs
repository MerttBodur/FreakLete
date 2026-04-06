using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class SettingsPage : ContentPage
{
	private readonly string? _userEmail;

	public SettingsPage(string? userEmail = null)
	{
		InitializeComponent();
		_userEmail = userEmail;
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	private async void OnChangePasswordClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PushAsync(new ChangePasswordPage(_userEmail), true);
	}

	private async void OnLanguageClicked(object? sender, TappedEventArgs e)
	{
		await DisplayAlert("Dil", "Dil seçimi bir sonraki güncellemede kullanıma sunulacaktır.", "Tamam");
	}

	private async void OnLeaveReviewClicked(object? sender, TappedEventArgs e)
	{
		await DisplayAlert("Yorum Bırak", "Mağaza yönlendirmesi bir sonraki güncellemede eklenecektir.", "Tamam");
	}

	private async void OnDonateClicked(object? sender, TappedEventArgs e)
	{
		await DisplayAlert("Bağış Yap", "Bağış entegrasyonu bir sonraki güncellemede eklenecektir.", "Tamam");
	}

	private async void OnSubscribeClicked(object? sender, TappedEventArgs e)
	{
		await DisplayAlert("Abone Ol", "Abonelik sistemi bir sonraki güncellemede eklenecektir.", "Tamam");
	}
}
