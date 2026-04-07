using FreakLete.Services;

namespace FreakLete;

public partial class DateSelectorPage : ContentPage
{
	private readonly Func<DateTime, Task> _onSelected;
	private int _selectedYear;
	private int _selectedMonth;
	private int _selectedDay;

	private static string[] MonthNames => AppLanguage.MonthAbbreviations;

	public DateSelectorPage(DateTime currentDate, Func<DateTime, Task> onSelected)
	{
		InitializeComponent();
		_onSelected = onSelected;
		_selectedYear = currentDate.Year;
		_selectedMonth = currentDate.Month;
		_selectedDay = currentDate.Day;

		ApplyLanguage();
		BuildYearChips();
		BuildMonthChips();
		BuildDayChips();
		UpdateDateLabel();
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
		ApplyLanguage();
		BuildMonthChips();
		UpdateDateLabel();
	}

	private void ApplyLanguage()
	{
		HeaderView.Title = AppLanguage.DateSelectorTitle;
		BadgeLabel.Text = AppLanguage.DateSelectorBadge;
		YearLabel.Text = AppLanguage.DateSelectorYear;
		MonthLabel.Text = AppLanguage.DateSelectorMonth;
		DayLabel.Text = AppLanguage.DateSelectorDay;
		DoneButton.Text = AppLanguage.DateSelectorDone;
	}

	private void BuildYearChips()
	{
		YearChips.Children.Clear();
		int currentYear = DateTime.Today.Year;
		for (int year = currentYear - 10; year >= 1940; year--)
		{
			int y = year;
			Button btn = CreateChip(year.ToString(), year == _selectedYear);
			btn.Clicked += (_, _) =>
			{
				_selectedYear = y;
				HighlightChips(YearChips, y.ToString());
				ClampDay();
				BuildDayChips();
				UpdateDateLabel();
			};
			YearChips.Children.Add(btn);
		}
	}

	private void BuildMonthChips()
	{
		MonthChips.Children.Clear();
		for (int m = 1; m <= 12; m++)
		{
			int month = m;
			Button btn = CreateChip(MonthNames[m - 1], m == _selectedMonth);
			btn.Clicked += (_, _) =>
			{
				_selectedMonth = month;
				HighlightChips(MonthChips, MonthNames[month - 1]);
				ClampDay();
				BuildDayChips();
				UpdateDateLabel();
			};
			MonthChips.Children.Add(btn);
		}
	}

	private void BuildDayChips()
	{
		DayChips.Children.Clear();
		int daysInMonth = DateTime.DaysInMonth(_selectedYear, _selectedMonth);
		for (int d = 1; d <= daysInMonth; d++)
		{
			int day = d;
			Button btn = CreateChip(d.ToString(), d == _selectedDay);
			btn.WidthRequest = 52;
			btn.Margin = new Thickness(0, 0, 6, 6);
			btn.Clicked += (_, _) =>
			{
				_selectedDay = day;
				HighlightChipsInFlex(DayChips, day.ToString());
				UpdateDateLabel();
			};
			DayChips.Children.Add(btn);
		}
	}

	private static Button CreateChip(string text, bool isSelected)
	{
		return new Button
		{
			Text = text,
			BackgroundColor = isSelected ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#1D1828"),
			TextColor = isSelected ? Colors.White : Color.FromArgb("#B3B2C5"),
			BorderColor = isSelected ? Color.FromArgb("#A78BFA") : Color.FromArgb("#342D46"),
			BorderWidth = 1,
			CornerRadius = 16,
			Padding = new Thickness(14, 8),
			MinimumHeightRequest = 40,
			FontSize = 14,
			FontFamily = "OpenSansSemibold"
		};
	}

	private static void HighlightChips(HorizontalStackLayout layout, string selected)
	{
		foreach (IView child in layout.Children)
		{
			if (child is Button btn)
			{
				bool isSel = btn.Text == selected;
				btn.BackgroundColor = isSel ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#1D1828");
				btn.TextColor = isSel ? Colors.White : Color.FromArgb("#B3B2C5");
				btn.BorderColor = isSel ? Color.FromArgb("#A78BFA") : Color.FromArgb("#342D46");
			}
		}
	}

	private static void HighlightChipsInFlex(FlexLayout layout, string selected)
	{
		foreach (IView child in layout.Children)
		{
			if (child is Button btn)
			{
				bool isSel = btn.Text == selected;
				btn.BackgroundColor = isSel ? Color.FromArgb("#8B5CF6") : Color.FromArgb("#1D1828");
				btn.TextColor = isSel ? Colors.White : Color.FromArgb("#B3B2C5");
				btn.BorderColor = isSel ? Color.FromArgb("#A78BFA") : Color.FromArgb("#342D46");
			}
		}
	}

	private void ClampDay()
	{
		int maxDay = DateTime.DaysInMonth(_selectedYear, _selectedMonth);
		if (_selectedDay > maxDay)
			_selectedDay = maxDay;
	}

	private void UpdateDateLabel()
	{
		SelectedDateLabel.Text = new DateTime(_selectedYear, _selectedMonth, _selectedDay).ToString("dd MMMM yyyy");
	}

	private async void OnDoneClicked(object? sender, EventArgs e)
	{
		await _onSelected(new DateTime(_selectedYear, _selectedMonth, _selectedDay));
		await Navigation.PopAsync(true);
	}

	private async void OnHeaderBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}
}
