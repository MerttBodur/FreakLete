using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

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
    public async Task SaveAthleteProfile_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/auth/profile/athlete", new { weightKg = 80.0 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveCoachProfile_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/api/auth/profile/coach", new { trainingDaysPerWeek = 3 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync("/api/auth/account");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Delete account ─────────────────────────────────────────────

    // ── Change Password ────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidRequest_Returns200AndNewPasswordWorks()
    {
        var email = $"cp-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "OldPassword1!";
        const string newPassword = "NewPassword1!";
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: oldPassword);
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email,
            currentPassword = oldPassword,
            newPassword,
            newPasswordRepeat = newPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Old password should fail login
        var oldLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = oldPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        // New password should succeed
        var newLogin = await AuthTestHelper.LoginAsync(_client, email, newPassword);
        Assert.False(string.IsNullOrEmpty(newLogin.Token));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var email = $"cp-wrong-{Guid.NewGuid():N}@example.com";
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: "CorrectPass1!");
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email,
            currentPassword = "WrongPass1!",
            newPassword = "BrandNew1!",
            newPasswordRepeat = "BrandNew1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_EmailMismatch_Returns400()
    {
        var email = $"cp-email-{Guid.NewGuid():N}@example.com";
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: "TestPassword1!");
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email = "wrong@example.com",
            currentPassword = "TestPassword1!",
            newPassword = "NewPassword1!",
            newPasswordRepeat = "NewPassword1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_MismatchedNewPasswords_Returns400()
    {
        var email = $"cp-mismatch-{Guid.NewGuid():N}@example.com";
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: "TestPassword1!");
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email,
            currentPassword = "TestPassword1!",
            newPassword = "NewPassword1!",
            newPasswordRepeat = "DifferentPassword1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_Returns400()
    {
        var email = $"cp-weak-{Guid.NewGuid():N}@example.com";
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: "TestPassword1!");
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email,
            currentPassword = "TestPassword1!",
            newPassword = "short",
            newPasswordRepeat = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", new
        {
            email = "test@example.com",
            currentPassword = "TestPassword1!",
            newPassword = "NewPassword1!",
            newPasswordRepeat = "NewPassword1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Email normalization ────────────────────────────────────────

    [Fact]
    public async Task Register_StoresNormalizedEmail()
    {
        var email = $"UPPER-{Guid.NewGuid():N}@Example.COM";
        var result = await AuthTestHelper.RegisterAsync(_client, email: email);

        Assert.Equal(email.ToLowerInvariant(), result.Email);
    }

    [Fact]
    public async Task Login_WorksWithUpperCaseEmail_WhenStoredNormalized()
    {
        var email = $"mixed-{Guid.NewGuid():N}@example.com";
        await AuthTestHelper.RegisterAsync(_client, email: email, password: "TestPassword123!");

        var result = await AuthTestHelper.LoginAsync(_client, email.ToUpperInvariant(), "TestPassword123!");
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task Register_DuplicateEmail_DifferentCase_Returns409()
    {
        var email = $"case-{Guid.NewGuid():N}@example.com";
        await AuthTestHelper.RegisterAsync(_client, email: email);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Dup",
            lastName = "User",
            email = email.ToUpperInvariant(),
            password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Password policy (register) ─────────────────────────────────

    [Fact]
    public async Task Register_PasswordWithoutUppercase_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = $"pw-{Guid.NewGuid():N}@example.com",
            password = "nouppercase1!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordWithoutSpecialChar_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = $"pw-{Guid.NewGuid():N}@example.com",
            password = "NoSpecialChar1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Delete account ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccount_WrongPassword_Returns400()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client, password: "TestPassword123!");
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
            {
                Content = JsonContent.Create(new { currentPassword = "WrongPassword1!" })
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_NoBody_IsRejected()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var response = await authedClient.DeleteAsync("/api/auth/account");

        // No body: ASP.NET Core returns 415 (no content-type) or 400 (model validation) —
        // either way the request is rejected and the account is not deleted.
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Expected rejection but got {response.StatusCode}");
    }

    [Fact]
    public async Task DeleteAccount_RemovesUserAndInvalidatesProfile()
    {
        const string password = "TestPassword123!";
        var auth = await AuthTestHelper.RegisterAsync(_client, password: password);
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var deleteResponse = await authedClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
            {
                Content = JsonContent.Create(new { currentPassword = password })
            });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // OnTokenValidated finds no user in DB → 401 before controller runs
        var profileResponse = await authedClient.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);
    }

    // ── Rate limiting ──────────────────────────────────────────────
    // Each test spins up an isolated child factory so the in-memory rate
    // limiter state starts fresh and doesn't bleed into other tests.

    [Fact]
    public async Task Login_RateLimit_Returns429AfterThreshold()
    {
        // Use a non-Testing environment so rate limiting is active.
        var rlFactory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var client = rlFactory.CreateClient();

        // login policy: 5 per minute — exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "rl-test@example.com",
                password = "AnyPassword1!"
            });
        }

        // 6th attempt must be rejected
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "rl-test@example.com",
            password = "AnyPassword1!"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Register_RateLimit_Returns429AfterThreshold()
    {
        // register policy: 3 per 10 minutes
        var rlFactory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var client = rlFactory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Test",
                lastName = "User",
                email = $"rl-reg-{Guid.NewGuid():N}@example.com",
                password = "TestPassword123!"
            });
        }

        // 4th attempt must be rejected
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = $"rl-reg-{Guid.NewGuid():N}@example.com",
            password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    // ── Token version / revocation ─────────────────────────────────

    [Fact]
    public async Task OldToken_Returns401_AfterPasswordChange()
    {
        var email = $"tv-old-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "OldPassword1!";
        const string newPassword = "NewPassword1!";

        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: oldPassword);
        var oldClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(oldClient, auth.Token);

        // Verify old token works before change
        var beforeResponse = await oldClient.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);

        // Change password (increments TokenVersion)
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);
        var changeResponse = await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email,
            currentPassword = oldPassword,
            newPassword,
            newPasswordRepeat = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        // Old token now has stale token_version — must be rejected
        var afterResponse = await oldClient.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, afterResponse.StatusCode);
    }

    [Fact]
    public async Task NewLoginToken_Works_AfterPasswordChange()
    {
        var email = $"tv-new-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "OldPassword1!";
        const string newPassword = "NewPassword1!";

        var auth = await AuthTestHelper.RegisterAsync(_client, email: email, password: oldPassword);
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        await authedClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            email,
            currentPassword = oldPassword,
            newPassword,
            newPasswordRepeat = newPassword
        });

        // Fresh login issues a token with the new TokenVersion
        var newAuth = await AuthTestHelper.LoginAsync(_client, email, newPassword);
        var newClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(newClient, newAuth.Token);

        var profileResponse = await newClient.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public async Task Token_Returns401_AfterAccountDeletion()
    {
        const string password = "TestPassword123!";
        var auth = await AuthTestHelper.RegisterAsync(_client, password: password);
        var authedClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(authedClient, auth.Token);

        var deleteResponse = await authedClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, "/api/auth/account")
            {
                Content = JsonContent.Create(new { currentPassword = password })
            });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // User no longer in DB — OnTokenValidated fails with 401
        var profileResponse = await authedClient.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);
    }

    [Fact]
    public async Task TokenWithoutVersionClaim_IsRejected()
    {
        // A JWT missing the token_version claim — OnTokenValidated must reject it
        var token = BuildCustomToken(userId: 999, includeVersionClaim: false, tokenVersion: 0);
        var client = _factory.CreateClient();
        AuthTestHelper.Authenticate(client, token);

        var response = await client.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithMismatchedVersion_IsRejected()
    {
        var email = $"tv-mismatch-{Guid.NewGuid():N}@example.com";
        var auth = await AuthTestHelper.RegisterAsync(_client, email: email);

        // DB has TokenVersion=0; token claims version 99
        var token = BuildCustomToken(userId: auth.UserId, includeVersionClaim: true, tokenVersion: 99);
        var client = _factory.CreateClient();
        AuthTestHelper.Authenticate(client, token);

        var response = await client.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string BuildCustomToken(int userId, bool includeVersionClaim, int tokenVersion)
    {
        const string testKey = "TestJwtKey-AtLeast32Characters-Long-Enough-For-HS256!";
        var signingKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(testKey));

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new(System.Security.Claims.ClaimTypes.Email, "test@example.com"),
            new(System.Security.Claims.ClaimTypes.GivenName, "Test")
        };

        if (includeVersionClaim)
            claims.Add(new System.Security.Claims.Claim("token_version", tokenVersion.ToString()));

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "FreakLete.Api",
            audience: "FreakLete.App",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                signingKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
