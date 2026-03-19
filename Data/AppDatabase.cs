using GymTracker.Models;
using SQLite;

namespace GymTracker.Data;

public class AppDatabase
{
	private SQLiteAsyncConnection? _database;

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
	}

	public async Task<int> CreateUserAsync(User user)
	{
		await InitAsync();
		return await _database!.InsertAsync(user);
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

	public async Task<int> SaveWorkoutAsync(Workout workout, List<ExerciseEntry> exercises)
	{
		await InitAsync();
		await _database!.InsertAsync(workout);

		foreach (ExerciseEntry exercise in exercises)
		{
			exercise.WorkoutId = workout.Id;
			await _database.InsertAsync(exercise);
		}

		return workout.Id;
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
		DateTime selectedDate = date.Date;

		return await _database!.Table<Workout>()
			.Where(workout => workout.UserId == userId && workout.WorkoutDate == selectedDate)
			.OrderByDescending(workout => workout.WorkoutDate)
			.ToListAsync();
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
}
