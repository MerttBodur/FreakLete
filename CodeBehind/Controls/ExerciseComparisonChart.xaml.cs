using Microcharts;
using SkiaSharp;

namespace FreakLete;

public partial class ExerciseComparisonChart : ContentView
{
	public static readonly BindableProperty Exercise1NameProperty =
		BindableProperty.Create(nameof(Exercise1Name), typeof(string), typeof(ExerciseComparisonChart), "Bench Press", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty Exercise2NameProperty =
		BindableProperty.Create(nameof(Exercise2Name), typeof(string), typeof(ExerciseComparisonChart), "Squat", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty Exercise1DataProperty =
		BindableProperty.Create(nameof(Exercise1Data), typeof(List<float>), typeof(ExerciseComparisonChart), null, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty Exercise2DataProperty =
		BindableProperty.Create(nameof(Exercise2Data), typeof(List<float>), typeof(ExerciseComparisonChart), null, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty Exercise1DeltaProperty =
		BindableProperty.Create(nameof(Exercise1Delta), typeof(string), typeof(ExerciseComparisonChart), "-", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty Exercise2DeltaProperty =
		BindableProperty.Create(nameof(Exercise2Delta), typeof(string), typeof(ExerciseComparisonChart), "-", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty DayLabelsProperty =
		BindableProperty.Create(nameof(DayLabels), typeof(List<string>), typeof(ExerciseComparisonChart), null, propertyChanged: OnPropertyChanged);

	public string Exercise1Name
	{
		get => (string)GetValue(Exercise1NameProperty);
		set => SetValue(Exercise1NameProperty, value);
	}

	public string Exercise2Name
	{
		get => (string)GetValue(Exercise2NameProperty);
		set => SetValue(Exercise2NameProperty, value);
	}

	public List<float> Exercise1Data
	{
		get => (List<float>)GetValue(Exercise1DataProperty);
		set => SetValue(Exercise1DataProperty, value);
	}

	public List<float> Exercise2Data
	{
		get => (List<float>)GetValue(Exercise2DataProperty);
		set => SetValue(Exercise2DataProperty, value);
	}

	public string Exercise1Delta
	{
		get => (string)GetValue(Exercise1DeltaProperty);
		set => SetValue(Exercise1DeltaProperty, value);
	}

	public string Exercise2Delta
	{
		get => (string)GetValue(Exercise2DeltaProperty);
		set => SetValue(Exercise2DeltaProperty, value);
	}

	public List<string> DayLabels
	{
		get => (List<string>)GetValue(DayLabelsProperty);
		set => SetValue(DayLabelsProperty, value);
	}

	public event EventHandler? ChangeExercisesClicked;

	public ExerciseComparisonChart()
	{
		InitializeComponent();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is ExerciseComparisonChart chart)
			chart.ApplyState();
	}

	private void OnChangeButtonTapped(object? sender, EventArgs e)
	{
		ChangeExercisesClicked?.Invoke(this, EventArgs.Empty);
	}

	private void ApplyState()
	{
		TitleLabel.Text = $"{Exercise1Name} vs {Exercise2Name}";
		Badge1Label.Text = $"{Exercise1Name} {Exercise1Delta}";
		Badge2Label.Text = $"{Exercise2Name} {Exercise2Delta}";
		Legend1Label.Text = $"{Exercise1Name} (kg)";
		Legend2Label.Text = $"{Exercise2Name} (kg)";

		var data1 = Exercise1Data;
		var data2 = Exercise2Data;
		var labels = DayLabels;

		bool hasData = (data1 != null && data1.Any(v => v > 0)) ||
		               (data2 != null && data2.Any(v => v > 0));

		EmptyStateLabel.IsVisible = !hasData;
		ChartView1.IsVisible = hasData;
		ChartView2.IsVisible = hasData;
		LegendRow.IsVisible = hasData;

		if (!hasData) return;

		// Calculate shared min/max for consistent Y-axis
		float allMax = 0;
		if (data1 != null) allMax = Math.Max(allMax, data1.Max());
		if (data2 != null) allMax = Math.Max(allMax, data2.Max());
		if (allMax == 0) allMax = 1;

		var accentColor = SKColor.Parse("#8B5CF6");
		var successColor = SKColor.Parse("#22C55E");
		var transparentBg = SKColor.Parse("#00000000");

		// Build chart 1 (Exercise 1 - Accent/Purple)
		if (data1 != null && data1.Count > 0)
		{
			var entries1 = new List<ChartEntry>();
			for (int i = 0; i < data1.Count; i++)
			{
				entries1.Add(new ChartEntry(data1[i])
				{
					Color = accentColor,
					Label = labels != null && i < labels.Count ? labels[i] : "",
					ValueLabel = data1[i] > 0 ? data1[i].ToString("0") : ""
				});
			}

			ChartView1.Chart = new LineChart
			{
				Entries = entries1,
				BackgroundColor = transparentBg,
				LineSize = 3,
				PointSize = 6,
				LabelColor = SKColor.Parse("#B3B2C5"),
				LabelTextSize = 24,
				ValueLabelTextSize = 20,
				MinValue = 0,
				MaxValue = allMax,
				LineMode = LineMode.Spline,
				PointMode = PointMode.Circle,
				ValueLabelOrientation = Orientation.Horizontal,
				LabelOrientation = Orientation.Horizontal
			};
		}

		// Build chart 2 (Exercise 2 - Success/Green) - overlay, no labels
		if (data2 != null && data2.Count > 0)
		{
			var entries2 = new List<ChartEntry>();
			for (int i = 0; i < data2.Count; i++)
			{
				entries2.Add(new ChartEntry(data2[i])
				{
					Color = successColor,
					Label = "", // No labels on overlay chart
					ValueLabel = data2[i] > 0 ? data2[i].ToString("0") : ""
				});
			}

			ChartView2.Chart = new LineChart
			{
				Entries = entries2,
				BackgroundColor = transparentBg,
				LineSize = 3,
				PointSize = 6,
				LabelColor = transparentBg,
				ValueLabelTextSize = 20,
				MinValue = 0,
				MaxValue = allMax,
				LineMode = LineMode.Spline,
				PointMode = PointMode.Circle,
				ValueLabelOrientation = Orientation.Horizontal,
				LabelOrientation = Orientation.Horizontal
			};
		}
	}
}
