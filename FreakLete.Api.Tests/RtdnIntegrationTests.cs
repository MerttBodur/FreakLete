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
public class RtdnIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private const string TestSecret = "test-rtdn-secret-value-1234";

    public RtdnIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Secret validation ──────────────────────────────────────────

    [Fact]
    public async Task Rtdn_MissingSecretConfig_Returns503()
    {
        // No RTDN secret configured — endpoint must reject
        var client = _factory.CreateClient();
        var response = await SendRtdnAsync(client, null, BuildPubSubBody("sub-001", BuildRtdnJson("tok_x", "freaklete_premium", 4)));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Rtdn_WrongSecret_Returns401()
    {
        var client = CreateRtdnClient();
        var response = await client.PostAsJsonAsync("/api/billing/googleplay/rtdn",
            BuildPubSubBody("msg-wrong", BuildRtdnJson("tok_x", "freaklete_premium", 4)));

        // Override header with wrong secret
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/billing/googleplay/rtdn")
        {
            Content = JsonContent.Create(BuildPubSubBody("msg-wrong", BuildRtdnJson("tok_x", "freaklete_premium", 4)))
        };
        req.Headers.Add("X-FreakLete-RTDN-Secret", "wrong-secret-value");
        var wrongClient = _factory.WithWebHostBuilder(b =>
            b.UseSetting("GooglePlay:RealTimeDeveloperNotificationSecret", TestSecret))
            .CreateClient();
        var r = await wrongClient.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Rtdn_MalformedBody_Returns400()
    {
        var client = CreateRtdnClient();
        // data is not valid base64 JSON
        var body = new
        {
            message = new { data = "!!!not-valid-base64!!!", messageId = "msg-malformed" },
            subscription = "sub"
        };
        var response = await SendRtdnAsync(client, TestSecret, body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rtdn_OneTimeProductNotification_Returns200Ignored()
    {
        var client = CreateRtdnClient();
        var rtdnJson = BuildOneTimeProductRtdnJson("tok_otp", "donate_5", 1);
        var response = await SendRtdnAsync(client, TestSecret, BuildPubSubBody("msg-otp", rtdnJson));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ignored", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Rtdn_UnknownPurchaseToken_Returns200Ignored()
    {
        var client = CreateRtdnClient();
        var rtdnJson = BuildRtdnJson($"tok_unknown_{Guid.NewGuid():N}", "freaklete_premium", 4);
        var response = await SendRtdnAsync(client, TestSecret, BuildPubSubBody($"msg-unk-{Guid.NewGuid():N}", rtdnJson));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ignored", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Rtdn_PackageNameMismatch_Returns400()
    {
        var client = CreateRtdnClient();
        var rtdnJson = BuildRtdnJson("tok_x", "freaklete_premium", 4, packageName: "com.evil.app");
        var response = await SendRtdnAsync(client, TestSecret, BuildPubSubBody("msg-pkg", rtdnJson));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rtdn_DuplicateMessageId_IsIdempotent()
    {
        var client = CreateRtdnClient();
        var messageId = $"msg-dup-{Guid.NewGuid():N}";
        var token = $"tok_dup_{Guid.NewGuid():N}";
        var rtdnJson = BuildRtdnJson(token, "freaklete_premium", 4);

        var r1 = await SendRtdnAsync(client, TestSecret, BuildPubSubBody(messageId, rtdnJson));
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var b1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ignored", b1.GetProperty("status").GetString()); // unknown token

        var r2 = await SendRtdnAsync(client, TestSecret, BuildPubSubBody(messageId, rtdnJson));
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var b2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("duplicate", b2.GetProperty("status").GetString());

        // Only one event recorded
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.GooglePlayRtdnEvents.CountAsync(e => e.MessageId == messageId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Rtdn_KnownToken_VerifyActive_UpdatesStateActive()
    {
        var (client, userClient) = await CreateRtdnClientWithVerifiedSubscription(
            BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(30), cancelled: false));

        // First create a purchase record via sync
        var token = $"tok_rtdn_active_{Guid.NewGuid():N}";
        var syncResponse = await userClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            purchaseState = 0
        });
        syncResponse.EnsureSuccessStatusCode();

        // Fire RTDN for the same token
        var messageId = $"msg-active-{Guid.NewGuid():N}";
        var rtdnJson = BuildRtdnJson(token, "freaklete_premium", 4); // 4 = SUBSCRIPTION_RENEWED
        var response = await SendRtdnAsync(client, TestSecret, BuildPubSubBody(messageId, rtdnJson));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("processed", body.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var purchase = await db.BillingPurchases.FirstAsync(p => p.PurchaseToken == token);
        Assert.Equal("active", purchase.State);
        Assert.True(purchase.EntitlementEndsAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Rtdn_KnownToken_VerifyExpired_UpdatesStateExpired_NoPremium()
    {
        var (client, userClient) = await CreateRtdnClientWithVerifiedSubscription(
            BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(30), cancelled: false),
            rtdnVerifyJson: BuildSubscriptionV2Json(DateTime.UtcNow.AddDays(-1), cancelled: false));

        // Sync first with active response
        var token = $"tok_rtdn_expired_{Guid.NewGuid():N}";
        var syncResponse = await userClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            purchaseState = 0
        });
        syncResponse.EnsureSuccessStatusCode();

        // Fire RTDN — verify will return expired response
        var messageId = $"msg-expired-{Guid.NewGuid():N}";
        var rtdnJson = BuildRtdnJson(token, "freaklete_premium", 13); // 13 = SUBSCRIPTION_EXPIRED
        var response = await SendRtdnAsync(client, TestSecret, BuildPubSubBody(messageId, rtdnJson));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var purchase = await db.BillingPurchases.FirstAsync(p => p.PurchaseToken == token);
        Assert.Equal("expired", purchase.State);

        // Status must show free
        var statusResponse = await userClient.GetAsync("/api/billing/status");
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("free", status.GetProperty("plan").GetString());
    }

    [Fact]
    public async Task Rtdn_KnownToken_VerifyNull_SetsVerificationFailed_NoPremium()
    {
        // No Google Play config on RTDN client — verify returns null
        var client = CreateRtdnClient(); // no GooglePlay service account config

        // Create purchase via a separately configured user client (no verify configured there either)
        var userAuth = await AuthTestHelper.RegisterAsync(_factory.CreateClient());
        var userClient = _factory.CreateClient();
        AuthTestHelper.Authenticate(userClient, userAuth.Token);

        var token = $"tok_rtdn_null_{Guid.NewGuid():N}";
        await userClient.PostAsJsonAsync("/api/billing/googleplay/sync", new
        {
            productId = "freaklete_premium",
            basePlanId = "monthly",
            purchaseToken = token,
            purchaseState = 0
        });

        // Force state to "active" so we can confirm RTDN resets it
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var p = await db.BillingPurchases.FirstAsync(b => b.PurchaseToken == token);
            p.State = "active";
            await db.SaveChangesAsync();
        }

        var messageId = $"msg-null-{Guid.NewGuid():N}";
        var rtdnJson = BuildRtdnJson(token, "freaklete_premium", 13);
        var response = await SendRtdnAsync(client, TestSecret, BuildPubSubBody(messageId, rtdnJson));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var purchase = await db2.BillingPurchases.FirstAsync(p => p.PurchaseToken == token);
        Assert.Equal("verification_failed", purchase.State);

        // Billing status free — verification_failed grants no premium
        var statusResponse = await userClient.GetAsync("/api/billing/status");
        var status = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("free", status.GetProperty("plan").GetString());
    }

    // ── Query param secret ─────────────────────────────────────────

    [Fact]
    public async Task Rtdn_ValidQuerySecret_Returns200Ignored()
    {
        // No header — secret supplied via ?secret= query param (Pub/Sub direct-push MVP)
        var client = CreateRtdnClient();
        var rtdnJson = BuildOneTimeProductRtdnJson($"tok-qs-{Guid.NewGuid():N}", "donate_5", 1);
        var msgId = $"msg-qs-valid-{Guid.NewGuid():N}";
        var response = await SendRtdnWithQuerySecretAsync(client, TestSecret, BuildPubSubBody(msgId, rtdnJson));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ignored", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Rtdn_WrongQuerySecret_Returns401()
    {
        var client = CreateRtdnClient();
        var rtdnJson = BuildOneTimeProductRtdnJson($"tok-qs-bad-{Guid.NewGuid():N}", "donate_5", 1);
        var response = await SendRtdnWithQuerySecretAsync(client, "wrong-secret-value",
            BuildPubSubBody($"msg-qs-bad-{Guid.NewGuid():N}", rtdnJson));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private HttpClient CreateRtdnClient() =>
        _factory.WithWebHostBuilder(b =>
            b.UseSetting("GooglePlay:RealTimeDeveloperNotificationSecret", TestSecret))
        .CreateClient();

    private async Task<(HttpClient rtdnClient, HttpClient userClient)> CreateRtdnClientWithVerifiedSubscription(
        string syncVerifyJson, string? rtdnVerifyJson = null)
    {
        rtdnVerifyJson ??= syncVerifyJson;

        // Shared handler that serves syncVerifyJson for the first call, rtdnVerifyJson for subsequent
        var handler = new DualResponseFakeHandler(syncVerifyJson, rtdnVerifyJson);

        var childFactory = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("GooglePlay:PackageName", "com.mert.freaklete");
            b.UseSetting("GooglePlay:ServiceAccountJsonBase64", CreateFakeServiceAccountJsonBase64());
            b.UseSetting("GooglePlay:RealTimeDeveloperNotificationSecret", TestSecret);
            b.ConfigureServices(services =>
            {
                services.AddHttpClient<GooglePlayVerificationService>()
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
            });
        });

        var userClient = childFactory.CreateClient();
        var auth = await AuthTestHelper.RegisterAsync(userClient);
        AuthTestHelper.Authenticate(userClient, auth.Token);

        var rtdnClient = childFactory.CreateClient();
        return (rtdnClient, userClient);
    }

    private static async Task<HttpResponseMessage> SendRtdnAsync(HttpClient client, string? secret, object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/billing/googleplay/rtdn")
        {
            Content = JsonContent.Create(body)
        };
        if (secret is not null)
            req.Headers.Add("X-FreakLete-RTDN-Secret", secret);
        return await client.SendAsync(req);
    }

    private static async Task<HttpResponseMessage> SendRtdnWithQuerySecretAsync(HttpClient client, string? secret, object body)
    {
        var url = secret is not null
            ? $"/api/billing/googleplay/rtdn?secret={Uri.EscapeDataString(secret)}"
            : "/api/billing/googleplay/rtdn";
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        return await client.SendAsync(req);
    }

    private static object BuildPubSubBody(string messageId, string rtdnJson) => new
    {
        message = new
        {
            data = Convert.ToBase64String(Encoding.UTF8.GetBytes(rtdnJson)),
            messageId,
            publishTime = DateTime.UtcNow.ToString("O")
        },
        subscription = "projects/test/subscriptions/play-rtdn"
    };

    private static string BuildRtdnJson(
        string purchaseToken, string subscriptionId, int notificationType,
        string packageName = "com.mert.freaklete") =>
        $$"""
        {
          "version": "1.0",
          "packageName": "{{packageName}}",
          "eventTimeMillis": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}" ,
          "subscriptionNotification": {
            "version": "1.0",
            "notificationType": {{notificationType}},
            "purchaseToken": "{{purchaseToken}}",
            "subscriptionId": "{{subscriptionId}}"
          }
        }
        """;

    private static string BuildOneTimeProductRtdnJson(
        string purchaseToken, string sku, int notificationType,
        string packageName = "com.mert.freaklete") =>
        $$"""
        {
          "version": "1.0",
          "packageName": "{{packageName}}",
          "eventTimeMillis": "{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}",
          "oneTimeProductNotification": {
            "version": "1.0",
            "notificationType": {{notificationType}},
            "purchaseToken": "{{purchaseToken}}",
            "sku": "{{sku}}"
          }
        }
        """;

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

    /// <summary>
    /// Returns syncJson for the first subscription verify call (used by /sync),
    /// rtdnJson for subsequent calls (used by RTDN handler).
    /// Both paths share the same handler instance within one test.
    /// </summary>
    private sealed class DualResponseFakeHandler : HttpMessageHandler
    {
        private readonly string _syncVerifyJson;
        private readonly string _rtdnVerifyJson;
        private int _subscriptionCallCount;

        public DualResponseFakeHandler(string syncVerifyJson, string rtdnVerifyJson)
        {
            _syncVerifyJson = syncVerifyJson;
            _rtdnVerifyJson = rtdnVerifyJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;

            if (request.Method == HttpMethod.Post &&
                url.Contains("oauth2.googleapis.com/token", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(Json("""{ "access_token": "fake-access-token" }"""));

            if (request.Method == HttpMethod.Get &&
                url.Contains("/purchases/subscriptionsv2/tokens/", StringComparison.OrdinalIgnoreCase))
            {
                var call = System.Threading.Interlocked.Increment(ref _subscriptionCallCount);
                var json = call == 1 ? _syncVerifyJson : _rtdnVerifyJson;
                return Task.FromResult(Json(json));
            }

            if (request.Method == HttpMethod.Post &&
                url.Contains(":acknowledge", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
    }
}
