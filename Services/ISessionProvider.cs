namespace FreakLete.Services;

/// <summary>
/// Session abstraction providing login state and sign-out operations.
/// Decouples UI pages from MAUI Preferences and SecureStorage implementation details.
/// </summary>
public interface ISessionProvider
{
	bool IsLoggedIn();
	void SignOut();
}
