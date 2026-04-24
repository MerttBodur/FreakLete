# Phase 2 — SetCardView + NewWorkoutPage Inline

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans.

**Goal:** `Xaml/Controls/SetCardView.xaml` reusable component oluştur. `NewWorkoutPage`'in Exercise Builder kısmını popup yerine inline set kartları ile yeniden yaz. SetCount/RepCount/RIR/Rest/Concentric entry'leri kaldırılır; her set kendi kartında tüm alanları taşır. `SetDetailsPopup` silinir. Component Phase 3/4'te reuse edilir.

**Architecture:** `SetCardView` bindable property'lerle driven. `NewWorkoutPage` `List<SetDetail>` tutar; `VerticalStackLayout` dinamik olarak `SetCardView` instance'larını render eder. `+ Add Set` önceki setten kopyalar; `− Remove Set` son seti siler (min 1). Submit'te `List<SetDetail>` `ExerciseEntry.Sets`'e yazılır; legacy alanlar (`Reps`, `RIR`, `RestSeconds`, `ConcentricTimeSeconds`, `Metric1Value`) saf static helper ile türetilir.

**Tech Stack:** .NET MAUI 10, C#, XAML, xUnit.

---

## Task 1: Extend `SetDetail` model

**Files:**
- Modify: `Models/SetDetail.cs`

- [ ] **Step 1: 3 alan ekle + `SetNumber` artık mutable**

```csharp
namespace FreakLete.Models;

public sealed class SetDetail
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double? Weight { get; set; }
    public int? Rir { get; set; }
    public int? RestSeconds { get; set; }
    public double? ConcentricSeconds { get; set; }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error (init → set değişikliği breaking değil).

- [ ] **Step 3: Commit**

```bash
git add Models/SetDetail.cs
git commit -m "feat: extend SetDetail with Rir/RestSeconds/ConcentricSeconds"
```

---

## Task 2: Extend mobile API client DTO

**Files:**
- Modify: `Services/ApiClient.cs`

- [ ] **Step 1: ApiExerciseSetDto tipini bul ve 3 alan ekle**

Read `Services/ApiClient.cs`. Mevcut Weight altına:
```csharp
public int? RIR { get; set; }
public int? RestSeconds { get; set; }
public double? ConcentricTimeSeconds { get; set; }
```

- [ ] **Step 2: Map'leri güncelle**

`SetDetail` ↔ DTO kopyalama yerleri:
- `Rir ↔ RIR`
- `RestSeconds ↔ RestSeconds`
- `ConcentricSeconds ↔ ConcentricTimeSeconds`

- [ ] **Step 3: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 4: Commit**

```bash
git add Services/ApiClient.cs
git commit -m "feat: extend mobile API client with per-set Rir/Rest/Concentric"
```

---

## Task 3: AppLanguage yeni stringler

**Files:**
- Modify: `Services/AppLanguage.cs`

- [ ] **Step 1: 4 string ekle**

Mevcut `NewWorkoutSetDetails*` bloğunun yanına:
```csharp
public static string NewWorkoutSetDetailsWeightRequired => IsTurkish
    ? "Her set için ağırlık girilmelidir."
    : "Weight must be entered for every set.";

public static string NewWorkoutAddSet => IsTurkish ? "+ Set Ekle" : "+ Add Set";

public static string NewWorkoutRemoveSet => IsTurkish ? "− Set Çıkar" : "− Remove Set";

