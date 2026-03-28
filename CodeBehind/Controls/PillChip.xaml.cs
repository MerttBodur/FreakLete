namespace FreakLete;

public partial class PillChip : ContentView
{
	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(PillChip), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty IsSelectedProperty =
		BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(PillChip), false, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty BackgroundColorProperty =
		BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(PillChip), Colors.Transparent, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty TextColorProperty =
		BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(PillChip), Colors.Transparent, propertyChanged: OnPropertyChanged);

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		set => SetValue(IsSelectedProperty, value);
	}

	public new Color BackgroundColor
	{
		get => (Color)GetValue(BackgroundColorProperty);
		set => SetValue(BackgroundColorProperty, value);
	}

	public new Color TextColor
	{
		get => (Color)GetValue(TextColorProperty);
		set => SetValue(TextColorProperty, value);
	}

	public PillChip()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is PillChip chip)
			chip.ApplyState();
	}

	private void ApplyState()
	{
		ChipLabel.Text = Text;
		
		if (IsSelected)
		{
			ChipBorder.BackgroundColor = (Application.Current?.Resources["Primary"] as Color) ?? Colors.Purple;
			ChipLabel.TextColor = (Application.Current?.Resources["TextPrimary"] as Color) ?? Colors.White;
		}
		else
		{
			ChipBorder.BackgroundColor = BackgroundColor != Colors.Transparent ? BackgroundColor : ((Application.Current?.Resources["SurfaceRaised"] as Color) ?? Colors.DarkGray);
			ChipLabel.TextColor = TextColor != Colors.Transparent ? TextColor : ((Application.Current?.Resources["TextSecondary"] as Color) ?? Colors.Gray);
		}
	}
}
