using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FreakLete.Page.Tests;

/// <summary>
/// A simple MAUI page that discovers and runs xunit tests within the
/// WinUI3-hosted process. Results are shown on-screen and written to
/// Debug output for CI/script consumption.
///
/// Run via: dotnet run --project FreakLete.Page.Tests
/// (NOT dotnet test — the tests need a real MAUI/WinUI3 host)
/// </summary>
public class TestRunnerPage : ContentPage
{
	private readonly Label _statusLabel;
	private readonly Label _resultsLabel;

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
	}

	private static readonly string LogFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		"page-test-results.txt");

	private static void Log(string msg)
	{
		try { File.AppendAllText(LogFile, msg + "\n"); } catch { }
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		Log("OnAppearing fired");
		await Task.Delay(100); // Let the UI settle

		try
		{
			await RunAllTests();
		}
		catch (Exception ex)
		{
			_statusLabel.Text = "Test runner crashed";
			_resultsLabel.Text = ex.ToString();
			System.Diagnostics.Debug.WriteLine($"TEST RUNNER CRASH: {ex}");

			var crashFile = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"page-test-results.txt");
			try { File.WriteAllText(crashFile, $"CRASH: {ex}"); } catch { }

			await Task.Delay(500);
			Microsoft.Maui.Controls.Application.Current?.Quit();
		}
	}

	private async Task RunAllTests()
	{
		Log("RunAllTests starting");
		var assembly = typeof(ProfilePageTests).Assembly;
		var testClasses = assembly.GetTypes()
			.Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<FactAttribute>() is not null))
			.ToList();

		int passed = 0, failed = 0;
		var results = new List<string>();

		foreach (var testClass in testClasses)
		{
			var instance = Activator.CreateInstance(testClass)!;
			var testMethods = testClass.GetMethods()
				.Where(m => m.GetCustomAttribute<FactAttribute>() is not null);

			foreach (var method in testMethods)
			{
				string name = $"{testClass.Name}.{method.Name}";
				try
				{
					var result = method.Invoke(instance, null);

					// Handle async test methods
					if (result is Task task)
						await task;

					passed++;
					results.Add($"  PASS: {name}");
					System.Diagnostics.Debug.WriteLine($"PASS: {name}");
				}
				catch (TargetInvocationException tie)
				{
					var ex = tie.InnerException ?? tie;
					failed++;
					results.Add($"  FAIL: {name}");
					results.Add($"        {ex.GetType().Name}: {ex.Message}");
					System.Diagnostics.Debug.WriteLine($"FAIL: {name}");
					System.Diagnostics.Debug.WriteLine($"  {ex}");
				}
				catch (Exception ex)
				{
					failed++;
					results.Add($"  FAIL: {name}");
					results.Add($"        {ex.GetType().Name}: {ex.Message}");
					System.Diagnostics.Debug.WriteLine($"FAIL: {name}");
					System.Diagnostics.Debug.WriteLine($"  {ex}");
				}

				// Update UI after each test
				_statusLabel.Text = $"Running... {passed + failed} tests ({passed} passed, {failed} failed)";
				_resultsLabel.Text = string.Join("\n", results);
				await Task.Delay(1); // Yield to UI thread
			}
		}

		string summary = $"Done: {passed + failed} tests — {passed} passed, {failed} failed";
		_statusLabel.Text = summary;
		_resultsLabel.Text = string.Join("\n", results);
		System.Diagnostics.Debug.WriteLine($"\n{summary}");

		// Write results to file for script/CI consumption
		var resultFile = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			"page-test-results.txt");
		try
		{
			var lines = new List<string> { summary };
			lines.AddRange(results);
			File.WriteAllLines(resultFile, lines);
		}
		catch { /* best effort */ }

		// Auto-exit after tests complete
		await Task.Delay(500);
		Microsoft.Maui.Controls.Application.Current?.Quit();
	}
}
