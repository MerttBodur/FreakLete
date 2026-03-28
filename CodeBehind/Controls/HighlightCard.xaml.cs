namespace FreakLete;

public partial class HighlightCard : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(HighlightCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ValueProperty =
		BindableProperty.Create(nameof(Value), typeof(string), typeof(HighlightCard), "0", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty UnitProperty =
		BindableProperty.Create(nameof(Unit), typeof(string), typeof(HighlightCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty BackgroundTintProperty =
		BindableProperty.Create(nameof(BackgroundTint), typeof(Color), typeof(HighlightCard), Colors.Transparent, propertyChanged: OnPropertyChanged);

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
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

	public Color BackgroundTint
	{
		get => (Color)GetValue(BackgroundTintProperty);
		set => SetValue(BackgroundTintProperty, value);
	}

	public HighlightCard()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is HighlightCard card)
			card.ApplyState();
	}

	private void ApplyState()
	{
		TitleLabel.Text = Title;
		ValueLabel.Text = Value;
		UnitLabel.Text = Unit;
		UnitLabel.IsVisible = !string.IsNullOrWhiteSpace(Unit);

		if (BackgroundTint != Colors.Transparent)
		{
			CardBorderElement.BackgroundColor = BackgroundTint;
		}
		else
		{
			CardBorderElement.BackgroundColor = (Application.Current?.Resources["Surface"] as Color) ?? Colors.DarkGray;
		}
	}
}
