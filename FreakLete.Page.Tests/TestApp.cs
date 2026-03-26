namespace FreakLete.Page.Tests;

/// <summary>
/// Minimal MAUI Application that hosts the test runner.
/// Provides a full WinUI3 context so real MAUI controls (Label, Entry, Editor,
/// ContentPage) can be instantiated without COM errors.
/// </summary>
public class TestApp : Application
{
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new TestRunnerPage());
	}
}
