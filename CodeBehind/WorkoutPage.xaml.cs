using System.Collections.ObjectModel;
using FreakLete.Services;

namespace FreakLete;

public partial class WorkoutPage : ContentPage
{
	private readonly ApiClient _api = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ApiClient>();
	private ObservableCollection<TrainingProgramListResponse> _templates = [];

	public WorkoutPage()
	{
		InitializeComponent();
		TemplatesCollectionView.ItemsSource = _templates;
		TemplatesCollectionView.SelectionChangedCommand = new Command<object>(OnTemplateSelected);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadPageDataAsync();
	}

	private async Task LoadPageDataAsync()
	{
		try
		{
			// Load training program templates
			var templatesResult = await _api.GetTrainingProgramsAsync();
			if (templatesResult.Success && templatesResult.Data is not null)
			{
				_templates.Clear();
				foreach (var template in templatesResult.Data)
				{
					_templates.Add(template);
				}
				NoTemplatesLabel.IsVisible = _templates.Count == 0;
			}
			else
			{
				NoTemplatesLabel.IsVisible = true;
			}

			// Load this week's workouts for stat
			var today = DateTime.Now;
			var weekStart = today.AddDays(-(int)today.DayOfWeek);
			var weekEnd = weekStart.AddDays(6);

			int workoutsThisWeek = 0;
			for (int i = 0; i < 7; i++)
			{
				var date = weekStart.AddDays(i);
				var workoutResult = await _api.GetWorkoutsByDateAsync(date);
				if (workoutResult.Success && workoutResult.Data is not null)
				{
					workoutsThisWeek += workoutResult.Data.Count;
				}
			}

			SessionsCountLabel.Text = workoutsThisWeek.ToString();
		}
		catch
		{
			NoTemplatesLabel.IsVisible = true;
		}
	}

	private async void OnOpenNewWorkoutClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new NewWorkoutPage(), true);
	}

	private async void OnOpenCalendarPageClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CalendarPage(), true);
	}

	private void OnHeaderCalendarClicked(object? sender, EventArgs e)
	{
		OnOpenCalendarPageClicked(sender, e);
	}

	private async void OnTemplateSelected(object? selectedItem)
	{
		if (selectedItem is not TrainingProgramListResponse template)
			return;

		// Show confirmation dialog with template details
		var daysText = template.DaysPerWeek == 1 ? "day" : "days";
		var message = $"Start a new workout using the {template.Name} program?\n\nGoal: {template.Goal}\nFrequency: {template.DaysPerWeek} {daysText}/week";

		var confirmed = await ConfirmDialogPage.ShowAsync(
			Navigation,
			title: "Create Workout from Template",
			message: message,
			confirmText: "Start Workout",
			cancelText: "Cancel");

		if (confirmed)
		{
			// Navigate to new workout page
			await Navigation.PushAsync(new NewWorkoutPage(), true);
		}

		// Clear selection so same template can be selected again
		TemplatesCollectionView.SelectedItem = null;
	}
}
