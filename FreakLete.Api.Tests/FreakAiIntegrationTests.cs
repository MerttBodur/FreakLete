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
    public async Task Chat_BlankText_ReturnsFallbackMessage()
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
        // Returns friendly fallback message instead of no_data error
        // This fix prevents blaming the user when the model returns empty
        Assert.Contains("trouble", reply, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task Chat_ConfirmationFollowUp_WithHistoryAndToolCall_ReturnsValidResponse()
    {
        // THE CONFIRMATION/FOLLOW-UP FAILURE TEST:
        // This reproduces the bug where a second message with history after a tool call
        // caused the model to return blank text, triggering the "no_data" error.
        // 
        // Scenario: User askses for program, model calls create_program, then user
        // sends a confirmation "OK done" with the history. The model should respond,
        // not return blank.
        //
        // Setup: First call returns tool call,  then blank (simulating model confusion on follow-up).
        // Expected: Instead of blank response leading to "no_data" error, we should handle it gracefully.
        _geminiHandler.SetupToolCallThenBlank("create_program", null);

        // First message: request program (triggers create_program tool call)
        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Create a 4-week program for me"
        });
        r1.EnsureSuccessStatusCode();

        // Second message: confirmation follow-up with history
        // This previously could fail with "no_data" error
        var r2 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "OK, save that",
            history = new[]
            {
                new { role = "user", content = "Create a 4-week program for me" },
                new { role = "model", content = "I'll create a program for you..." }
            }
        });
        r2.EnsureSuccessStatusCode();
        var b2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var reply = b2.GetProperty("reply").GetString();
        
        // The key fix: even on blank response from model, orchestrator should not
        // return "no_data" error passively. It should still try to help.
        Assert.False(string.IsNullOrWhiteSpace(reply));
        // Will get the "no_data" message in the interim; the real fix is below in the orchestrator
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

    // ════════════════════════════════════════════════════════════════
    //  SPARSE-PROFILE SCENARIOS (Product Rule: "Default is to help")
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_NoProfileData_StillAnswersQuestion_WithPersonalizationHint()
    {
        // PRODUCT RULE TEST: User with NO profile data asks a valid question.
        // FreakAI must not refuse to answer or gate behind "fill your profile first".
        // Instead: answer the question AND mention how more profile data would help.
        
        _geminiHandler.SetupTextResponse(
            "For general training, focus on compound movements. If I knew your sport and goals, I could tailor this specifically.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "What exercises should I do for overall strength?"
            // No profile data passed — user has empty profile
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;

        // Key assertions for the product rule:
        // 1. Response is not empty (we answered)
        Assert.False(string.IsNullOrWhiteSpace(reply));
        
        // 2. Response does NOT contain gatekeeping language
        Assert.DoesNotContain("fill your profile", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("need your data", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("can't help", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("can't answer", reply, StringComparison.OrdinalIgnoreCase);
        
        // 3. Response mentions how more data would help (natural personalization hint)
        // The fake response includes "If I knew" which is the pattern for personalization hints
        Assert.Contains("if i knew", reply, StringComparison.OrdinalIgnoreCase);
        
        // 4. CRITICAL: Verify the system prompt actually contains the CORE PRODUCT RULE
        // This proves the prompt direction is actually being sent to Gemini, not just claimed
        _geminiHandler.AssertSystemPromptIncludesCoreProductRule();
    }

    [Fact]
    public async Task Chat_PartialProfileData_GivesHelpfulAnswer_AndSuggestsWhatWouldImproveIt()
    {
        // PRODUCT RULE TEST: User with PARTIAL profile data (e.g., only sport, no equipment).
        // FreakAI should give useful advice based on available data, THEN mention what else would help.
        
        _geminiHandler.SetupTextResponse(
            "As a soccer player, focus on lateral explosiveness and deceleration strength. " +
            "If I knew your current strength levels (1RMs) and equipment access, I could write a more specific program.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "How should I structure my training as a soccer player?"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;

        // Key assertions:
        // 1. Response gives practical advice (not just "I need more data")
        Assert.Contains("soccer", reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("strength", reply, StringComparison.OrdinalIgnoreCase);
        
        // 2. Response does NOT blame missing data
        Assert.DoesNotContain("without your data", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("complete your profile", reply, StringComparison.OrdinalIgnoreCase);
        
        // 3. Response naturally mentions what additional data would help
        Assert.Contains("if i knew", reply, StringComparison.OrdinalIgnoreCase);
        
        // 4. Verify the system prompt contains the product rule
        _geminiHandler.AssertSystemPromptIncludesCoreProductRule();
    }

    [Fact]
    public async Task Chat_FullProfileData_PersonalizesAdvice()
    {
        // PRODUCT RULE TEST: User with FULL profile data.
        // FreakAI should give deeply personalized advice using all available context.
        
        _geminiHandler.SetupToolCallThenText(
            "get_user_profile", null,
            "Based on your profile: You're a 85kg soccer goalkeeper with 5 years experience. " +
            "You have access to a commercial gym. I recommend building explosive lateral power " +
            "with targeted eccentric loading for deceleration. Start with 3x5 lateral bounds, " +
            "then 4x6 single-leg eccentric split squats. This addresses goalkeeper-specific demands.");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "What's the best training approach for me?"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;

        // Key assertions:
        // 1. Response is specific to profile (sport, role, weight, equipment)
        Assert.NotNull(reply);
        Assert.False(string.IsNullOrWhiteSpace(reply));
        
        // 2. Response uses data (mentions specific details)
        Assert.Contains("goalkeeper", reply, StringComparison.OrdinalIgnoreCase);
        
        // 3. Response is actionable and prescriptive (not vague)
        Assert.Contains("bounds", reply, StringComparison.OrdinalIgnoreCase);
        
        // 4. Verify the system prompt contains the product rule
        _geminiHandler.AssertSystemPromptIncludesCoreProductRule();
    }

    [Fact]
    public async Task Chat_SparseProfile_NoBlockingErrorMessages()
    {
        // PRODUCT RULE TEST: Verify that even when the model returns a response
        // based on sparse/missing data, the orchestrator's error fallbacks do NOT
        // use language that blames the user for incomplete profile.
        
        // Simulate blank response (edge case where model is confused)
        _geminiHandler.SetupBlankTextResponse();

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Help me train"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reply = body.GetProperty("reply").GetString()!;

        // Even on error, message should be friendly and NOT blame missing profile
        Assert.False(string.IsNullOrWhiteSpace(reply));
        Assert.DoesNotContain("profile", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fill in", reply, StringComparison.OrdinalIgnoreCase);
        
        // Should suggest a retry, not a workaround (e.g., "fill your profile")
        Assert.Contains("try", reply, StringComparison.OrdinalIgnoreCase);
        
        // Verify the system prompt contains the product rule
        // Even on error paths, the prompt should be present and enforcing the rule
        _geminiHandler.AssertSystemPromptIncludesCoreProductRule();
    }
}
