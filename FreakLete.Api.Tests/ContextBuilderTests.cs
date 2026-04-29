using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using FreakLete.Api.Services.Rag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreakLete.Api.Tests;

[Collection("Api")]
public class ContextBuilderTests : IAsyncLifetime
{
    private readonly FreakLeteApiFactory _factory;
    private int _userId;

    public ContextBuilderTests(FreakLeteApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "ctx@test.com",
            PasswordHash = "x",
            SportName = "Football",
            Position = "WR",
            GymExperienceLevel = "Intermediate",
            PrimaryTrainingGoal = "Athletic Performance",
            SecondaryTrainingGoal = "Muscle Gain",
            AvailableEquipment = "Commercial Gym",
            WeightKg = 82,
            HeightCm = 180,
            DietaryPreference = "Omnivore",
            TrainingDaysPerWeek = 4,
            PreferredSessionDurationMinutes = 75
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        _userId = user.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProgramGenerate_IncludesProfileGoalsEquipment()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();

        var ctx = await sut.BuildAsync(_userId, FreakAiUsageIntent.ProgramGenerate, "Build me a 4-day plan");

        Assert.NotNull(ctx);
        Assert.Contains("Football", ctx!.UserProfile!);
        Assert.Contains("Athletic Performance", ctx.Goals!);
        Assert.Equal("Commercial Gym", ctx.Equipment);
    }

    [Fact]
    public async Task NutritionGuidance_IncludesBodyAndDiet_NotEquipment()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();

        var ctx = await sut.BuildAsync(_userId, FreakAiUsageIntent.NutritionGuidance, "What should I eat?");

        Assert.NotNull(ctx);
        Assert.Contains("82", ctx!.UserProfile!);
        Assert.Contains("Omnivore", ctx.UserProfile!);
        Assert.Null(ctx.Equipment);
    }

    [Fact]
    public async Task GeneralChat_IsMinimal()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();

        var ctx = await sut.BuildAsync(_userId, FreakAiUsageIntent.GeneralChat, "Hi");

        Assert.NotNull(ctx);
        Assert.Contains("Football", ctx!.UserProfile!);
        Assert.Null(ctx.Equipment);
        Assert.Null(ctx.CurrentProgram);
    }

    [Fact]
    public async Task UnknownUser_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContextBuilder>();

        var ctx = await sut.BuildAsync(int.MaxValue, FreakAiUsageIntent.GeneralChat, "Hi");

        Assert.Null(ctx);
    }
}
