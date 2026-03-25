using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class SportCatalogIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SportCatalogIntegrationTests(FreakLeteApiFactory factory)
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
    //  AUTH
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/SportCatalog");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET ALL
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_ReturnsNonEmptyList()
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        Assert.NotNull(sports);
        Assert.True(sports.Count > 50, $"Expected 50+ sports, got {sports.Count}");
    }

    [Fact]
    public async Task GetAll_ResponseShape_HasExpectedFields()
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        var first = sports![0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("category", out _));
        Assert.True(first.TryGetProperty("hasPositions", out _));
        Assert.True(first.TryGetProperty("positions", out _));
    }

    // ════════════════════════════════════════════════════════════════
    //  KNOWN SPORTS
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("soccer", "Soccer", "Team Sports")]
    [InlineData("basketball", "Basketball", "Team Sports")]
    [InlineData("powerlifting", "Powerlifting", "Strength Sports")]
    [InlineData("sprinting", "Sprinting", "Track and Field")]
    [InlineData("swimming", "Swimming", "Water Sports")]
    [InlineData("tennis", "Tennis", "Racket Sports")]
    [InlineData("boxing", "Boxing", "Combat Sports")]
    public async Task GetAll_ContainsKnownSport(string expectedId, string expectedName, string expectedCategory)
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        var match = sports!.FirstOrDefault(s => s.GetProperty("id").GetString() == expectedId);
        Assert.NotEqual(default, match);
        Assert.Equal(expectedName, match.GetProperty("name").GetString());
        Assert.Equal(expectedCategory, match.GetProperty("category").GetString());
    }

    // ════════════════════════════════════════════════════════════════
    //  SPORTS WITH POSITIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SportWithPositions_HasNonEmptyPositionsArray()
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        var soccer = sports!.First(s => s.GetProperty("id").GetString() == "soccer");
        Assert.True(soccer.GetProperty("hasPositions").GetBoolean());

        var positions = soccer.GetProperty("positions").EnumerateArray().ToList();
        Assert.True(positions.Count > 0, "Soccer should have positions");
        Assert.Contains(positions, p => p.GetString() == "Goalkeeper");
        Assert.Contains(positions, p => p.GetString() == "Striker");
    }

    [Fact]
    public async Task SportWithoutPositions_HasEmptyPositionsArray()
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        var powerlifting = sports!.First(s => s.GetProperty("id").GetString() == "powerlifting");
        Assert.False(powerlifting.GetProperty("hasPositions").GetBoolean());

        var positions = powerlifting.GetProperty("positions").EnumerateArray().ToList();
        Assert.Empty(positions);
    }

    // ════════════════════════════════════════════════════════════════
    //  GET POSITIONS ENDPOINT
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPositions_ValidSport_ReturnsPositions()
    {
        var client = await AuthenticateAsync();
        var positions = await client.GetFromJsonAsync<List<string>>("/api/SportCatalog/basketball/positions", JsonOpts);

        Assert.NotNull(positions);
        Assert.Contains("Point Guard", positions);
        Assert.Contains("Center", positions);
        Assert.Equal(5, positions.Count); // Basketball has exactly 5 positions
    }

    [Fact]
    public async Task GetPositions_SportWithoutPositions_ReturnsEmptyArray()
    {
        var client = await AuthenticateAsync();
        var positions = await client.GetFromJsonAsync<List<string>>("/api/SportCatalog/powerlifting/positions", JsonOpts);

        Assert.NotNull(positions);
        Assert.Empty(positions);
    }

    [Fact]
    public async Task GetPositions_NonExistentSport_Returns404()
    {
        var client = await AuthenticateAsync();
        var response = await client.GetAsync("/api/SportCatalog/nonexistent-sport/positions");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  CATEGORIES
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_CoversExpectedCategories()
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        var categories = sports!.Select(s => s.GetProperty("category").GetString()).Distinct().ToList();

        Assert.Contains("Team Sports", categories);
        Assert.Contains("Racket Sports", categories);
        Assert.Contains("Combat Sports", categories);
        Assert.Contains("Strength Sports", categories);
        Assert.Contains("Track and Field", categories);
        Assert.Contains("Water Sports", categories);
    }

    [Fact]
    public async Task GetAll_AllSportsHaveNonEmptyIdAndName()
    {
        var client = await AuthenticateAsync();
        var sports = await client.GetFromJsonAsync<List<JsonElement>>("/api/SportCatalog", JsonOpts);

        foreach (var sport in sports!)
        {
            var id = sport.GetProperty("id").GetString();
            var name = sport.GetProperty("name").GetString();
            Assert.False(string.IsNullOrWhiteSpace(id), "Sport id should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(name), "Sport name should not be empty");
        }
    }
}