public static string NewWorkoutAdvancedDetails => IsTurkish ? "Detaylar" : "Details";
```

- [ ] **Step 2: Build → 0 error**

- [ ] **Step 3: Commit**

```bash
git add Services/AppLanguage.cs
git commit -m "feat: add localized strings for inline set cards"
```

---

## Task 4: `SetCardView` control

**Files:**
- Create: `Xaml/Controls/SetCardView.xaml`
- Create: `Xaml/Controls/SetCardView.xaml.cs`

- [ ] **Step 1: XAML**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:FreakLete"
             x:Class="FreakLete.Xaml.Controls.SetCardView">
    <Border Style="{StaticResource CardBorder}"
            BackgroundColor="{StaticResource SurfaceRaised}"
            Padding="12">
        <VerticalStackLayout Spacing="10">

            <Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
                <Label x:Name="SetHeaderLabel"
                       Grid.Column="0"
                       Style="{StaticResource SubHeadline}"
                       VerticalOptions="Center" />
                <ImageButton x:Name="ExpandToggle"
                             Grid.Column="1"
                             Source="icon_chevron_down.svg"
                             BackgroundColor="Transparent"
                             Padding="8"
                             WidthRequest="32"
                             HeightRequest="32"
                             Clicked="OnExpandToggleClicked" />
            </Grid>

            <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                <VerticalStackLayout Grid.Column="0" Spacing="4">
                    <Label x:Name="WeightLabel"
                           Style="{StaticResource BodyMuted}"
                           FontSize="11" />
                    <local:InputShell>
                        <Entry x:Name="WeightEntry"
                               Keyboard="Numeric"
                               TextChanged="OnWeightChanged" />
                    </local:InputShell>
                </VerticalStackLayout>
                <VerticalStackLayout Grid.Column="1" Spacing="4">
                    <Label x:Name="RepsLabel"
                           Style="{StaticResource BodyMuted}"
                           FontSize="11" />
                    <local:InputShell>
                        <Entry x:Name="RepsEntry"
                               Keyboard="Numeric"
                               TextChanged="OnRepsChanged" />
                    </local:InputShell>
                </VerticalStackLayout>
            </Grid>

            <VerticalStackLayout x:Name="AdvancedSection"
                                 IsVisible="False"
                                 Spacing="10">
                <Grid ColumnDefinitions="*,*,*" ColumnSpacing="8">
                    <VerticalStackLayout Grid.Column="0" Spacing="4">
                        <Label x:Name="RirLabel"
                               Style="{StaticResource BodyMuted}"
                               FontSize="11" />
                        <local:InputShell>
                            <Entry x:Name="RirEntry"
                                   Keyboard="Numeric"
                                   Placeholder="Opt"
                                   TextChanged="OnRirChanged" />
                        </local:InputShell>
                    </VerticalStackLayout>
                    <VerticalStackLayout Grid.Column="1" Spacing="4">
                        <Label x:Name="RestLabel"
                               Style="{StaticResource BodyMuted}"
                               FontSize="11" />
                        <local:InputShell>
                            <Entry x:Name="RestEntry"
                                   Keyboard="Numeric"
                                   Placeholder="Opt"
                                   TextChanged="OnRestChanged" />
                        </local:InputShell>
                    </VerticalStackLayout>
                    <VerticalStackLayout x:Name="ConcentricContainer"
                                         Grid.Column="2"
                                         Spacing="4">
                        <Label x:Name="ConcentricLabel"
                               Style="{StaticResource BodyMuted}"
                               FontSize="11" />
                        <local:InputShell>
                            <Entry x:Name="ConcentricEntry"
                                   Keyboard="Numeric"
                                   Placeholder="Opt"
                                   TextChanged="OnConcentricChanged" />
                        </local:InputShell>
                    </VerticalStackLayout>
                </Grid>
            </VerticalStackLayout>

        </VerticalStackLayout>
    </Border>
</ContentView>
```

- [ ] **Step 2: Code-behind**

