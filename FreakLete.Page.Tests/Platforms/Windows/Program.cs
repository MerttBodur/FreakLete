namespace FreakLete.Page.Tests.WinUI;

public static class Program
{
	private static readonly string LogFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		"page-test-results.txt");
	private static readonly TimeSpan StartupWatchdogTimeout = TimeSpan.FromSeconds(90);

	private static void AppendRunnerFailureContract(string reason, string failingName)
	{
		try
		{
			File.AppendAllLines(LogFile,
			[
				$"RUNNER ERROR: {reason}",
				"0 tests: 0 passed, 1 failed",
				$"FAILED TESTS: {failingName}"
			]);
			global::FreakLete.Page.Tests.HostedTestExecutor.WriteArtifact(
				new global::FreakLete.Page.Tests.HostedTestRunResult
				{
					Passed = 0,
					Failed = 1,
					FailingTestNames = [failingName],
					Lines = [$"  RUNNER FAILURE: {reason}"]
				},
				reason);
		}
		catch
		{
			// Best effort fallback for contract output.
		}
	}

	[STAThread]
	static void Main(string[] args)
	{
		bool completed = false;

		try
		{
			File.WriteAllText(LogFile, "STARTING...\n");
			_ = Task.Run(async () =>
			{
				await Task.Delay(StartupWatchdogTimeout);
				if (completed)
					return;

				AppendRunnerFailureContract(
					$"WATCHDOG TIMEOUT after {StartupWatchdogTimeout.TotalSeconds:0}s - runner did not finish.",
					"(runner-timeout)");
				Environment.ExitCode = 1;
				Environment.Exit(1);
			});

			WinRT.ComWrappersSupport.InitializeComWrappers();
			File.AppendAllText(LogFile, "COM initialized\n");

			var result = global::FreakLete.Page.Tests.HostedTestExecutor
				.RunAsync(TimeSpan.FromSeconds(5))
				.GetAwaiter()
				.GetResult();

			global::FreakLete.Page.Tests.HostedTestExecutor.WriteArtifact(result);
			completed = true;
			Environment.ExitCode = result.Failed == 0 ? 0 : 1;
		}
		catch (Exception ex)
		{
			AppendRunnerFailureContract(
				$"Program.Main crash: {ex.GetType().Name}: {ex.Message}",
				"(runner-bootstrap-crash)");
			completed = true;
			Environment.ExitCode = 1;
		}
	}
}
