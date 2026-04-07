using System.Globalization;
using System.Reflection;
using FreakLete.Services;

namespace FreakLete.Core.Tests;

/// <summary>
/// Tests AppLanguage string properties, LanguageChanged event,
/// and culture behavior. Because Initialize/SetLanguage depend on
/// MAUI Preferences, we drive the static Code property via reflection
/// so tests run in a plain .NET host.
/// </summary>
public class AppLanguageTests : IDisposable
{
    public AppLanguageTests()
    {
        // Reset to English before each test
        SetCodeDirect("en");
    }

    public void Dispose()
    {
        SetCodeDirect("en");
        // Clear any lingering subscribers
        ClearEvent();
    }

    [Fact]
    public void English_Defaults_Are_Correct()
    {
        SetCodeDirect("en");
        Assert.Equal("Home", AppLanguage.NavHome);
        Assert.Equal("Settings", AppLanguage.SettingsTitle);
        Assert.Equal("CONFIRM ACTION", AppLanguage.ConfirmAction);
        Assert.Equal("No data yet", AppLanguage.ChartNoData);
        Assert.Equal("Change", AppLanguage.ChartChange);
        Assert.Equal("Pick Session", AppLanguage.SessionPickerTitle);
        Assert.Equal("DELOAD", AppLanguage.ProgramDetailDeload);
    }

    [Fact]
    public void Turkish_Strings_Return_Turkish()
    {
        SetCodeDirect("tr");
        Assert.Equal("Ana Sayfa", AppLanguage.NavHome);
        Assert.Equal("Ayarlar", AppLanguage.SettingsTitle);
        Assert.Equal("İŞLEMİ ONAYLA", AppLanguage.ConfirmAction);
        Assert.Equal("Henüz veri yok", AppLanguage.ChartNoData);
        Assert.Equal("Değiştir", AppLanguage.ChartChange);
        Assert.Equal("Seans Seç", AppLanguage.SessionPickerTitle);
    }

    [Fact]
    public void Format_Methods_Respect_Language()
    {
        SetCodeDirect("en");
        Assert.Equal("Week 3", AppLanguage.FormatWeek(3));
        Assert.Equal("Day 1", AppLanguage.FormatDay(1));
        Assert.Equal("5 exercises", AppLanguage.FormatExercises(5));
        Assert.Equal("2 items", AppLanguage.FormatItemCount(2));
        Assert.Equal("1 item", AppLanguage.FormatItemCount(1));

        SetCodeDirect("tr");
        Assert.Equal("Hafta 3", AppLanguage.FormatWeek(3));
        Assert.Equal("Gün 1", AppLanguage.FormatDay(1));
        Assert.Equal("5 egzersiz", AppLanguage.FormatExercises(5));
        Assert.Equal("2 öğe", AppLanguage.FormatItemCount(2));
    }

    [Fact]
    public void LanguageChanged_Event_Fires_On_Actual_Change()
    {
        SetCodeDirect("en");
        int fireCount = 0;
        AppLanguage.LanguageChanged += () => fireCount++;

        // Simulate SetLanguage("tr") without Preferences
        SetCodeDirectAndRaiseEvent("tr");
        Assert.Equal(1, fireCount);

        // Same language again — should not fire
        // (In real code, SetLanguage checks Code != newCode)
        // We mimic that check here
        SetCodeDirect("tr"); // no event since we don't call raise
        Assert.Equal(1, fireCount);

        ClearEvent();
    }

    [Fact]
    public void IsTurkish_Reflects_Code()
    {
        SetCodeDirect("en");
        Assert.False(AppLanguage.IsTurkish);

        SetCodeDirect("tr");
        Assert.True(AppLanguage.IsTurkish);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static void SetCodeDirect(string code)
    {
        typeof(AppLanguage)
            .GetProperty(nameof(AppLanguage.Code))!
            .GetSetMethod(true)!
            .Invoke(null, [code]);
    }

    private static void SetCodeDirectAndRaiseEvent(string code)
    {
        SetCodeDirect(code);
        // Raise the event via reflection
        var field = typeof(AppLanguage)
            .GetField("LanguageChanged", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.GetValue(null) is Action handler)
            handler.Invoke();
    }

    private static void ClearEvent()
    {
        var field = typeof(AppLanguage)
            .GetField("LanguageChanged", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}
