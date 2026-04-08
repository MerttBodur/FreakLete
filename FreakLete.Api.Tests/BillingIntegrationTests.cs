using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class BillingIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly FakeGeminiHandler _geminiHandler = new();
    private HttpClient _client = null!;
    private HttpClient _rawClient = null!;

    public BillingIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

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

        var auth = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, auth.Token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ════════════════════════════════════════════════════════════════
    //  GET /api/billing/status
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BillingStatus_Unauthenticated_Returns401()
    {
        var response = await _rawClient.GetAsync("/api/billing/status");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BillingStatus_FreeUser_ReturnsFreePlan()
    {
        var response = await _client.GetAsync("/api/billing/status");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("free", body.GetProperty("plan").GetString());
        Assert.False(body.GetProperty("isPremiumActive").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("subscriptionEndsAtUtc").ValueKind);
        Assert.Equal(3, body.GetProperty("generalChatRemainingToday").GetInt32());
        Assert.Equal(1, body.GetProperty("programGenerateRemainingThisMonth").GetInt32());
        Assert.Equal(1, body.GetProperty("programAnalyzeRemainingThisMonth").GetInt32());
    }

    [Fact]
    public async Task BillingStatus_PremiumUser_ReturnsPremiumPlan()
    {
        await InsertPremiumSubscriptionAsync();

        var response = await _client.GetAsync("/api/billing/status");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("premium", body.GetProperty("plan").GetString());
        Assert.True(body.GetProperty("isPremiumActive").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("subscriptionEndsAtUtc").ValueKind);
        Assert.Equal(150, body.GetProperty("generalChatRemainingToday").GetInt32());
        Assert.Equal(60, body.GetProperty("programGenerateRemainingThisMonth").GetInt32());
        Assert.Equal(120, body.GetProperty("programAnalyzeRemainingThisMonth").GetInt32());
    }

    [Fact]
    public async Task BillingStatus_ExpiredSubscription_ReturnsFree()
    {
        await InsertExpiredSubscriptionAsync();

        var response = await _client.GetAsync("/api/billing/status");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("free", body.GetProperty("plan").GetString());
        Assert.False(body.GetProperty("isPremiumActive").GetBoolean());
    }

    // ════════════════════════════════════════════════════════════════
    //  FREE QUOTA EXHAUSTION PER BUCKET
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FreeUser_GeneralChat_BlockedAfter3()
    {
        _geminiHandler.SetupTextResponse("OK");

        // 3 successful chats
        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
            {
                message = "Hello coach",
                intent = "general_chat"
            });
            r.EnsureSuccessStatusCode();
        }

        // 4th should be blocked
        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello again",
            intent = "general_chat"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("general_chat", body.GetProperty("intent").GetString());
        Assert.Equal("free", body.GetProperty("plan").GetString());
    }

    [Fact]
    public async Task FreeUser_ProgramGenerate_BlockedAfter1()
    {
        _geminiHandler.SetupTextResponse("Here's your program");

        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Create a program for me",
            intent = "program_generate"
        });
        r1.EnsureSuccessStatusCode();

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Create another program",
            intent = "program_generate"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task FreeUser_ProgramAnalyze_BlockedAfter1()
    {
        _geminiHandler.SetupTextResponse("Analysis result");

        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Analyze my program",
            intent = "program_analyze"
        });
        r1.EnsureSuccessStatusCode();

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Analyze again",
            intent = "program_analyze"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task FreeUser_NutritionGuidance_BlockedAfter1()
    {
        _geminiHandler.SetupTextResponse("Eat protein");

        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "What should I eat?",
            intent = "nutrition_guidance"
        });
        r1.EnsureSuccessStatusCode();

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "More nutrition advice",
            intent = "nutrition_guidance"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  PROGRAM_VIEW COUNTS AS GENERAL_CHAT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProgramView_MapsToGeneralChat_CountsAgainstChatQuota()
    {
        _geminiHandler.SetupTextResponse("Here's your program view");

        // Use 3 general_chat via program_view intent
        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
            {
                message = "Show me my program",
                intent = "program_view"
            });
            r.EnsureSuccessStatusCode();
        }

        // 4th program_view should be blocked (maps to general_chat, limit=3)
        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Show program again",
            intent = "program_view"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  PREMIUM HIDDEN CAP ENFORCEMENT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PremiumUser_GeneralChat_AllowsUp150_BlocksAt151()
    {
        await InsertPremiumSubscriptionAsync();
        _geminiHandler.SetupTextResponse("OK");

        // Seed 150 usage records directly to avoid 150 HTTP round-trips
        await SeedUsageRecordsAsync("general_chat", 150, "premium");

        // 151st should be blocked
        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello",
            intent = "general_chat"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("premium", body.GetProperty("plan").GetString());
    }

    [Fact]
    public async Task PremiumUser_ProgramGenerate_DailyCapAt8()
    {
        await InsertPremiumSubscriptionAsync();
        _geminiHandler.SetupTextResponse("Program");

        await SeedUsageRecordsAsync("program_generate", 8, "premium");

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Create program",
            intent = "program_generate"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  FALLBACK CLASSIFIER
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FallbackClassifier_ProgramGenerateKeyword_Blocked()
    {
        _geminiHandler.SetupTextResponse("Done");

        // First use up the free program_generate quota
        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Create a 4-week training program for me",
            // no intent — classifier should detect program_generate
        });
        r1.EnsureSuccessStatusCode();

        // Second should be blocked by classifier detecting program_generate
        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Write a new weekly program for hypertrophy",
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task FallbackClassifier_NutritionKeyword_Blocked()
    {
        _geminiHandler.SetupTextResponse("Eat well");

        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "What's a good meal plan for bulking?",
        });
        r1.EnsureSuccessStatusCode();

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "How many calories should I eat for nutrition?",
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task FallbackClassifier_GeneralMessage_CountsAsGeneralChat()
    {
        _geminiHandler.SetupTextResponse("Reply");

        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
            {
                message = "How do I improve my deadlift form?",
            });
            r.EnsureSuccessStatusCode();
        }

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Any tips for recovery?",
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  EXPIRED ENTITLEMENT -> FREE FALLBACK
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExpiredSubscription_FallsBackToFreeLimits()
    {
        await InsertExpiredSubscriptionAsync();
        _geminiHandler.SetupTextResponse("OK");

        // Should have free limits (3 general_chat/day)
        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
            {
                message = "Hello",
                intent = "general_chat"
            });
            r.EnsureSuccessStatusCode();
        }

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello again",
            intent = "general_chat"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("free", body.GetProperty("plan").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  QUOTA 429 RESPONSE STRUCTURE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task QuotaDenied_ResponseContainsMetadata()
    {
        _geminiHandler.SetupTextResponse("OK");

        // Exhaust general_chat
        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
            {
                message = "Hello",
                intent = "general_chat"
            });
            r.EnsureSuccessStatusCode();
        }

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello",
            intent = "general_chat"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("message", out _));
        Assert.True(body.TryGetProperty("intent", out _));
        Assert.True(body.TryGetProperty("plan", out _));
        Assert.True(body.TryGetProperty("window", out _));
        Assert.True(body.TryGetProperty("limit", out _));
        Assert.True(body.TryGetProperty("used", out _));
        Assert.True(body.TryGetProperty("resetsAtUtc", out _));
    }

    [Fact]
    public async Task QuotaDenied_Turkish_ReturnsLocalizedMessage()
    {
        _geminiHandler.SetupTextResponse("OK");

        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
            {
                message = "Merhaba koç",
                intent = "general_chat"
            });
            r.EnsureSuccessStatusCode();
        }

        var blocked = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Merhaba koç, bana yardım et",
            intent = "general_chat"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        var msg = body.GetProperty("message").GetString()!;
        Assert.Contains("limitine", msg, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  EXISTING FREAKAI BEHAVIOR PRESERVED
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_WithinQuota_StillReturnsReply()
    {
        _geminiHandler.SetupTextResponse("Test coaching reply");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "How should I train today?"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Test coaching reply", body.GetProperty("reply").GetString());
    }

    [Fact]
    public async Task Chat_WithIntentField_StillWorks()
    {
        _geminiHandler.SetupTextResponse("Here's your analysis");

        var response = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Analyze my training",
            intent = "program_analyze"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Here's your analysis", body.GetProperty("reply").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════

    private async Task InsertPremiumSubscriptionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync();
        db.BillingPurchases.Add(new BillingPurchase
        {
            UserId = user.Id,
            Platform = "android",
            Kind = "subscription",
            ProductId = "freaklete_premium",
            BasePlanId = "monthly",
            PurchaseToken = $"tok_{Guid.NewGuid():N}",
            OrderId = $"ord_{Guid.NewGuid():N}",
            State = "active",
            EntitlementStartsAtUtc = DateTime.UtcNow.AddDays(-10),
            EntitlementEndsAtUtc = DateTime.UtcNow.AddDays(20)
        });
        await db.SaveChangesAsync();
    }

    private async Task InsertExpiredSubscriptionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync();
        db.BillingPurchases.Add(new BillingPurchase
        {
            UserId = user.Id,
            Platform = "android",
            Kind = "subscription",
            ProductId = "freaklete_premium",
            BasePlanId = "monthly",
            PurchaseToken = $"tok_{Guid.NewGuid():N}",
            OrderId = $"ord_{Guid.NewGuid():N}",
            State = "active",
            EntitlementStartsAtUtc = DateTime.UtcNow.AddDays(-40),
            EntitlementEndsAtUtc = DateTime.UtcNow.AddDays(-10) // expired
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedUsageRecordsAsync(string intent, int count, string plan)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync();
        for (int i = 0; i < count; i++)
        {
            db.AiUsageRecords.Add(new AiUsageRecord
            {
                UserId = user.Id,
                Intent = intent,
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-i),
                WasBlocked = false,
                PlanAtTime = plan
            });
        }
        await db.SaveChangesAsync();
    }
}