```csharp
using FreakLete.Services;

namespace FreakLete.Xaml.Controls;

public partial class SetCardView : ContentView
{
    public static readonly BindableProperty SetNumberProperty =
        BindableProperty.Create(nameof(SetNumber), typeof(int), typeof(SetCardView), 0,
            propertyChanged: (b, _, _) => ((SetCardView)b).UpdateHeader());

    public static readonly BindableProperty WeightProperty =
        BindableProperty.Create(nameof(Weight), typeof(double?), typeof(SetCardView), null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, v) => ((SetCardView)b).SyncEntry(((SetCardView)b).WeightEntry, v));

    public static readonly BindableProperty RepsProperty =
        BindableProperty.Create(nameof(Reps), typeof(int?), typeof(SetCardView), null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, v) => ((SetCardView)b).SyncEntry(((SetCardView)b).RepsEntry, v));

    public static readonly BindableProperty RirProperty =
        BindableProperty.Create(nameof(Rir), typeof(int?), typeof(SetCardView), null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, v) => ((SetCardView)b).SyncEntry(((SetCardView)b).RirEntry, v));

    public static readonly BindableProperty RestSecondsProperty =
        BindableProperty.Create(nameof(RestSeconds), typeof(int?), typeof(SetCardView), null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, v) => ((SetCardView)b).SyncEntry(((SetCardView)b).RestEntry, v));

    public static readonly BindableProperty ConcentricSecondsProperty =
        BindableProperty.Create(nameof(ConcentricSeconds), typeof(double?), typeof(SetCardView), null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, v) => ((SetCardView)b).SyncEntry(((SetCardView)b).ConcentricEntry, v));

    public static readonly BindableProperty IsExpandedProperty =
        BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(SetCardView), false,
            propertyChanged: (b, _, _) => ((SetCardView)b).UpdateExpanded());

    public static readonly BindableProperty IsReadOnlyProperty =
        BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(SetCardView), false,
            propertyChanged: (b, _, _) => ((SetCardView)b).UpdateReadOnly());

    public static readonly BindableProperty ShowConcentricProperty =
        BindableProperty.Create(nameof(ShowConcentric), typeof(bool), typeof(SetCardView), true,
            propertyChanged: (b, _, _) => ((SetCardView)b).UpdateConcentricVisibility());

    public int SetNumber { get => (int)GetValue(SetNumberProperty); set => SetValue(SetNumberProperty, value); }
    public double? Weight { get => (double?)GetValue(WeightProperty); set => SetValue(WeightProperty, value); }
    public int? Reps { get => (int?)GetValue(RepsProperty); set => SetValue(RepsProperty, value); }
    public int? Rir { get => (int?)GetValue(RirProperty); set => SetValue(RirProperty, value); }
    public int? RestSeconds { get => (int?)GetValue(RestSecondsProperty); set => SetValue(RestSecondsProperty, value); }
    public double? ConcentricSeconds { get => (double?)GetValue(ConcentricSecondsProperty); set => SetValue(ConcentricSecondsProperty, value); }
    public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
    public bool ShowConcentric { get => (bool)GetValue(ShowConcentricProperty); set => SetValue(ShowConcentricProperty, value); }

    public event EventHandler? ValueChanged;

    private bool _syncing;

    public SetCardView()
    {
        InitializeComponent();
        ApplyLanguage();
        UpdateHeader();
        UpdateExpanded();
        UpdateConcentricVisibility();
    }

    private void ApplyLanguage()
    {
        WeightLabel.Text = AppLanguage.NewWorkoutSetColumnWeight;
        RepsLabel.Text = AppLanguage.NewWorkoutSetColumnReps;
        RirLabel.Text = "RIR";
        RestLabel.Text = AppLanguage.NewWorkoutRestSecondsLabel;
        ConcentricLabel.Text = AppLanguage.NewWorkoutConcentricTimeLabel;
    }

    private void UpdateHeader() => SetHeaderLabel.Text = $"Set {SetNumber}";
    private void UpdateExpanded()
    {
        AdvancedSection.IsVisible = IsExpanded;
        ExpandToggle.Source = IsExpanded ? "icon_chevron_up.svg" : "icon_chevron_down.svg";
    }
    private void UpdateConcentricVisibility() => ConcentricContainer.IsVisible = ShowConcentric;
    private void UpdateReadOnly()
    {
        WeightEntry.IsReadOnly = IsReadOnly;
        RepsEntry.IsReadOnly = IsReadOnly;
        RirEntry.IsReadOnly = IsReadOnly;
        RestEntry.IsReadOnly = IsReadOnly;
        ConcentricEntry.IsReadOnly = IsReadOnly;
    }

    private void SyncEntry(Entry entry, object? value)
    {
        _syncing = true;
        entry.Text = value switch
        {
            null => string.Empty,
            double d => d.ToString("0.##"),
            int i => i.ToString(),
            _ => value.ToString() ?? string.Empty
        };
        _syncing = false;
    }

    private void OnExpandToggleClicked(object? sender, EventArgs e) => IsExpanded = !IsExpanded;

    private void OnWeightChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        Weight = MetricInput.TryParseFlexibleDouble(WeightEntry.Text, out var w) && w > 0 ? w : null;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void OnRepsChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        Reps = int.TryParse(RepsEntry.Text, out var r) && r > 0 ? r : null;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void OnRirChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        Rir = int.TryParse(RirEntry.Text, out var r) ? r : null;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void OnRestChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        RestSeconds = int.TryParse(RestEntry.Text, out var r) ? r : null;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void OnConcentricChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing) return;
        ConcentricSeconds = MetricInput.TryParseFlexibleDouble(ConcentricEntry.Text, out var c) && c > 0 ? c : null;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void FocusWeight() => WeightEntry.Focus();
}
```

