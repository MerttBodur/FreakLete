using FreakLete.Services;

namespace FreakLete;

public partial class BottomNavBar : ContentView
{
	public const string HomeTab = "home";
	public const string WorkoutTab = "workout";
	public const string FreakAiTab = "freakai";
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
		ApplyLanguage();
		UpdateIndicators();
		Loaded += (_, _) => AppLanguage.LanguageChanged += OnLanguageChanged;
		Unloaded += (_, _) => AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged() => ApplyLanguage();

	private void ApplyLanguage()
	{
		HomeLabel.Text = AppLanguage.NavHome;
		WorkoutLabel.Text = AppLanguage.NavWorkout;
		FreakAiLabel.Text = AppLanguage.NavFreakAi;
		CalculationsLabel.Text = AppLanguage.NavCalc;
		ProfileLabel.Text = AppLanguage.NavProfile;
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
		var accentSoft = (Application.Current?.Resources["AccentSoft"] as Color) ?? Color.FromArgb("#2F2346");
		var accentGlow = (Application.Current?.Resources["AccentGlow"] as Color) ?? Color.FromArgb("#A78BFA");
		var textMuted = (Application.Current?.Resources["TextMuted"] as Color) ?? Color.FromArgb("#8A889B");

		UpdateTab(HomePill, HomeIcon, HomeLabel, ActiveTab == HomeTab, "nav_home_active.svg", "nav_home_inactive.svg", accentSoft, accentGlow, textMuted);
		UpdateTab(WorkoutPill, WorkoutIcon, WorkoutLabel, ActiveTab == WorkoutTab, "nav_dumbbell_active.svg", "nav_dumbbell_inactive.svg", accentSoft, accentGlow, textMuted);
		UpdateTab(FreakAiPill, FreakAiIcon, FreakAiLabel, ActiveTab == FreakAiTab, "nav_notebook_active.svg", "nav_notebook_inactive.svg", accentSoft, accentGlow, textMuted);
		UpdateTab(CalculationsPill, CalculationsIcon, CalculationsLabel, ActiveTab == CalculationsTab, "nav_plates_active.svg", "nav_plates_inactive.svg", accentSoft, accentGlow, textMuted);
		UpdateTab(ProfilePill, ProfileIcon, ProfileLabel, ActiveTab == ProfileTab, "nav_profile_active.svg", "nav_profile_inactive.svg", accentSoft, accentGlow, textMuted);
	}

	private static void UpdateTab(Border pill, Image icon, Label label, bool isActive, string activeIcon, string inactiveIcon, Color accentSoft, Color accentGlow, Color textMuted)
	{
		pill.BackgroundColor = isActive ? accentSoft : Colors.Transparent;
		icon.Source = isActive ? activeIcon : inactiveIcon;
		icon.Opacity = 1;
		label.IsVisible = isActive;
		label.TextColor = isActive ? accentGlow : textMuted;
	}

	private async void OnHomeClicked(object? sender, TappedEventArgs e)
	{
		if (sender is VisualElement element)
			await AnimatePressAsync(element);

		await TabNavigationHelper.SwitchToTabAsync(() => new HomePage());
	}

	private async void OnWorkoutClicked(object? sender, TappedEventArgs e)
	{
		if (sender is VisualElement element)
			await AnimatePressAsync(element);

		await TabNavigationHelper.SwitchToTabAsync(() => new WorkoutPage());
	}

	private async void OnFreakAiClicked(object? sender, TappedEventArgs e)
	{
		if (sender is VisualElement element)
			await AnimatePressAsync(element);

		await TabNavigationHelper.SwitchToTabAsync(() => new FreakAiPage());
	}

	private async void OnCalculationsClicked(object? sender, TappedEventArgs e)
	{
		if (sender is VisualElement element)
			await AnimatePressAsync(element);

		await TabNavigationHelper.SwitchToTabAsync(() => new CalculationsPage());
	}

	private async void OnProfileClicked(object? sender, TappedEventArgs e)
	{
		if (sender is VisualElement element)
			await AnimatePressAsync(element);

		await TabNavigationHelper.SwitchToTabAsync(() => new ProfilePage());
	}

	private static async Task AnimatePressAsync(VisualElement element)
	{
		await element.ScaleToAsync(0.90, 70, Easing.CubicOut);
		await element.ScaleToAsync(1, 110, Easing.CubicIn);
	}
}
