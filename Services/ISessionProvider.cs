namespace FreakLete.Services;

/// <summary>
/// Session operations used by ProfilePage. Enables page-behavior testing
/// without MAUI Preferences/SecureStorage.
/// </summary>
public interface ISessionProvider
{
	bool IsLoggedIn();
	void SignOut();
}
