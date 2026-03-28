namespace FreakLete;

public partial class LineChartCard : ContentView
{
	public class ChartDataPoint
	{
		public required string Label { get; set; }
		public required double Value { get; set; }
	}

	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(LineChartCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty DataProperty =
		BindableProperty.Create(nameof(Data), typeof(System.Collections.IEnumerable), typeof(LineChartCard), null, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty MaxValueProperty =
		BindableProperty.Create(nameof(MaxValue), typeof(double), typeof(LineChartCard), 100.0, propertyChanged: OnPropertyChanged);

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public System.Collections.IEnumerable Data
	{
		get => (System.Collections.IEnumerable)GetValue(DataProperty);
		set => SetValue(DataProperty, value);
	}

	public double MaxValue
	{
		get => (double)GetValue(MaxValueProperty);
		set => SetValue(MaxValueProperty, value);
	}

	public LineChartCard()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is LineChartCard card)
			card.ApplyState();
	}

	private void ApplyState()
	{
		TitleLabel.Text = Title;
		TitleLabel.IsVisible = !string.IsNullOrEmpty(Title);

		ChartContainer.Children.Clear();

		if (Data == null)
		{
			ShowEmptyState();
			return;
		}

		var points = new List<ChartDataPoint>();
		foreach (var item in Data)
		{
			if (item is ChartDataPoint point)
				points.Add(point);
		}

		if (points.Count == 0)
		{
			ShowEmptyState();
			return;
		}

		// Determine max value for scaling
		var maxVal = points.Max(p => p.Value);
		if (maxVal == 0) maxVal = 100;
		if (MaxValue > 0) maxVal = MaxValue;

		// Create horizontal bar chart
		var chartLayout = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(8, 0) };

		foreach (var point in points.OrderBy(p => p.Label))
		{
			var rowLayout = new VerticalStackLayout { Spacing = 2 };

			var labelLayout = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 0, 0, 2) };
			var label = new Label
			{
				Text = point.Label,
				FontSize = 11,
				TextColor = Color.FromArgb("#C9C3DA"),
				WidthRequest = 60,
				VerticalTextAlignment = TextAlignment.Center
			};

			var valueLabel = new Label
			{
				Text = $"{point.Value:F0}",
				FontSize = 11,
				FontFamily = "OpenSansSemibold",
				TextColor = Color.FromArgb("#E8DAFF"),
				HorizontalOptions = LayoutOptions.End
			};

			labelLayout.Add(label);
			labelLayout.Add(valueLabel);

			var barHeight = (point.Value / maxVal) * 24;
			var barColor = Color.FromArgb("#7C4DFF");
			var barView = new BoxView
			{
				Color = barColor,
				HeightRequest = Math.Max(2, barHeight),
				CornerRadius = 2
			};

			rowLayout.Add(labelLayout);
			rowLayout.Add(barView);

			chartLayout.Add(rowLayout);
		}

		ChartContainer.Add(chartLayout);
	}

	private void ShowEmptyState()
	{
		var emptyLabel = new Label
		{
			Text = "No data available",
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			FontSize = 12,
			TextColor = Color.FromArgb("#8B7FA3")
		};
		ChartContainer.Add(emptyLabel);
	}
}
