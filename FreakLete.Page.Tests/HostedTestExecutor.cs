using System.Reflection;
using Xunit;

namespace FreakLete.Page.Tests;

public sealed class HostedTestRunResult
{
	public int Passed { get; init; }
	public int Failed { get; init; }
	public int Total => Passed + Failed;
	public List<string> FailingTestNames { get; init; } = [];
	public List<string> Lines { get; init; } = [];
}

public static class HostedTestExecutor
{
	private static readonly string ResultFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		"page-test-results.txt");

	public static async Task<HostedTestRunResult> RunAsync(TimeSpan testTimeout, Action<string>? onProgress = null)
	{
		int passed = 0;
		int failed = 0;
		var failingTestNames = new List<string>();
		var lines = new List<string>();

		try
		{
			var assembly = typeof(ProfilePageTests).Assembly;
			var testClasses = assembly.GetTypes()
				.Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<FactAttribute>() is not null))
				.OrderBy(t => t.Name)
				.ToList();

			foreach (var testClass in testClasses)
			{
				var instance = Activator.CreateInstance(testClass)!;
				var testMethods = testClass.GetMethods()
					.Where(m => m.GetCustomAttribute<FactAttribute>() is not null)
					.OrderBy(m => m.Name)
					.ToList();

				foreach (var method in testMethods)
				{
					string name = $"{testClass.Name}.{method.Name}";
					try
					{
						var result = method.Invoke(instance, null);
						if (result is Task task)
						{
							var completed = await Task.WhenAny(task, Task.Delay(testTimeout));
							if (completed != task)
								throw new TimeoutException($"Timed out after {testTimeout.TotalSeconds:0}s");
							await task;
						}

						passed++;
						lines.Add($"  PASS: {name}");
					}
					catch (TargetInvocationException tie)
					{
						var ex = tie.InnerException ?? tie;
						failed++;
						failingTestNames.Add(name);
						lines.Add($"  FAIL: {name}");
						lines.Add($"        {ex.GetType().Name}: {ex.Message}");
					}
					catch (Exception ex)
					{
						failed++;
						failingTestNames.Add(name);
						lines.Add($"  FAIL: {name}");
						lines.Add($"        {ex.GetType().Name}: {ex.Message}");
					}

					onProgress?.Invoke($"Running... {passed + failed} ({passed} passed, {failed} failed)");
				}
			}
		}
		catch (Exception ex)
		{
			failed++;
			failingTestNames.Add("(runner-crash)");
			lines.Add($"  RUNNER CRASH: {ex.GetType().Name}: {ex.Message}");
		}

		return new HostedTestRunResult
		{
			Passed = passed,
			Failed = failed,
			FailingTestNames = failingTestNames,
			Lines = lines
		};
	}

	public static void WriteArtifact(HostedTestRunResult result, string? runnerError = null)
	{
		var output = new List<string>();
		if (!string.IsNullOrWhiteSpace(runnerError))
			output.Add($"RUNNER ERROR: {runnerError}");

		output.Add($"{result.Total} tests: {result.Passed} passed, {result.Failed} failed");
		output.Add(result.FailingTestNames.Count == 0
			? "FAILED TESTS: (none)"
			: $"FAILED TESTS: {string.Join(", ", result.FailingTestNames)}");
		output.AddRange(result.Lines);

		File.WriteAllLines(ResultFile, output);
	}
}
