using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FreakLete.Page.Tests.WinUI;

public static class Program
{
	private static readonly string LogFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		"page-test-results.txt");

	[STAThread]
	static void Main(string[] args)
	{
		try
		{
			File.WriteAllText(LogFile, "STARTING...\n");

			WinRT.ComWrappersSupport.InitializeComWrappers();
			File.AppendAllText(LogFile, "COM initialized\n");

			Microsoft.UI.Xaml.Application.Start(_ =>
			{
				File.AppendAllText(LogFile, "Application.Start callback entered\n");
				var context = new DispatcherQueueSynchronizationContext(
					DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				new App();
				File.AppendAllText(LogFile, "App created\n");
			});
		}
		catch (Exception ex)
		{
			File.WriteAllText(LogFile, $"CRASH IN MAIN: {ex}");
		}
	}
}
