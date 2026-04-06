using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Helpers;

public static class ExerciseInputRowBuilder
{
	public sealed class SetData
	{
		public int SetNumber { get; init; }
		public Entry RepsEntry { get; init; } = null!;
		public Entry WeightEntry { get; init; } = null!;
		public Entry RirEntry { get; init; } = null!;
		public Label RestLabel { get; init; } = null!;
		public int RestSeconds { get; set; }
		public Border Row { get; init; } = null!;
	}

	public sealed class ExerciseRowData
	{
		public ExerciseEntry Entry { get; init; } = null!;
		public ProgramExerciseResponse TemplateExercise { get; init; } = null!;
		// Legacy single-row entries (AddWorkoutFromProgramPage)
		public Entry? SetsEntry { get; init; }
		public Entry? RepsEntry { get; init; }
		public Entry? RirEntry { get; init; }
		public Entry? RestEntry { get; init; }
		// Per-set live tracking (StartWorkoutSessionPage)
		public List<SetData> SetRows { get; init; } = [];
		public VerticalStackLayout? SetsContainer { get; init; }
	}

	/// <summary>
	/// Legacy build — single row with Sets/Reps/RIR/Rest entries.
	/// Used by AddWorkoutFromProgramPage.
	/// </summary>
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
	/// Live workout build — per-set rows with expand/collapse.
	/// Used by StartWorkoutSessionPage.
	/// </summary>
	public static (View View, ExerciseRowData Data) BuildLive(
		ProgramExerciseResponse templateExercise,
		ExerciseEntry prefilled,
		Action<SetData>? onSetTapped = null)
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

