using System.Text.Json;

namespace GymTracker;

public static class WorkoutStorage
{
	private const string StorageKey = "workout_records_v1";

	public static List<WorkoutRecord> Load()
	{
		string? json = Preferences.Default.Get(StorageKey, string.Empty);
		if (string.IsNullOrWhiteSpace(json))
		{
			return new List<WorkoutRecord>();
		}

		try
		{
			return JsonSerializer.Deserialize<List<WorkoutRecord>>(json) ?? new List<WorkoutRecord>();
		}
		catch
		{
			return new List<WorkoutRecord>();
		}
	}

	public static void Save(List<WorkoutRecord> workouts)
	{
		string json = JsonSerializer.Serialize(workouts);
		Preferences.Default.Set(StorageKey, json);
	}
}

public sealed class WorkoutRecord
{
	public DateTime Date { get; set; }
	public string WorkoutName { get; set; } = string.Empty;
	public List<ExerciseRecord> Exercises { get; set; } = new();
}

public sealed class ExerciseRecord
{
	public string ExerciseName { get; set; } = string.Empty;
	public int SetCount { get; set; }
	public int RepCount { get; set; }
	public int? RestSeconds { get; set; }
}
