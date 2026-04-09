using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class BillingSyncIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly FakeGeminiHandler _geminiHandler = new();
    private HttpClient _client = null!;
    private HttpClient _rawClient = null!;

    public BillingSyncIntegrationTests(FreakLeteApiFactory factory)
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
    //  POST /api/billing/googleplay/sync
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sync_Unauthenticated_Returns401()
    {
        var response = await _rawClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            purchaseToken = "tok_test",
            purchaseState = 0
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sync_Subscription_CreatesRecord()
    {
        var token = $"tok_{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            orderId = "order_123",
            purchaseState = 0,
            isAcknowledged = false
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("verification_failed", body.GetProperty("state").GetString());
        Assert.Equal("subscription", body.GetProperty("kind").GetString());

        // Verify record exists in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var record = await db.BillingPurchases.FirstOrDefaultAsync(p => p.PurchaseToken == token);
        Assert.NotNull(record);
        Assert.Equal("subscription", record.Kind);
        Assert.Equal("android", record.Platform);
        Assert.Equal("verification_failed", record.State);
    }

    [Fact]
    public async Task Sync_Donation_CreatesRecord()
    {
        var token = $"tok_{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "donate_5",
            purchaseToken = token,
            orderId = "order_d5",
            purchaseState = 0
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("donation", body.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Sync_Idempotent_SameTokenTwice()
    {
        var token = $"tok_{Guid.NewGuid():N}";
        var payload = new
        {
            productId = "freaklete_premium",
            basePlanId = "annual",
            purchaseToken = token,
            orderId = "order_idempotent",
            purchaseState = 0
        };

        var r1 = await _client.PostAsJsonAsync("/api/billing/googleplay/sync", payload);
        r1.EnsureSuccessStatusCode();

        var r2 = await _client.PostAsJsonAsync("/api/billing/googleplay/sync", payload);
        r2.EnsureSuccessStatusCode();

        // Should still have only one record
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.BillingPurchases.CountAsync(p => p.PurchaseToken == token);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Sync_ExpiredSubscription_StateReflected()
    {
        var verifiedClient = await CreateAuthenticatedBillingClientWithSubscriptionResponseAsync(
            BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(-1), cancelled: false));

        var token = $"tok_{Guid.NewGuid():N}";

        var response = await verifiedClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            purchaseState = 0
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("expired", body.GetProperty("state").GetString());
    }

    [Fact]
    public async Task Sync_Subscription_PremiumStatus()
    {
        var verifiedClient = await CreateAuthenticatedBillingClientWithSubscriptionResponseAsync(
            BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(30), cancelled: false));

        var token = $"tok_{Guid.NewGuid():N}";
        await verifiedClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            purchaseState = 0
        });

        // Check billing status now returns premium
        var statusResponse = await verifiedClient.GetAsync("/api/billing/status");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("premium", status.GetProperty("plan").GetString());
        Assert.True(status.GetProperty("isPremiumActive").GetBoolean());
    }

    [Fact]
    public async Task Restore_Subscription_EntitlementRefreshed()
    {
        var verifiedClient = await CreateAuthenticatedBillingClientWithSubscriptionResponseAsync(
            BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(365), cancelled: false));

        // Simulate: user has a subscription record that was synced
        var token = $"tok_{Guid.NewGuid():N}";
        await verifiedClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "annual",
            purchaseToken = token,
            purchaseState = 0
        });

        var statusResponse = await verifiedClient.GetAsync("/api/billing/status");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(status.GetProperty("isPremiumActive").GetBoolean());
    }

    [Fact]
    public async Task Sync_VerifiedCancelledSubscription_StateReflected()
    {
        var verifiedClient = await CreateAuthenticatedBillingClientWithSubscriptionResponseAsync(
            BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(30), cancelled: true));

        var token = $"tok_{Guid.NewGuid():N}";
        var response = await verifiedClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            purchaseState = 0
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("cancelled", body.GetProperty("state").GetString());

        var statusResponse = await verifiedClient.GetAsync("/api/billing/status");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("free", status.GetProperty("plan").GetString());
        Assert.False(status.GetProperty("isPremiumActive").GetBoolean());
    }

    // ════════════════════════════════════════════════════════════════
    //  PHASE 2 REGRESSION: TOOL-CALL BASED QUOTA ACCOUNTING
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToolCallPromotion_CreateProgramTool_CountsAsProgramGenerate()
    {
        // Setup: Gemini returns a function call for create_program, then text
        var toolArgs = JsonSerializer.Deserialize<JsonElement>(
            """{"name":"Test Program","goal":"Strength","daysPerWeek":4,"weeks":[]}""");
        _geminiHandler.SetupToolCallThenText("create_program", toolArgs, "Your program is ready!");

        // Send a general message — classified as general_chat
        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Help me with training",
            intent = "general_chat"
        });
        // This should succeed (general_chat has 3/day, program_generate has 1/month)
        r1.EnsureSuccessStatusCode();

        // Now check: the usage should have been recorded as program_generate
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync();
        var usage = await db.AiUsageRecords
            .Where(r => r.UserId == user.Id && !r.WasBlocked)
            .OrderByDescending(r => r.OccurredAtUtc)
            .FirstAsync();

        Assert.Equal("program_generate", usage.Intent);
    }

    // ════════════════════════════════════════════════════════════════
    //  PHASE 2 REGRESSION: NUTRITION NEXT-AVAILABLE CORRECTNESS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NutritionNextAvailable_ReportsEarliestInWindow()
    {
        _geminiHandler.SetupTextResponse("Eat well");

        // Use up nutrition guidance
        var r1 = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Beslenme önerisi ver",
            intent = "nutrition_guidance"
        });
        r1.EnsureSuccessStatusCode();

        // Check billing status: nextAvailable should be ~14 days from now
        var statusResponse = await _client.GetAsync("/api/billing/status");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();

        var nextAvailable = status.GetProperty("nutritionGuidanceNextAvailableAtUtc");
        Assert.NotEqual(JsonValueKind.Null, nextAvailable.ValueKind);

        var nextDate = DateTime.Parse(nextAvailable.GetString()!);
        var expectedMin = DateTime.UtcNow.AddDays(13);
        var expectedMax = DateTime.UtcNow.AddDays(15);
        Assert.True(nextDate >= expectedMin && nextDate <= expectedMax,
            $"Expected next available ~14 days from now, got {nextDate}");
    }

    [Fact]
    public async Task NutritionNextAvailable_OldRecordsIgnored()
    {
        // Insert an old nutrition usage record (20 days ago, outside window)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync();
            db.AiUsageRecords.Add(new AiUsageRecord
            {
                UserId = user.Id,
                Intent = "nutrition_guidance",
                OccurredAtUtc = DateTime.UtcNow.AddDays(-20),
                WasBlocked = false,
                PlanAtTime = "free"
            });
            await db.SaveChangesAsync();
        }

        // Billing status should show no next-available (old record is outside window)
        var statusResponse = await _client.GetAsync("/api/billing/status");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();

        var nextAvailable = status.GetProperty("nutritionGuidanceNextAvailableAtUtc");
        Assert.Equal(JsonValueKind.Null, nextAvailable.ValueKind);
    }

    private async Task<HttpClient> CreateAuthenticatedBillingClientWithSubscriptionResponseAsync(string subscriptionVerifyJson)
    {
        var childFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("GooglePlay:PackageName", "com.freaklete.test");
            builder.UseSetting("GooglePlay:ServiceAccountJsonBase64", CreateFakeServiceAccountJsonBase64());

            builder.ConfigureServices(services =>
            {
                services.AddHttpClient<GeminiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => _geminiHandler);

                services.AddHttpClient<GooglePlayVerificationService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeGooglePlayHttpMessageHandler(subscriptionVerifyJson));
            });
        });

        var verifiedClient = childFactory.CreateClient();
        var auth = await AuthTestHelper.RegisterAsync(verifiedClient);
        AuthTestHelper.Authenticate(verifiedClient, auth.Token);
        return verifiedClient;
    }

    private static string BuildSubscriptionV2Json(DateTime expiryUtc, bool cancelled)
    {
        var start = DateTime.UtcNow.AddDays(-7).ToString("O");
        var expiry = expiryUtc.ToString("O");

        return cancelled
            ? $$"""
              {
                "startTime": "{{start}}",
                "acknowledgementState": "ACKNOWLEDGED",
                "lineItems": [{ "expiryTime": "{{expiry}}" }],
                "canceledStateContext": { "userInitiatedCancellation": {} }
              }
              """
            : $$"""
              {
                "startTime": "{{start}}",
                "acknowledgementState": "ACKNOWLEDGED",
                "lineItems": [{ "expiryTime": "{{expiry}}" }]
              }
              """;
    }

    private static string CreateFakeServiceAccountJsonBase64()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        var pem = $"-----BEGIN PRIVATE KEY-----\n{InsertLineBreaks(pkcs8)}\n-----END PRIVATE KEY-----";

        var json = JsonSerializer.Serialize(new
        {
            client_email = "freaklete-tests@example.iam.gserviceaccount.com",
            private_key = pem,
            token_uri = "https://oauth2.googleapis.com/token"
        });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string InsertLineBreaks(string text)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i += 64)
        {
            var chunkLength = Math.Min(64, text.Length - i);
            sb.Append(text, i, chunkLength);
            if (i + chunkLength < text.Length)
                sb.Append('\n');
        }

        return sb.ToString();
    }

    private sealed class FakeGooglePlayHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _subscriptionVerifyJson;

        public FakeGooglePlayHttpMessageHandler(string subscriptionVerifyJson)
        {
            _subscriptionVerifyJson = subscriptionVerifyJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;

            if (request.Method == HttpMethod.Post && url.Contains("oauth2.googleapis.com/token", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse("""{ "access_token": "fake-access-token" }"""));
            }

            if (request.Method == HttpMethod.Get && url.Contains("/purchases/subscriptionsv2/tokens/", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateJsonResponse(_subscriptionVerifyJson));
            }

            if (request.Method == HttpMethod.Post && url.Contains(":acknowledge", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage CreateJsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
