namespace FreakLete.Services;

/// <summary>
/// Central configuration for the mobile app.
/// Set <see cref="ApiBaseUrl"/> before any API call to override the
/// default development URLs.  When null the compile-time defaults
/// (localhost / 10.0.2.2) are used.
/// </summary>
public static class AppConfig
{
	/// <summary>
	/// Production API base URL (e.g. "https://api.freaklete.com").
	/// Leave null for local development.
	/// </summary>
	public static string? ApiBaseUrl { get; set; }
}
