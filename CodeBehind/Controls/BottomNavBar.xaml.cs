namespace GymTracker;

public partial class BottomNavBar : ContentView
{
	public const string HomeTab = "home";
	public const string WorkoutTab = "workout";
	public const string OneRmTab = "onerm";
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
		OneRmIndicator.IsVisible = ActiveTab == OneRmTab;
		ProfileIndicator.IsVisible = ActiveTab == ProfileTab;

		HomeButton.Opacity = ActiveTab == HomeTab ? 1 : 0.55;
		WorkoutButton.Opacity = ActiveTab == WorkoutTab ? 1 : 0.55;
		OneRmButton.Opacity = ActiveTab == OneRmTab ? 1 : 0.55;
		ProfileButton.Opacity = ActiveTab == ProfileTab ? 1 : 0.55;
	}

	private async void OnHomeClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		var navigation = GetNavigation();
		if (navigation is null)
		{
			return;
		}

		await navigation.PopToRootAsync(true);
	}

	private async void OnWorkoutClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await NavigateFromRootAsync(() => new WorkoutPage());
	}

	private async void OnOneRmClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await NavigateFromRootAsync(() => new OneRmPage());
	}

	private async void OnProfileClicked(object? sender, EventArgs e)
	{
		if (sender is VisualElement element)
		{
			await AnimatePressAsync(element);
		}

		await NavigateFromRootAsync(() => new ProfilePage());
	}

	private static INavigation? GetNavigation()
	{
		return Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
	}

	private static async Task NavigateFromRootAsync(Func<Page> createPage)
	{
		var navigation = GetNavigation();
		if (navigation is null)
		{
			return;
		}

		await navigation.PopToRootAsync(false);

		Page rootPage = navigation.NavigationStack.FirstOrDefault() ?? new HomePage();
		Page nextPage = createPage();
		if (rootPage.GetType() == nextPage.GetType())
		{
			return;
		}

		await navigation.PushAsync(nextPage, true);
	}

	private static async Task AnimatePressAsync(VisualElement element)
	{
		await element.ScaleToAsync(0.88, 70, Easing.CubicOut);
		await element.ScaleToAsync(1, 110, Easing.CubicIn);
	}
}
