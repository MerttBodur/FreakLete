namespace FreakLete;

public partial class ActionTile : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(ActionTile), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty DescriptionProperty =
		BindableProperty.Create(nameof(Description), typeof(string), typeof(ActionTile), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ButtonTextProperty =
		BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(ActionTile), "Go", propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty IsSecondaryProperty =
		BindableProperty.Create(nameof(IsSecondary), typeof(bool), typeof(ActionTile), false, propertyChanged: OnPropertyChanged);

	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	public string Description
	{
		get => (string)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	public string ButtonText
	{
		get => (string)GetValue(ButtonTextProperty);
		set => SetValue(ButtonTextProperty, value);
	}

	public bool IsSecondary
	{
		get => (bool)GetValue(IsSecondaryProperty);
		set => SetValue(IsSecondaryProperty, value);
	}

	public event EventHandler? ButtonClicked;

	public ActionTile()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is ActionTile tile)
			tile.ApplyState();
	}

	private void ApplyState()
	{
		TitleLabel.Text = Title;
		DescriptionLabel.Text = Description;
		ActionButton.Text = ButtonText;

		if (IsSecondary)
		{
			ActionButton.BackgroundColor = (Color)Application.Current!.Resources["SurfaceStrong"];
		}
		else
		{
			ActionButton.BackgroundColor = (Color)Application.Current!.Resources["Accent"];
		}
	}

	private void OnActionButtonClicked(object? sender, EventArgs e)
	{
		ButtonClicked?.Invoke(this, EventArgs.Empty);
	}
}
