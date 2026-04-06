using FreakLete.Helpers;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class SessionPickerPage : ContentPage
{
	private readonly TaskCompletionSource<SessionPickerHelper.SessionOption?> _tcs;
	private readonly List<SessionPickerHelper.SessionOption> _options;

	public SessionPickerPage(
		List<SessionPickerHelper.SessionOption> options,
		TaskCompletionSource<SessionPickerHelper.SessionOption?> tcs)
	{
		InitializeComponent();
		_tcs = tcs;
		_options = options;
		BuildSessionCards();
	}

	private void BuildSessionCards()
	{
		SessionsContainer.Children.Clear();

		foreach (var option in _options)
		{
			SessionsContainer.Children.Add(CreateSessionCard(option));
		}
	}

	private View CreateSessionCard(SessionPickerHelper.SessionOption option)
	{
		var card = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 18 },
			BackgroundColor = GetColor("SurfaceRaised", "#1D1828"),
			Stroke = new SolidColorBrush(GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			Padding = new Thickness(18, 16)
		};

		var content = new VerticalStackLayout { Spacing = 8 };

		// Session display name
		content.Children.Add(new Label
		{
			Text = option.DisplayName,
			FontSize = 16,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB")
		});

		// Focus line if session has focus
		if (!string.IsNullOrWhiteSpace(option.Session.Focus))
		{
			content.Children.Add(new Label
			{
				Text = option.Session.Focus,
				FontSize = 12,
				FontFamily = "OpenSansRegular",
				TextColor = GetColor("TextSecondary", "#B3B2C5")
			});
		}

		// Exercise count + first few exercise names
		var exercises = option.Session.Exercises ?? [];
		if (exercises.Count > 0)
		{
			var pills = new HorizontalStackLayout { Spacing = 8 };

			pills.Children.Add(CreatePill($"{exercises.Count} exercises"));

			// Show first 2 exercise names
			var preview = exercises.OrderBy(e => e.Order).Take(2).Select(e => e.ExerciseName);
			pills.Children.Add(CreatePill(string.Join(", ", preview)));

			content.Children.Add(pills);
		}

		card.Content = content;

		card.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(async () =>
			{
				await Navigation.PopAsync(true);
				_tcs.TrySetResult(option);
			})
		});

		return card;
	}

	private Border CreatePill(string text)
	{
		var pill = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = GetColor("AccentSoft", "#2F2346"),
			Stroke = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(10, 4),
			VerticalOptions = LayoutOptions.Center
		};
		pill.Content = new Label
		{
			Text = text,
			FontSize = 11,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("AccentGlow", "#A78BFA")
		};
		return pill;
	}

	private async void OnCancelClicked(object? sender, TappedEventArgs e)
	{
		_tcs.TrySetResult(null);
		await Navigation.PopAsync(true);
	}

	protected override bool OnBackButtonPressed()
	{
		_tcs.TrySetResult(null);
		return base.OnBackButtonPressed();
	}

	private static Color GetColor(string key, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
			return color;
		return Color.FromArgb(fallback);
	}
}
