using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class ProgramDetailPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly int _programId;
	private readonly bool _isStarterTemplate;

	public ProgramDetailPage(int programId, bool isStarterTemplate = false)
	{
		InitializeComponent();
		_programId = programId;
		_isStarterTemplate = isStarterTemplate;
		_api = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ApiClient>();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProgramAsync();
	}

	private async Task LoadProgramAsync()
	{
		var result = _isStarterTemplate
			? await _api.GetStarterTemplateByIdAsync(_programId)
			: await _api.GetProgramByIdAsync(_programId);

		if (!result.Success || result.Data is null)
		{
			await Navigation.PopAsync();
			return;
		}

		var program = result.Data;

		ProgramNameLabel.Text = program.Name;
		ProgramDescriptionLabel.Text = program.Description;
		ProgramDescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(program.Description);

		GoalPillLabel.Text = program.Goal;
		GoalPill.IsVisible = !string.IsNullOrWhiteSpace(program.Goal);

		FrequencyPillLabel.Text = $"{program.DaysPerWeek}x/week";

		if (program.SessionDurationMinutes > 0)
		{
			DurationPillLabel.Text = $"{program.SessionDurationMinutes} min";
			DurationPill.IsVisible = true;
		}

		if (!string.IsNullOrWhiteSpace(program.Status))
		{
			if (_isStarterTemplate)
			{
				StatusBadgeLabel.Text = "TEMPLATE";
				StatusBadge.IsVisible = true;
				var accentSoft = (Application.Current?.Resources["AccentSoft"] as Color) ?? Color.FromArgb("#2F2346");
				StatusBadge.BackgroundColor = accentSoft;
			}
			else
			{
				StatusBadgeLabel.Text = program.Status.ToUpperInvariant();
				StatusBadge.IsVisible = true;

				var accentSoft = (Application.Current?.Resources["AccentSoft"] as Color) ?? Color.FromArgb("#2F2346");
				var successSoft = (Application.Current?.Resources["SuccessSoft"] as Color) ?? Color.FromArgb("#0D2818");
				StatusBadge.BackgroundColor = program.Status == "active" ? successSoft : accentSoft;
			}
		}

		// Stats
		int totalSessions = program.Weeks.Sum(w => w.Sessions.Count);
		int totalExercises = program.Weeks.Sum(w => w.Sessions.Sum(s => s.Exercises.Count));

		WeeksCountLabel.Text = program.Weeks.Count.ToString();
		SessionsCountLabel.Text = totalSessions.ToString();
		ExercisesCountLabel.Text = totalExercises.ToString();

		BuildWeekStructure(program.Weeks);

		// Update CTA for starter templates
		if (_isStarterTemplate)
		{
			StartWorkoutButton.Text = "Bu Template'i Kullan";
		}
	}

	private void BuildWeekStructure(List<ProgramWeekResponse> weeks)
	{
		WeeksContainer.Children.Clear();

		foreach (var week in weeks.OrderBy(w => w.WeekNumber))
		{
			var weekSection = new VerticalStackLayout { Spacing = 12 };

			// Week header
			var weekHeader = new HorizontalStackLayout { Spacing = 10 };
			weekHeader.Children.Add(new Label
			{
				Text = $"Week {week.WeekNumber}",
				FontSize = 20,
				FontFamily = "OpenSansSemibold",
				TextColor = GetColor("TextPrimary", "#F7F7FB"),
				VerticalOptions = LayoutOptions.Center
			});

			if (week.IsDeload)
			{
				weekHeader.Children.Add(CreatePill("DELOAD", "WarningSoft", "Warning"));
			}

			weekSection.Children.Add(weekHeader);

			if (!string.IsNullOrWhiteSpace(week.Focus))
			{
				weekSection.Children.Add(new Label
				{
					Text = week.Focus,
					FontSize = 13,
					FontFamily = "OpenSansRegular",
					TextColor = GetColor("TextSecondary", "#B3B2C5")
				});
			}

			// Sessions
			foreach (var session in week.Sessions.OrderBy(s => s.DayNumber))
			{
				weekSection.Children.Add(CreateSessionCard(session));
			}

			WeeksContainer.Children.Add(weekSection);
		}
	}

	private View CreateSessionCard(ProgramSessionResponse session)
	{
		var card = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 18 },
			BackgroundColor = GetColor("SurfaceRaised", "#1D1828"),
			Stroke = new SolidColorBrush(GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			Padding = new Thickness(18, 16)
		};

		var content = new VerticalStackLayout { Spacing = 10 };

		// Session name
		var sessionTitle = !string.IsNullOrWhiteSpace(session.SessionName)
			? session.SessionName
			: $"Day {session.DayNumber}";

		content.Children.Add(new Label
		{
			Text = sessionTitle,
			FontSize = 16,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB")
		});

		if (!string.IsNullOrWhiteSpace(session.Focus))
		{
			content.Children.Add(new Label
			{
				Text = session.Focus,
				FontSize = 12,
				FontFamily = "OpenSansRegular",
				TextColor = GetColor("TextSecondary", "#B3B2C5")
			});
		}

		// Exercises
		foreach (var exercise in session.Exercises.OrderBy(ex => ex.Order))
		{
			content.Children.Add(CreateExerciseRow(exercise));
		}

		card.Content = content;
		return card;
	}

	private View CreateExerciseRow(ProgramExerciseResponse exercise)
	{
		var row = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			Padding = new Thickness(0, 4)
		};

		var left = new VerticalStackLayout { Spacing = 2 };
		left.Children.Add(new Label
		{
			Text = exercise.ExerciseName,
			FontSize = 14,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB")
		});

		var detail = $"{exercise.Sets} x {exercise.RepsOrDuration}";
		if (!string.IsNullOrWhiteSpace(exercise.Notes))
			detail += $" - {exercise.Notes}";

		left.Children.Add(new Label
		{
			Text = detail,
			FontSize = 12,
			FontFamily = "OpenSansRegular",
			TextColor = GetColor("TextSecondary", "#B3B2C5")
		});

		row.Children.Add(left);
		Grid.SetColumn(left, 0);

		if (!string.IsNullOrWhiteSpace(exercise.IntensityGuidance))
		{
			var intensity = new Label
			{
				Text = exercise.IntensityGuidance,
				FontSize = 11,
				FontFamily = "OpenSansSemibold",
				TextColor = GetColor("AccentGlow", "#A78BFA"),
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End
			};
			row.Children.Add(intensity);
			Grid.SetColumn(intensity, 1);
		}

		return row;
	}

	private Border CreatePill(string text, string bgColorKey, string textColorKey)
	{
		var pill = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = GetColor(bgColorKey, "#2F2346"),
			Stroke = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(10, 4),
			VerticalOptions = LayoutOptions.Center
		};

		pill.Content = new Label
		{
			Text = text,
			FontSize = 10,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor(textColorKey, "#A78BFA")
		};

		return pill;
	}

	private static Color GetColor(string key, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
			return color;
		return Color.FromArgb(fallback);
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		if (_isStarterTemplate)
		{
			StartWorkoutButton.IsEnabled = false;
			StartWorkoutButton.Text = "Kopyalanıyor...";

			var result = await _api.CloneStarterTemplateAsync(_programId);
			if (result.Success && result.Data is not null)
			{
				// Navigate to the cloned program detail (now user-owned)
				var clonedPage = new ProgramDetailPage(result.Data.Id);
				Navigation.InsertPageBefore(clonedPage, this);
				await Navigation.PopAsync(true);
			}
			else
			{
				StartWorkoutButton.IsEnabled = true;
				StartWorkoutButton.Text = "Bu Template'i Kullan";
				await DisplayAlertAsync("Hata", result.Error ?? "Template kopyalanamadı", "Tamam");
			}
			return;
		}

		await Navigation.PushAsync(new NewWorkoutPage(), true);
	}
}
