namespace FreakLete.Helpers;

using FreakLete.Services;

public static class SessionPickerHelper
{
	public sealed class SessionOption
	{
		public int WeekNumber { get; init; }
		public int DayNumber { get; init; }
		public string DisplayName { get; init; } = "";
		public ProgramSessionResponse Session { get; init; } = null!;
	}

	public static List<SessionOption> FlattenSessions(TrainingProgramResponse program)
	{
		var options = new List<SessionOption>();
		foreach (var week in program.Weeks.OrderBy(w => w.WeekNumber))
		{
			foreach (var session in week.Sessions.OrderBy(s => s.DayNumber))
			{
				string label = !string.IsNullOrWhiteSpace(session.SessionName)
					? $"Week {week.WeekNumber} - {session.SessionName}"
					: $"Week {week.WeekNumber} - Day {session.DayNumber}";

				if (!string.IsNullOrWhiteSpace(session.Focus))
					label += $" ({session.Focus})";

				options.Add(new SessionOption
				{
					WeekNumber = week.WeekNumber,
					DayNumber = session.DayNumber,
					DisplayName = label,
					Session = session
				});
			}
		}
		return options;
	}

	/// <summary>
	/// Shows a session picker if multiple sessions exist, auto-picks if only one.
	/// Returns null if cancelled or no sessions available.
	/// </summary>
	public static async Task<SessionOption?> PickSessionAsync(Page page, TrainingProgramResponse program)
	{
		var options = FlattenSessions(program);
		if (options.Count == 0) return null;
		if (options.Count == 1) return options[0];

		var labels = options.Select(o => o.DisplayName).ToArray();
		string? chosen = await page.DisplayActionSheet("Seans Seç", "İptal", null, labels);
		if (string.IsNullOrEmpty(chosen) || chosen == "İptal") return null;

		return options.FirstOrDefault(o => o.DisplayName == chosen);
	}
}
