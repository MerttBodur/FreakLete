namespace FreakLete;

public class ChartItem
{
	public string Label { get; set; } = string.Empty;
	public double Value { get; set; }
	public double HeightInPixels { get; set; }
}

public partial class BarChartCard : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(BarChartCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ItemsProperty =
		BindableProperty.Create(nameof(Items), typeof(List<ChartItem>), typeof(BarChartCard), null, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty MaxValueProperty =
		BindableProperty.Create(nameof(MaxValue), typeof(double), typeof(BarChartCard), 10d, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty SummaryTextProperty =
		BindableProperty.Create(nameof(SummaryText), typeof(string), typeof(BarChartCard), string.Empty, propertyChanged: OnPropertyChanged);

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public List<ChartItem> Items
	{
		get => (List<ChartItem>)GetValue(ItemsProperty);
		set => SetValue(ItemsProperty, value);
	}

	public double MaxValue
	{
		get => (double)GetValue(MaxValueProperty);
		set => SetValue(MaxValueProperty, value);
	}

	public string SummaryText
	{
		get => (string)GetValue(SummaryTextProperty);
		set => SetValue(SummaryTextProperty, value);
	}

	public BarChartCard()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is BarChartCard card)
			card.ApplyState();
	}

	private void ApplyState()
	{
		TitleLabel.Text = Title;

		if (FindByName("SummaryLabel") is Label summaryLabel)
		{
			summaryLabel.Text = SummaryText;
		}

		if (Items == null || Items.Count == 0)
		{
			return;
		}

		double maxItemValue = Items.Max(item => item.Value);
		if (maxItemValue > MaxValue)
		{
			MaxValue = maxItemValue;
		}

		double effectiveMax = Math.Max(MaxValue, 1);

		foreach (var item in Items)
		{
			item.HeightInPixels = (item.Value / effectiveMax) * 80;
		}
	}
}
