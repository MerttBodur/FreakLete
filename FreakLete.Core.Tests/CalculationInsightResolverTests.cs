using FreakLete.Services;
using static FreakLete.Services.CalculationInsightResolver;

namespace FreakLete.Core.Tests;

public class CalculationInsightResolverTests
{
    // ── ClassifyRatio (shared band engine) ──────────────────────────────────

    [Theory]
    [InlineData(0.5, 1.0, 2.0, 3.0, InsightBand.NeedsWork)]
    [InlineData(1.0, 1.0, 2.0, 3.0, InsightBand.Adequate)]
    [InlineData(1.5, 1.0, 2.0, 3.0, InsightBand.Adequate)]
    [InlineData(2.0, 1.0, 2.0, 3.0, InsightBand.Good)]
    [InlineData(2.9, 1.0, 2.0, 3.0, InsightBand.Good)]
    [InlineData(3.0, 1.0, 2.0, 3.0, InsightBand.Elite)]
    [InlineData(5.0, 1.0, 2.0, 3.0, InsightBand.Elite)]
    public void ClassifyRatio_Boundaries(double value, double adeq, double good, double elite, InsightBand expected)
    {
        Assert.Equal(expected, ClassifyRatio(value, adeq, good, elite));
    }

    // ── IsSupportedOneRmMovement ─────────────────────────────────────────────

    [Theory]
    [InlineData("Bench Press", true)]
    [InlineData("bench press", true)]
    [InlineData("BENCH PRESS", true)]
    [InlineData("Back Squat", true)]
    [InlineData("Deadlift", true)]
    [InlineData("Military Press", true)]
    [InlineData("Overhead Press", true)]
    [InlineData("Power Clean", true)]
    public void IsSupportedOneRmMovement_SupportedCases(string name, bool expected)
    {
        Assert.Equal(expected, IsSupportedOneRmMovement(name));
    }

    [Theory]
    [InlineData("Front Squat")]
    [InlineData("Romanian Deadlift")]
    [InlineData("Pull Up")]
    [InlineData("")]
    [InlineData("Snatch")]
    public void IsSupportedOneRmMovement_UnsupportedCases_ReturnsFalse(string name)
    {
        Assert.False(IsSupportedOneRmMovement(name));
    }

    // ── ResolveOneRm ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveOneRm_UnsupportedMovement_ReturnsNull()
    {
        var result = ResolveOneRm("Front Squat", 120, 80);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveOneRm_MissingBodyweight_ReturnsNull()
    {
        var result = ResolveOneRm("Bench Press", 100, null);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveOneRm_ZeroBodyweight_ReturnsNull()
    {
        var result = ResolveOneRm("Bench Press", 100, 0);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveOneRm_BenchPress_NeedsWork()
    {
        // ratio = 50/100 = 0.5, threshold adequate = 0.80
        var result = ResolveOneRm("Bench Press", 50, 100);
        Assert.NotNull(result);
        Assert.Equal(InsightBand.NeedsWork, result!.Band);
    }

    [Fact]
    public void ResolveOneRm_BenchPress_Adequate()
    {
        // ratio = 90/100 = 0.90, adequate=0.80, good=1.10
        var result = ResolveOneRm("Bench Press", 90, 100);
        Assert.NotNull(result);
        Assert.Equal(InsightBand.Adequate, result!.Band);
    }

    [Fact]
    public void ResolveOneRm_BenchPress_Good()
    {
        // ratio = 120/100 = 1.20, good=1.10, elite=1.40
        var result = ResolveOneRm("Bench Press", 120, 100);
        Assert.NotNull(result);
        Assert.Equal(InsightBand.Good, result!.Band);
    }

    [Fact]
    public void ResolveOneRm_BenchPress_Elite()
    {
        // ratio = 150/100 = 1.50, elite=1.40
        var result = ResolveOneRm("Bench Press", 150, 100);
        Assert.NotNull(result);
        Assert.Equal(InsightBand.Elite, result!.Band);
    }

    [Fact]
    public void ResolveOneRm_Deadlift_Elite()
    {
        // ratio = 200/100 = 2.0, elite=2.0
        var result = ResolveOneRm("Deadlift", 200, 100);
        Assert.NotNull(result);
        Assert.Equal(InsightBand.Elite, result!.Band);
    }

    [Fact]
    public void ResolveOneRm_BackSquat_Good()
    {
        // ratio = 150/100 = 1.5, good=1.40, elite=1.80
        var result = ResolveOneRm("Back Squat", 150, 100);
        Assert.NotNull(result);
        Assert.Equal(InsightBand.Good, result!.Band);
    }

    [Fact]
    public void ResolveOneRm_InsightContainsNonEmptyTexts()
    {
        var result = ResolveOneRm("Bench Press", 100, 80);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.BandLabel));
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.False(string.IsNullOrWhiteSpace(result.SportContext));
        Assert.False(string.IsNullOrWhiteSpace(result.GlobalContext));
    }

    [Theory]
    [InlineData("Military Press")]
    [InlineData("Overhead Press")]
    public void ResolveOneRm_OhpVariants_AreSupported(string name)
    {
        // Both aliases share same threshold — just ensure no null
        var result = ResolveOneRm(name, 70, 100);
        Assert.NotNull(result);
    }

    // ── ResolveRsi ───────────────────────────────────────────────────────────

    [Fact]
    public void ResolveRsi_ZeroOrNegative_ReturnsNull()
    {
        Assert.Null(ResolveRsi(0));
        Assert.Null(ResolveRsi(-1));
    }

    [Theory]
    [InlineData(0.5, InsightBand.NeedsWork)]
    [InlineData(1.0, InsightBand.Adequate)]
    [InlineData(1.5, InsightBand.Adequate)]
    [InlineData(2.0, InsightBand.Good)]
    [InlineData(2.5, InsightBand.Good)]
    [InlineData(3.0, InsightBand.Elite)]
    [InlineData(4.0, InsightBand.Elite)]
    public void ResolveRsi_Bands(double rsi, InsightBand expected)
    {
        var result = ResolveRsi(rsi);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Band);
    }

