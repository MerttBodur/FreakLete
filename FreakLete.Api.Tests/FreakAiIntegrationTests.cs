using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FreakLete.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class FreakAiIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly FakeGeminiHandler _geminiHandler = new();
    private HttpClient _client = null!;
    private HttpClient _rawClient = null!; // no auth header

    public FreakAiIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        // Create a child factory that injects the fake Gemini handler
        var childFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<GeminiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => _geminiHandler);
            });
        });

        _rawClient = childFactory.CreateClient();
        _client = childFactory.CreateClient();

        // Register a user and authenticate
        var auth = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, auth.Token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ════════════════════════════════════════════════════════════════
    //  AUTH / ACCESS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_Unauthenticated_Returns401()
    {
        var response = await _rawClient.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_EmptyMessage_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_MessageTooLong_Returns400()
    {
        var longMessage = new string('a', 2001);
        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = longMessage
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_MissingMessageField_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  SUCCESS CONTRACT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_ValidRequest_ReturnsReplyShape()
    {
        _geminiHandler.SetupTextResponse("Test coaching reply");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "How should I train today?"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("reply", out var reply));
        Assert.Equal("Test coaching reply", reply.GetString());
    }

    [Fact]
    public async Task Chat_WithHistory_ReturnsReply()
    {
        _geminiHandler.SetupTextResponse("Based on our conversation, try squats.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "What exercise should I do?",
            history = new[]
            {
                new { role = "user", content = "I want to build leg strength" },
                new { role = "model", content = "Great goal! Let me help." }
            }
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Based on our conversation, try squats.", body.GetProperty("reply").GetString());
    }

    [Fact]
    public async Task Chat_ToolCallHappyPath_ReturnsReplyAfterToolExecution()
    {
        // Simulate: Gemini calls get_user_profile, gets result, then returns text
        _geminiHandler.SetupToolCallThenText(
            "get_user_profile", null,
            "Based on your profile, here's my recommendation.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Analyze my profile"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Based on your profile, here's my recommendation.", body.GetProperty("reply").GetString());
    }

    [Fact]
    public async Task Chat_MaxLength2000_Succeeds()
    {
        var exactMax = new string('a', 2000);
        _geminiHandler.SetupTextResponse("Got your message.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = exactMax
        });

        response.EnsureSuccessStatusCode();
    }

    // ════════════════════════════════════════════════════════════════
    //  ERROR MAPPING
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_GeminiApiError_Returns502()
    {
        _geminiHandler.SetupHttpError(HttpStatusCode.InternalServerError, "Gemini server error");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("message", out var msg));
        Assert.False(string.IsNullOrWhiteSpace(msg.GetString()));
    }

    [Fact]
    public async Task Chat_GeminiApiError_ReturnsLocalizedMessage_English()
    {
        _geminiHandler.SetupHttpError(HttpStatusCode.InternalServerError);

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello coach"
        });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var msg = body.GetProperty("message").GetString()!;
        // English fallback error message
        Assert.Contains("error", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_GeminiApiError_ReturnsLocalizedMessage_Turkish()
    {
        _geminiHandler.SetupHttpError(HttpStatusCode.InternalServerError);

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            // Turkish message triggers Turkish error response
            message = "Merhaba, benim antrenman programım nasıl olmalı?"
        });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var msg = body.GetProperty("message").GetString()!;
        // Turkish localized error
        Assert.Contains("tekrar", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_NetworkError_Returns503()
    {
        _geminiHandler.SetupNetworkError();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Chat_NetworkError_ReturnsLocalizedMessage()
    {
        _geminiHandler.SetupNetworkError();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var msg = body.GetProperty("message").GetString()!;
        Assert.Contains("connection", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_Timeout_Returns504()
    {
        _geminiHandler.SetupTimeout();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Chat_Timeout_ReturnsLocalizedMessage_Turkish()
    {
        _geminiHandler.SetupTimeout();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Benim için bir program oluştur"
        });

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var msg = body.GetProperty("message").GetString()!;
        Assert.Contains("zaman", msg, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  ORCHESTRATOR BEHAVIOR
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_EmptyCandidate_ReturnsGracefulError()
    {
        _geminiHandler.SetupEmptyCandidate();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        // Orchestrator catches empty candidate and returns a localized error as 200
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(reply));
        // English empty_response fallback
        Assert.Contains("try again", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_BlankText_ReturnsNoDataError()
    {
        _geminiHandler.SetupBlankTextResponse();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(reply));
        // Returns no_data localized message
        Assert.Contains("data", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_MaxToolRoundsExceeded_ReturnsTooComplexError()
    {
        // Gemini always returns a function call — orchestrator hits MaxToolRounds (5)
        _geminiHandler.SetupInfiniteToolCalls("get_user_profile");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;
        // too_complex localized message
        Assert.Contains("simpler", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_MaxToolRoundsExceeded_Turkish_ReturnsLocalizedError()
    {
        _geminiHandler.SetupInfiniteToolCalls("get_user_profile");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Benim antrenman programım için bir analiz yap"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;
        // Turkish too_complex message
        Assert.Contains("basit", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_EmptyCandidate_Turkish_ReturnsLocalizedError()
    {
        _geminiHandler.SetupEmptyCandidate();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Merhaba, bana yardım eder misin?"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;
        // Turkish empty_response message
        Assert.Contains("tekrar", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_ToolCallWithArgs_ExecutesCorrectly()
    {
        // Simulate Gemini calling calculate_one_rm with specific args
        var args = JsonSerializer.Deserialize<JsonElement>("""{"weightKg": 100, "reps": 5}""");
        _geminiHandler.SetupToolCallThenText(
            "calculate_one_rm", args,
            "Your estimated 1RM is 116.7 kg.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "I benched 100kg for 5 reps, what's my 1RM?"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Your estimated 1RM is 116.7 kg.", body.GetProperty("reply").GetString());
    }

    [Fact]
    public async Task Chat_UnknownTool_HandledGracefully()
    {
        // Gemini requests a tool that doesn't exist — toolExecutor returns error JSON,
        // then Gemini responds with text
        _geminiHandler.SetupToolCallThenText(
            "nonexistent_tool", null,
            "I couldn't find that information.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Do something weird"
        });

        // Should still succeed — unknown tool returns error JSON, Gemini handles it
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("reply").GetString()));
    }

    [Fact]
    public async Task Chat_NullHistory_Succeeds()
    {
        _geminiHandler.SetupTextResponse("Fresh conversation!");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Start a new chat",
            history = (object?)null
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Fresh conversation!", body.GetProperty("reply").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  MULTI-TURN & CHAINED TOOL SCENARIOS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_MultiTurn_AccumulatedHistoryPreserved()
    {
        // Simulate a 3-turn conversation where the client sends accumulated history
        _geminiHandler.SetupTextResponse("Turn 1 reply");
        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "What should I train today?"
        });
        r1.EnsureSuccessStatusCode();
        var b1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Turn 1 reply", b1.GetProperty("reply").GetString());

        // Turn 2: client sends history from turn 1
        _geminiHandler.SetupTextResponse("Turn 2 reply with context");
        var r2 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Can you add more detail?",
            history = new[]
            {
                new { role = "user", content = "What should I train today?" },
                new { role = "model", content = "Turn 1 reply" }
            }
        });
        r2.EnsureSuccessStatusCode();
        var b2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Turn 2 reply with context", b2.GetProperty("reply").GetString());

        // Turn 3: client sends full accumulated history
        _geminiHandler.SetupTextResponse("Turn 3 final recommendation");
        var r3 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Thanks, let's finalize the plan",
            history = new[]
            {
                new { role = "user", content = "What should I train today?" },
                new { role = "model", content = "Turn 1 reply" },
                new { role = "user", content = "Can you add more detail?" },
                new { role = "model", content = "Turn 2 reply with context" }
            }
        });
        r3.EnsureSuccessStatusCode();
        var b3 = await r3.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Turn 3 final recommendation", b3.GetProperty("reply").GetString());
    }

    [Fact]
    public async Task Chat_ChainedToolCalls_BothToolsExecuteBeforeFinalReply()
    {
        // Gemini calls get_user_profile first, then calculate_one_rm, then returns text
        var calcArgs = JsonSerializer.Deserialize<JsonElement>("""{"weightKg": 80, "reps": 8}""");
        _geminiHandler.SetupTwoToolCallsThenText(
            "get_user_profile", null,
            "calculate_one_rm", calcArgs,
            "Based on your profile and 1RM of 98.5kg, here's your plan.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Analyze my strength and create a plan"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Based on your profile and 1RM of 98.5kg, here's your plan.",
            body.GetProperty("reply").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  LOCALIZATION CONSISTENCY
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Hello coach", "en")]
    [InlineData("Merhaba, nasıl antrenman yapmalıyım?", "tr")]
    public async Task Chat_LanguageDetection_ReturnsReply(string message, string expectedLang)
    {
        _geminiHandler.SetupTextResponse($"Reply in {expectedLang}");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"Reply in {expectedLang}", body.GetProperty("reply").GetString());
    }

    [Fact]
    public async Task GetLocalizedError_English_AiError()
    {
        var msg = FreakAiOrchestrator.GetLocalizedError("Hello", "ai_error");
        Assert.Contains("error", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLocalizedError_Turkish_AiError()
    {
        var msg = FreakAiOrchestrator.GetLocalizedError("Merhaba, benim antrenman programım", "ai_error");
        Assert.Contains("tekrar", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLocalizedError_English_Timeout()
    {
        var msg = FreakAiOrchestrator.GetLocalizedError("Hello", "timeout");
        Assert.Contains("timed out", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLocalizedError_English_NetworkError()
    {
        var msg = FreakAiOrchestrator.GetLocalizedError("Hello", "network_error");
        Assert.Contains("connection", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLocalizedError_UnknownErrorType_ReturnsFallback()
    {
        var msg = FreakAiOrchestrator.GetLocalizedError("Hello", "something_unknown");
        Assert.False(string.IsNullOrWhiteSpace(msg));
    }
}
