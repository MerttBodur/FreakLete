using System.Net;
using System.Net.Http.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Register ───────────────────────────────────────────────────

    [Fact]
    public async Task Register_ReturnsTokenAndUserId()
    {
        var result = await AuthTestHelper.RegisterAsync(_client);

        Assert.True(result.UserId > 0);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("Test", result.FirstName);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await AuthTestHelper.RegisterAsync(_client, email: email);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Dup",
            lastName = "User",
            email,
            password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "User", "a@b.com", "12345678")]   // empty first name — model validation
    [InlineData("Test", "", "a@b.com", "12345678")]    // empty last name
    [InlineData("Test", "User", "not-email", "12345678")] // invalid email
    [InlineData("Test", "User", "a@b.com", "short")]   // password < 8 chars
    public async Task Register_InvalidInput_Returns400(
        string firstName, string lastName, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName,
            lastName,
            email,
            password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Login ──────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        const string password = "TestPassword123!";
        await AuthTestHelper.RegisterAsync(_client, email: email, password: password);

        var result = await AuthTestHelper.LoginAsync(_client, email, password);

        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrong-{Guid.NewGuid():N}@example.com";
        await AuthTestHelper.RegisterAsync(_client, email: email, password: "CorrectPassword1!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonexistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "Whatever123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Unauthorized access ────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/auth/profile", new { firstName = "X" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/auth/account");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Delete account ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccount_RemovesUserAndInvalidatesProfile()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var deleteResponse = await authedClient.DeleteAsync("/api/auth/account");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Profile should now return 404 (user is gone, but token still parses — user lookup fails)
        var profileResponse = await authedClient.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.NotFound, profileResponse.StatusCode);
    }
}
