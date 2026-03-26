namespace FreakLete.Page.Tests;

/// <summary>
/// Minimal MAUI Application that hosts the test runner.
/// Provides a full WinUI3 context so real MAUI controls (Label, Entry, Editor,
/// ContentPage) can be instantiated without COM errors.
/// </summary>
public class TestApp : Application
{
	private static readonly string LogFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		"page-test-results.txt");

	protected override Window CreateWindow(IActivationState? activationState)
	{
		File.AppendAllText(LogFile, "CreateWindow called\n");
		var runner = new TestRunnerPage();
		File.AppendAllText(LogFile, "TestRunnerPage created\n");
		var window = new Window(runner);
		File.AppendAllText(LogFile, "Window created\n");

		// Ensure tests run even if OnAppearing/Loaded don't fire in headless WinUI3.
		window.Created += (_, _) =>
		{
			File.AppendAllText(LogFile, "Window.Created event fired\n");
			try
			{
				runner.Dispatcher.Dispatch(() =>
				{
					File.AppendAllText(LogFile, "Dispatcher.Dispatch callback entered\n");
					runner.StartTests();
				});
			}
			catch (Exception ex)
			{
				File.AppendAllText(LogFile, $"Dispatcher.Dispatch failed: {ex}\n");
			}
		};

		return window;
	}
}
