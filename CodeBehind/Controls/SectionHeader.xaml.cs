namespace FreakLete;

public partial class SectionHeader : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(SectionHeader), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty SubtitleProperty =
		BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SectionHeader), string.Empty, propertyChanged: OnPropertyChanged);

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

	public SectionHeader()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is SectionHeader header)
			header.ApplyState();
	}

	private void ApplyState()
	{
		TitleLabel.Text = Title;
		SubtitleLabel.Text = Subtitle;
		SubtitleLabel.IsVisible = !string.IsNullOrWhiteSpace(Subtitle);
	}
}
