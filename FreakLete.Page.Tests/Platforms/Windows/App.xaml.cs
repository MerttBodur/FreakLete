using Microsoft.UI.Xaml;

namespace FreakLete.Page.Tests.WinUI;

public partial class App : MauiWinUIApplication
{
	private static readonly string LogFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		"page-test-results.txt");

	public App()
	{
		InitializeComponent();
	}

	protected override MauiApp CreateMauiApp()
	{
		File.AppendAllText(LogFile, "App.CreateMauiApp called\n");
		return global::FreakLete.Page.Tests.MauiProgram.CreateMauiApp();
	}
}
