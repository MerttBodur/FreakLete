namespace FreakLete.Services;

public class UserSession
{
	private const string CurrentUserIdKey = "current_user_id";
	private const string TokenKey = "auth_token";
	private const string UserEmailKey = "user_email";
	private const string UserFirstNameKey = "user_first_name";

	private string? _cachedToken;
	private Task<string?>? _tokenLoadTask;
	private bool _migrated;

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

		_cachedToken = token;
		_tokenLoadTask = null;
		SecureStorage.Default.SetAsync(TokenKey, token).ConfigureAwait(false);

		if (!string.IsNullOrEmpty(email))
			Preferences.Default.Set(UserEmailKey, email);
		if (!string.IsNullOrEmpty(firstName))
			Preferences.Default.Set(UserFirstNameKey, firstName);
	}

	public string? GetToken()
	{
		if (_cachedToken is not null)
			return _cachedToken;

		MigrateTokenFromPreferences();
		if (_cachedToken is not null)
			return _cachedToken;

		try
		{
			string? token = LoadTokenAsync().GetAwaiter().GetResult();
			if (!string.IsNullOrEmpty(token))
			{
				_cachedToken = token;
				return token;
			}
		}
		catch
		{
			// SecureStorage can throw on some platforms; fall back gracefully
			_tokenLoadTask = null;
		}

		return null;
	}

	/// <summary>
	/// Warms the token cache without blocking. SecureStorage (Android Keystore)
	/// can take hundreds of ms on first access — call this at startup so later
	/// synchronous GetToken() calls hit the cache instead of blocking the UI thread.
	/// </summary>
	public async Task PreloadTokenAsync()
	{
		if (_cachedToken is not null)
			return;

		MigrateTokenFromPreferences();
		if (_cachedToken is not null)
			return;

		try
		{
			string? token = await LoadTokenAsync().ConfigureAwait(false);
			if (!string.IsNullOrEmpty(token))
				_cachedToken = token;
		}
		catch
		{
			// Best-effort warm-up; GetToken() retries synchronously if needed
			_tokenLoadTask = null;
		}
	}

	private Task<string?> LoadTokenAsync()
	{
		_tokenLoadTask ??= SecureStorage.Default.GetAsync(TokenKey);
		return _tokenLoadTask;
	}

	private void MigrateTokenFromPreferences()
	{
		if (_migrated)
			return;

		_migrated = true;

		string legacyToken = Preferences.Default.Get(TokenKey, string.Empty);
		if (string.IsNullOrEmpty(legacyToken))
			return;

		try
		{
			_cachedToken = legacyToken;
			SecureStorage.Default.SetAsync(TokenKey, legacyToken).ConfigureAwait(false);
		}
		catch
		{
			// Best-effort migration
		}

		Preferences.Default.Remove(TokenKey);
	}

	public string GetEmail() => Preferences.Default.Get(UserEmailKey, string.Empty);
	public string GetFirstName() => Preferences.Default.Get(UserFirstNameKey, string.Empty);

	public void SignOut()
	{
		_cachedToken = null;
		_tokenLoadTask = null;
		Preferences.Default.Remove(CurrentUserIdKey);
		SecureStorage.Default.Remove(TokenKey);
		Preferences.Default.Remove(UserEmailKey);
		Preferences.Default.Remove(UserFirstNameKey);
	}
}