> **NOT:** `AppLanguage.NewWorkoutRestSecondsLabel` / `NewWorkoutConcentricTimeLabel` mevcut (mevcut NewWorkoutPage kullanıyor). Yoksa Task 3'e ekle.

- [ ] **Step 3: Build → 0 error**
- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/SetCardView.xaml Xaml/Controls/SetCardView.xaml.cs
git commit -m "feat: add SetCardView reusable control"
```

---

## Task 5: Saf helper — `ExerciseEntryLegacyDeriver`

**Files:**
- Create: `Helpers/ExerciseEntryLegacyDeriver.cs`

- [ ] **Step 1: Helper yaz**

```csharp
using FreakLete.Models;

namespace FreakLete.Helpers;

public static class ExerciseEntryLegacyDeriver
{
    public readonly record struct Legacy(
        int SetsCount, int Reps, double? MaxWeight,
        int? Rir, int? RestSeconds, double? ConcentricSeconds);

    public static Legacy Derive(IReadOnlyList<SetDetail> sets)
    {
        if (sets.Count == 0)
            throw new ArgumentException("Empty set list", nameof(sets));
        var last = sets[^1];
        double? max = sets.Where(s => s.Weight.HasValue)
            .Select(s => s.Weight!.Value)
            .DefaultIfEmpty(0)
            .Max();
        if (max == 0) max = null;
        return new Legacy(sets.Count, last.Reps, max, last.Rir, last.RestSeconds, last.ConcentricSeconds);
    }
}
```

- [ ] **Step 2: Build → 0 error**
- [ ] **Step 3: Commit**

```bash
git add Helpers/ExerciseEntryLegacyDeriver.cs
git commit -m "feat: add ExerciseEntryLegacyDeriver helper"
```

---

## Task 6: Unit tests for deriver

**Files:**
- Create: `FreakLete.Core.Tests/ExerciseEntryLegacyDerivationTests.cs`
- Modify: `FreakLete.Core.Tests/FreakLete.Core.Tests.csproj`

- [ ] **Step 1: csproj'a link ekle**

`<ItemGroup>` Compile Include bloğuna:
```xml
<Compile Include="..\Helpers\ExerciseEntryLegacyDeriver.cs" Link="Linked\ExerciseEntryLegacyDeriver.cs" />
```

- [ ] **Step 2: Test dosyası**

```csharp
using FreakLete.Helpers;
using FreakLete.Models;

namespace FreakLete.Core.Tests;

