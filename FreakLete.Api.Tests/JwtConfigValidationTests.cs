using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FreakLete.Api.Tests;

/// <summary>
/// Verifies startup-time JWT configuration validation:
/// - placeholder keys are rejected
/// - keys shorter than 32 UTF-8 bytes are rejected
/// - missing issuer/audience are rejected
/// - the existing test key passes
/// </summary>
public class JwtConfigValidationTests
{
    // ── Passing cases ────────────────────────────────────────────────

    [Fact]
    public void ValidTestKey_StartsSuccessfully()
    {
        // This is the key used by FreakLeteApiFactory — must not throw.
        var factory = BuildFactory(key: "TestJwtKey-AtLeast32Characters-Long-Enough-For-HS256!");
        // CreateClient forces the host to start; if startup throws, this throws.
        var ex = Record.Exception(() => factory.CreateClient());
        Assert.Null(ex);
    }

    // ── Placeholder keys ─────────────────────────────────────────────

    [Theory]
    [InlineData("OVERRIDE_VIA_ENVIRONMENT_OR_APPSETTINGS")]
    [InlineData("override_via_environment_or_appsettings")] // case-insensitive
    [InlineData("OVERRIDE_VIA_ENVIRONMENT_VARIABLE")]
    public void PlaceholderKey_ThrowsOnStartup(string placeholder)
    {
        var factory = BuildFactory(key: placeholder);
        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Short key ────────────────────────────────────────────────────

    [Theory]
    [InlineData("short")]          // 5 bytes
    [InlineData("only31byteslong_not_enough_X")]  // 31 bytes
    public void ShortKey_ThrowsOnStartup(string shortKey)
    {
        var factory = BuildFactory(key: shortKey);
        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("32", ex.Message);
    }

    // ── Missing issuer / audience ─────────────────────────────────────

    [Fact]
    public void MissingIssuer_ThrowsOnStartup()
    {
        var factory = BuildFactory(issuer: "");
        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Issuer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingAudience_ThrowsOnStartup()
    {
        var factory = BuildFactory(audience: "");
        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Audience", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Builder helper ───────────────────────────────────────────────

    private static WebApplicationFactory<Program> BuildFactory(
        string key = "TestJwtKey-AtLeast32Characters-Long-Enough-For-HS256!",
        string issuer = "FreakLete.Api",
        string audience = "FreakLete.App")
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Host=localhost;Port=5432;Database=freaklete_test;Username=postgres;Password=4qxabc2p5ower+-");
            builder.UseSetting("Jwt:Key", key);
            builder.UseSetting("Jwt:Issuer", issuer);
            builder.UseSetting("Jwt:Audience", audience);
            builder.UseSetting("Gemini:ApiKey", "fake-gemini-key-for-tests");
            builder.UseSetting("Gemini:Model", "gemini-2.5-flash-lite");
        });
    }
}
