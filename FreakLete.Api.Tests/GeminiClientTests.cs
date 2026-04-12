using System.Net;
using FreakLete.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FreakLete.Api.Tests;

/// <summary>
/// Unit tests for GeminiClient log sanitization and response parsing.
/// Uses FakeGeminiHandler — no real HTTP calls.
/// </summary>
public class GeminiClientTests
{
    private static GeminiClient BuildClient(FakeGeminiHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = new GeminiOptions
        {
            ApiKey = "fake-key",
            Model = "gemini-2.5-flash-lite"
        };
        return new GeminiClient(httpClient, options, NullLogger<GeminiClient>.Instance);
    }

    private static GeminiRequest SimpleRequest() => new()
    {
        Contents = [new GeminiContent { Role = "user", Parts = [new GeminiPart { Text = "Hello" }] }]
    };

    // ── Error body not in exception message ──────────────────────────

    [Fact]
    public async Task ApiError_ExceptionMessage_DoesNotContainProviderBody()
    {
        const string sensitiveBody = "{\"error\":{\"code\":429,\"message\":\"You have exceeded your quota\"}}";
        var handler = new FakeGeminiHandler();
        handler.SetupHttpError(HttpStatusCode.TooManyRequests, sensitiveBody);

        var client = BuildClient(handler);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateContentAsync(SimpleRequest()));

        Assert.DoesNotContain(sensitiveBody, ex.Message);
        Assert.DoesNotContain("quota", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiError_ExceptionMessage_ContainsStatusCode()
    {
        var handler = new FakeGeminiHandler();
        handler.SetupHttpError(HttpStatusCode.InternalServerError, "some internal error detail");

        var client = BuildClient(handler);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateContentAsync(SimpleRequest()));

        // Status code is safe metadata; must be present (rendered as name or numeric)
        Assert.True(
            ex.Message.Contains("500") || ex.Message.Contains("InternalServerError"),
            $"Expected status code in message but got: {ex.Message}");
    }

    // ── Successful response parsing unchanged ────────────────────────

    [Fact]
    public async Task SuccessResponse_ParsesCorrectly()
    {
        var handler = new FakeGeminiHandler();
        handler.SetupTextResponse("Parsed successfully");

        var client = BuildClient(handler);
        var result = await client.GenerateContentAsync(SimpleRequest());

        Assert.NotNull(result.Candidates);
        Assert.Single(result.Candidates!);
        var text = result.Candidates![0].Content?.Parts[0].Text;
        Assert.Equal("Parsed successfully", text);
    }

    [Fact]
    public async Task SuccessResponse_ReturnsNonNullCandidates()
    {
        var handler = new FakeGeminiHandler();
        handler.SetupTextResponse("ok");

        var client = BuildClient(handler);
        var result = await client.GenerateContentAsync(SimpleRequest());

        Assert.NotNull(result);
        Assert.NotNull(result.Candidates);
    }
}
