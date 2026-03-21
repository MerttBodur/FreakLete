using GymTracker.Models;
using SQLite;

namespace GymTracker.Data;

public class AppDatabase
{
	private SQLiteAsyncConnection? _database;

	public async Task EnsureCreatedAsync()
	{
		await InitAsync();
	}

	private async Task InitAsync()
	{
		if (_database is not null)
		{
			return;
		}

		string databasePath = Path.Combine(FileSystem.AppDataDirectory, "gymtracker.db3");
		_database = new SQLiteAsyncConnection(
			databasePath,
			SQLiteOpenFlags.ReadWrite |
			SQLiteOpenFlags.Create |
			SQLiteOpenFlags.SharedCache);

		await _database.CreateTableAsync<User>();
		await _database.CreateTableAsync<Workout>();
		await _database.CreateTableAsync<ExerciseEntry>();
		await _database.CreateTableAsync<PrEntry>();
		await _database.CreateTableAsync<AthleticPerformanceEntry>();
		await _database.CreateTableAsync<MovementGoal>();
		await _database.CreateTableAsync<ProfilePrEntry>();

		await EnsureColumnAsync(nameof(ExerciseEntry), nameof(ExerciseEntry.ExerciseCategory), "TEXT NOT NULL DEFAULT ''");
		await EnsureColumnAsync(nameof(ExerciseEntry), nameof(ExerciseEntry.TrackingMode), $"TEXT NOT NULL DEFAULT '{nameof(ExerciseTrackingMode.Strength)}'");
		await EnsureColumnAsync(nameof(ExerciseEntry), nameof(ExerciseEntry.Metric1Value), "REAL NULL");
		await EnsureColumnAsync(nameof(ExerciseEntry), nameof(ExerciseEntry.Metric1Unit), "TEXT NOT NULL DEFAULT ''");
		await EnsureColumnAsync(nameof(ExerciseEntry), nameof(ExerciseEntry.Metric2Value), "REAL NULL");
		await EnsureColumnAsync(nameof(ExerciseEntry), nameof(ExerciseEntry.Metric2Unit), "TEXT NOT NULL DEFAULT ''");

		await EnsureColumnAsync(nameof(AthleticPerformanceEntry), nameof(AthleticPerformanceEntry.MovementCategory), "TEXT NOT NULL DEFAULT ''");
		await EnsureColumnAsync(nameof(AthleticPerformanceEntry), nameof(AthleticPerformanceEntry.SecondaryValue), "REAL NULL");
		await EnsureColumnAsync(nameof(AthleticPerformanceEntry), nameof(AthleticPerformanceEntry.SecondaryUnit), "TEXT NOT NULL DEFAULT ''");
	}

	private async Task EnsureColumnAsync(string tableName, string columnName, string columnDefinition)
	{
		var columns = await _database!.GetTableInfoAsync(tableName);
		bool exists = columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
		if (!exists)
		{
			await _database.ExecuteAsync($"ALTER TABLE [{tableName}] ADD COLUMN [{columnName}] {columnDefinition}");
		}
	}

	public async Task<int> CreateUserAsync(User user)
	{
		await InitAsync();
		return await _database!.InsertAsync(user);
	}

	public async Task<bool> EmailExistsAsync(string email)
	{
		await InitAsync();
		int count = await _database!.Table<User>()
			.Where(user => user.Email == email)
			.CountAsync();

		return count > 0;
	}

	public async Task<User?> GetUserByEmailAsync(string email)
	{
		await InitAsync();
		return await _database!.Table<User>()
			.FirstOrDefaultAsync(user => user.Email == email);
	}

