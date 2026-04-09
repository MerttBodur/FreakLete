using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class FreakAiPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly List<FreakAiChatMessage> _history = [];
	private bool _isSending;
	private CancellationTokenSource? _loadingAnimationCts;
	private BillingStatusResponse? _billingStatus;

	public FreakAiPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		ApplyLanguage();
	}

	/// <summary>
	/// Sets all user-facing strings based on device language.
	/// </summary>
	private void ApplyLanguage()
	{
		// Quick action buttons
		BtnGenerateProgram.Text = AppLanguage.QuickGenerateProgram;
		BtnViewProgram.Text = AppLanguage.QuickViewProgram;
		BtnAnalyzeTraining.Text = AppLanguage.QuickAnalyzeTraining;
		BtnNutritionHelp.Text = AppLanguage.QuickNutritionHelp;

		// Welcome card
		WelcomeTitle.Text = AppLanguage.WelcomeTitle;
		WelcomeBody.Text = AppLanguage.WelcomeBody;
		WelcomeHint.Text = AppLanguage.WelcomeHint;

		// Active program card label
		ActiveProgramLabel.Text = AppLanguage.ActiveProgramLabel;

		// Input placeholder
		MessageEditor.Placeholder = AppLanguage.InputPlaceholder;

		// Usage card labels
		UsageChatLabel.Text = AppLanguage.FreakAiChatRemaining;
		UsageGenerateLabel.Text = AppLanguage.FreakAiGenerateRemaining;
		UsageAnalyzeLabel.Text = AppLanguage.FreakAiAnalyzeRemaining;
		UsageNutritionLabel.Text = AppLanguage.FreakAiNutritionAvailable;
		UsageUpgradeBtn.Text = AppLanguage.FreakAiUpgradeCta;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await Task.WhenAll(LoadActiveProgramCard(), LoadBillingStatusAsync());
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
				ActiveProgramDetails.Text = AppLanguage.FormatProgramDetails(
					result.Data.Goal, result.Data.DaysPerWeek, result.Data.Weeks.Count);
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

	// ── Billing status ──────────────────────────────────────

	private async Task LoadBillingStatusAsync()
	{
		try
		{
			var result = await _api.GetBillingStatusAsync();
			if (result.Success && result.Data is not null)
			{
				_billingStatus = result.Data;
				UpdateUsageCard();
			}
		}
		catch
		{
			// Non-critical; card stays hidden
		}
	}

	private void UpdateUsageCard()
	{
		if (_billingStatus is null) return;

		UsageCard.IsVisible = true;

		if (_billingStatus.IsPremiumActive)
		{
			UsagePlanBadge.Text = AppLanguage.FreakAiPlanPremium;
			UsageUpgradeBtn.IsVisible = false;
			UsageChatValue.Text = AppLanguage.FreakAiUnlimited;
			UsageGenerateValue.Text = AppLanguage.FreakAiUnlimited;
			UsageAnalyzeValue.Text = AppLanguage.FreakAiUnlimited;
			UsageNutritionValue.Text = AppLanguage.FreakAiNutritionReady;
		}
		else
		{
			UsagePlanBadge.Text = AppLanguage.FreakAiPlanFree;
			UsageUpgradeBtn.IsVisible = true;
			UsageChatValue.Text = AppLanguage.FormatRemainingToday(_billingStatus.GeneralChatRemainingToday);
			UsageGenerateValue.Text = AppLanguage.FormatRemainingMonth(_billingStatus.ProgramGenerateRemainingThisMonth);
			UsageAnalyzeValue.Text = AppLanguage.FormatRemainingMonth(_billingStatus.ProgramAnalyzeRemainingThisMonth);
			UsageNutritionValue.Text = _billingStatus.NutritionGuidanceNextAvailableAtUtc is null
				? AppLanguage.FreakAiNutritionReady
				: AppLanguage.FormatNutritionNextAt(_billingStatus.NutritionGuidanceNextAvailableAtUtc.Value);
		}
	}

	private async void OnUsageUpgradeClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new SettingsPage());
	}

	// ── Quick actions (language-aware prompts + intent) ──────

	private void OnGenerateProgramClicked(object? sender, EventArgs e)
		=> SendQuickMessage(AppLanguage.PromptGenerateProgram, "program_generate");

	private void OnViewProgramClicked(object? sender, EventArgs e)
		=> SendQuickMessage(AppLanguage.PromptViewProgram, "program_view");

	private void OnAnalyzeTrainingClicked(object? sender, EventArgs e)
		=> SendQuickMessage(AppLanguage.PromptAnalyzeTraining, "program_analyze");

	private void OnNutritionHelpClicked(object? sender, EventArgs e)
		=> SendQuickMessage(AppLanguage.PromptNutritionHelp, "nutrition_guidance");

	private void SendQuickMessage(string message, string? intent = null)
	{
		if (_isSending) return;
		_pendingIntent = intent;
		MessageEditor.Text = message;
		OnSendClicked(null, EventArgs.Empty);
	}

	private string? _pendingIntent;

	private async void OnSendClicked(object? sender, EventArgs e)
	{
		if (_isSending) return;

		string message = MessageEditor.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(message)) return;

		_isSending = true;
		MessageEditor.Text = string.Empty;
		SendButton.IsEnabled = false;

		// Capture and clear pending intent.
		// Quick actions set _pendingIntent explicitly; free-text leaves it null so the
		// backend (source of truth) classifies the intent from the message content.
		string? intent = _pendingIntent;
		_pendingIntent = null;

		// Hide welcome on first message
		if (_history.Count == 0)
			WelcomeBorder.IsVisible = false;

		AddChatBubble(message, isUser: true);
		_history.Add(new FreakAiChatMessage { Role = "user", Content = message });

		// Determine if this is a heavy operation for better loading UX
		bool isProgramGeneration = IsProgramGenerationRequest(message);

		ShowLoadingIndicator(isProgramGeneration);
		await ScrollToBottom();

		try
		{
			var result = await _api.FreakAiChatAsync(message, _history.Count > 1 ? _history.SkipLast(1).ToList() : null, intent);

			HideLoadingIndicator();

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
					 reply.Contains("oluşturdum", StringComparison.OrdinalIgnoreCase) ||
					 reply.Contains("aktif", StringComparison.OrdinalIgnoreCase) ||
					 reply.Contains("active", StringComparison.OrdinalIgnoreCase)))
				{
					await LoadActiveProgramCard();
				}

				// Refresh usage card after successful request
				_ = LoadBillingStatusAsync();
			}
			else if (result.StatusCode == 429)
			{
				HandleQuotaExhausted(result.Error, result.QuotaResetsAt);
			}
			else
			{
				string error = result.Error ?? AppLanguage.ErrorNoResponse;
				AddChatBubble(error, isUser: false);
			}
		}
		catch
		{
			HideLoadingIndicator();
			AddChatBubble(AppLanguage.ErrorConnectionFailed, isUser: false);
		}
		finally
		{
			_isSending = false;
			SendButton.IsEnabled = true;
			await ScrollToBottom();
		}
	}

	// ── Quota exhausted handling ─────────────────────────────

	private void HandleQuotaExhausted(string? serverMessage, DateTime? resetsAt)
	{
		bool isPremium = _billingStatus?.IsPremiumActive ?? false;

		// Use the server's specific quota message; fall back to generic strings only if absent.
		string message = !string.IsNullOrWhiteSpace(serverMessage)
			? serverMessage
			: isPremium ? AppLanguage.QuotaExhaustedPremium : AppLanguage.QuotaExhaustedFree;

		// Append reset time if the server provided it and it's in the future.
		if (resetsAt.HasValue && resetsAt.Value > DateTime.UtcNow)
			message += $"\n{AppLanguage.FormatQuotaResetsAt(resetsAt.Value)}";

		AddChatBubble(message, isUser: false);

		if (!isPremium)
		{
			AddUpgradeBubble();
		}

		// Refresh usage card
		_ = LoadBillingStatusAsync();
	}

	private void AddUpgradeBubble()
	{
		var btn = new Button
		{
			Text = AppLanguage.QuotaUpgradeButton,
			BackgroundColor = Color.FromArgb("#8B5CF6"),
			TextColor = Colors.White,
			FontFamily = "OpenSansSemibold",
			FontSize = 13,
			CornerRadius = 14,
			Padding = new Thickness(16, 8),
			HorizontalOptions = LayoutOptions.Start
		};
		btn.Clicked += async (_, _) => await Navigation.PushAsync(new SettingsPage());

		var bubble = new Border
		{
			BackgroundColor = Color.FromArgb("#1A1A2E"),
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
			Stroke = Color.FromArgb("#333355"),
			StrokeThickness = 1,
			Padding = new Thickness(14, 10),
			Margin = new Thickness(0, 0, 48, 0),
			HorizontalOptions = LayoutOptions.Start,
			Content = btn
		};

		ChatContainer.Children.Add(bubble);
	}

	// ── Loading indicator with progressive messages ──────────

	private void ShowLoadingIndicator(bool isHeavyOperation)
	{
		LoadingLabel.Text = AppLanguage.LoadingDefault;
		LoadingIndicator.IsVisible = true;

		_loadingAnimationCts?.Cancel();
		_loadingAnimationCts = new CancellationTokenSource();
		var token = _loadingAnimationCts.Token;

		_ = AnimateLoadingText(isHeavyOperation, token);
	}

	private void HideLoadingIndicator()
	{
		_loadingAnimationCts?.Cancel();
		_loadingAnimationCts = null;
		LoadingIndicator.IsVisible = false;
	}

	private async Task AnimateLoadingText(bool isHeavyOperation, CancellationToken ct)
	{
		try
		{
			string[] phases = isHeavyOperation
				? AppLanguage.LoadingPhasesHeavy
				: AppLanguage.LoadingPhasesLight;

			for (int i = 0; i < phases.Length; i++)
			{
				if (ct.IsCancellationRequested) return;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (!ct.IsCancellationRequested)
						LoadingLabel.Text = phases[i];
				});

				int delayMs = isHeavyOperation ? 4000 : 3000;
				await Task.Delay(delayMs, ct);
			}
		}
		catch (TaskCanceledException)
		{
			// Expected when response arrives
		}
	}

	private static bool IsProgramGenerationRequest(string message)
	{
		string lower = message.ToLowerInvariant();
		return lower.Contains("program") || lower.Contains("write me") ||
		       lower.Contains("generate") || lower.Contains("create") ||
		       lower.Contains("yaz") || lower.Contains("oluştur") ||
		       lower.Contains("antrenman programı");
	}

	// ── Chat bubble rendering ───────────────────────────────

	private void AddChatBubble(string text, bool isUser)
	{
		var label = new Label
		{
			Text = "", // Start empty for typing animation
			FontSize = 14,
			TextColor = Colors.White,
			LineBreakMode = LineBreakMode.WordWrap
		};

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
			Content = label
		};

		ChatContainer.Children.Add(bubble);

		// For user messages, set text immediately. For assistant, reveal progressively
		if (isUser)
		{
			label.Text = text;
		}
		else
		{
			// Assistant message: progressive reveal (typing effect)
			_ = RevealTextProgressively(label, text);
		}
	}

	private async Task RevealTextProgressively(Label label, string fullText)
	{
		// Progressive reveal: faster for short messages, slower for longer ones
		// This avoids fake delays for quick responses
		int delayMs = fullText.Length > 200 ? 15 : 0; // Only animate if text is substantial

		if (delayMs == 0)
		{
			// Short response: just set it
			label.Text = fullText;
		}
		else
		{
			// Longer response: reveal character by character
			label.Text = "";
			for (int i = 0; i < fullText.Length; i++)
			{
				label.Text = fullText.Substring(0, i + 1);
				await Task.Delay(delayMs);
			}
		}

		await ScrollToBottom();
	}

	private async Task ScrollToBottom()
	{
		await Task.Delay(50);
		await ChatScrollView.ScrollToAsync(0, ChatContainer.Height, true);
	}
}
