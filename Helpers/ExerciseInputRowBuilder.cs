using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Helpers;

public static class ExerciseInputRowBuilder
{
	public sealed class ExerciseRowData
	{
		public ExerciseEntry Entry { get; init; } = null!;
		public ProgramExerciseResponse TemplateExercise { get; init; } = null!;
		public Entry SetsEntry { get; init; } = null!;
		public Entry RepsEntry { get; init; } = null!;
		public Entry RirEntry { get; init; } = null!;
		public Entry RestEntry { get; init; } = null!;
	}

	public static (View View, ExerciseRowData Data) Build(
		ProgramExerciseResponse templateExercise,
		ExerciseEntry prefilled)
	{
		var card = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 16 },
			BackgroundColor = GetColor("SurfaceRaised", "#1D1828"),
			Stroke = new SolidColorBrush(GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			Padding = new Thickness(16, 14)
		};

		var content = new VerticalStackLayout { Spacing = 10 };

		// Exercise name
		content.Children.Add(new Label
		{
			Text = templateExercise.ExerciseName,
			FontSize = 15,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB")
		});

		// Template hint
		string hint = ProgramExerciseConverter.BuildTemplateHint(templateExercise);
		if (!string.IsNullOrEmpty(hint))
		{
			content.Children.Add(new Label
			{
				Text = hint,
				FontSize = 11,
				FontFamily = "OpenSansRegular",
				TextColor = GetColor("AccentGlow", "#A78BFA")
			});
		}

		// Input grid: Sets | Reps | RIR | Rest
		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star)
			},
			ColumnSpacing = 8,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			RowSpacing = 4
		};

		// Headers
		AddHeader(grid, "Sets", 0);
		AddHeader(grid, "Reps", 1);
		AddHeader(grid, "RIR", 2);
		AddHeader(grid, "Rest(s)", 3);

		// Entries
		var setsEntry = CreateEntry(prefilled.Sets > 0 ? prefilled.Sets.ToString() : "", "0");
		var repsEntry = CreateEntry(prefilled.Reps > 0 ? prefilled.Reps.ToString() : "", "0");
		var rirEntry = CreateEntry(prefilled.RIR?.ToString() ?? "", "—");
		var restEntry = CreateEntry(prefilled.RestSeconds?.ToString() ?? "", "60");

		grid.Children.Add(setsEntry);
		Grid.SetColumn(setsEntry, 0);
		Grid.SetRow(setsEntry, 1);

		grid.Children.Add(repsEntry);
		Grid.SetColumn(repsEntry, 1);
		Grid.SetRow(repsEntry, 1);

		grid.Children.Add(rirEntry);
		Grid.SetColumn(rirEntry, 2);
		Grid.SetRow(rirEntry, 1);

		grid.Children.Add(restEntry);
		Grid.SetColumn(restEntry, 3);
		Grid.SetRow(restEntry, 1);

		content.Children.Add(grid);
		card.Content = content;

		var data = new ExerciseRowData
		{
			Entry = prefilled,
			TemplateExercise = templateExercise,
			SetsEntry = setsEntry,
			RepsEntry = repsEntry,
			RirEntry = rirEntry,
			RestEntry = restEntry
		};

		return (card, data);
	}

	/// <summary>
	/// Reads current values from the input entries back into an ExerciseEntry.
	/// </summary>
	public static ExerciseEntry ReadValues(ExerciseRowData data)
	{
		var entry = data.Entry;
		entry.Sets = int.TryParse(data.SetsEntry.Text, out int s) ? s : 0;
		entry.Reps = int.TryParse(data.RepsEntry.Text, out int r) ? r : 0;
		entry.RIR = int.TryParse(data.RirEntry.Text, out int rir) ? rir : null;
		entry.RestSeconds = int.TryParse(data.RestEntry.Text, out int rest) ? rest : null;
		return entry;
	}

	private static void AddHeader(Grid grid, string text, int column)
	{
		var label = new Label
		{
			Text = text,
			FontSize = 10,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextSecondary", "#B3B2C5"),
			HorizontalTextAlignment = TextAlignment.Center
		};
		grid.Children.Add(label);
		Grid.SetColumn(label, column);
		Grid.SetRow(label, 0);
	}

	private static Entry CreateEntry(string value, string placeholder)
	{
		return new Entry
		{
			Text = value,
			Placeholder = placeholder,
			Keyboard = Keyboard.Numeric,
			FontSize = 14,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB"),
			PlaceholderColor = GetColor("TextMuted", "#6B6780"),
			BackgroundColor = GetColor("Surface", "#13101C"),
			HorizontalTextAlignment = TextAlignment.Center
		};
	}

	private static Color GetColor(string key, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
			return color;
		return Color.FromArgb(fallback);
	}
}
