namespace FreakLete;

public partial class ProfileHeroCard : ContentView
{
	public static readonly BindableProperty NameProperty =
		BindableProperty.Create(nameof(Name), typeof(string), typeof(ProfileHeroCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty SubtitleProperty =
		BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(ProfileHeroCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty AvatarSourceProperty =
		BindableProperty.Create(nameof(AvatarSource), typeof(string), typeof(ProfileHeroCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty BackgroundImageSourceProperty =
		BindableProperty.Create(nameof(BackgroundImageSource), typeof(string), typeof(ProfileHeroCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty BackgroundColorProperty =
		BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(ProfileHeroCard), Colors.Transparent, propertyChanged: OnPropertyChanged);

	public string Name
	{
		get => (string)GetValue(NameProperty);
		set => SetValue(NameProperty, value);
	}

	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	public string AvatarSource
	{
		get => (string)GetValue(AvatarSourceProperty);
		set => SetValue(AvatarSourceProperty, value);
	}

	public string BackgroundImageSource
	{
		get => (string)GetValue(BackgroundImageSourceProperty);
		set => SetValue(BackgroundImageSourceProperty, value);
	}

	public new Color BackgroundColor
	{
		get => (Color)GetValue(BackgroundColorProperty);
		set => SetValue(BackgroundColorProperty, value);
	}

	public ProfileHeroCard()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is ProfileHeroCard card)
			card.ApplyState();
	}

	private void ApplyState()
	{
		NameLabel.Text = Name;
		SubtitleLabel.Text = Subtitle;
		SubtitleLabel.IsVisible = !string.IsNullOrWhiteSpace(Subtitle);

		if (!string.IsNullOrWhiteSpace(AvatarSource))
		{
			AvatarImage.Source = AvatarSource;
		}

		if (!string.IsNullOrWhiteSpace(BackgroundImageSource))
		{
			BackgroundImage.Source = BackgroundImageSource;
			BackgroundImage.IsVisible = true;
		}
		else
		{
			BackgroundImage.IsVisible = false;
		}

		if (BackgroundColor != Colors.Transparent)
		{
			HeroBorder.BackgroundColor = BackgroundColor;
		}
		else
		{
			HeroBorder.BackgroundColor = (Application.Current?.Resources["SurfaceRaised"] as Color) ?? Colors.DarkGray;
		}
	}
}
