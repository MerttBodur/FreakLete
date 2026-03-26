namespace FreakLete.Page.Tests;

/// <summary>
/// MAUI page that discovers and runs xunit [Fact] tests within
/// the WinUI3-hosted process, then writes results to a file and exits.
///
/// This is NOT a standard dotnet-test project. MAUI controls (Label,
/// Entry, Editor, ContentPage) require a WinUI3 application context,
/// so this runner IS that context.
///
/// Run:   dotnet run --project FreakLete.Page.Tests
/// Results: ~/page-test-results.txt
/// </summary>
public class TestRunnerPage : ContentPage
{
	private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(3);

	private readonly Label _statusLabel;
	private readonly Label _resultsLabel;

	private bool _testsStarted;

	public TestRunnerPage()
	{
		_statusLabel = new Label
		{
			Text = "Running tests...",
			FontSize = 20,
			HorizontalTextAlignment = TextAlignment.Center,
			Margin = new Thickness(20, 40, 20, 10)
		};
		_resultsLabel = new Label
		{
			Text = "",
			FontSize = 13,
			FontFamily = "Consolas",
			Margin = new Thickness(20, 10)
		};
		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Children = { _statusLabel, _resultsLabel }
			}
		};

		// Use Loaded event — more reliable than OnAppearing in WinUI3 hosted context
		Loaded += (_, _) => RunTestsOnce();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		// Fallback if Loaded didn't fire
		RunTestsOnce();
	}

	public void StartTests() => RunTestsOnce();

	private async void RunTestsOnce()
	{
		if (_testsStarted) return;
		_testsStarted = true;

		await Task.Delay(200);

		var result = await HostedTestExecutor.RunAsync(TestTimeout, status => _statusLabel.Text = status);

		string summary = $"{result.Total} tests: {result.Passed} passed, {result.Failed} failed";
		var failedNamesSummary = result.FailingTestNames.Count == 0
			? "FAILED TESTS: (none)"
			: $"FAILED TESTS: {string.Join(", ", result.FailingTestNames)}";
		_statusLabel.Text = summary;
		_resultsLabel.Text = string.Join("\n", result.Lines);

		HostedTestExecutor.WriteArtifact(result);

		await Task.Delay(1000);
		Environment.ExitCode = result.Failed == 0 ? 0 : 1;
		Application.Current?.Quit();
	}
}