public class ExerciseEntryLegacyDerivationTests
{
    [Fact]
    public void MaxWeight_TakesMax()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 5, Weight = 90 },
            new() { SetNumber = 2, Reps = 5, Weight = 110 },
            new() { SetNumber = 3, Reps = 5, Weight = 100 }
        };
        Assert.Equal(110, ExerciseEntryLegacyDeriver.Derive(sets).MaxWeight);
    }

    [Fact]
    public void Reps_ReturnsLastSetReps()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 5, Weight = 90 },
            new() { SetNumber = 2, Reps = 3, Weight = 110 }
        };
        Assert.Equal(3, ExerciseEntryLegacyDeriver.Derive(sets).Reps);
    }

    [Fact]
    public void Rir_Rest_Concentric_FromLastSet()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 5, Weight = 90, Rir = 3, RestSeconds = 90, ConcentricSeconds = 1.5 },
            new() { SetNumber = 2, Reps = 5, Weight = 100, Rir = 1, RestSeconds = 150, ConcentricSeconds = 1.2 }
        };
        var d = ExerciseEntryLegacyDeriver.Derive(sets);
        Assert.Equal(1, d.Rir);
        Assert.Equal(150, d.RestSeconds);
        Assert.Equal(1.2, d.ConcentricSeconds);
    }

    [Fact]
    public void AllWeightsNull_MaxIsNull()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 5, Weight = null },
            new() { SetNumber = 2, Reps = 5, Weight = null }
        };
        Assert.Null(ExerciseEntryLegacyDeriver.Derive(sets).MaxWeight);
    }

    [Fact]
    public void EmptyList_Throws()
    {
        Assert.Throws<ArgumentException>(() => ExerciseEntryLegacyDeriver.Derive(new List<SetDetail>()));
    }
}
```

- [ ] **Step 3: Test koş**

Run: `dotnet test FreakLete.Core.Tests --filter "FullyQualifiedName~ExerciseEntryLegacyDerivationTests"`
Expected: 5/5 PASS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Core.Tests/ExerciseEntryLegacyDerivationTests.cs FreakLete.Core.Tests/FreakLete.Core.Tests.csproj
git commit -m "test: ExerciseEntryLegacyDeriver derivation rules"
```

---

## Task 7: NewWorkoutPage.xaml — inline layout

**Files:**
- Modify: `Xaml/NewWorkoutPage.xaml`

- [ ] **Step 1: `StrengthInputsSection` tamamen yeniden yaz**

Mevcut içindeki `SetCountEntry`, `RepCountEntry`, `RirEntry`, `RestSecondsEntry`, `ConcentricTimeEntry` Grid'leri + `StrengthTimingContainer` kaldır. Yerine:

```xml
<VerticalStackLayout x:Name="StrengthInputsSection"
                     Spacing="10"
                     IsVisible="False">

    <Label x:Name="SetsHeaderLabel"
           Text="Sets"
           Style="{StaticResource BodyMuted}" />

    <VerticalStackLayout x:Name="SetCardsContainer" Spacing="8" />

    <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
        <Button x:Name="RemoveSetButton"
                Grid.Column="0"
                Style="{StaticResource SecondaryButton}"
                Clicked="OnRemoveSetClicked" />
        <Button x:Name="AddSetButton"
                Grid.Column="1"
                Style="{StaticResource SecondaryButton}"
                Clicked="OnAddSetClicked" />
    </Grid>
</VerticalStackLayout>
```

> Bu adım tek başına build kırılmasına yol açar; Task 8 ile birlikte commit at.

---

## Task 8: NewWorkoutPage.xaml.cs — inline akış

**Files:**
- Modify: `CodeBehind/NewWorkoutPage.xaml.cs`

- [ ] **Step 1: Private field'lar**

```csharp
private readonly List<SetDetail> _currentSets = new();
private readonly List<Xaml.Controls.SetCardView> _currentSetCards = new();
```

- [ ] **Step 2: Metodlar ekle**

