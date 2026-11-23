using Android.Util;

namespace C04Repro;

// Every log line carries the "C04" tag; run-cells.sh filters on it.
public class ProbePage : ContentPage
{
	const string Tag = "C04";
	readonly BoxView box = new() { WidthRequest = 100, HeightRequest = 100, Color = Colors.Red };

	public ProbePage()
	{
		Content = new VerticalStackLayout { Children = { box } };
		Loaded += async (_, _) => await RunAsync();
	}

	async Task RunAsync()
	{
		var rate = Scenario.Rate;
		Log.Info(Tag, $"C04 STARTED rate={rate}");

		try
		{
			new Animation(value => box.Opacity = value, 0, 1).Commit(box, "Rate", rate: rate, length: 1000);
		}
		catch (Exception ex)
		{
			Log.Error(Tag, $"C04 CAUGHT {ex}");
		}

		await Task.Delay(2000);
		Log.Info(Tag, $"C04 SURVIVED rate={rate}");
	}
}
