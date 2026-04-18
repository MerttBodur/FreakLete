using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class ExerciseDetailPage : ContentPage
{
	private readonly ExerciseCatalogItem _exercise;
	private enum Tab { Instructions, Mistakes, Progression }
	private Tab _activeTab = Tab.Instructions;

	public ExerciseDetailPage(ExerciseCatalogItem exercise)
	{
		InitializeComponent();
		_exercise = exercise;
		BindExercise();
		SetTab(Tab.Instructions);
	}

	private void BindExercise()
	{
		var isTurkish = AppLanguage.IsTurkish;
		var name = isTurkish && !string.IsNullOrWhiteSpace(_exercise.TurkishName)
			? _exercise.TurkishName
			: _exercise.DisplayName;

		ExerciseNameHeader.Text = name;

		// Video
		if (!string.IsNullOrWhiteSpace(_exercise.MediaUrl))
		{
			VideoPlayer.Source = MediaSource.FromUri(_exercise.MediaUrl);
			VideoPlayer.IsVisible = true;
			NoVideoPlaceholder.IsVisible = false;
		}

		// Chips
		AddChips(_exercise.PrimaryMuscles, isPrimary: true);
		AddChips(_exercise.SecondaryMuscles, isPrimary: false);
		if (!string.IsNullOrWhiteSpace(_exercise.Equipment))
			AddChip(_exercise.Equipment, isPrimary: false);
		if (!string.IsNullOrWhiteSpace(_exercise.Mechanic))
			AddChip(_exercise.Mechanic, isPrimary: false);

		// Tab labels
		TabInstructions.Text = isTurkish ? "Nasıl Yapılır" : "How To";
		TabMistakes.Text = isTurkish ? "Sık Hatalar" : "Mistakes";
		TabProgression.Text = "Progression";

		// Tab content
		InstructionsLabel.Text = string.Join("\n\n", _exercise.Instructions);
		MistakesLabel.Text = _exercise.CommonMistakes;
		ProgressionLabel.Text = BuildProgressionText();

		// Hide tabs with no content
		TabMistakes.IsVisible = !string.IsNullOrWhiteSpace(_exercise.CommonMistakes);
		TabProgression.IsVisible = !string.IsNullOrWhiteSpace(_exercise.Progression) || !string.IsNullOrWhiteSpace(_exercise.Regression);
	}

	private string BuildProgressionText()
	{
		var parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(_exercise.Progression))
			parts.Add($"Next: {_exercise.Progression}");
		if (!string.IsNullOrWhiteSpace(_exercise.Regression))
			parts.Add($"Easier: {_exercise.Regression}");
		return string.Join("\n\n", parts);
	}

	private void AddChips(IEnumerable<string> values, bool isPrimary)
	{
		foreach (var v in values.Where(s => !string.IsNullOrWhiteSpace(s)))
			AddChip(v, isPrimary);
	}

	private void AddChip(string text, bool isPrimary)
	{
		var chip = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = isPrimary
				? (Color)Application.Current!.Resources["AccentSoft"]
				: (Color)Application.Current!.Resources["SurfaceRaised"],
			Stroke = isPrimary
				? Colors.Transparent
				: (Color)Application.Current!.Resources["SurfaceBorder"],
			Padding = new Thickness(10, 4),
			Margin = new Thickness(0, 0, 6, 6),
			Content = new Label
			{
				Text = text,
				FontSize = 11,
				FontFamily = "OpenSansSemibold",
				TextColor = isPrimary
					? (Color)Application.Current!.Resources["AccentGlow"]
					: (Color)Application.Current!.Resources["TextMuted"]
			}
		};
		ChipRow.Add(chip);
	}

	private void SetTab(Tab tab)
	{
		_activeTab = tab;

		PanelInstructions.IsVisible = tab == Tab.Instructions;
		PanelMistakes.IsVisible = tab == Tab.Mistakes;
		PanelProgression.IsVisible = tab == Tab.Progression;

		TabInstructions.TextColor = tab == Tab.Instructions
			? (Color)Application.Current!.Resources["Accent"]
			: (Color)Application.Current!.Resources["TextMuted"];
		TabMistakes.TextColor = tab == Tab.Mistakes
			? (Color)Application.Current!.Resources["Accent"]
			: (Color)Application.Current!.Resources["TextMuted"];
		TabProgression.TextColor = tab == Tab.Progression
			? (Color)Application.Current!.Resources["Accent"]
			: (Color)Application.Current!.Resources["TextMuted"];
	}

	private void OnTabInstructionsClicked(object? sender, EventArgs e) => SetTab(Tab.Instructions);
	private void OnTabMistakesClicked(object? sender, EventArgs e) => SetTab(Tab.Mistakes);
	private void OnTabProgressionClicked(object? sender, EventArgs e) => SetTab(Tab.Progression);

	private void OnVideoMediaFailed(object? sender, MediaFailedEventArgs e)
	{
		VideoPlayer.IsVisible = false;
		NoVideoPlaceholder.IsVisible = true;
		VideoErrorLabel.Text = $"Error: {e.ErrorMessage}";
		VideoErrorLabel.IsVisible = true;
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		VideoPlayer.Stop();
		await Navigation.PopAsync(true);
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		VideoPlayer.Stop();
	}
}
