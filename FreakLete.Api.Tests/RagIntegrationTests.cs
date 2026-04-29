using System.Net.Http.Json;
using FreakLete.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class RagIntegrationTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private readonly FakeGeminiHandler _geminiHandler = new();
    private HttpClient _client = null!;

    public RagIntegrationTests(FreakLeteApiFactory factory) => _factory = factory;

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

        _client = childFactory.CreateClient();
        var auth = await AuthTestHelper.RegisterAsync(_client);
        AuthTestHelper.Authenticate(_client, auth.Token);

        var profile = await _client.PutAsJsonAsync("/api/Auth/profile/athlete", new
        {
            firstName = "Test",
            lastName = "User",
            sportName = "Football",
            position = "WR",
            gymExperienceLevel = "Intermediate",
            primaryTrainingGoal = "Athletic Performance",
            secondaryTrainingGoal = "Muscle Gain",
            availableEquipment = "Commercial Gym"
        });
        profile.EnsureSuccessStatusCode();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Chat_GeneralChat_SystemPromptContainsUserContextBlock()
    {
        _geminiHandler.SetupTextResponse("Reply");

        var resp = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello",
            intent = FreakAiUsageIntent.GeneralChat
        });

        resp.EnsureSuccessStatusCode();
        Assert.True(_geminiHandler.VerifySystemPromptContains("USER CONTEXT"));
        Assert.True(_geminiHandler.VerifySystemPromptContains("Football"));
    }

    [Fact]
    public async Task Chat_ProgramGenerate_SystemPromptIncludesGoalsAndEquipment()
    {
        _geminiHandler.SetupTextResponse("Reply");

        var resp = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Build me a 4-day program",
            intent = FreakAiUsageIntent.ProgramGenerate
        });

        resp.EnsureSuccessStatusCode();
        Assert.True(_geminiHandler.VerifySystemPromptContains("Athletic Performance"));
        Assert.True(_geminiHandler.VerifySystemPromptContains("Commercial Gym"));
    }

    [Fact]
    public async Task Chat_StaticPromptCorePresentEvenWithContext()
    {
        _geminiHandler.SetupTextResponse("Reply");

        var resp = await _client.PostAsJsonAsync("/api/FreakAi/chat", new
        {
            message = "Hello",
            intent = FreakAiUsageIntent.GeneralChat
        });

        resp.EnsureSuccessStatusCode();
        _geminiHandler.AssertSystemPromptIncludesCoreProductRule();
    }
}
