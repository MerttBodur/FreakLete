using System.Windows.Input;

namespace FreakLete;

public partial class QuickAccessTile : ContentView
{
	public static readonly BindableProperty TitleProperty =
		BindableProperty.Create(nameof(Title), typeof(string), typeof(QuickAccessTile), string.Empty,
			propertyChanged: (b, o, n) => ((QuickAccessTile)b).TitleLabel.Text = (string)n);

	public static readonly BindableProperty SubtitleProperty =
		BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(QuickAccessTile), string.Empty,
			propertyChanged: (b, o, n) => ((QuickAccessTile)b).SubtitleLabel.Text = (string)n);

	public static readonly BindableProperty IconSourceProperty =
		BindableProperty.Create(nameof(IconSource), typeof(string), typeof(QuickAccessTile), string.Empty,
			propertyChanged: (b, o, n) => ((QuickAccessTile)b).ApplyIconSource((string)n));

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(QuickAccessTile), null);

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

	public string IconSource
	{
		get => (string)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public QuickAccessTile() => InitializeComponent();

	private void ApplyIconSource(string source)
	{
		TileIcon.Source = string.IsNullOrWhiteSpace(source)
			? null
			: ImageSource.FromFile(source);
	}
}
