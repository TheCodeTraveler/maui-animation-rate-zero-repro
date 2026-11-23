using Microsoft.Maui.Hosting;

namespace C04Repro;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();
		return builder.Build();
	}
}
