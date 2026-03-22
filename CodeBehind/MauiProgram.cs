using FreakLete.Services;
using Microsoft.Extensions.Logging;

namespace FreakLete;

public static class MauiProgram
{
	public static IServiceProvider Services { get; private set; } = null!;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<UserSession>();
		builder.Services.AddSingleton<ApiClient>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		MauiApp app = builder.Build();
		Services = app.Services;
		return app;
	}
}
