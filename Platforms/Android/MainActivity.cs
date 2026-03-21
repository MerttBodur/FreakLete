using Android.App;
using Android.Content.PM;
using Android.OS;

namespace GymTracker;

[Activity(Theme = "@style/Theme.GymTracker", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
		{
			Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#161125"));
			Window?.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#100D1A"));
		}
	}
}
