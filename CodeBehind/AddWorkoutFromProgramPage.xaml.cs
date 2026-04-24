using FreakLete.Helpers;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete;

public partial class AddWorkoutFromProgramPage : ContentPage
{
	private readonly ApiClient _api;
	private readonly string _workoutName;
	private readonly ProgramSessionResponse _session;
	private readonly List<ExerciseInputRowBuilder.ExerciseRowData> _rowData = [];

	public AddWorkoutFromProgramPage(string programName, string sessionDisplayName, ProgramSessionResponse session)
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = session;
		_workoutName = $"{programName} - {sessionDisplayName}";
		WorkoutNameLabel.Text = _workoutName;
		WorkoutDatePicker.Date = DateTime.Now.Date;
		ApplyLanguage();
		BuildExerciseRows();
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

	private void OnLanguageChanged() => ApplyLanguage();

	private void ApplyLanguage()
	{
		PageTitleLabel.Text = AppLanguage.AddFromProgramTitle;
		DateLabel.Text = AppLanguage.AddFromProgramDate;
		SaveButton.Text = AppLanguage.AddFromProgramSave;
	}

	private void BuildExerciseRows()
	{
		ExercisesContainer.Children.Clear();
		_rowData.Clear();

		var exercises = _session.Exercises ?? [];
		foreach (var pe in exercises.OrderBy(e => e.Order))
		{
			var prefilled = ProgramExerciseConverter.Convert(pe);
			var (view, data) = ExerciseInputRowBuilder.Build(pe, prefilled);
			_rowData.Add(data);
			ExercisesContainer.Children.Add(view);
		}
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		ErrorLabel.IsVisible = false;

		var exercises = new List<object>();
		foreach (var row in _rowData)
		{
			var entry = ExerciseInputRowBuilder.ReadValues(row);
			if (entry.SetsCount <= 0)
			{
				ErrorLabel.Text = $"{entry.ExerciseName}: {AppLanguage.AddFromProgramSetRequired}";
				ErrorLabel.IsVisible = true;
				return;
			}

			exercises.Add(new
			{
				exerciseName = entry.ExerciseName,
				exerciseCategory = entry.ExerciseCategory,
				trackingMode = entry.TrackingMode,
				setsCount = entry.SetsCount,
				sets = entry.Sets.Select(s => new
				{
					setNumber = s.SetNumber,
					reps = s.Reps,
					weight = s.Weight
				}).ToList(),
				reps = entry.Reps,
				rir = entry.RIR,
				restSeconds = entry.RestSeconds,
				groundContactTimeMs = entry.GroundContactTimeMs,
				concentricTimeSeconds = entry.ConcentricTimeSeconds,
				metric1Value = entry.Metric1Value,
				metric1Unit = entry.Metric1Unit,
				metric2Value = entry.Metric2Value,
				metric2Unit = entry.Metric2Unit
			});
		}

		if (exercises.Count == 0)
		{
			ErrorLabel.Text = AppLanguage.AddFromProgramNeedExercise;
			ErrorLabel.IsVisible = true;
			return;
		}

		SaveButton.IsEnabled = false;
		SaveButton.Text = AppLanguage.AddFromProgramSaving;

		var workoutData = new
		{
			workoutName = _workoutName,
			workoutDate = $"{WorkoutDatePicker.Date:yyyy-MM-dd}",
			exercises
		};

		var result = await _api.CreateWorkoutAsync(workoutData);
		if (result.Success)
		{
			await Navigation.PopAsync(true);
		}
		else
		{
			SaveButton.IsEnabled = true;
			SaveButton.Text = AppLanguage.AddFromProgramSave;
			ErrorLabel.Text = result.Error ?? AppLanguage.AddFromProgramFailed;
			ErrorLabel.IsVisible = true;
		}
	}

	private async void OnBackClicked(object? sender, TappedEventArgs e)
	{
		await Navigation.PopAsync(true);
	}
}