```csharp
private void InitializeSetsForSelectedExercise()
{
    _currentSets.Clear();
    _currentSetCards.Clear();
    SetCardsContainer.Children.Clear();

    if (_selectedExerciseItem?.TrackingMode != ExerciseTrackingMode.Strength)
        return;

    AddSet(copyFromPrevious: false);
    UpdateRemoveSetEnabled();
}

private void AddSet(bool copyFromPrevious)
{
    var prev = _currentSets.Count > 0 ? _currentSets[^1] : null;
    var newSet = new SetDetail
    {
        SetNumber = _currentSets.Count + 1,
        Reps = copyFromPrevious && prev is not null ? prev.Reps : 0,
        Weight = copyFromPrevious ? prev?.Weight : null,
        Rir = copyFromPrevious ? prev?.Rir : null,
        RestSeconds = copyFromPrevious ? prev?.RestSeconds : null,
        ConcentricSeconds = copyFromPrevious ? prev?.ConcentricSeconds : null
    };
    _currentSets.Add(newSet);

    var card = new Xaml.Controls.SetCardView
    {
        SetNumber = newSet.SetNumber,
        Weight = newSet.Weight,
        Reps = newSet.Reps > 0 ? newSet.Reps : null,
        Rir = newSet.Rir,
        RestSeconds = newSet.RestSeconds,
        ConcentricSeconds = newSet.ConcentricSeconds,
        ShowConcentric = ShouldShowConcentric(_selectedExerciseItem)
    };
    card.ValueChanged += (_, _) =>
    {
        newSet.Weight = card.Weight;
        newSet.Reps = card.Reps ?? 0;
        newSet.Rir = card.Rir;
        newSet.RestSeconds = card.RestSeconds;
        newSet.ConcentricSeconds = card.ConcentricSeconds;
    };
    _currentSetCards.Add(card);
    SetCardsContainer.Children.Add(card);

    if (copyFromPrevious)
        Dispatcher.Dispatch(() => card.FocusWeight());
}

private void OnAddSetClicked(object? sender, EventArgs e)
{
    AddSet(copyFromPrevious: true);
    UpdateRemoveSetEnabled();
}

private void OnRemoveSetClicked(object? sender, EventArgs e)
{
    if (_currentSets.Count <= 1) return;
    _currentSets.RemoveAt(_currentSets.Count - 1);
    var lastCard = _currentSetCards[^1];
    SetCardsContainer.Children.Remove(lastCard);
    _currentSetCards.RemoveAt(_currentSetCards.Count - 1);
    UpdateRemoveSetEnabled();
}

private void UpdateRemoveSetEnabled() => RemoveSetButton.IsEnabled = _currentSets.Count > 1;

private static bool ShouldShowConcentric(ExerciseCatalogItem? item)
{
    // Mevcut StrengthTimingContainer visibility rule'ını buraya taşı.
    // Placeholder:
    return item is not null && item.TrackingMode == ExerciseTrackingMode.Strength;
}
```

> **NOT:** `ShouldShowConcentric`: kaldırılan `StrengthTimingContainer.IsVisible` nerede set ediliyordu? O mantığı oraya kopyala (tier / catalog rule).

- [ ] **Step 3: Egzersiz seçildikten sonra `InitializeSetsForSelectedExercise()` çağır**

`OnChooseExerciseClicked` popup'tan dönüş yapan method ya da `ShowSelectedExercise` sonunda çağrılacak.

- [ ] **Step 4: `OnAddExerciseClicked` — popup akışını kaldır, validation inline**

Mevcut popup çağrısı içeren Strength dalını değiştir:
```csharp
private void OnAddExerciseClicked(object? sender, EventArgs e)
{
    ClearError();
    if (_selectedExerciseItem is null)
    {
        ShowError(AppLanguage.NewWorkoutExerciseRequired);
        return;
    }

    if (_selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
    {
        if (_currentSets.Count == 0)
        {
            ShowError(AppLanguage.NewWorkoutSetError);
            return;
        }

        for (int i = 0; i < _currentSets.Count; i++)
        {
            var s = _currentSets[i];
            if (s.Reps <= 0)
            {
                ShowError(AppLanguage.NewWorkoutSetDetailsRepsRequired);
                _currentSetCards[i].IsExpanded = true;
                return;
            }
            if (!s.Weight.HasValue || s.Weight.Value <= 0)
            {
                ShowError(AppLanguage.NewWorkoutSetDetailsWeightRequired);
                _currentSetCards[i].IsExpanded = true;
                return;
            }
        }
    }

    ExerciseEntry? entry = BuildExerciseEntryFromInputs();
    if (entry is null) return;
    _exercises.Add(entry);
    ClearExerciseInputs();
    RefreshExercisesList();
}
```

- [ ] **Step 5: `BuildExerciseEntryFromInputs` Strength dalı**

