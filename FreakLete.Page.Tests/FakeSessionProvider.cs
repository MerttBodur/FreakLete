using FreakLete.Services;

namespace FreakLete.Page.Tests;

internal class FakeSessionProvider : ISessionProvider
{
	public bool LoggedIn { get; set; } = true;
	public bool SignedOut { get; private set; }

	public bool IsLoggedIn() => LoggedIn;

	public void SignOut()
	{
		LoggedIn = false;
		SignedOut = true;
	}
}
