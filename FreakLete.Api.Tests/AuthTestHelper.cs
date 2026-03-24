using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

/// <summary>
/// Shared helpers for registering users and attaching JWT tokens in integration tests.
/// </summary>
public static class AuthTestHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public record AuthResult(int UserId, string Email, string FirstName, string Token);

    public static async Task<AuthResult> RegisterAsync(
        HttpClient client,
        string firstName = "Test",
        string lastName = "User",
        string? email = null,
        string password = "TestPassword123!")
    {
        email ??= $"test-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName,
            lastName,
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOpts);
        return body!;
    }

    public static async Task<AuthResult> LoginAsync(
        HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOpts);
        return body!;
    }

    /// <summary>
    /// Sets the Authorization: Bearer header on the client.
    /// </summary>
    public static void Authenticate(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}
