using FreakLete.Api.Services;

namespace FreakLete.Api.Tests;

public class IntentClassifierTests
{
    [Theory]
    [InlineData("program_generate", "anything", "program_generate")]
    [InlineData("program_analyze", "anything", "program_analyze")]
    [InlineData("nutrition_guidance", "anything", "nutrition_guidance")]
    [InlineData("general_chat", "anything", "general_chat")]
    public void ExplicitValidIntent_ReturnedAsIs(string intent, string message, string expected)
    {
        Assert.Equal(expected, IntentClassifier.Classify(intent, message));
    }

    [Fact]
    public void ProgramView_MapsToGeneralChat()
    {
        Assert.Equal("general_chat", IntentClassifier.Classify("program_view", "show my program"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid_intent")]
    public void InvalidIntent_FallsBackToClassifier(string? intent)
    {
        // General message -> general_chat
        Assert.Equal("general_chat", IntentClassifier.Classify(intent, "How do I improve my squat?"));
    }

    // ── Program generate detection ──────────────────────

    [Theory]
    [InlineData("Create a 4-week training program for me")]
    [InlineData("Bana bir program oluştur")]
    [InlineData("Write a push pull legs program")]
    [InlineData("Generate program for hypertrophy")]
    [InlineData("Make me a new program")]
    [InlineData("Build a program for strength")]
    [InlineData("I need a weekly program")]
    [InlineData("Design a program for me")]
    [InlineData("Antrenman programı yaz")]
    [InlineData("Haftalık program hazırla")]
    [InlineData("upper lower split program")]
    public void FallbackClassifier_DetectsProgramGenerate(string message)
    {
        Assert.Equal("program_generate", IntentClassifier.Classify(null, message));
    }

    // ── Program analyze detection ───────────────────────

    [Theory]
    [InlineData("Analyze my program")]
    [InlineData("Review my program please")]
    [InlineData("Programımı analiz et")]
    [InlineData("What do you think of my program?")]
    [InlineData("Evaluate my program")]
    public void FallbackClassifier_DetectsProgramAnalyze(string message)
    {
        Assert.Equal("program_analyze", IntentClassifier.Classify(null, message));
    }

    // ── Nutrition detection ─────────────────────────────

    [Theory]
    [InlineData("What should I eat for bulking?")]
    [InlineData("Give me a meal plan")]
    [InlineData("How many calories do I need?")]
    [InlineData("Beslenme planı oluştur")]
    [InlineData("Ne yemeliyim?")]
    [InlineData("Protein intake advice")]
    [InlineData("I need a diet plan")]
    public void FallbackClassifier_DetectsNutritionGuidance(string message)
    {
        Assert.Equal("nutrition_guidance", IntentClassifier.Classify(null, message));
    }

    // ── General chat fallback ───────────────────────────

    [Theory]
    [InlineData("How should I warm up?")]
    [InlineData("What's a good rest time between sets?")]
    [InlineData("Tips for better sleep")]
    [InlineData("Hello coach")]
    public void FallbackClassifier_FallsToGeneralChat(string message)
    {
        Assert.Equal("general_chat", IntentClassifier.Classify(null, message));
    }
}
