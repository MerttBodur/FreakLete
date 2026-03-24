using System.Collections.ObjectModel;

namespace FreakLete;

public partial class OptionPickerPage : ContentPage
{
	private readonly Action<string> _onSelected;
	private readonly Func<Task>? _onRetry;
	private readonly string? _currentSelection;

	// All items (unfiltered)
	private readonly List<OptionItem> _allItems = [];

	// Category state
	private readonly List<string> _categories = [];
	private string? _selectedCategory;
	private string _searchText = string.Empty;

	// Search threshold: show search bar when options exceed this count
	private const int SearchThreshold = 12;

	public ObservableCollection<OptionItem> VisibleOptions { get; } = [];

	/// <summary>
	/// Simple constructor for small option lists (backward compatible).
	/// </summary>
	public OptionPickerPage(
		string title,
		IEnumerable<string> options,
		string? currentSelection,
		Action<string> onSelected)
		: this(title, options.Select(o => new OptionItem { Text = o }), currentSelection, onSelected, null, null)
	{
	}

	/// <summary>
	/// Advanced constructor with category grouping, optional retry callback.
	/// </summary>
	public OptionPickerPage(
		string title,
		IEnumerable<OptionItem> items,
		string? currentSelection,
		Action<string> onSelected,
		IEnumerable<string>? categories,
		Func<Task>? onRetry)
	{
		InitializeComponent();
		_onSelected = onSelected;
		_onRetry = onRetry;
		_currentSelection = currentSelection;

		HeaderView.Title = title;
		PageTitleLabel.Text = title;

		// Build items
		foreach (var item in items)
		{
			bool isSelected = string.Equals(item.Text, currentSelection, StringComparison.OrdinalIgnoreCase);
			item.Background = isSelected ? Color.FromArgb("#2F2346") : Color.FromArgb("#1D1828");
			item.Foreground = isSelected ? Color.FromArgb("#A78BFA") : Color.FromArgb("#F7F7FB");
			_allItems.Add(item);
		}

		// Build categories
		if (categories is not null)
		{
			_categories.AddRange(categories);
		}

		// Show search if enough items
		SearchContainer.IsVisible = _allItems.Count > SearchThreshold;

		// Show category chips
		if (_categories.Count > 0)
		{
			CategoryScrollView.IsVisible = true;
			BuildCategoryChips();
			// Select "All" by default
			_selectedCategory = null;
		}

		ApplyFilter();
		OptionsCollection.ItemsSource = VisibleOptions;
	}

	// ── State management ────────────────────────────────────────

	/// <summary>Show loading spinner, hide other states.</summary>
	public void ShowLoading()
	{
		LoadingView.IsVisible = true;
		EmptyView.IsVisible = false;
		ErrorView.IsVisible = false;
		OptionsCollection.IsVisible = false;
	}

	/// <summary>Show error with optional retry.</summary>
	public void ShowError(string message)
	{
		LoadingView.IsVisible = false;
		EmptyView.IsVisible = false;
		ErrorView.IsVisible = true;
		ErrorLabel.Text = message;
		RetryButton.IsVisible = _onRetry is not null;
		OptionsCollection.IsVisible = false;
	}

	/// <summary>Replace items after async load.</summary>
	public void SetItems(IEnumerable<OptionItem> items, IEnumerable<string>? categories = null)
	{
		_allItems.Clear();
		foreach (var item in items)
		{
			bool isSelected = string.Equals(item.Text, _currentSelection, StringComparison.OrdinalIgnoreCase);
			item.Background = isSelected ? Color.FromArgb("#2F2346") : Color.FromArgb("#1D1828");
			item.Foreground = isSelected ? Color.FromArgb("#A78BFA") : Color.FromArgb("#F7F7FB");
			_allItems.Add(item);
		}

		SearchContainer.IsVisible = _allItems.Count > SearchThreshold;

		if (categories is not null)
		{
			_categories.Clear();
			_categories.AddRange(categories);
			CategoryScrollView.IsVisible = _categories.Count > 0;
			BuildCategoryChips();
		}

		LoadingView.IsVisible = false;
		ErrorView.IsVisible = false;
		OptionsCollection.IsVisible = true;

		ApplyFilter();
	}

	// ── Filter logic ────────────────────────────────────────────

