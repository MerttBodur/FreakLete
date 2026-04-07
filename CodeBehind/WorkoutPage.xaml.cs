using System.Collections.ObjectModel;
using System.Globalization;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public class DaysPerWeekFormatConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int days) return AppLanguage.FormatXPerWeek(days);
		return value?.ToString() ?? "";
	}
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}

public partial class WorkoutPage : ContentPage
{
	private readonly ApiClient _api = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ApiClient>();
	private readonly ObservableCollection<TrainingProgramListResponse> _programs = [];
	private TrainingProgramResponse? _activeProgram;
	private List<TrainingProgramListResponse> _allPrograms = [];
	private List<RecommendedProgramInfo> _recommendedPrograms = [];
	private string? _selectedGoalFilter;
	private readonly HashSet<int> _starterTemplateIds = [];

	public WorkoutPage()
	{
		InitializeComponent();
		ProgramsCollectionView.ItemsSource = _programs;
		ApplyLanguage();
	}

	private void ApplyLanguage()
	{
		PageTitleLabel.Text = AppLanguage.WorkoutPageTitle;
		PageSubtitleLabel.Text = AppLanguage.WorkoutPageSubtitle;
		ActiveProgramBadge.Text = AppLanguage.WorkoutActiveProgram;
		HeroStartBtn.Text = AppLanguage.WorkoutStartWorkout;
		EmptyGetStartedBadge.Text = AppLanguage.WorkoutGetStarted;
		EmptyNoActiveLabel.Text = AppLanguage.WorkoutNoActiveProgram;
		EmptyDescLabel.Text = AppLanguage.WorkoutNoActiveDesc;
		EmptyQuickWorkoutBtn.Text = AppLanguage.WorkoutQuickWorkout;
		ThisWeekBadge.Text = AppLanguage.WorkoutThisWeek;
		SessionsSubLabel.Text = AppLanguage.WorkoutSessions;
		ProgramsBadge.Text = AppLanguage.WorkoutPrograms;
		AvailableSubLabel.Text = AppLanguage.WorkoutAvailable;
		RecommendedLabel.Text = AppLanguage.WorkoutRecommended;
		AllProgramsLabel.Text = AppLanguage.WorkoutAllPrograms;
		NoProgramsLabel.Text = AppLanguage.WorkoutNoPrograms;
		QuickAddLabel.Text = AppLanguage.WorkoutQuickAdd;
		CalendarLabel.Text = AppLanguage.WorkoutCalendar;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged() => ApplyLanguage();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;
		await LoadPageDataAsync();
	}

