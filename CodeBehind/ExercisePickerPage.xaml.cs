using System.Collections.ObjectModel;
using GymTracker.Models;
using GymTracker.Services;

namespace GymTracker;

public partial class ExercisePickerPage : ContentPage
{
	private readonly Action<ExerciseCatalogItem> _onSelected;
	private readonly List<string> _allowedCategories;
	private string _selectedCategory = string.Empty;

	public ObservableCollection<CategoryChipItem> Categories { get; } = [];
	public ObservableCollection<ExerciseCatalogItem> VisibleExercises { get; } = [];

	public ExercisePickerPage(
		string pageTitle,
		IEnumerable<string> allowedCategories,
		Action<ExerciseCatalogItem> onSelected)
	{
		InitializeComponent();
		BindingContext = this;

		_onSelected = onSelected;
		_allowedCategories = allowedCategories.ToList();

		HeaderView.Title = pageTitle;
		PageTitleLabel.Text = pageTitle;

		BuildCategories();
		SelectCategory(_allowedCategories.FirstOrDefault() ?? ExerciseCatalog.Categories.First());
	}

	private void BuildCategories()
	{
		Categories.Clear();

		foreach (string category in _allowedCategories)
		{
			Categories.Add(new CategoryChipItem
			{
				Name = category
			});
		}
	}

	private void SelectCategory(string category)
	{
		_selectedCategory = category;

		foreach (CategoryChipItem item in Categories)
		{
			bool isSelected = item.Name == category;
			item.BackgroundColor = isSelected ? Color.FromArgb("#7C4DFF") : Color.FromArgb("#1B1727");
			item.TextColor = isSelected ? Colors.White : Color.FromArgb("#C9C3DA");
			item.BorderColor = isSelected ? Color.FromArgb("#9F7BFF") : Color.FromArgb("#2A2437");
		}

		RefreshExerciseList();
	}

	private void RefreshExerciseList()
	{
		VisibleExercises.Clear();

		foreach (ExerciseCatalogItem item in ExerciseCatalog.GetItemsByCategory(_selectedCategory))
		{
			VisibleExercises.Add(item);
		}
	}

	private void OnCategoryClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not CategoryChipItem item)
		{
			return;
		}

		SelectCategory(item.Name);
	}

	private async void OnExerciseTapped(object? sender, TappedEventArgs e)
	{
		if (sender is not Border border || border.BindingContext is not ExerciseCatalogItem item)
		{
			return;
		}

		_onSelected(item);
		await Navigation.PopAsync(true);
	}

	private async void OnHeaderBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync(true);
	}

	public sealed class CategoryChipItem : BindableObject
	{
		public string Name { get; set; } = string.Empty;

		private Color _backgroundColor = Color.FromArgb("#1B1727");
		public Color BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;
				OnPropertyChanged();
			}
		}

		private Color _textColor = Color.FromArgb("#C9C3DA");
		public Color TextColor
		{
			get => _textColor;
			set
			{
				_textColor = value;
				OnPropertyChanged();
			}
		}

		private Color _borderColor = Color.FromArgb("#2A2437");
		public Color BorderColor
		{
			get => _borderColor;
			set
			{
				_borderColor = value;
				OnPropertyChanged();
			}
		}
	}
}
