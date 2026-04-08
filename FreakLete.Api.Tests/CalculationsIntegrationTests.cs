using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class CalculationsIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CalculationsIntegrationTests(FreakLeteApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> AuthenticateAsync()
    {
        var auth = await AuthTestHelper.RegisterAsync(_client);
        var c = _factory.CreateClient();
        AuthTestHelper.Authenticate(c, auth.Token);
        return c;
    }

    // ════════════════════════════════════════════════════════════════
    //  1RM — AUTH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OneRm_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 5, rir = 2 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  1RM — VALID REQUESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OneRm_ValidRequest_ReturnsExpectedShape()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 5, rir = 2 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        Assert.True(result.TryGetProperty("oneRm", out var oneRm));
        Assert.True(oneRm.GetDouble() > 0);

        Assert.True(result.TryGetProperty("rmTable", out var rmTable));
        var entries = rmTable.EnumerateArray().ToList();
        Assert.Equal(8, entries.Count);
    }

    [Fact]
    public async Task OneRm_CorrectCalculation()
    {
        // Formula: weightKg * (1 + ((reps + rir) / 30.0))
        // 100 * (1 + ((5 + 2) / 30.0)) = 100 * 1.2333... = 123.3
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 5, rir = 2 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        var oneRm = result.GetProperty("oneRm").GetDouble();
        Assert.Equal(123.3, oneRm, 1); // rounded to 1 decimal
    }

    [Fact]
    public async Task OneRm_RmTable_EntriesAreDecreasing()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 5, rir = 0 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);
        var entries = result.GetProperty("rmTable").EnumerateArray().ToList();

        // Each RM entry should have Rm and Weight
        for (int i = 0; i < entries.Count; i++)
        {
            Assert.Equal(i + 1, entries[i].GetProperty("rm").GetInt32());
            Assert.True(entries[i].GetProperty("weight").GetDouble() > 0);
        }

        // Weights should decrease as RM increases
        for (int i = 1; i < entries.Count; i++)
        {
            Assert.True(
                entries[i].GetProperty("weight").GetDouble() < entries[i - 1].GetProperty("weight").GetDouble(),
                $"RM {i + 1} weight should be less than RM {i} weight");
        }
    }

    [Fact]
    public async Task OneRm_1RepWith0RIR_ReturnsInputWeight()
    {
        // 1RM with 1 rep, 0 RIR: 100 * (1 + (1/30)) = 103.3
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 1, rir = 0 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        var oneRm = result.GetProperty("oneRm").GetDouble();
        Assert.Equal(103.3, oneRm, 1);
    }

    // ════════════════════════════════════════════════════════════════
    //  1RM — BOUNDARY VALUES
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OneRm_MaxBoundary_Returns200()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 500, reps = 30, rir = 10 });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task OneRm_MinBoundary_Returns200()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 1, reps = 1, rir = 0 });
        response.EnsureSuccessStatusCode();
    }

    // ════════════════════════════════════════════════════════════════
    //  1RM — INVALID INPUT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task OneRm_ZeroWeight_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 0, reps = 5, rir = 2 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OneRm_ZeroReps_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 0, rir = 2 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OneRm_NegativeRIR_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 5, rir = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OneRm_WeightExceedsMax_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 501, reps = 5, rir = 2 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OneRm_RepsExceedsMax_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 31, rir = 2 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OneRm_RIRExceedsMax_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/one-rm",
            new { weightKg = 100, reps = 5, rir = 11 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  RSI — AUTH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Rsi_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 40.0, groundContactTimeSeconds = 0.2 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  RSI — VALID REQUESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Rsi_ValidRequest_ReturnsExpectedValue()
    {
        // RSI = (jumpHeightCm / 100) / gctSeconds = (40 / 100) / 0.2 = 0.4 / 0.2 = 2.0
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 40.0, groundContactTimeSeconds = 0.2 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var rsi = JsonSerializer.Deserialize<double>(body);

        Assert.Equal(2.0, rsi, 3);
    }

    [Fact]
    public async Task Rsi_HighJump_ShortGct_HighRsi()
    {
        // RSI = (60 / 100) / 0.15 = 4.0
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 60.0, groundContactTimeSeconds = 0.15 });
        response.EnsureSuccessStatusCode();

        var rsi = JsonSerializer.Deserialize<double>(await response.Content.ReadAsStringAsync());
        Assert.Equal(4.0, rsi, 3);
    }

    // ════════════════════════════════════════════════════════════════
    //  RSI — BOUNDARY VALUES
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Rsi_MinBoundary_Returns200()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 0.1, groundContactTimeSeconds = 0.01 });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Rsi_MaxBoundary_Returns200()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 200.0, groundContactTimeSeconds = 5.0 });
        response.EnsureSuccessStatusCode();
    }

    // ════════════════════════════════════════════════════════════════
    //  RSI — INVALID INPUT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Rsi_ZeroHeight_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 0.0, groundContactTimeSeconds = 0.2 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rsi_ZeroGct_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 40.0, groundContactTimeSeconds = 0.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rsi_HeightExceedsMax_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 201.0, groundContactTimeSeconds = 0.2 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rsi_GctExceedsMax_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/rsi",
            new { jumpHeightCm = 40.0, groundContactTimeSeconds = 5.1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  FFMI — AUTH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Ffmi_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 80.0, heightCm = 178.0, bodyFatPercentage = 15.0 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  FFMI — VALID REQUESTS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Ffmi_ValidRequest_ReturnsExpectedShape()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 80.0, heightCm = 178.0, bodyFatPercentage = 15.0 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        Assert.True(result.TryGetProperty("normalizedFfmi", out var nf));
        Assert.True(nf.GetDouble() > 0);
        Assert.True(result.TryGetProperty("rawFfmi", out var rf));
        Assert.True(rf.GetDouble() > 0);
        Assert.True(result.TryGetProperty("leanBodyMassKg", out var lbm));
        Assert.True(lbm.GetDouble() > 0);
    }

    [Fact]
    public async Task Ffmi_CorrectCalculation()
    {
        // LBM = 80 * (1 - 0.15) = 68
        // rawFFMI = 68 / 1.78^2 = 21.46
        // normalizedFFMI = 21.46 + 6.1 * (1.8 - 1.78) = 21.58
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 80.0, heightCm = 178.0, bodyFatPercentage = 15.0 });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

        Assert.Equal(21.58, result.GetProperty("normalizedFfmi").GetDouble(), 1);
        Assert.Equal(21.46, result.GetProperty("rawFfmi").GetDouble(), 1);
        Assert.Equal(68.0, result.GetProperty("leanBodyMassKg").GetDouble(), 1);
    }

    // ════════════════════════════════════════════════════════════════
    //  FFMI — BOUNDARY VALUES
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Ffmi_MinBoundary_Returns200()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 0.1, heightCm = 1.0, bodyFatPercentage = 0.1 });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Ffmi_MaxBoundary_Returns200()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 500.0, heightCm = 300.0, bodyFatPercentage = 99.9 });
        response.EnsureSuccessStatusCode();
    }

    // ════════════════════════════════════════════════════════════════
    //  FFMI — INVALID INPUT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Ffmi_ZeroWeight_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 0.0, heightCm = 178.0, bodyFatPercentage = 15.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ffmi_ZeroHeight_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 80.0, heightCm = 0.0, bodyFatPercentage = 15.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ffmi_BodyFat100_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 80.0, heightCm = 178.0, bodyFatPercentage = 100.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ffmi_NegativeBodyFat_Returns400()
    {
        var client = await AuthenticateAsync();
        var response = await client.PostAsJsonAsync("/api/Calculations/ffmi",
            new { weightKg = 80.0, heightCm = 178.0, bodyFatPercentage = -1.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
