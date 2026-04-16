using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete;

public partial class ExerciseCatalogPage : ContentPage
{
	private IReadOnlyList<ExerciseCatalogItem> _allExercises = [];
	private string _selectedCategory = "All";
	private string _searchText = string.Empty;

	public ExerciseCatalogPage()
	{
		InitializeComponent();
		BuildCategoryChips();
		LoadExercises();

		PageTitleLabel.Text = AppLanguage.IsTurkish ? "Egzersiz Kataloğu" : "Exercise Catalog";
		SearchEntry.Placeholder = AppLanguage.IsTurkish ? "Egzersiz ara..." : "Search exercises...";
	}

	private void BuildCategoryChips()
	{
		var allLabel = AppLanguage.IsTurkish ? "Tümü" : "All";

		ChipContainer.Children.Clear();
		ChipContainer.Children.Add(MakeChip(allLabel, "All", true));

		foreach (var cat in ExerciseCatalog.Categories)
			ChipContainer.Children.Add(MakeChip(cat, cat, false));
	}

	private Border MakeChip(string label, string value, bool selected)
	{
		var chip = new Border
		{
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
			BackgroundColor = selected
				? (Color)Application.Current!.Resources["Accent"]
				: (Color)Application.Current!.Resources["SurfaceRaised"],
			Stroke = selected
				? Colors.Transparent
				: (Color)Application.Current!.Resources["SurfaceBorder"],
			Padding = new Thickness(14, 6),
			BindingContext = value
		};
		chip.Content = new Label
		{
			Text = label,
			FontSize = 12,
			FontFamily = "OpenSansSemibold",
			TextColor = selected
				? Colors.White
				: (Color)Application.Current!.Resources["TextSecondary"]
		};
		chip.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(() => OnChipSelected(chip))
		});
		return chip;
	}

	private void OnChipSelected(Border selected)
	{
		_selectedCategory = (string)selected.BindingContext;

		foreach (var child in ChipContainer.Children.OfType<Border>())
		{
			bool isSelected = child == selected;
			child.BackgroundColor = isSelected
				? (Color)Application.Current!.Resources["Accent"]
				: (Color)Application.Current!.Resources["SurfaceRaised"];
			child.Stroke = isSelected
				? Colors.Transparent
				: (Color)Application.Current!.Resources["SurfaceBorder"];
			if (child.Content is Label lbl)
				lbl.TextColor = isSelected
					? Colors.White
					: (Color)Application.Current!.Resources["TextSecondary"];
		}

		ApplyFilters();
	}

	private void LoadExercises()
	{
		_allExercises = ExerciseCatalog.GetAllItems();
		ApplyFilters();
	}

	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		_searchText = e.NewTextValue ?? string.Empty;
		ApplyFilters();
	}

	private void ApplyFilters()
	{
		var filtered = _allExercises.AsEnumerable();

		if (_selectedCategory != "All")
			filtered = filtered.Where(x => x.Category == _selectedCategory);

		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			var q = _searchText.ToLower();
			filtered = filtered.Where(x =>
				x.DisplayName.ToLower().Contains(q) ||
				x.Name.ToLower().Contains(q) ||
				x.TurkishName.ToLower().Contains(q) ||
				x.PrimaryMuscles.Any(m => m.ToLower().Contains(q)));
		}

		var grouped = filtered
			.GroupBy(x => x.Category)
			.OrderBy(g => g.Key)
			.Select(g => new ExerciseGroup(g.Key, g.OrderBy(x => x.RecommendedRank).ThenBy(x => x.DisplayName)))
			.ToList();

		ExerciseList.ItemsSource = grouped;
	}

	private async void OnExerciseTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border) return;
		if (border.BindingContext is not ExerciseCatalogItem item) return;
		await Navigation.PushAsync(new ExerciseDetailPage(item), true);
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}
}

public class ExerciseGroup : List<ExerciseCatalogItem>
{
	public string Key { get; }
	public ExerciseGroup(string key, IEnumerable<ExerciseCatalogItem> items) : base(items)
	{
		Key = key;
	}
}
