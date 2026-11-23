using Android.App;
using Android.Content.PM;
using Android.OS;

namespace C04Repro;

[Activity(Name = "com.example.c04repro.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		Scenario.Rate = (uint)(Intent?.GetIntExtra("rate", 1) ?? 1);
		base.OnCreate(savedInstanceState);
	}
}
