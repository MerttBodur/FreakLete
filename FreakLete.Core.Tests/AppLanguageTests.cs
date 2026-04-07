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
        Assert.Equal("3x/week", AppLanguage.FormatFrequencyPerWeek(3));
        Assert.Equal("Weight must be a positive number.", AppLanguage.FormatMustBePositive("Weight"));
        Assert.Equal("Sport list could not be loaded.", AppLanguage.SportCatalogLoadError);
        Assert.Equal("Sport catalog request failed.", AppLanguage.SportCatalogRequestFailed);
        // Shared pages
        Assert.Equal("SELECT", AppLanguage.PickerSelect);
        Assert.Equal("Search...", AppLanguage.PickerSearch);
        Assert.Equal("Loading...", AppLanguage.PickerLoading);
        Assert.Equal("Retry", AppLanguage.PickerRetry);
        Assert.Equal("No results found", AppLanguage.PickerNoResults);
        Assert.Equal("No options available", AppLanguage.PickerNoOptions);
        Assert.Equal("EXERCISE BROWSER", AppLanguage.ExPickerBadge);
        Assert.Equal("View", AppLanguage.ExPickerView);
        Assert.Equal("DATE OF BIRTH", AppLanguage.DateSelectorBadge);
        Assert.Equal("Year", AppLanguage.DateSelectorYear);
        Assert.Equal("Done", AppLanguage.DateSelectorDone);
        Assert.Equal("SUCCESS", AppLanguage.DialogSuccess);
        Assert.Equal("Continue", AppLanguage.DialogContinue);
        Assert.Equal("Preparing your session...", AppLanguage.StartupPreparing);
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
        Assert.Equal("DİNLENME", AppLanguage.ProgramDetailDeload);
        Assert.Equal("Haftada 3x", AppLanguage.FormatFrequencyPerWeek(3));
        Assert.Equal("Ağırlık pozitif bir sayı olmalıdır.", AppLanguage.FormatMustBePositive("Ağırlık"));
        Assert.Equal("Spor listesi yüklenemedi.", AppLanguage.SportCatalogLoadError);
        Assert.Equal("Spor kataloğu isteği başarısız oldu.", AppLanguage.SportCatalogRequestFailed);
        // Shared pages
        Assert.Equal("SEÇ", AppLanguage.PickerSelect);
        Assert.Equal("Ara...", AppLanguage.PickerSearch);
        Assert.Equal("Yükleniyor...", AppLanguage.PickerLoading);
        Assert.Equal("Tekrar Dene", AppLanguage.PickerRetry);
        Assert.Equal("Sonuç bulunamadı", AppLanguage.PickerNoResults);
        Assert.Equal("Mevcut seçenek yok", AppLanguage.PickerNoOptions);
        Assert.Equal("EGZERSİZ TARAYICISI", AppLanguage.ExPickerBadge);
        Assert.Equal("Gör", AppLanguage.ExPickerView);
        Assert.Equal("DOĞUM TARİHİ", AppLanguage.DateSelectorBadge);
        Assert.Equal("Yıl", AppLanguage.DateSelectorYear);
        Assert.Equal("Tamam", AppLanguage.DateSelectorDone);
        Assert.Equal("BAŞARILI", AppLanguage.DialogSuccess);
        Assert.Equal("Devam", AppLanguage.DialogContinue);
        Assert.Equal("Oturum hazırlanıyor...", AppLanguage.StartupPreparing);
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
    public void MonthAbbreviations_Respect_Language()
    {
        SetCodeDirect("en");
        string[] enMonths = AppLanguage.MonthAbbreviations;
        Assert.Equal(12, enMonths.Length);
        Assert.Equal("Jan", enMonths[0]);
        Assert.Equal("Dec", enMonths[11]);

        SetCodeDirect("tr");
        string[] trMonths = AppLanguage.MonthAbbreviations;
        Assert.Equal(12, trMonths.Length);
        Assert.Equal("Oca", trMonths[0]);
        Assert.Equal("Ara", trMonths[11]);
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