	private void ApplyFilter()
	{
		VisibleOptions.Clear();

		IEnumerable<OptionItem> filtered = _allItems;

		// Category filter
		if (!string.IsNullOrEmpty(_selectedCategory))
		{
			filtered = filtered.Where(o =>
				string.Equals(o.GroupName, _selectedCategory, StringComparison.OrdinalIgnoreCase));
		}

		// Search filter
		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			filtered = filtered.Where(o =>
				o.Text.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
		}

		foreach (var item in filtered)
		{
			VisibleOptions.Add(item);
		}

		// Show empty state if needed
		bool hasItems = VisibleOptions.Count > 0;
		OptionsCollection.IsVisible = hasItems;
		EmptyView.IsVisible = !hasItems && !LoadingView.IsVisible && !ErrorView.IsVisible;

		if (!hasItems && !string.IsNullOrWhiteSpace(_searchText))
		{
			EmptyLabel.Text = "No results found";
			EmptyHintLabel.Text = "Try a different search term";
			EmptyHintLabel.IsVisible = true;
		}
		else if (!hasItems)
		{
			EmptyLabel.Text = "No options available";
			EmptyHintLabel.IsVisible = false;
		}
	}

	// ── Category chips ──────────────────────────────────────────

	private void BuildCategoryChips()
	{
		CategoryChipsLayout.Children.Clear();

		// "All" chip
		var allChip = CreateCategoryChip("All", _selectedCategory is null);
		CategoryChipsLayout.Children.Add(allChip);

		foreach (string cat in _categories)
		{
			var chip = CreateCategoryChip(cat, string.Equals(cat, _selectedCategory, StringComparison.OrdinalIgnoreCase));
			CategoryChipsLayout.Children.Add(chip);
		}
	}

	private Button CreateCategoryChip(string text, bool isSelected)
	{
		var chip = new Button
		{
			Text = text,
			BackgroundColor = isSelected ? Color.FromArgb("#7C4DFF") : Color.FromArgb("#1B1727"),
			TextColor = isSelected ? Colors.White : Color.FromArgb("#C9C3DA"),
			BorderColor = isSelected ? Color.FromArgb("#9F7BFF") : Color.FromArgb("#2A2437"),
			BorderWidth = 1,
			CornerRadius = 18,
			Padding = new Thickness(16, 10),
			FontSize = 13,
			MinimumHeightRequest = 36
		};

		chip.Clicked += OnCategoryChipClicked;
		return chip;
	}

	private void OnCategoryChipClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button) return;

		string text = button.Text;
		_selectedCategory = text == "All" ? null : text;

		// Update chip visuals
		foreach (var child in CategoryChipsLayout.Children)
		{
			if (child is Button btn)
			{
				bool isActive = btn.Text == (text == "All" ? "All" : text) && btn == button
					|| (text == "All" && btn.Text == "All");

				// Simplify: just check if this is the clicked button
				isActive = btn == button;

				btn.BackgroundColor = isActive ? Color.FromArgb("#7C4DFF") : Color.FromArgb("#1B1727");
				btn.TextColor = isActive ? Colors.White : Color.FromArgb("#C9C3DA");
				btn.BorderColor = isActive ? Color.FromArgb("#9F7BFF") : Color.FromArgb("#2A2437");
			}
		}

		ApplyFilter();
	}

	// ── Event handlers ──────────────────────────────────────────

	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		_searchText = e.NewTextValue?.Trim() ?? string.Empty;
		ApplyFilter();
	}

	private async void OnOptionTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border || border.BindingContext is not OptionItem item)
			return;

		_onSelected(item.Text);
		await Navigation.PopAsync(true);
	}

	private async void OnRetryClicked(object? sender, EventArgs e)
	{
		if (_onRetry is null) return;

		ErrorView.IsVisible = false;
		ShowLoading();
		await _onRetry();
	}

	private async void OnHeaderBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	// ── Option item model ───────────────────────────────────────

	public class OptionItem
	{
		public string Text { get; set; } = string.Empty;
		public string GroupName { get; set; } = string.Empty;
		public bool HasGroup => !string.IsNullOrEmpty(GroupName);
		public Color Background { get; set; } = Color.FromArgb("#1D1828");
		public Color Foreground { get; set; } = Color.FromArgb("#F7F7FB");
	}
}
