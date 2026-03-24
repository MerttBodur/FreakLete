namespace FreakLete;

public partial class HeroPanel : ContentView
{
	public static readonly BindableProperty EyebrowProperty =
		BindableProperty.Create(nameof(Eyebrow), typeof(string), typeof(HeroPanel), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(HeroPanel), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty SubtitleProperty =
		BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(HeroPanel), string.Empty, propertyChanged: OnPropertyChanged);

	public string Eyebrow
	{
		get => (string)GetValue(EyebrowProperty);
		set => SetValue(EyebrowProperty, value);
	}

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	public HeroPanel()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is HeroPanel panel)
			panel.ApplyState();
	}

	private void ApplyState()
	{
		EyebrowLabel.Text = Eyebrow;
		TitleLabel.Text = Title;
		SubtitleLabel.Text = Subtitle;
		SubtitleLabel.IsVisible = !string.IsNullOrWhiteSpace(Subtitle);
	}
}
