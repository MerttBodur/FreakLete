using System.Windows.Input;

namespace FreakLete;

public partial class FeatureCard : ContentView
{
	public static readonly BindableProperty IconSourceProperty =
		BindableProperty.Create(nameof(IconSource), typeof(string), typeof(FeatureCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(FeatureCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty DescriptionProperty =
		BindableProperty.Create(nameof(Description), typeof(string), typeof(FeatureCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ButtonTextProperty =
		BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(FeatureCard), string.Empty, propertyChanged: OnPropertyChanged);

	public static readonly BindableProperty ButtonCommandProperty =
		BindableProperty.Create(nameof(ButtonCommand), typeof(ICommand), typeof(FeatureCard), null, propertyChanged: OnPropertyChanged);

	public string IconSource
	{
		get => (string)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

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

	public ICommand ButtonCommand
	{
		get => (ICommand)GetValue(ButtonCommandProperty);
		set => SetValue(ButtonCommandProperty, value);
	}

	public FeatureCard()
	{
		InitializeComponent();
		ApplyState();
	}

	private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is FeatureCard card)
			card.ApplyState();
	}

	private void ApplyState()
	{
		IconLabel.Text = IconSource;
		TitleLabel.Text = Title;
		DescriptionLabel.Text = Description;
		ActionButton.Text = ButtonText;
		ActionButton.Command = ButtonCommand;
		ActionButton.IsVisible = !string.IsNullOrWhiteSpace(ButtonText);
	}
}