    [Fact]
    public void ResolveRsi_WithSport_SportContextMentionsSport()
    {
        var result = ResolveRsi(2.0, "Basketball");
        Assert.NotNull(result);
        Assert.Contains("Basketball", result!.SportContext);
    }

    [Fact]
    public void ResolveRsi_WithoutSport_SportContextIsGeneric()
    {
        var result = ResolveRsi(2.0, null);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.SportContext));
    }

    [Fact]
    public void ResolveRsi_InsightContainsNonEmptyTexts()
    {
        var result = ResolveRsi(1.5);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.BandLabel));
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.False(string.IsNullOrWhiteSpace(result.GlobalContext));
    }

    // ── ResolveFfmi ───────────────────────────────────────────────────────────

    [Fact]
    public void ResolveFfmi_NullSex_ReturnsNull()
    {
        Assert.Null(ResolveFfmi(20, null));
    }

    [Fact]
    public void ResolveFfmi_EmptySex_ReturnsNull()
    {
        Assert.Null(ResolveFfmi(20, ""));
    }

    [Fact]
    public void ResolveFfmi_UnrecognizedSex_ReturnsNull()
    {
        Assert.Null(ResolveFfmi(20, "other"));
        Assert.Null(ResolveFfmi(20, "non-binary"));
        Assert.Null(ResolveFfmi(20, "X"));
    }

    [Fact]
    public void ResolveFfmi_ZeroOrNegativeFfmi_ReturnsNull()
    {
        Assert.Null(ResolveFfmi(0, "male"));
        Assert.Null(ResolveFfmi(-1, "male"));
    }

    [Theory]
    [InlineData(16.0, "male",   InsightBand.NeedsWork)]   // < 18.0
    [InlineData(18.0, "male",   InsightBand.Adequate)]    // >= 18.0
    [InlineData(19.0, "male",   InsightBand.Adequate)]    // < 20.0
    [InlineData(20.0, "male",   InsightBand.Good)]        // >= 20.0
    [InlineData(21.0, "male",   InsightBand.Good)]        // < 22.5
    [InlineData(22.5, "male",   InsightBand.Elite)]       // >= 22.5
    [InlineData(25.0, "male",   InsightBand.Elite)]
    public void ResolveFfmi_MaleBands(double ffmi, string sex, InsightBand expected)
    {
        var result = ResolveFfmi(ffmi, sex);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Band);
    }

    [Theory]
    [InlineData(13.0, "female", InsightBand.NeedsWork)]   // < 15.0
    [InlineData(15.0, "female", InsightBand.Adequate)]    // >= 15.0
    [InlineData(16.0, "female", InsightBand.Adequate)]    // < 17.0
    [InlineData(17.0, "female", InsightBand.Good)]        // >= 17.0
    [InlineData(18.0, "female", InsightBand.Good)]        // < 19.0
    [InlineData(19.0, "female", InsightBand.Elite)]       // >= 19.0
    [InlineData(22.0, "female", InsightBand.Elite)]
    public void ResolveFfmi_FemaleBands(double ffmi, string sex, InsightBand expected)
    {
        var result = ResolveFfmi(ffmi, sex);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Band);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("Male")]
    [InlineData("MALE")]
    [InlineData("m")]
    [InlineData("M")]
    public void ResolveFfmi_MaleSexVariants_Recognized(string sex)
    {
        var result = ResolveFfmi(20, sex);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("female")]
    [InlineData("Female")]
    [InlineData("FEMALE")]
    [InlineData("f")]
    [InlineData("F")]
    public void ResolveFfmi_FemaleSexVariants_Recognized(string sex)
    {
        var result = ResolveFfmi(17, sex);
        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveFfmi_InsightContainsNonEmptyTexts()
    {
        var result = ResolveFfmi(20, "male");
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.BandLabel));
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
        Assert.False(string.IsNullOrWhiteSpace(result.SportContext));
        Assert.False(string.IsNullOrWhiteSpace(result.GlobalContext));
    }

    // ── No-fake-tier invariants ───────────────────────────────────────────────

    [Fact]
    public void ResolveOneRm_FakeTierInvariant_UnsupportedMovementNeverReturnsResult()
    {
        // Any 1RM value for unsupported movement → always null
        foreach (var movement in new[] { "Pull Up", "Leg Press", "Barbell Row", "Front Squat", "" })
        {
            Assert.Null(ResolveOneRm(movement, 200, 80));
        }
    }

    [Fact]
    public void ResolveFfmi_FakeTierInvariant_MissingOrUnknownSexNeverReturnsResult()
    {
        Assert.Null(ResolveFfmi(25, null));
        Assert.Null(ResolveFfmi(25, ""));
        Assert.Null(ResolveFfmi(25, "unknown"));
    }
}