	public async Task<User?> GetUserByIdAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<User>()
			.FirstOrDefaultAsync(user => user.Id == userId);
	}

	public async Task<int> UpdateUserAsync(User user)
	{
		await InitAsync();
		return await _database!.UpdateAsync(user);
	}

	public async Task<int> DeleteUserAsync(int userId)
	{
		await InitAsync();

		List<Workout> workouts = await GetWorkoutsByUserAsync(userId);
		foreach (Workout workout in workouts)
		{
			await DeleteWorkoutAsync(workout.Id);
		}

		List<PrEntry> prEntries = await GetPrEntriesByUserAsync(userId);
		foreach (PrEntry entry in prEntries)
		{
			await _database!.DeleteAsync(entry);
		}

		List<AthleticPerformanceEntry> performanceEntries = await GetAthleticPerformanceEntriesByUserAsync(userId);
		foreach (AthleticPerformanceEntry entry in performanceEntries)
		{
			await _database!.DeleteAsync(entry);
		}

		List<MovementGoal> goals = await GetMovementGoalsByUserAsync(userId);
		foreach (MovementGoal goal in goals)
		{
			await _database!.DeleteAsync(goal);
		}

		List<ProfilePrEntry> profilePrEntries = await GetProfilePrEntriesByUserAsync(userId);
		foreach (ProfilePrEntry entry in profilePrEntries)
		{
			await _database!.DeleteAsync(entry);
		}

		User? user = await GetUserByIdAsync(userId);
		if (user is null)
		{
			return 0;
		}

		return await _database!.DeleteAsync(user);
	}

	public async Task<int> SaveWorkoutAsync(Workout workout, List<ExerciseEntry> exercises)
	{
		await InitAsync();
		workout.WorkoutDate = workout.WorkoutDate.Date;
		await _database!.InsertAsync(workout);

		foreach (ExerciseEntry exercise in exercises)
		{
			exercise.WorkoutId = workout.Id;
			await _database.InsertAsync(exercise);
		}

		return workout.Id;
	}

	public async Task<int> UpdateWorkoutAsync(Workout workout, List<ExerciseEntry> exercises)
	{
		await InitAsync();
		workout.WorkoutDate = workout.WorkoutDate.Date;
		await _database!.UpdateAsync(workout);

		List<ExerciseEntry> existingExercises = await GetExercisesByWorkoutIdAsync(workout.Id);
		foreach (ExerciseEntry exercise in existingExercises)
		{
			await _database.DeleteAsync(exercise);
		}

		foreach (ExerciseEntry exercise in exercises)
		{
			exercise.Id = 0;
			exercise.WorkoutId = workout.Id;
			await _database.InsertAsync(exercise);
		}

		return workout.Id;
	}

	public async Task<Workout?> GetWorkoutByIdAsync(int workoutId)
	{
		await InitAsync();
		return await _database!.Table<Workout>()
			.FirstOrDefaultAsync(workout => workout.Id == workoutId);
	}

	public async Task<List<Workout>> GetWorkoutsByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<Workout>()
			.Where(workout => workout.UserId == userId)
			.OrderByDescending(workout => workout.WorkoutDate)
			.ToListAsync();
	}

	public async Task<List<Workout>> GetWorkoutsByUserAndDateAsync(int userId, DateTime date)
	{
		await InitAsync();
		DateTime start = date.Date;
		DateTime end = start.AddDays(1);

		return await _database!.Table<Workout>()
			.Where(workout => workout.UserId == userId &&
							  workout.WorkoutDate >= start &&
							  workout.WorkoutDate < end)
			.OrderByDescending(workout => workout.WorkoutDate)
			.ToListAsync();
	}

	public async Task<int> GetWorkoutCountByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<Workout>()
			.Where(workout => workout.UserId == userId)
			.CountAsync();
	}

	public async Task<int> DeleteWorkoutAsync(int workoutId)
	{
		await InitAsync();

		List<ExerciseEntry> exercises = await GetExercisesByWorkoutIdAsync(workoutId);
		foreach (ExerciseEntry exercise in exercises)
		{
			await _database!.DeleteAsync(exercise);
		}

		Workout? workout = await GetWorkoutByIdAsync(workoutId);
		if (workout is null)
		{
			return 0;
		}

		return await _database!.DeleteAsync(workout);
	}

	public async Task<List<ExerciseEntry>> GetExercisesByWorkoutIdAsync(int workoutId)
	{
		await InitAsync();
		return await _database!.Table<ExerciseEntry>()
			.Where(exercise => exercise.WorkoutId == workoutId)
			.ToListAsync();
	}

	public async Task<int> SavePrEntryAsync(PrEntry prEntry)
	{
		await InitAsync();
		return await _database!.InsertAsync(prEntry);
	}

	public async Task<List<PrEntry>> GetPrEntriesByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<PrEntry>()
			.Where(entry => entry.UserId == userId)
			.OrderByDescending(entry => entry.CreatedAt)
			.ToListAsync();
	}

	public async Task<int> GetPrCountByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<PrEntry>()
			.Where(entry => entry.UserId == userId)
			.CountAsync();
	}

	public async Task<int> DeletePrEntryAsync(int prEntryId)
	{
		await InitAsync();

		PrEntry? entry = await _database!.Table<PrEntry>()
			.FirstOrDefaultAsync(item => item.Id == prEntryId);

		if (entry is null)
		{
			return 0;
		}

		return await _database.DeleteAsync(entry);
	}

	public async Task<int> UpdatePrEntryAsync(PrEntry prEntry)
	{
		await InitAsync();
		return await _database!.UpdateAsync(prEntry);
	}

	public async Task<int> SaveAthleticPerformanceEntryAsync(AthleticPerformanceEntry entry)
	{
		await InitAsync();
		return await _database!.InsertAsync(entry);
	}

	public async Task<List<AthleticPerformanceEntry>> GetAthleticPerformanceEntriesByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<AthleticPerformanceEntry>()
			.Where(entry => entry.UserId == userId)
			.OrderByDescending(entry => entry.RecordedAt)
			.ToListAsync();
	}

	public async Task<List<AthleticPerformanceEntry>> GetAthleticPerformanceEntriesByUserAndMovementAsync(int userId, string movementName)
	{
		await InitAsync();
		return await _database!.Table<AthleticPerformanceEntry>()
			.Where(entry => entry.UserId == userId && entry.MovementName == movementName)
			.OrderByDescending(entry => entry.RecordedAt)
			.ToListAsync();
	}

	public async Task<int> DeleteAthleticPerformanceEntryAsync(int entryId)
	{
		await InitAsync();

		AthleticPerformanceEntry? entry = await _database!.Table<AthleticPerformanceEntry>()
			.FirstOrDefaultAsync(item => item.Id == entryId);

		if (entry is null)
		{
			return 0;
		}

		return await _database.DeleteAsync(entry);
	}

	public async Task<int> UpdateAthleticPerformanceEntryAsync(AthleticPerformanceEntry entry)
	{
		await InitAsync();
		return await _database!.UpdateAsync(entry);
	}

	public async Task<int> SaveMovementGoalAsync(MovementGoal goal)
	{
		await InitAsync();

		MovementGoal? existingGoal = await _database!.Table<MovementGoal>()
			.FirstOrDefaultAsync(item => item.UserId == goal.UserId && item.MovementName == goal.MovementName);

		if (existingGoal is null)
		{
			return await _database.InsertAsync(goal);
		}

		existingGoal.TargetValue = goal.TargetValue;
		existingGoal.Unit = goal.Unit;
		return await _database.UpdateAsync(existingGoal);
	}

	public async Task<List<MovementGoal>> GetMovementGoalsByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<MovementGoal>()
			.Where(goal => goal.UserId == userId)
			.OrderBy(goal => goal.MovementName)
			.ToListAsync();
	}

	public async Task<MovementGoal?> GetMovementGoalByUserAndMovementAsync(int userId, string movementName)
	{
		await InitAsync();
		return await _database!.Table<MovementGoal>()
			.FirstOrDefaultAsync(goal => goal.UserId == userId && goal.MovementName == movementName);
	}

	public async Task<int> DeleteMovementGoalAsync(int goalId)
	{
		await InitAsync();

		MovementGoal? goal = await _database!.Table<MovementGoal>()
			.FirstOrDefaultAsync(item => item.Id == goalId);

		if (goal is null)
		{
			return 0;
		}

		return await _database.DeleteAsync(goal);
	}

	public async Task<int> UpdateMovementGoalAsync(MovementGoal goal)
	{
		await InitAsync();
		return await _database!.UpdateAsync(goal);
	}

	public async Task<int> SaveProfilePrEntryAsync(ProfilePrEntry entry)
	{
		await InitAsync();
		return await _database!.InsertAsync(entry);
	}

	public async Task<List<ProfilePrEntry>> GetProfilePrEntriesByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<ProfilePrEntry>()
			.Where(entry => entry.UserId == userId)
			.OrderByDescending(entry => entry.RecordedAt)
			.ToListAsync();
	}

	public async Task<int> GetProfilePrCountByUserAsync(int userId)
	{
		await InitAsync();
		return await _database!.Table<ProfilePrEntry>()
			.Where(entry => entry.UserId == userId)
			.CountAsync();
	}

	public async Task<int> DeleteProfilePrEntryAsync(int entryId)
	{
		await InitAsync();

		ProfilePrEntry? entry = await _database!.Table<ProfilePrEntry>()
			.FirstOrDefaultAsync(item => item.Id == entryId);

		if (entry is null)
		{
			return 0;
		}

		return await _database.DeleteAsync(entry);
	}

	public async Task<int> UpdateProfilePrEntryAsync(ProfilePrEntry entry)
	{
		await InitAsync();
		return await _database!.UpdateAsync(entry);
	}
}
