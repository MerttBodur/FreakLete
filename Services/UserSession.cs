namespace GymTracker.Services;

public class UserSession
{
	private const string CurrentUserIdKey = "current_user_id";

	public int? GetCurrentUserId()
	{
		int userId = Preferences.Default.Get(CurrentUserIdKey, 0);
		return userId > 0 ? userId : null;
	}

	public bool IsLoggedIn()
	{
		return GetCurrentUserId().HasValue;
	}

	public void SignIn(int userId)
	{
		Preferences.Default.Set(CurrentUserIdKey, userId);
	}

	public void SignOut()
	{
		Preferences.Default.Remove(CurrentUserIdKey);
	}
}
