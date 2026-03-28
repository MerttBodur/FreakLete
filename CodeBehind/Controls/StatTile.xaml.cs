namespace FreakLete;

public partial class StatTile : ContentView
{
	public static readonly BindableProperty StatValueProperty =
		BindableProperty.Create(nameof(StatValue), typeof(string), typeof(StatTile), "0", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty StatLabelProperty =
		BindableProperty.Create(nameof(StatLabel), typeof(string), typeof(StatTile), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty TrendIconProperty =
		BindableProperty.Create(nameof(TrendIcon), typeof(string), typeof(StatTile), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty TrendColorProperty =
		BindableProperty.Create(nameof(TrendColor), typeof(Color), typeof(StatTile), Colors.Transparent, propertyChanged: OnPropertyChanged);

	public string StatValue
	{
		get => (string)GetValue(StatValueProperty);
		set => SetValue(StatValueProperty, value);
	}

	public string StatLabel
	{
		get => (string)GetValue(StatLabelProperty);
		set => SetValue(StatLabelProperty, value);
	}

	public string TrendIcon
	{
		get => (string)GetValue(TrendIconProperty);
		set => SetValue(TrendIconProperty, value);
	}

	public Color TrendColor
	{
		get => (Color)GetValue(TrendColorProperty);
		set => SetValue(TrendColorProperty, value);
	}

	public StatTile()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is StatTile tile)
			tile.ApplyState();
	}

	private void ApplyState()
	{
		StatValueLabel.Text = StatValue;
		StatLabelLabel.Text = StatLabel;
		TrendIconLabel.Text = TrendIcon;
		TrendIconLabel.IsVisible = !string.IsNullOrWhiteSpace(TrendIcon);
		
		if (!string.IsNullOrWhiteSpace(TrendIcon) && TrendColor != Colors.Transparent)
		{
			TrendIconLabel.TextColor = TrendColor;
		}
	}
}
