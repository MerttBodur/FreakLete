namespace FreakLete;

public partial class TopHeader : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(TopHeader), string.Empty, propertyChanged: OnHeaderPropertyChanged);

	public static readonly BindableProperty ShowBackButtonProperty =
		BindableProperty.Create(nameof(ShowBackButton), typeof(bool), typeof(TopHeader), false, propertyChanged: OnHeaderPropertyChanged);

	public static readonly BindableProperty ActionIconProperty =
		BindableProperty.Create(nameof(ActionIcon), typeof(string), typeof(TopHeader), string.Empty, propertyChanged: OnHeaderPropertyChanged);

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public bool ShowBackButton
	{
		get => (bool)GetValue(ShowBackButtonProperty);
		set => SetValue(ShowBackButtonProperty, value);
	}

	public string ActionIcon
	{
		get => (string)GetValue(ActionIconProperty);
		set => SetValue(ActionIconProperty, value);
	}

	public event EventHandler? BackClicked;
	public event EventHandler? ActionClicked;

	public TopHeader()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnHeaderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is TopHeader header)
		{
			header.ApplyState();
		}
	}

	private void ApplyState()
	{
		TitleLabel.Text = Title;
		BackButton.IsVisible = ShowBackButton;
		ActionButton.IsVisible = !string.IsNullOrWhiteSpace(ActionIcon);
		ActionButton.Source = string.IsNullOrWhiteSpace(ActionIcon) ? null : ImageSource.FromFile(ActionIcon);
	}

	private void OnBackButtonClicked(object? sender, EventArgs e)
	{
		BackClicked?.Invoke(this, EventArgs.Empty);
	}

	private void OnActionButtonClicked(object? sender, EventArgs e)
	{
		ActionClicked?.Invoke(this, EventArgs.Empty);
	}
}