```csharp
if (_selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
{
    var sets = _currentSets.Select((s, i) => new SetDetail
    {
        SetNumber = i + 1,
        Reps = s.Reps,
        Weight = s.Weight,
        Rir = s.Rir,
        RestSeconds = s.RestSeconds,
        ConcentricSeconds = s.ConcentricSeconds
    }).ToList();

    var legacy = Helpers.ExerciseEntryLegacyDeriver.Derive(sets);

    return new ExerciseEntry
    {
        ExerciseName = _selectedExerciseItem.Name,
        ExerciseCategory = _selectedExerciseItem.Category,
        TrackingMode = nameof(ExerciseTrackingMode.Strength),
        Sets = sets,
        SetsCount = legacy.SetsCount,
        Reps = legacy.Reps,
        Metric1Value = legacy.MaxWeight,
        Metric1Unit = legacy.MaxWeight.HasValue ? "kg" : string.Empty,
        RIR = legacy.Rir,
        RestSeconds = legacy.RestSeconds,
        ConcentricTimeSeconds = legacy.ConcentricSeconds
    };
}
```

- [ ] **Step 6: `ClearExerciseInputs`'a set temizleme ekle**

```csharp
_currentSets.Clear();
_currentSetCards.Clear();
SetCardsContainer.Children.Clear();
```

- [ ] **Step 7: `ApplyLanguage` buton text'leri**

```csharp
AddSetButton.Text = AppLanguage.NewWorkoutAddSet;
RemoveSetButton.Text = AppLanguage.NewWorkoutRemoveSet;
```

- [ ] **Step 8: Popup / aggregator import'larını kaldır**

`SetDetailsPopup` ve `SetDetailsAggregator` referans/using satırları varsa temizle.

- [ ] **Step 9: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 10: Commit (Task 7 + Task 8 birlikte)**

```bash
git add Xaml/NewWorkoutPage.xaml CodeBehind/NewWorkoutPage.xaml.cs
git commit -m "feat: inline per-set input on NewWorkoutPage"
```

---

## Task 9: SetDetailsPopup silme

**Files:**
- Delete: `Xaml/Controls/SetDetailsPopup.xaml`
- Delete: `Xaml/Controls/SetDetailsPopup.xaml.cs`

- [ ] **Step 1: Dosyaları sil**

```bash
git rm Xaml/Controls/SetDetailsPopup.xaml Xaml/Controls/SetDetailsPopup.xaml.cs
```

- [ ] **Step 2: Build → 0 error**
- [ ] **Step 3: Commit**

```bash
git commit -m "chore: remove SetDetailsPopup (replaced by inline cards)"
```

---

## Task 10: Regression + manuel

- [ ] **Step 1: Tüm unit testler**

Run: `dotnet test FreakLete.Core.Tests`
Expected: Tüm testler PASS.

- [ ] **Step 2: Mobile build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Manuel Android**

- Egzersiz seç → 1 set kartı görünür.
- `+ Add Set` → yeni kart, değerler önceki setten kopyalandı, Weight focus.
- `− Remove Set` → son kart silinir, 1 kaldığında buton disabled.
- Detaylar toggle → RIR/Rest/Concentric görünür.
- Weight boş → hata, kart expand.
- Reps boş → hata, kart expand.
- Add Exercise → liste'e eklenir.
- Custom tracking → set kartı yok, akış bozulmamış.

---

## Risks

- **`Dispatcher.Dispatch` focus:** Kart DOM'a eklenir eklenmez Entry hazır olmayabilir — tek frame beklemek pratik yöntem.
- **`ShouldShowConcentric`:** Placeholder. Kaldırılan `StrengthTimingContainer.IsVisible` mantığını buraya birebir taşı; tier-based rule çalışmalı.
- **Mevcut field referansları:** `SetCountEntry`, `RepCountEntry`, `RirEntry`, `RestSecondsEntry`, `ConcentricTimeEntry` → XAML'den kaldırılınca code-behind hataları tarar; tümünü sil.
- **Popup silme sırası:** Task 9 → Task 8 sonrası olmalı; aksi halde Task 8'de code-behind popup import'u olabiliyorsa build kırılır.
