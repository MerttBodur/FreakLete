namespace FreakLete;

public partial class BarChartCard : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(BarChartCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty DataProperty =
		BindableProperty.Create(nameof(Data), typeof(System.Collections.IEnumerable), typeof(BarChartCard), null, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty MaxValueProperty =
		BindableProperty.Create(nameof(MaxValue), typeof(int), typeof(BarChartCard), 100, propertyChanged: OnPropertyChanged);

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

	public int MaxValue
	{
		get => (int)GetValue(MaxValueProperty);
		set => SetValue(MaxValueProperty, value);
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
	}
}
