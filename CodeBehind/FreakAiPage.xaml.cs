using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class FreakAiPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly List<FreakAiChatMessage> _history = [];
	private bool _isSending;

	public FreakAiPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadActiveProgramCard();
	}

	private async Task LoadActiveProgramCard()
	{
		try
		{
			var result = await _api.GetActiveProgramAsync();
			if (result.Success && result.Data is not null)
			{
				ActiveProgramCard.IsVisible = true;
				ActiveProgramName.Text = result.Data.Name;
				ActiveProgramDetails.Text = $"{result.Data.Goal} · {result.Data.DaysPerWeek} days/week · {result.Data.Weeks.Count} weeks";
			}
			else
			{
				ActiveProgramCard.IsVisible = false;
			}
		}
		catch
		{
			ActiveProgramCard.IsVisible = false;
		}
	}

	private void OnGenerateProgramClicked(object? sender, EventArgs e)
	{
		SendQuickMessage("Write me a personalized training program based on my profile, goals, equipment, and current performance data.");
	}

	private void OnViewProgramClicked(object? sender, EventArgs e)
	{
		SendQuickMessage("Show me my current active training program in detail.");
	}

	private void OnAnalyzeTrainingClicked(object? sender, EventArgs e)
	{
		SendQuickMessage("Analyze my recent training data. What are my strengths, weaknesses, and what should I focus on next?");
	}

	private void OnNutritionHelpClicked(object? sender, EventArgs e)
	{
		SendQuickMessage("Based on my profile, goals, and training load, give me personalized nutrition guidance.");
	}

	private void SendQuickMessage(string message)
	{
		if (_isSending) return;
		MessageEditor.Text = message;
		OnSendClicked(null, EventArgs.Empty);
	}

	private async void OnSendClicked(object? sender, EventArgs e)
	{
		if (_isSending) return;

		string message = MessageEditor.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(message)) return;

		_isSending = true;
		MessageEditor.Text = string.Empty;
		SendButton.IsEnabled = false;

		// Hide welcome on first message
		if (_history.Count == 0)
			WelcomeBorder.IsVisible = false;

		AddChatBubble(message, isUser: true);
		_history.Add(new FreakAiChatMessage { Role = "user", Content = message });

		LoadingIndicator.IsVisible = true;
		await ScrollToBottom();

		try
		{
			var result = await _api.FreakAiChatAsync(message, _history.Count > 1 ? _history.SkipLast(1).ToList() : null);

			LoadingIndicator.IsVisible = false;

			if (result.Success && result.Data is not null)
			{
				string reply = result.Data.Reply;
				AddChatBubble(reply, isUser: false);
				_history.Add(new FreakAiChatMessage { Role = "assistant", Content = reply });

				// Refresh program card if AI might have created/modified a program
				if (reply.Contains("program", StringComparison.OrdinalIgnoreCase) &&
					(reply.Contains("created", StringComparison.OrdinalIgnoreCase) ||
					 reply.Contains("adjusted", StringComparison.OrdinalIgnoreCase) ||
					 reply.Contains("oluşturuldu", StringComparison.OrdinalIgnoreCase) ||
					 reply.Contains("active", StringComparison.OrdinalIgnoreCase)))
				{
					await LoadActiveProgramCard();
				}
			}
			else
			{
				string error = result.Error ?? "Failed to get response.";
				AddChatBubble($"Error: {error}", isUser: false);
			}
		}
		catch (Exception ex)
		{
			LoadingIndicator.IsVisible = false;
			AddChatBubble($"Connection error: {ex.Message}", isUser: false);
		}
		finally
		{
			_isSending = false;
			SendButton.IsEnabled = true;
			await ScrollToBottom();
		}
	}

	private void AddChatBubble(string text, bool isUser)
	{
		var bubble = new Border
		{
			BackgroundColor = isUser
				? Color.FromArgb("#2A2545")
				: Color.FromArgb("#1A1A2E"),
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
			Stroke = Color.FromArgb("#333355"),
			StrokeThickness = 1,
			Padding = new Thickness(14, 10),
			Margin = isUser
				? new Thickness(48, 0, 0, 0)
				: new Thickness(0, 0, 48, 0),
			HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
			Content = new Label
			{
				Text = text,
				FontSize = 14,
				TextColor = Colors.White,
				LineBreakMode = LineBreakMode.WordWrap
			}
		};

		ChatContainer.Children.Add(bubble);
	}

	private async Task ScrollToBottom()
	{
		await Task.Delay(50);
		await ChatScrollView.ScrollToAsync(0, ChatContainer.Height, true);
	}
}
