using System.Collections.ObjectModel;

namespace FreakLete;

public partial class OptionPickerPage : ContentPage
{
	private readonly Action<string> _onSelected;

	public ObservableCollection<OptionItem> Options { get; } = [];

	public OptionPickerPage(
		string title,
		IEnumerable<string> options,
		string? currentSelection,
		Action<string> onSelected)
	{
		InitializeComponent();
		_onSelected = onSelected;

		HeaderView.Title = title;
		PageTitleLabel.Text = title;

		foreach (string option in options)
		{
			bool isSelected = string.Equals(option, currentSelection, StringComparison.OrdinalIgnoreCase);
			Options.Add(new OptionItem
			{
				Text = option,
				Background = isSelected ? Color.FromArgb("#2F2346") : Color.FromArgb("#1D1828"),
				Foreground = isSelected ? Color.FromArgb("#A78BFA") : Color.FromArgb("#F7F7FB")
			});
		}

		OptionsCollection.ItemsSource = Options;
	}

	private async void OnOptionTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border || border.BindingContext is not OptionItem item)
			return;

		_onSelected(item.Text);
		await Navigation.PopAsync(true);
	}

	private async void OnHeaderBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	public class OptionItem
	{
		public string Text { get; set; } = string.Empty;
		public Color Background { get; set; } = Color.FromArgb("#1D1828");
		public Color Foreground { get; set; } = Color.FromArgb("#F7F7FB");
	}
}
