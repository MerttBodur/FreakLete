using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class TabSwitcher : ContentView
{
	public static readonly BindableProperty ItemsProperty =
		BindableProperty.Create(nameof(Items), typeof(IList<string>), typeof(TabSwitcher), null,
			propertyChanged: (b, o, n) => ((TabSwitcher)b).RebuildTabs());

	public static readonly BindableProperty SelectedIndexProperty =
		BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(TabSwitcher), 0,
			propertyChanged: (b, o, n) => ((TabSwitcher)b).UpdateSelection((int)n));

	public IList<string>? Items
	{
		get => (IList<string>?)GetValue(ItemsProperty);
		set => SetValue(ItemsProperty, value);
	}

	public int SelectedIndex
	{
		get => (int)GetValue(SelectedIndexProperty);
		set => SetValue(SelectedIndexProperty, value);
	}

	public event EventHandler<int>? TabSelected;

	public TabSwitcher() => InitializeComponent();

	private void RebuildTabs()
	{
		TabsGrid.Children.Clear();
		TabsGrid.ColumnDefinitions.Clear();
		if (Items is null) return;

		for (int i = 0; i < Items.Count; i++)
		{
			TabsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			var idx = i;
			var label = new Label
			{
				Text = Items[i],
				FontFamily = "OpenSansSemibold",
				FontSize = 13,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				TextColor = (Color)Application.Current!.Resources["TextMuted"]
			};

			var pill = new Border
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
				Padding = new Thickness(0, 9),
				BackgroundColor = Colors.Transparent,
				Content = label
			};

			var tap = new TapGestureRecognizer();
			tap.Tapped += (_, _) =>
			{
				SelectedIndex = idx;
				TabSelected?.Invoke(this, idx);
			};
			pill.GestureRecognizers.Add(tap);

			Grid.SetColumn(pill, i);
			TabsGrid.Children.Add(pill);
		}

		UpdateSelection(SelectedIndex);
	}

	private void UpdateSelection(int selected)
	{
		var accent = (Color)Application.Current!.Resources["Accent"];
		var textPrimary = (Color)Application.Current!.Resources["TextPrimary"];
		var textMuted = (Color)Application.Current!.Resources["TextMuted"];

		for (int i = 0; i < TabsGrid.Children.Count; i++)
		{
			if (TabsGrid.Children[i] is not Border pill) continue;
			if (pill.Content is not Label label) continue;

			if (i == selected)
			{
				pill.BackgroundColor = accent;
				label.TextColor = textPrimary;
			}
			else
			{
				pill.BackgroundColor = Colors.Transparent;
				label.TextColor = textMuted;
			}
		}
	}
}
