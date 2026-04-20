namespace FreakLete;

public partial class MetricTile : ContentView
{
	public static readonly BindableProperty LabelProperty =
		BindableProperty.Create(nameof(Label), typeof(string), typeof(MetricTile), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ValueProperty =
		BindableProperty.Create(nameof(Value), typeof(string), typeof(MetricTile), "-", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty UnitProperty =
		BindableProperty.Create(nameof(Unit), typeof(string), typeof(MetricTile), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ValueColorProperty =
		BindableProperty.Create(nameof(ValueColor), typeof(Color), typeof(MetricTile), null, propertyChanged: OnPropertyChanged);

	public string Label
	{
		get => (string)GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public string Unit
	{
		get => (string)GetValue(UnitProperty);
		set => SetValue(UnitProperty, value);
	}

	public Color? ValueColor
	{
		get => (Color?)GetValue(ValueColorProperty);
		set => SetValue(ValueColorProperty, value);
	}

	public MetricTile()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is MetricTile tile)
			tile.ApplyState();
	}

	private void ApplyState()
	{
		MetricLabel.Text = Label;
		ValueLabel.Text = Value;
		UnitLabel.Text = Unit;
		UnitLabel.IsVisible = !string.IsNullOrWhiteSpace(Unit);
		ValueLabel.TextColor = ValueColor ?? (Color)Application.Current!.Resources["AccentGlow"];
	}
}
