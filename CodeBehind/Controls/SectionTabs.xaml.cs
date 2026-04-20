using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class SectionTabs : ContentView
{
	public static readonly BindableProperty ItemsProperty =
		BindableProperty.Create(nameof(Items), typeof(IList<string>), typeof(SectionTabs), null,
			propertyChanged: (b, o, n) => ((SectionTabs)b).RebuildTabs());

	public static readonly BindableProperty SelectedIndexProperty =
		BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(SectionTabs), 0,
			propertyChanged: (b, o, n) => ((SectionTabs)b).UpdateSelection((int)n));

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

	public SectionTabs() => InitializeComponent();

	private void RebuildTabs()
	{
		TabsContainer.Children.Clear();
		if (Items is null) return;

		for (int i = 0; i < Items.Count; i++)
		{
			var idx = i;
			var label = new Label
			{
				Text = Items[i],
				FontFamily = "OpenSansSemibold",
				FontSize = 14,
				TextColor = (Color)Application.Current!.Resources["TextMuted"]
			};

			var pill = new Border
			{
				StrokeThickness = 0,
				StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
				Padding = new Thickness(16, 8),
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
			TabsContainer.Children.Add(pill);
		}

		UpdateSelection(SelectedIndex);
	}

	private void UpdateSelection(int selected)
	{
		var accentSoft = (Color)Application.Current!.Resources["AccentSoft"];
		var accentGlow = (Color)Application.Current!.Resources["AccentGlow"];
		var textMuted = (Color)Application.Current!.Resources["TextMuted"];

		for (int i = 0; i < TabsContainer.Children.Count; i++)
		{
			if (TabsContainer.Children[i] is not Border pill) continue;
			if (pill.Content is not Label label) continue;

			if (i == selected)
			{
				pill.BackgroundColor = accentSoft;
				label.TextColor = accentGlow;
			}
			else
			{
				pill.BackgroundColor = Colors.Transparent;
				label.TextColor = textMuted;
			}
		}
	}
}
