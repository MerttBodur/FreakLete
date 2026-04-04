using FreakLete.Services;

namespace FreakLete.Core.Tests;

/// <summary>
/// Tests the FlattenSessions logic from SessionPickerHelper.
/// Duplicated here because the helper references MAUI Page type
/// which is unavailable in the test project.
/// </summary>
public class SessionPickerHelperTests
{
	private static List<FlatSession> FlattenSessions(TrainingProgramResponse program)
	{
		var options = new List<FlatSession>();
		var weeks = program.Weeks ?? [];
		foreach (var week in weeks.OrderBy(w => w.WeekNumber))
		{
			var sessions = week.Sessions ?? [];
			foreach (var session in sessions.OrderBy(s => s.DayNumber))
			{
				string label = !string.IsNullOrWhiteSpace(session.SessionName)
					? $"Week {week.WeekNumber} - {session.SessionName}"
					: $"Week {week.WeekNumber} - Day {session.DayNumber}";

				if (!string.IsNullOrWhiteSpace(session.Focus))
					label += $" ({session.Focus})";

				options.Add(new FlatSession
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

	private sealed class FlatSession
	{
		public int WeekNumber { get; init; }
		public int DayNumber { get; init; }
		public string DisplayName { get; init; } = "";
		public ProgramSessionResponse Session { get; init; } = null!;
	}

	// ═══════════════════════════════════════════════════════
	//  Empty / null weeks
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void FlattenSessions_EmptyWeeks_ReturnsEmpty()
	{
		var program = new TrainingProgramResponse { Weeks = [] };
		Assert.Empty(FlattenSessions(program));
	}

	[Fact]
	public void FlattenSessions_NullWeeks_ReturnsEmpty()
	{
		var program = new TrainingProgramResponse { Weeks = null! };
		Assert.Empty(FlattenSessions(program));
	}

	[Fact]
	public void FlattenSessions_WeekWithNullSessions_ReturnsEmpty()
	{
		var program = new TrainingProgramResponse
		{
			Weeks = [new ProgramWeekResponse { WeekNumber = 1, Sessions = null! }]
		};
		Assert.Empty(FlattenSessions(program));
	}

	// ═══════════════════════════════════════════════════════
	//  Normal cases
	// ═══════════════════════════════════════════════════════

	[Fact]
	public void FlattenSessions_SingleSession_ReturnsSingle()
	{
		var program = new TrainingProgramResponse
		{
			Weeks =
			[
				new ProgramWeekResponse
				{
					WeekNumber = 1,
					Sessions = [new ProgramSessionResponse { DayNumber = 1, SessionName = "Push" }]
				}
			]
		};

		var result = FlattenSessions(program);
		Assert.Single(result);
		Assert.Equal("Week 1 - Push", result[0].DisplayName);
		Assert.Equal(1, result[0].WeekNumber);
		Assert.Equal(1, result[0].DayNumber);
	}

	[Fact]
	public void FlattenSessions_MultipleSessions_OrderedCorrectly()
	{
		var program = new TrainingProgramResponse
		{
			Weeks =
			[
				new ProgramWeekResponse
				{
					WeekNumber = 2,
					Sessions =
					[
						new ProgramSessionResponse { DayNumber = 2, SessionName = "Pull" },
						new ProgramSessionResponse { DayNumber = 1, SessionName = "Push" }
					]
				},
				new ProgramWeekResponse
				{
					WeekNumber = 1,
					Sessions = [new ProgramSessionResponse { DayNumber = 1, SessionName = "Legs" }]
				}
			]
		};

		var result = FlattenSessions(program);
		Assert.Equal(3, result.Count);
		Assert.Equal("Week 1 - Legs", result[0].DisplayName);
		Assert.Equal("Week 2 - Push", result[1].DisplayName);
		Assert.Equal("Week 2 - Pull", result[2].DisplayName);
	}

	[Fact]
	public void FlattenSessions_NoSessionName_FallbackToDayNumber()
	{
		var program = new TrainingProgramResponse
		{
			Weeks =
			[
				new ProgramWeekResponse
				{
					WeekNumber = 1,
					Sessions = [new ProgramSessionResponse { DayNumber = 3, SessionName = "" }]
				}
			]
		};

		var result = FlattenSessions(program);
		Assert.Equal("Week 1 - Day 3", result[0].DisplayName);
	}

	[Fact]
	public void FlattenSessions_WithFocus_AppendedToLabel()
	{
		var program = new TrainingProgramResponse
		{
			Weeks =
			[
				new ProgramWeekResponse
				{
					WeekNumber = 1,
					Sessions =
					[
						new ProgramSessionResponse { DayNumber = 1, SessionName = "Upper", Focus = "Hypertrophy" }
					]
				}
			]
		};

		var result = FlattenSessions(program);
		Assert.Equal("Week 1 - Upper (Hypertrophy)", result[0].DisplayName);
	}

	[Fact]
	public void FlattenSessions_SessionObjectPreserved()
	{
		var session = new ProgramSessionResponse
		{
			DayNumber = 1,
			SessionName = "Test",
			Exercises = [new ProgramExerciseResponse { ExerciseName = "Squat", Sets = 5 }]
		};

		var program = new TrainingProgramResponse
		{
			Weeks = [new ProgramWeekResponse { WeekNumber = 1, Sessions = [session] }]
		};

		var result = FlattenSessions(program);
		Assert.Same(session, result[0].Session);
	}
}
