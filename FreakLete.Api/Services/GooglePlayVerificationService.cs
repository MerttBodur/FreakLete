using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FreakLete.Api.Services;

/// <summary>
/// Verifies Google Play purchases via the Android Publisher API.
/// Uses service account for authentication.
/// </summary>
public class GooglePlayVerificationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GooglePlayVerificationService> _logger;

    public GooglePlayVerificationService(
        HttpClient http,
        IConfiguration config,
        ILogger<GooglePlayVerificationService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrEmpty(_config["GooglePlay:PackageName"]) &&
        !string.IsNullOrEmpty(_config["GooglePlay:ServiceAccountJsonBase64"]);

    /// <summary>
    /// Verify a subscription purchase with Google Play.
    /// Returns normalized state and entitlement window.
    /// </summary>
    public async Task<SubscriptionVerifyResult?> VerifySubscriptionAsync(
        string purchaseToken, string productId, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var packageName = _config["GooglePlay:PackageName"]!;
        var accessToken = await GetAccessTokenAsync(ct);
        if (accessToken is null) return null;

        try
        {
            var url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/subscriptionsv2/tokens/{purchaseToken}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Play subscription verify failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            var state = NormalizeSubscriptionState(json);
            var startTime = json.TryGetProperty("startTime", out var st)
                ? DateTime.Parse(st.GetString()!).ToUniversalTime()
                : DateTime.UtcNow;
            var expiryTime = json.TryGetProperty("lineItems", out var items)
                && items.EnumerateArray().FirstOrDefault()
                    .TryGetProperty("expiryTime", out var et)
                ? DateTime.Parse(et.GetString()!).ToUniversalTime()
                : DateTime.UtcNow.AddMonths(1);
            var acknowledged = json.TryGetProperty("acknowledgementState", out var ack)
                && ack.GetString() == "ACKNOWLEDGED";

            return new SubscriptionVerifyResult
            {
                State = state,
                EntitlementStartsAtUtc = startTime,
                EntitlementEndsAtUtc = expiryTime,
                IsAcknowledged = acknowledged,
                NeedsAcknowledge = !acknowledged && state == "active"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying subscription for product {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Acknowledge a subscription if not yet acknowledged.
    /// </summary>
    public async Task<bool> AcknowledgeSubscriptionAsync(
        string purchaseToken, string productId, CancellationToken ct = default)
    {
        if (!IsConfigured) return false;

        var packageName = _config["GooglePlay:PackageName"]!;
        var accessToken = await GetAccessTokenAsync(ct);
        if (accessToken is null) return false;

        try
        {
            var url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/subscriptionsv2/tokens/{purchaseToken}:acknowledge";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging subscription");
            return false;
        }
    }

    /// <summary>
    /// Verify a one-time (donate) product purchase.
    /// </summary>
    public async Task<DonationVerifyResult?> VerifyDonationAsync(
        string purchaseToken, string productId, CancellationToken ct = default)
    {
        if (!IsConfigured) return null;

        var packageName = _config["GooglePlay:PackageName"]!;
        var accessToken = await GetAccessTokenAsync(ct);
        if (accessToken is null) return null;

        try
        {
            var url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/{productId}/tokens/{purchaseToken}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Play donation verify failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            var purchaseState = json.TryGetProperty("purchaseState", out var ps) ? ps.GetInt32() : -1;
            var consumptionState = json.TryGetProperty("consumptionState", out var cs) ? cs.GetInt32() : 0;

            return new DonationVerifyResult
            {
                IsPurchased = purchaseState == 0, // 0 = purchased
                IsConsumed = consumptionState == 1,
                NeedsConsume = purchaseState == 0 && consumptionState == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying donation {Product}", productId);
            return null;
        }
    }

    /// <summary>
    /// Consume a one-time product (for donations).
    /// </summary>
    public async Task<bool> ConsumeDonationAsync(
        string purchaseToken, string productId, CancellationToken ct = default)
    {
        if (!IsConfigured) return false;

        var packageName = _config["GooglePlay:PackageName"]!;
        var accessToken = await GetAccessTokenAsync(ct);
        if (accessToken is null) return false;

        try
        {
            var url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/{productId}/tokens/{purchaseToken}:consume";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming donation");
            return false;
        }
    }

    // ── Private helpers ──────────────────────────────────────

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var base64 = _config["GooglePlay:ServiceAccountJsonBase64"];
        if (string.IsNullOrEmpty(base64)) return null;

        try
        {
            var jsonBytes = Convert.FromBase64String(base64);
            var json = JsonDocument.Parse(jsonBytes);

            var clientEmail = json.RootElement.GetProperty("client_email").GetString();
            var privateKeyPem = json.RootElement.GetProperty("private_key").GetString();
            var tokenUri = json.RootElement.GetProperty("token_uri").GetString()
                ?? "https://oauth2.googleapis.com/token";

            if (clientEmail is null || privateKeyPem is null)
                return null;

            // Build JWT for service account
            var now = DateTimeOffset.UtcNow;
            var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
                new { alg = "RS256", typ = "JWT" }));

            var claims = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
            {
                iss = clientEmail,
                scope = "https://www.googleapis.com/auth/androidpublisher",
                aud = tokenUri,
                iat = now.ToUnixTimeSeconds(),
                exp = now.AddMinutes(30).ToUnixTimeSeconds()
            }));

            var unsignedToken = $"{header}.{claims}";
            var signature = SignRs256(unsignedToken, privateKeyPem);
            var jwt = $"{unsignedToken}.{signature}";

            // Exchange JWT for access token
            var tokenRequest = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", jwt)
            ]);

            var tokenResponse = await _http.PostAsync(tokenUri, tokenRequest, ct);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Google access token: {Status}", tokenResponse.StatusCode);
                return null;
            }

            var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return tokenJson.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Google Play access token");
            return null;
        }
    }

    private static string NormalizeSubscriptionState(JsonElement json)
    {
        if (!json.TryGetProperty("lineItems", out var items))
            return "pending";

        var firstItem = items.EnumerateArray().FirstOrDefault();
        if (firstItem.ValueKind == JsonValueKind.Undefined)
            return "pending";

        if (!firstItem.TryGetProperty("expiryTime", out var expiry))
            return "active";

        var expiryTime = DateTime.Parse(expiry.GetString()!).ToUniversalTime();
        if (expiryTime < DateTime.UtcNow)
            return "expired";

        if (json.TryGetProperty("canceledStateContext", out _))
            return "cancelled";

        return "active";
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string SignRs256(string data, string privateKeyPem)
    {
        var keyData = privateKeyPem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "");

        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(keyData), out _);

        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(data),
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        return Base64UrlEncode(signature);
    }
}

public class SubscriptionVerifyResult
{
    public string State { get; init; } = "pending";
    public DateTime EntitlementStartsAtUtc { get; init; }
    public DateTime EntitlementEndsAtUtc { get; init; }
    public bool IsAcknowledged { get; init; }
    public bool NeedsAcknowledge { get; init; }
}

public class DonationVerifyResult
{
    public bool IsPurchased { get; init; }
    public bool IsConsumed { get; init; }
    public bool NeedsConsume { get; init; }
}
