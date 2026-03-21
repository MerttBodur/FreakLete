namespace GymTracker.Data;

internal static class AppDataDirectoryProvider
{
	public static string GetDatabasePath(string fileName)
	{
		return Path.Combine(FileSystem.AppDataDirectory, fileName);
	}
}
