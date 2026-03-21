using GymTracker.Services;

namespace GymTracker;

public partial class BottomNavBar : ContentView
{
	public const string HomeTab = "home";
	public const string WorkoutTab = "workout";
	public const string CalculationsTab = "calculations";
	public const string ProfileTab = "profile";

	public static readonly BindableProperty ActiveTabProperty =
		BindableProperty.Create(
			nameof(ActiveTab),
			typeof(string),
			typeof(BottomNavBar),
			HomeTab,
			propertyChanged: OnActiveTabChanged);

	public string ActiveTab
	{
		get => (string)GetValue(ActiveTabProperty);
		set => SetValue(ActiveTabProperty, value);
	}

	public BottomNavBar()
	{
		InitializeComponent();
		UpdateIndicators();
	}

	private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is BottomNavBar bar)
		{
			bar.UpdateIndicators();
		}
	}

	private void UpdateIndicators()
	{
		HomeIndicator.IsVisible = ActiveTab == HomeTab;
		WorkoutIndicator.IsVisible = ActiveTab == WorkoutTab;
		CalculationsIndicator.IsVisible = ActiveTab == CalculationsTab;
		ProfileIndicator.IsVisible = ActiveTab == ProfileTab;

		HomeButton.Opacity = ActiveTab == HomeTab ? 1 : 0.55;
		WorkoutButton.Opacity = ActiveTab == WorkoutTab ? 1 : 0.55;
		CalculationsButton.Opacity = ActiveTab == CalculationsTab ? 1 : 0.55;
		ProfileButton.Opacity = ActiveTab == ProfileTab ? 1 : 0.55;
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await TabNavigationHelper.SwitchToTabAsync(() => new HomePage());
	}

	private async void OnWorkoutClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await TabNavigationHelper.SwitchToTabAsync(() => new WorkoutPage());
	}

	private async void OnCalculationsClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await TabNavigationHelper.SwitchToTabAsync(() => new CalculationsPage());
	}

	private async void OnProfileClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await TabNavigationHelper.SwitchToTabAsync(() => new ProfilePage());
	}

	private static async Task AnimatePressAsync(VisualElement element)
	{
		await element.ScaleToAsync(0.88, 70, Easing.CubicOut);
		await element.ScaleToAsync(1, 110, Easing.CubicIn);
	}
}
