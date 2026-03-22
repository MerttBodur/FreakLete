namespace FreakLete.Services;

public class UserSession
{
	private const string CurrentUserIdKey = "current_user_id";
	private const string TokenKey = "auth_token";
	private const string UserEmailKey = "user_email";
	private const string UserFirstNameKey = "user_first_name";

	public int? GetCurrentUserId()
	{
		int userId = Preferences.Default.Get(CurrentUserIdKey, 0);
		return userId > 0 ? userId : null;
	}

	public bool IsLoggedIn()
	{
		return GetCurrentUserId().HasValue && !string.IsNullOrEmpty(GetToken());
	}

	public void SignIn(int userId, string token, string email = "", string firstName = "")
	{
		Preferences.Default.Set(CurrentUserIdKey, userId);
		Preferences.Default.Set(TokenKey, token);
		if (!string.IsNullOrEmpty(email))
			Preferences.Default.Set(UserEmailKey, email);
		if (!string.IsNullOrEmpty(firstName))
			Preferences.Default.Set(UserFirstNameKey, firstName);
	}

	// Backward compat: local-only sign in (no token)
	public void SignIn(int userId)
	{
		Preferences.Default.Set(CurrentUserIdKey, userId);
	}

	public string? GetToken()
	{
		var token = Preferences.Default.Get(TokenKey, string.Empty);
		return string.IsNullOrEmpty(token) ? null : token;
	}

	public string GetEmail() => Preferences.Default.Get(UserEmailKey, string.Empty);
	public string GetFirstName() => Preferences.Default.Get(UserFirstNameKey, string.Empty);

	public void SignOut()
	{
		Preferences.Default.Remove(CurrentUserIdKey);
		Preferences.Default.Remove(TokenKey);
		Preferences.Default.Remove(UserEmailKey);
		Preferences.Default.Remove(UserFirstNameKey);
	}
}
