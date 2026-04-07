using FreakLete.Helpers;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class ProgramDetailPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly int _programId;
	private readonly bool _isStarterTemplate;
	private TrainingProgramResponse? _program;
	private bool _pickingSession;

	public ProgramDetailPage(int programId, bool isStarterTemplate = false)
	{
		InitializeComponent();
		ApplyLanguage();
		_programId = programId;
		_isStarterTemplate = isStarterTemplate;
		_api = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ApiClient>();
	}

	private void ApplyLanguage()
	{
		PageTitleLabel.Text = AppLanguage.ProgramDetailTitle;
		TrainingProgramBadge.Text = AppLanguage.ProgramDetailTrainingProgram;
		WeeksSubLabel.Text = AppLanguage.ProgramDetailWeeks;
		SessionsSubLabel.Text = AppLanguage.ProgramDetailSessions;
		ExercisesSubLabel.Text = AppLanguage.ProgramDetailExercises;
		StartWorkoutButton.Text = AppLanguage.ProgramDetailStart;
		AddWorkoutButton.Text = AppLanguage.ProgramDetailAddWorkout;
		StartLiveWorkoutButton.Text = AppLanguage.ProgramDetailStartWorkout;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;
		if (_pickingSession)
		{
			_pickingSession = false;
			return;
		}
		await LoadProgramAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private async void OnLanguageChanged()
	{
		ApplyLanguage();
		await LoadProgramAsync();
	}

	private async Task LoadProgramAsync()
	{
		var result = _isStarterTemplate
			? await _api.GetStarterTemplateByIdAsync(_programId)
			: await _api.GetProgramByIdAsync(_programId);

		if (!result.Success || result.Data is null)
		{
			await DisplayAlert(AppLanguage.SharedError, result.Error ?? AppLanguage.ProgramDetailLoadError, AppLanguage.SharedOk);
			await Navigation.PopAsync();
			return;
		}

		var program = result.Data;
		_program = program;

		ProgramNameLabel.Text = program.Name;
		ProgramDescriptionLabel.Text = program.Description;
		ProgramDescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(program.Description);

		GoalPillLabel.Text = program.Goal;
		GoalPill.IsVisible = !string.IsNullOrWhiteSpace(program.Goal);

		FrequencyPillLabel.Text = AppLanguage.FormatFrequencyPerWeek(program.DaysPerWeek);

		if (program.SessionDurationMinutes > 0)
		{
			DurationPillLabel.Text = AppLanguage.FormatDurationMinutes(program.SessionDurationMinutes);
			DurationPill.IsVisible = true;
		}

		if (!string.IsNullOrWhiteSpace(program.Status))
		{
			if (_isStarterTemplate)
			{
				StatusBadgeLabel.Text = AppLanguage.ProgramDetailTemplate;
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
		var weeks = program.Weeks ?? [];
		int totalSessions = weeks.Sum(w => (w.Sessions ?? []).Count);
		int totalExercises = weeks.Sum(w => (w.Sessions ?? []).Sum(s => (s.Exercises ?? []).Count));

		WeeksCountLabel.Text = weeks.Count.ToString();
		SessionsCountLabel.Text = totalSessions.ToString();
		ExercisesCountLabel.Text = totalExercises.ToString();

		BuildWeekStructure(weeks);

		// Bottom buttons: always show Add + Start; hide clone-only button
		StartWorkoutButton.IsVisible = false;
		AddWorkoutButton.IsVisible = true;
		StartLiveWorkoutButton.IsVisible = true;
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
				Text = AppLanguage.FormatWeek(week.WeekNumber),
				FontSize = 20,
				FontFamily = "OpenSansSemibold",
				TextColor = GetColor("TextPrimary", "#F7F7FB"),
				VerticalOptions = LayoutOptions.Center
			});

			if (week.IsDeload)
			{
				weekHeader.Children.Add(CreatePill(AppLanguage.ProgramDetailDeload, "WarningSoft", "Warning"));
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
			: AppLanguage.FormatDay(session.DayNumber);

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

	private async void OnAddWorkoutClicked(object? sender, EventArgs e)
	{
		if (_program is null)
		{
			await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProgramDetailDataError, AppLanguage.SharedOk);
			return;
		}

		_pickingSession = true;
		var session = await SessionPickerHelper.PickSessionAsync(this, _program);
		if (session is null)
		{
			_pickingSession = false;
			if (SessionPickerHelper.FlattenSessions(_program).Count == 0)
				await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProgramDetailNoSessions, AppLanguage.SharedOk);
			return;
		}

		await Navigation.PushAsync(
			new AddWorkoutFromProgramPage(_program.Name, session.DisplayName, session.Session), true);
	}

	private async void OnStartWorkoutClicked(object? sender, EventArgs e)
	{
		if (_program is null)
		{
			await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProgramDetailDataError, AppLanguage.SharedOk);
			return;
		}

		_pickingSession = true;
		var session = await SessionPickerHelper.PickSessionAsync(this, _program);
		if (session is null)
		{
			_pickingSession = false;
			if (SessionPickerHelper.FlattenSessions(_program).Count == 0)
				await DisplayAlert(AppLanguage.SharedError, AppLanguage.ProgramDetailNoSessions, AppLanguage.SharedOk);
			return;
		}

		await Navigation.PushAsync(
			new StartWorkoutSessionPage(_program.Name, session.DisplayName, session.Session), true);
	}
}