	private async Task LoadPageDataAsync()
	{
		try
		{
			// Load programs and active program in parallel
			var programsTask = _api.GetTrainingProgramsAsync();
			var activeTask = _api.GetActiveProgramAsync();

			// Load weekly workout count in parallel
			var today = DateTime.Now;
			var weekStart = today.AddDays(-(int)today.DayOfWeek);
			var weeklyTasks = Enumerable.Range(0, 7)
				.Select(i => _api.GetWorkoutsByDateAsync(weekStart.AddDays(i)))
				.ToArray();

			await Task.WhenAll(programsTask, activeTask, Task.WhenAll(weeklyTasks));

			// Process program list — merge user programs + starter templates
			var userProgramsResult = programsTask.Result;
			var starterResult = await _api.GetStarterTemplatesAsync();

			_programs.Clear();
			_starterTemplateIds.Clear();
			int userProgramCount = 0;

			if (userProgramsResult.Success && userProgramsResult.Data is not null)
			{
				foreach (var program in userProgramsResult.Data)
				{
					_programs.Add(program);
					userProgramCount++;
				}
			}

			if (starterResult.Success && starterResult.Data is not null)
			{
				var existingNames = new HashSet<string>(
					_programs.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
				foreach (var program in starterResult.Data)
				{
					if (!existingNames.Contains(program.Name))
					{
						_programs.Add(program);
						_starterTemplateIds.Add(program.Id);
					}
				}
			}

			NoProgramsLabel.IsVisible = _programs.Count == 0;
			ProgramCountLabel.Text = userProgramCount.ToString();

			// Process active program
			var activeResult = activeTask.Result;
			if (activeResult.Success && activeResult.Data is not null)
			{
				_activeProgram = activeResult.Data;
				HeroProgramCard.IsVisible = true;
				EmptyHeroCard.IsVisible = false;

				HeroProgramName.Text = _activeProgram.Name;
				HeroProgramGoal.Text = _activeProgram.Goal;
				HeroFrequencyLabel.Text = AppLanguage.FormatXPerWeek(_activeProgram.DaysPerWeek);

				if (_activeProgram.SessionDurationMinutes > 0)
				{
					HeroDurationLabel.Text = AppLanguage.FormatMinutes(_activeProgram.SessionDurationMinutes);
					HeroDurationPill.IsVisible = true;
				}
			}
			else
			{
				_activeProgram = null;
				HeroProgramCard.IsVisible = false;
				EmptyHeroCard.IsVisible = true;
			}

			// Process weekly count
			int workoutsThisWeek = weeklyTasks
				.Select(t => t.Result)
				.Where(r => r.Success && r.Data is not null)
				.Sum(r => r.Data!.Count);

			SessionsCountLabel.Text = workoutsThisWeek.ToString();
			HeroWeeklyLabel.Text = AppLanguage.FormatThisWeek(workoutsThisWeek);

			// Build recommendations: exclude active program, take up to 4
			_allPrograms = _programs.ToList();

			var candidates = _allPrograms
				.Where(p => _activeProgram is null || p.Id != _activeProgram.Id)
				.Take(5)
				.ToList();

			// Load full details for recommended programs in parallel
			if (candidates.Count > 0)
			{
				var detailTasks = candidates.Select(c =>
					_starterTemplateIds.Contains(c.Id)
						? _api.GetStarterTemplateByIdAsync(c.Id)
						: _api.GetProgramByIdAsync(c.Id)
				).ToArray();
				await Task.WhenAll(detailTasks);

				_recommendedPrograms = candidates.Select((c, i) =>
				{
					var detail = detailTasks[i].Result;
					if (!detail.Success || detail.Data is null)
						return null;

					var full = detail.Data;
					string? formatPill = null;
					if (full.Weeks is not null)
					{
						formatPill = full.Weeks
							.SelectMany(w => w.Sessions)
							.SelectMany(s => s.Exercises)
							.Select(e => e.RepsOrDuration)
							.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
					}

					return new RecommendedProgramInfo
					{
						Id = c.Id,
						Name = c.Name,
						Goal = c.Goal,
						Status = c.Status,
						DaysPerWeek = c.DaysPerWeek,
						SessionDurationMinutes = full.SessionDurationMinutes,
						FormatPill = formatPill
					};
				}).Where(r => r is not null).ToList()!;
			}
			else
			{
				_recommendedPrograms = [];
			}

			// Build goal chips if 2+ distinct goals exist
			BuildGoalChips();
			ApplyGoalFilter();
		}
		catch
		{
			NoProgramsLabel.IsVisible = true;
			SessionsCountLabel.Text = "0";
			ProgramCountLabel.Text = "0";
			HeroProgramCard.IsVisible = false;
			EmptyHeroCard.IsVisible = true;
		}
	}

	private void BuildGoalChips()
	{
		var distinctGoals = _allPrograms
			.Select(p => p.Goal)
			.Where(g => !string.IsNullOrWhiteSpace(g))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		GoalChipsContainer.Children.Clear();

		if (distinctGoals.Count < 2)
		{
			GoalChipsScroll.IsVisible = false;
			_selectedGoalFilter = null;
			return;
		}

		GoalChipsScroll.IsVisible = true;

		// "All" chip
		GoalChipsContainer.Children.Add(CreateGoalChip(AppLanguage.SharedAll, _selectedGoalFilter is null));

		foreach (var goal in distinctGoals)
		{
			GoalChipsContainer.Children.Add(CreateGoalChip(goal, string.Equals(_selectedGoalFilter, goal, StringComparison.OrdinalIgnoreCase)));
		}
	}

	private Border CreateGoalChip(string text, bool isActive)
	{
		var chip = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 14 },
			BackgroundColor = isActive ? GetColor("AccentSoft", "#2F2346") : GetColor("SurfaceRaised", "#1D1828"),
			Stroke = new SolidColorBrush(isActive ? GetColor("Accent", "#8B5CF6") : GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			Padding = new Thickness(14, 7)
		};

		chip.Content = new Label
		{
			Text = text,
			FontSize = 12,
			FontFamily = "OpenSansSemibold",
			TextColor = isActive ? GetColor("AccentGlow", "#A78BFA") : GetColor("TextSecondary", "#B3B2C5")
		};

		chip.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(() =>
			{
				_selectedGoalFilter = text == AppLanguage.SharedAll ? null : text;
				BuildGoalChips();
				ApplyGoalFilter();
			})
		});

		return chip;
	}

	private void ApplyGoalFilter()
	{
		// Filter recommended cards
		var filteredRecs = _selectedGoalFilter is null
			? _recommendedPrograms
			: _recommendedPrograms.Where(r =>
				string.Equals(r.Goal, _selectedGoalFilter, StringComparison.OrdinalIgnoreCase)).ToList();

		RecommendedCardsContainer.Children.Clear();
		RecommendedSection.IsVisible = filteredRecs.Count > 0;

		foreach (var rec in filteredRecs)
		{
			RecommendedCardsContainer.Children.Add(CreateRecommendedCard(rec));
		}

		// Filter all programs
		var filteredPrograms = _selectedGoalFilter is null
			? _allPrograms
			: _allPrograms.Where(p =>
				string.Equals(p.Goal, _selectedGoalFilter, StringComparison.OrdinalIgnoreCase)).ToList();

		_programs.Clear();
		foreach (var p in filteredPrograms)
			_programs.Add(p);

		NoProgramsLabel.IsVisible = _programs.Count == 0;
	}

	private View CreateRecommendedCard(RecommendedProgramInfo rec)
	{
		var card = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 20 },
			Stroke = new SolidColorBrush(GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			BackgroundColor = GetColor("SurfaceRaised", "#1D1828"),
			Padding = new Thickness(20, 18)
		};

		var content = new VerticalStackLayout { Spacing = 10 };

		// Title + badge row
		var titleRow = new HorizontalStackLayout { Spacing = 10 };
		titleRow.Children.Add(new Label
		{
			Text = rec.Name,
			FontSize = 17,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB"),
			LineBreakMode = LineBreakMode.TailTruncation,
			HorizontalOptions = LayoutOptions.FillAndExpand
		});

		var badgeText = !string.IsNullOrWhiteSpace(rec.Goal) ? rec.Goal : rec.Status;
		if (!string.IsNullOrWhiteSpace(badgeText))
		{
			var badge = new Border
			{
				StrokeShape = new RoundRectangle { CornerRadius = 10 },
				BackgroundColor = GetColor("AccentSoft", "#2F2346"),
				Stroke = new SolidColorBrush(Colors.Transparent),
				Padding = new Thickness(10, 4),
				VerticalOptions = LayoutOptions.Center
			};
			badge.Content = new Label
			{
				Text = badgeText,
				FontSize = 10,
				FontFamily = "OpenSansSemibold",
				TextColor = GetColor("AccentGlow", "#A78BFA")
			};
			titleRow.Children.Add(badge);
		}

		content.Children.Add(titleRow);

		// Pills row
		var pills = new HorizontalStackLayout { Spacing = 8 };

		pills.Children.Add(CreateSmallPill(AppLanguage.FormatXPerWeek(rec.DaysPerWeek)));

		if (rec.SessionDurationMinutes > 0)
			pills.Children.Add(CreateSmallPill(AppLanguage.FormatMinutes(rec.SessionDurationMinutes)));

		if (!string.IsNullOrWhiteSpace(rec.FormatPill))
			pills.Children.Add(CreateSmallPill(rec.FormatPill));

		content.Children.Add(pills);

		card.Content = content;

		card.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(async () =>
			{
				await Navigation.PushAsync(new ProgramDetailPage(rec.Id, _starterTemplateIds.Contains(rec.Id)), true);
			})
		});

		return card;
	}

	private Border CreateSmallPill(string text)
	{
		var pill = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = GetColor("SurfaceRaised", "#1D1828"),
			Stroke = new SolidColorBrush(GetColor("SurfaceBorder", "#342D46")),
			Padding = new Thickness(8, 4),
			VerticalOptions = LayoutOptions.Center
		};
		pill.Content = new Label
		{
			Text = text,
			FontSize = 11,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextSecondary", "#B3B2C5")
		};
		return pill;
	}

	private static Color GetColor(string key, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
			return color;
		return Color.FromArgb(fallback);
	}

	private sealed class RecommendedProgramInfo
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public string Goal { get; set; } = "";
		public string Status { get; set; } = "";
		public int DaysPerWeek { get; set; }
		public int SessionDurationMinutes { get; set; }
		public string? FormatPill { get; set; }
	}

	private async void OnHeroStartWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new NewWorkoutPage(), true);
	}

	private async void OnHeroCardTapped(object? sender, TappedEventArgs e)
	{
		if (_activeProgram is not null)
			await Navigation.PushAsync(new ProgramDetailPage(_activeProgram.Id), true);
	}

	private async void OnProgramCardTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not VisualElement element) return;

		await element.ScaleToAsync(0.97, 80, Easing.CubicOut);
		await element.ScaleToAsync(1, 100, Easing.CubicIn);

		if (element.BindingContext is not TrainingProgramListResponse program) return;

		await Navigation.PushAsync(new ProgramDetailPage(program.Id, _starterTemplateIds.Contains(program.Id)), true);
	}

	private async void OnOpenNewWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new NewWorkoutPage(), true);
	}

	private async void OnOpenCalendarPageClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CalendarPage(), true);
	}

	private async void OnGoToFreakAiClicked(object? sender, EventArgs e)
	{
		await TabNavigationHelper.SwitchToTabAsync(() => new FreakAiPage());
	}
}
