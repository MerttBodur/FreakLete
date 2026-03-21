using System.Collections.ObjectModel;
using GymTracker.Data;
using GymTracker.Models;
using GymTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymTracker;

public partial class CalendarPage : ContentPage
{
	private readonly AppDatabase _database;
	private readonly UserSession _session;
	private readonly List<Workout> _allWorkouts = new();
	private DateTime _currentMonth;
	private DateTime _selectedDate;

	public ObservableCollection<CalendarDayCell> CalendarDays { get; } = new();

	public CalendarPage()
	{
		InitializeComponent();
		_database = MauiProgram.Services.GetRequiredService<AppDatabase>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		BindingContext = this;
		_selectedDate = DateTime.Today;
		_currentMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await ReloadWorkoutsAsync();
		BuildCalendar();
	}

	private async Task ReloadWorkoutsAsync()
	{
		_allWorkouts.Clear();
		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			return;
		}

		List<Workout> workouts = await _database.GetWorkoutsByUserAsync(currentUserId.Value);
		_allWorkouts.AddRange(workouts);
	}

	private void OnPreviousMonthClicked(object? sender, EventArgs e)
	{
		_currentMonth = _currentMonth.AddMonths(-1);
		_selectedDate = NormalizeSelectedDateForMonth(_selectedDate, _currentMonth);
		BuildCalendar();
	}

	private void OnNextMonthClicked(object? sender, EventArgs e)
	{
		_currentMonth = _currentMonth.AddMonths(1);
		_selectedDate = NormalizeSelectedDateForMonth(_selectedDate, _currentMonth);
		BuildCalendar();
	}

	private static DateTime NormalizeSelectedDateForMonth(DateTime selected, DateTime monthStart)
	{
		int maxDay = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
		int day = Math.Min(selected.Day, maxDay);
		return new DateTime(monthStart.Year, monthStart.Month, day);
	}

	private void BuildCalendar()
	{
		CalendarDays.Clear();

		MonthLabel.Text = _currentMonth.ToString("MMMM yyyy");
		SelectedDateLabel.Text = $"Selected: {_selectedDate:dd MMM yyyy}";

		int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
		int startOffset = GetMondayBasedOffset(_currentMonth.DayOfWeek);

		for (int i = 0; i < startOffset; i++)
		{
			CalendarDays.Add(CalendarDayCell.Empty());
		}

		for (int day = 1; day <= daysInMonth; day++)
		{
			DateTime date = new(_currentMonth.Year, _currentMonth.Month, day);
			bool hasWorkout = _allWorkouts.Any(x => x.WorkoutDate.Date == date.Date);
			bool isSelected = date.Date == _selectedDate.Date;

			CalendarDays.Add(new CalendarDayCell
			{
				Date = date,
				DayText = day.ToString(),
				IsSelectable = true,
				HasWorkout = hasWorkout,
				CellBackgroundColor = isSelected ? Color.FromArgb("#D62828") : Colors.Transparent,
				DayTextColor = Colors.White,
				MarkerColor = hasWorkout ? Color.FromArgb("#D62828") : Colors.Transparent,
				CellOpacity = 1
			});
		}

		while (CalendarDays.Count % 7 != 0)
		{
			CalendarDays.Add(CalendarDayCell.Empty());
		}

		_ = RefreshWorkoutsForDateAsync(_selectedDate);
	}

	private static int GetMondayBasedOffset(DayOfWeek dayOfWeek)
	{
		int sundayBased = (int)dayOfWeek;
		return (sundayBased + 6) % 7;
	}

	private void OnDayTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border || border.BindingContext is not CalendarDayCell cell)
		{
			return;
		}

		if (!cell.IsSelectable || cell.Date is null)
		{
			return;
		}

		_selectedDate = cell.Date.Value;
		BuildCalendar();
	}

	private async Task RefreshWorkoutsForDateAsync(DateTime selectedDate)
	{
		int? currentUserId = _session.GetCurrentUserId();
		if (!currentUserId.HasValue)
		{
			SavedWorkoutsView.ItemsSource = new List<CalendarWorkoutItem>();
			return;
		}

		List<Workout> workouts = await _database.GetWorkoutsByUserAndDateAsync(currentUserId.Value, selectedDate);
		List<CalendarWorkoutItem> items = new();

		foreach (Workout workout in workouts)
		{
			List<ExerciseEntry> exercises = await _database.GetExercisesByWorkoutIdAsync(workout.Id);
			items.Add(new CalendarWorkoutItem
			{
				WorkoutId = workout.Id,
				WorkoutName = workout.WorkoutName,
				ExercisesText = BuildExerciseText(exercises)
			});
		}

		SavedWorkoutsView.ItemsSource = items;
	}

	private async void OnDeleteWorkoutInvoked(object? sender, EventArgs e)
	{
		if (sender is not SwipeItem swipeItem || swipeItem.BindingContext is not CalendarWorkoutItem item)
		{
			return;
		}

		bool confirmed = await DisplayAlertAsync("Delete Workout", $"Delete '{item.WorkoutName}'?", "Delete", "Cancel");
		if (!confirmed)
		{
			return;
		}

		await _database.DeleteWorkoutAsync(item.WorkoutId);
		await ReloadWorkoutsAsync();
		BuildCalendar();
	}

	private async void OnEditWorkoutInvoked(object? sender, EventArgs e)
	{
		if (sender is not SwipeItem swipeItem || swipeItem.BindingContext is not CalendarWorkoutItem item)
		{
			return;
		}

		await Navigation.PushAsync(new NewWorkoutPage(item.WorkoutId), false);
	}

	private static string BuildExerciseText(List<ExerciseEntry> exercises)
	{
		return string.Join(" | ", exercises.Select(x =>
		{
			string baseText = x.RIR.HasValue
				? $"{x.ExerciseName} ({x.Sets}x{x.Reps}, RIR{x.RIR.Value})"
				: $"{x.ExerciseName} ({x.Sets}x{x.Reps})";

			return x.RestSeconds.HasValue
				? $"{baseText}, Rest {x.RestSeconds.Value}s"
				: baseText;
		}));
	}

	public sealed class CalendarDayCell
	{
		public DateTime? Date { get; set; }
		public string DayText { get; set; } = string.Empty;
		public bool IsSelectable { get; set; }
		public bool HasWorkout { get; set; }
		public Color CellBackgroundColor { get; set; } = Colors.Transparent;
		public Color DayTextColor { get; set; } = Colors.White;
		public Color MarkerColor { get; set; } = Colors.Transparent;
		public double CellOpacity { get; set; } = 1;

		public static CalendarDayCell Empty()
		{
			return new CalendarDayCell
			{
				DayText = string.Empty,
				IsSelectable = false,
				HasWorkout = false,
				CellBackgroundColor = Colors.Transparent,
				DayTextColor = Colors.Transparent,
				MarkerColor = Colors.Transparent,
				CellOpacity = 0.35
			};
		}
	}

	private sealed class CalendarWorkoutItem
	{
		public int WorkoutId { get; set; }
		public string WorkoutName { get; set; } = string.Empty;
		public string ExercisesText { get; set; } = string.Empty;
	}
}