		// Header: exercise name + expand arrow
		var headerGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};

		headerGrid.Children.Add(new Label
		{
			Text = templateExercise.ExerciseName,
			FontSize = 15,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextPrimary", "#F7F7FB"),
			VerticalOptions = LayoutOptions.Center
		});

		var arrowLabel = new Label
		{
			Text = "\u25BC",
			FontSize = 12,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextSecondary", "#B3B2C5"),
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.End
		};
		headerGrid.Children.Add(arrowLabel);
		Grid.SetColumn(arrowLabel, 1);

		content.Children.Add(headerGrid);

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

		// Sets container
		var setsContainer = new VerticalStackLayout { Spacing = 6 };

		// Column headers
		var headersGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(new GridLength(36)),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star)
			},
			ColumnSpacing = 6,
			Padding = new Thickness(4, 0)
		};
		AddLiveHeader(headersGrid, "Set", 0);
		AddLiveHeader(headersGrid, "Tekrar", 1);
		AddLiveHeader(headersGrid, "Kg", 2);
		AddLiveHeader(headersGrid, "RIR", 3);
		AddLiveHeader(headersGrid, "Dinl.", 4);
		setsContainer.Children.Add(headersGrid);

		var setRows = new List<SetData>();
		int setCount = prefilled.Sets > 0 ? prefilled.Sets : 3;
		for (int i = 1; i <= setCount; i++)
		{
			var setData = BuildSetRow(i, prefilled, onSetTapped);
			setRows.Add(setData);
			setsContainer.Children.Add(setData.Row);
		}

		content.Children.Add(setsContainer);

		// Toggle expand/collapse on header tap
		headerGrid.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(() =>
			{
				setsContainer.IsVisible = !setsContainer.IsVisible;
				arrowLabel.Text = setsContainer.IsVisible ? "\u25BC" : "\u25B6";
			})
		});

		card.Content = content;

		var data = new ExerciseRowData
		{
			Entry = prefilled,
			TemplateExercise = templateExercise,
			SetRows = setRows,
			SetsContainer = setsContainer
		};

		return (card, data);
	}

	/// <summary>
	/// Reads current values from the input entries back into an ExerciseEntry.
	/// Handles both legacy (single-row) and live (per-set) modes.
	/// </summary>
	public static ExerciseEntry ReadValues(ExerciseRowData data)
	{
		var entry = data.Entry;

		if (data.SetRows.Count > 0)
		{
			// Live mode — aggregate from per-set data
			entry.Sets = data.SetRows.Count;

			var lastSet = data.SetRows[^1];
			entry.Reps = int.TryParse(lastSet.RepsEntry.Text, out int r) ? r : 0;
			entry.RIR = int.TryParse(lastSet.RirEntry.Text, out int rir) ? rir : null;

			var restValues = data.SetRows
				.Where(s => s.RestSeconds > 0)
				.Select(s => s.RestSeconds)
				.ToList();
			entry.RestSeconds = restValues.Count > 0 ? (int)restValues.Average() : null;

			double maxWeight = 0;
			foreach (var set in data.SetRows)
			{
				if (double.TryParse(set.WeightEntry.Text, out double w) && w > maxWeight)
					maxWeight = w;
			}
			if (maxWeight > 0)
			{
				entry.Metric1Value = maxWeight;
				if (string.IsNullOrEmpty(entry.Metric1Unit))
					entry.Metric1Unit = "kg";
			}
		}
		else
		{
			// Legacy mode
			entry.Sets = int.TryParse(data.SetsEntry?.Text, out int s) ? s : 0;
			entry.Reps = int.TryParse(data.RepsEntry?.Text, out int r) ? r : 0;
			entry.RIR = int.TryParse(data.RirEntry?.Text, out int rir) ? rir : null;
			entry.RestSeconds = int.TryParse(data.RestEntry?.Text, out int rest) ? rest : null;
		}

		return entry;
	}

	private static SetData BuildSetRow(
		int setNumber,
		ExerciseEntry prefilled,
		Action<SetData>? onSetTapped)
	{
		var row = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			BackgroundColor = GetColor("Surface", "#13101C"),
			Stroke = new SolidColorBrush(GetColor("SurfaceBorder", "#342D46")),
			StrokeThickness = 1,
			Padding = new Thickness(4, 6)
		};

		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(new GridLength(36)),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star)
			},
			ColumnSpacing = 6
		};

		// Set number
		var setLabel = new Label
		{
			Text = setNumber.ToString(),
			FontSize = 13,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("AccentGlow", "#A78BFA"),
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalOptions = LayoutOptions.Center
		};
		grid.Children.Add(setLabel);
		Grid.SetColumn(setLabel, 0);

		// Reps
		var repsEntry = CreateEntry(prefilled.Reps > 0 ? prefilled.Reps.ToString() : "", "0");
		grid.Children.Add(repsEntry);
		Grid.SetColumn(repsEntry, 1);

		// Weight
		string weightText = prefilled.Metric1Value is > 0
			? prefilled.Metric1Value.Value.ToString("0.#")
			: "";
		var weightEntry = CreateEntry(weightText, "0");
		grid.Children.Add(weightEntry);
		Grid.SetColumn(weightEntry, 2);

		// RIR
		var rirEntry = CreateEntry(prefilled.RIR?.ToString() ?? "", "\u2014");
		grid.Children.Add(rirEntry);
		Grid.SetColumn(rirEntry, 3);

		// Rest (display-only, filled by timer)
		var restLabel = new Label
		{
			Text = "--",
			FontSize = 13,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextSecondary", "#B3B2C5"),
			HorizontalTextAlignment = TextAlignment.Center,
			VerticalOptions = LayoutOptions.Center
		};
		grid.Children.Add(restLabel);
		Grid.SetColumn(restLabel, 4);

		row.Content = grid;

		var setData = new SetData
		{
			SetNumber = setNumber,
			RepsEntry = repsEntry,
			WeightEntry = weightEntry,
			RirEntry = rirEntry,
			RestLabel = restLabel,
			RestSeconds = 0,
			Row = row
		};

		// Select on row tap
		row.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(() => onSetTapped?.Invoke(setData))
		});

		// Also select when any entry in the row gets focus
		repsEntry.Focused += (_, _) => onSetTapped?.Invoke(setData);
		weightEntry.Focused += (_, _) => onSetTapped?.Invoke(setData);
		rirEntry.Focused += (_, _) => onSetTapped?.Invoke(setData);

		return setData;
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

	private static void AddLiveHeader(Grid grid, string text, int column)
	{
		var label = new Label
		{
			Text = text,
			FontSize = 9,
			FontFamily = "OpenSansSemibold",
			TextColor = GetColor("TextMuted", "#6B6780"),
			HorizontalTextAlignment = TextAlignment.Center
		};
		grid.Children.Add(label);
		Grid.SetColumn(label, column);
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
