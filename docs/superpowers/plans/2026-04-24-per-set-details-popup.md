# Per-Set Weight & Reps Popup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After user enters Set Count in the Exercise Builder (NewWorkoutPage), open a popup where they can enter weight and reps for each individual set; aggregate the result into the existing `ExerciseEntry` model.

**Architecture:** Add a new `SetDetailsPopup` ContentView (CommunityToolkit popup pattern, matching `TierCongratsPopup`). The popup collects a `List<SetDetail>` (weight+reps per set) and returns it via `TaskCompletionSource`. A static `SetDetailsAggregator` extracts the aggregation logic for testability. `NewWorkoutPage.OnAddExerciseClicked` is updated: for Strength tracking mode, instead of calling `BuildExerciseEntryFromInputs` directly, it opens the popup, awaits the result, then aggregates into `ExerciseEntry.Sets/Reps/Metric1Value` (matching the existing `ExerciseInputRowBuilder.BuildLive` aggregation pattern).

**Tech Stack:** .NET MAUI 10, CommunityToolkit.Maui (`ShowPopupAsync` / `ClosePopupAsync`), xUnit for tests. No backend changes.

**Scope decisions (intentional):**
- No backend/DB/migration changes — per-set data is collected in UI and aggregated on save.
- Aggregation: `Sets` = entered set count, `Reps` = last set's reps, `Metric1Value` = max weight across sets, `Metric1Unit` = `"kg"`.
- The existing "Rep Count" field on the page is kept as a *default reps* value that pre-fills every popup row.
- Only Strength tracking mode uses the popup. Custom tracking flow (Metric1/Metric2) is unchanged.
- Turkish + English UI strings via existing `AppLanguage` service.

---

## File Structure

**New files:**
- `Models/SetDetail.cs` — client-side class: `{ int SetNumber, int Reps, double? Weight }`.
- `Services/SetDetailsAggregator.cs` — static helper with `Aggregate(IReadOnlyList<SetDetail>) → (Sets, Reps, MaxWeight)` used by the popup save path and covered by unit tests.
- `Xaml/Controls/SetDetailsPopup.xaml` — popup `ContentView` layout.
- `Xaml/Controls/SetDetailsPopup.xaml.cs` — popup code-behind + `ShowAsync(Page, int setCount, int? defaultReps) → Task<List<SetDetail>?>`.
- `FreakLete.Core.Tests/SetDetailsAggregatorTests.cs` — tests for aggregation logic.

**Modified files:**
- `Services/AppLanguage.cs` — add strings: popup title, subtitle, column headers, Save/Cancel, validation error.
- `CodeBehind/NewWorkoutPage.xaml.cs` — intercept strength-mode Add click to open popup; update `BuildExerciseEntryFromInputs` signature to accept pre-collected set details; extend `FormatPrimarySummary` to show max weight.

---

## Task 1: Add `SetDetail` client model

**Files:**
- Create: `Models/SetDetail.cs`

- [ ] **Step 1: Write the model file**

```csharp
namespace FreakLete.Models;

public sealed class SetDetail
{
    public int SetNumber { get; init; }
    public int Reps { get; set; }
    public double? Weight { get; set; }
}
```

- [ ] **Step 2: Build to confirm no errors**

Run: `dotnet build FreakLete.csproj -c Debug --nologo -v q`
Expected: Build succeeded, 0 errors (warnings allowed).

- [ ] **Step 3: Commit**

```bash
git add Models/SetDetail.cs
git commit -m "feat: add SetDetail client model"
```

---

## Task 2: Add `SetDetailsAggregator` with failing tests first

**Files:**
- Create: `FreakLete.Core.Tests/SetDetailsAggregatorTests.cs`
- Create: `Services/SetDetailsAggregator.cs`

- [ ] **Step 1: Write the failing tests**

Create file `FreakLete.Core.Tests/SetDetailsAggregatorTests.cs`:

```csharp
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class SetDetailsAggregatorTests
{
    [Fact]
    public void Aggregate_ThreeIdenticalSets_ReturnsCountRepsAndWeight()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 10, Weight = 100 },
            new() { SetNumber = 2, Reps = 10, Weight = 100 },
            new() { SetNumber = 3, Reps = 10, Weight = 100 }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Equal(3, result.Sets);
        Assert.Equal(10, result.Reps);
        Assert.Equal(100, result.MaxWeight);
    }

    [Fact]
    public void Aggregate_VaryingWeight_ReturnsMaxWeight()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 10, Weight = 60 },
            new() { SetNumber = 2, Reps = 10, Weight = 80 },
            new() { SetNumber = 3, Reps = 10, Weight = 100 }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Equal(100, result.MaxWeight);
    }

    [Fact]
    public void Aggregate_VaryingReps_ReturnsLastSetReps()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 12, Weight = 100 },
            new() { SetNumber = 2, Reps = 10, Weight = 100 },
            new() { SetNumber = 3, Reps = 8, Weight = 100 }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Equal(8, result.Reps);
    }

    [Fact]
    public void Aggregate_AllWeightsNull_ReturnsNullMaxWeight()
    {
        var sets = new List<SetDetail>
        {
            new() { SetNumber = 1, Reps = 10, Weight = null },
            new() { SetNumber = 2, Reps = 10, Weight = null }
        };

        var result = SetDetailsAggregator.Aggregate(sets);

        Assert.Null(result.MaxWeight);
        Assert.Equal(2, result.Sets);
        Assert.Equal(10, result.Reps);
    }

    [Fact]
    public void Aggregate_EmptyList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SetDetailsAggregator.Aggregate(new List<SetDetail>()));
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

Run: `dotnet test FreakLete.Core.Tests --filter "FullyQualifiedName~SetDetailsAggregatorTests" --nologo -v q`
Expected: Build fails with `SetDetailsAggregator` not found (or `Services.SetDetailsAggregator` not found).

- [ ] **Step 3: Implement `SetDetailsAggregator`**

Create file `Services/SetDetailsAggregator.cs`:

```csharp
using FreakLete.Models;

namespace FreakLete.Services;

public static class SetDetailsAggregator
{
    public readonly record struct AggregatedResult(int Sets, int Reps, double? MaxWeight);

    public static AggregatedResult Aggregate(IReadOnlyList<SetDetail> sets)
    {
        if (sets.Count == 0)
        {
            throw new ArgumentException("Set list must not be empty.", nameof(sets));
        }

        int setCount = sets.Count;
        int lastReps = sets[^1].Reps;

        double? maxWeight = null;
        foreach (var s in sets)
        {
            if (s.Weight.HasValue && (maxWeight is null || s.Weight.Value > maxWeight.Value))
            {
                maxWeight = s.Weight.Value;
            }
        }

        return new AggregatedResult(setCount, lastReps, maxWeight);
    }
}
```

- [ ] **Step 4: Run tests to confirm they pass**

Run: `dotnet test FreakLete.Core.Tests --filter "FullyQualifiedName~SetDetailsAggregatorTests" --nologo -v q`
Expected: `Passed: 5, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add Services/SetDetailsAggregator.cs FreakLete.Core.Tests/SetDetailsAggregatorTests.cs
git commit -m "feat: add SetDetailsAggregator with aggregation logic"
```

---

## Task 3: Add `AppLanguage` strings for popup

**Files:**
- Modify: `Services/AppLanguage.cs`

- [ ] **Step 1: Locate the NewWorkout section in AppLanguage.cs**

Search with Grep for `NewWorkoutSetCount` inside `Services/AppLanguage.cs`. The new strings belong in the same section to match the existing Turkish-first / English-second pattern.

- [ ] **Step 2: Add new localized strings**

Add these members to the `AppLanguage` static class, placed immediately after the existing `NewWorkoutSetCount` property:

```csharp
public static string NewWorkoutSetDetailsTitle => IsTurkish ? "Set Detayları" : "Set Details";
public static string NewWorkoutSetDetailsSubtitle => IsTurkish
    ? "Her set için ağırlık ve tekrar sayısını gir."
    : "Enter weight and reps for each set.";
public static string NewWorkoutSetColumnSet => IsTurkish ? "Set" : "Set";
public static string NewWorkoutSetColumnWeight => IsTurkish ? "Ağırlık (kg)" : "Weight (kg)";
public static string NewWorkoutSetColumnReps => IsTurkish ? "Tekrar" : "Reps";
public static string NewWorkoutSetDetailsSave => IsTurkish ? "Kaydet" : "Save";
public static string NewWorkoutSetDetailsCancel => IsTurkish ? "İptal" : "Cancel";
public static string NewWorkoutSetDetailsRepsRequired => IsTurkish
    ? "Her set için tekrar sayısı girilmelidir."
    : "Reps must be entered for every set.";
```

- [ ] **Step 3: Build to verify no errors**

Run: `dotnet build FreakLete.csproj -c Debug --nologo -v q`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Services/AppLanguage.cs
git commit -m "feat: add localized strings for set details popup"
```

---

## Task 4: Create `SetDetailsPopup` XAML layout

**Files:**
- Create: `Xaml/Controls/SetDetailsPopup.xaml`

- [ ] **Step 1: Write the XAML**

Create file `Xaml/Controls/SetDetailsPopup.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.Xaml.Controls.SetDetailsPopup"
             BackgroundColor="Transparent">
    <Border BackgroundColor="{StaticResource Surface}"
            Stroke="{StaticResource SurfaceBorder}"
            StrokeThickness="1"
            StrokeShape="RoundRectangle 24"
            Padding="20"
            WidthRequest="340">
        <VerticalStackLayout Spacing="14">

            <Label x:Name="TitleLabel"
                   Style="{StaticResource SubHeadline}"
                   HorizontalTextAlignment="Center" />

            <Label x:Name="SubtitleLabel"
                   Style="{StaticResource BodyMuted}"
                   HorizontalTextAlignment="Center"
                   FontSize="12" />

            <Grid ColumnDefinitions="36,*,*"
                  ColumnSpacing="8"
                  Padding="4,0">
                <Label x:Name="SetHeaderLabel"
                       Grid.Column="0"
                       Style="{StaticResource BodyMuted}"
                       FontSize="10"
                       HorizontalTextAlignment="Center" />
                <Label x:Name="WeightHeaderLabel"
                       Grid.Column="1"
                       Style="{StaticResource BodyMuted}"
                       FontSize="10"
                       HorizontalTextAlignment="Center" />
                <Label x:Name="RepsHeaderLabel"
                       Grid.Column="2"
                       Style="{StaticResource BodyMuted}"
                       FontSize="10"
                       HorizontalTextAlignment="Center" />
            </Grid>

            <ScrollView MaximumHeightRequest="360">
                <VerticalStackLayout x:Name="RowsContainer" Spacing="6" />
            </ScrollView>

            <Label x:Name="ErrorLabel"
                   TextColor="{StaticResource Danger}"
                   FontSize="12"
                   IsVisible="False"
                   HorizontalTextAlignment="Center" />

            <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                <Button x:Name="CancelButton"
                        Grid.Column="0"
                        Style="{StaticResource SecondaryButton}"
                        Clicked="OnCancelClicked" />
                <Button x:Name="SaveButton"
                        Grid.Column="1"
                        BackgroundColor="{StaticResource Accent}"
                        TextColor="{StaticResource TextPrimary}"
                        FontFamily="OpenSansSemibold"
                        CornerRadius="18"
                        HeightRequest="44"
                        Clicked="OnSaveClicked" />
            </Grid>

        </VerticalStackLayout>
    </Border>
</ContentView>
```

- [ ] **Step 2: Build to confirm XAML parses**

Run: `dotnet build FreakLete.csproj -c Debug --nologo -v q`
Expected: Build fails because the code-behind file is missing. Do NOT commit yet — proceed to Task 5.

---

## Task 5: Create `SetDetailsPopup` code-behind

**Files:**
- Create: `Xaml/Controls/SetDetailsPopup.xaml.cs`

- [ ] **Step 1: Write the code-behind**

Create file `Xaml/Controls/SetDetailsPopup.xaml.cs`:

```csharp
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Xaml.Controls;

public partial class SetDetailsPopup : ContentView
{
    private readonly Page _page;
    private readonly List<Entry> _weightEntries = new();
    private readonly List<Entry> _repsEntries = new();
    private readonly TaskCompletionSource<List<SetDetail>?> _result = new();
    private bool _isClosing;

    private SetDetailsPopup(Page page, int setCount, int? defaultReps)
    {
        _page = page;
        InitializeComponent();
        ApplyLanguage();
        BuildRows(setCount, defaultReps);
    }

    public static async Task<List<SetDetail>?> ShowAsync(Page page, int setCount, int? defaultReps)
    {
        var popup = new SetDetailsPopup(page, setCount, defaultReps);
        _ = page.ShowPopupAsync(popup, new PopupOptions
        {
            CanBeDismissedByTappingOutsideOfPopup = false,
            PageOverlayColor = Color.FromArgb("#AA000000"),
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(24),
                Fill = new SolidColorBrush(Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Transparent),
                StrokeThickness = 0
            }
        });
        return await popup._result.Task;
    }

    private void ApplyLanguage()
    {
        TitleLabel.Text = AppLanguage.NewWorkoutSetDetailsTitle;
        SubtitleLabel.Text = AppLanguage.NewWorkoutSetDetailsSubtitle;
        SetHeaderLabel.Text = AppLanguage.NewWorkoutSetColumnSet;
        WeightHeaderLabel.Text = AppLanguage.NewWorkoutSetColumnWeight;
        RepsHeaderLabel.Text = AppLanguage.NewWorkoutSetColumnReps;
        CancelButton.Text = AppLanguage.NewWorkoutSetDetailsCancel;
        SaveButton.Text = AppLanguage.NewWorkoutSetDetailsSave;
    }

    private void BuildRows(int setCount, int? defaultReps)
    {
        for (int i = 1; i <= setCount; i++)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8
            };

            var setLabel = new Label
            {
                Text = i.ToString(),
                FontSize = 13,
                FontFamily = "OpenSansSemibold",
                TextColor = ColorResources.GetColor("AccentGlow", "#A78BFA"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(setLabel, 0);
            grid.Children.Add(setLabel);

            var weightEntry = BuildNumericEntry("0");
            Grid.SetColumn(weightEntry, 1);
            grid.Children.Add(weightEntry);
            _weightEntries.Add(weightEntry);

            var repsEntry = BuildNumericEntry("0");
            if (defaultReps is > 0)
            {
                repsEntry.Text = defaultReps.Value.ToString();
            }
            Grid.SetColumn(repsEntry, 2);
            grid.Children.Add(repsEntry);
            _repsEntries.Add(repsEntry);

            RowsContainer.Children.Add(grid);
        }
    }

    private static Entry BuildNumericEntry(string placeholder)
    {
        return new Entry
        {
            Placeholder = placeholder,
            Keyboard = Keyboard.Numeric,
            FontSize = 14,
            FontFamily = "OpenSansSemibold",
            TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB"),
            PlaceholderColor = ColorResources.GetColor("TextMuted", "#6B6780"),
            BackgroundColor = ColorResources.GetColor("Surface", "#13101C"),
            HorizontalTextAlignment = TextAlignment.Center
        };
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isClosing) return;

        var collected = new List<SetDetail>();
        for (int i = 0; i < _repsEntries.Count; i++)
        {
            if (!int.TryParse(_repsEntries[i].Text, out int reps) || reps <= 0)
            {
                ErrorLabel.Text = AppLanguage.NewWorkoutSetDetailsRepsRequired;
                ErrorLabel.IsVisible = true;
                return;
            }

            double? weight = null;
            if (!string.IsNullOrWhiteSpace(_weightEntries[i].Text))
            {
                if (MetricInput.TryParseFlexibleDouble(_weightEntries[i].Text, out double w) && w > 0)
                {
                    weight = w;
                }
            }

            collected.Add(new SetDetail
            {
                SetNumber = i + 1,
                Reps = reps,
                Weight = weight
            });
        }

        _isClosing = true;
        _result.TrySetResult(collected);
        await _page.ClosePopupAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _result.TrySetResult(null);
        await _page.ClosePopupAsync();
    }
}
```

- [ ] **Step 2: Build to confirm compilation**

Run: `dotnet build FreakLete.csproj -c Debug --nologo -v q`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Xaml/Controls/SetDetailsPopup.xaml Xaml/Controls/SetDetailsPopup.xaml.cs
git commit -m "feat: add SetDetailsPopup control for per-set input"
```

---

## Task 6: Wire popup into `NewWorkoutPage.OnAddExerciseClicked`

**Files:**
- Modify: `CodeBehind/NewWorkoutPage.xaml.cs`

- [ ] **Step 1: Replace `OnAddExerciseClicked` to open the popup for strength mode**

Locate the existing `OnAddExerciseClicked` method (around line 138 in `CodeBehind/NewWorkoutPage.xaml.cs`). Replace the whole method with:

```csharp
private async void OnAddExerciseClicked(object? sender, EventArgs e)
{
    ClearError();

    if (_selectedExerciseItem is null)
    {
        ShowError(AppLanguage.NewWorkoutChooseFirst);
        return;
    }

    bool isStrength = _selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength;
    List<SetDetail>? setDetails = null;

    if (isStrength)
    {
        if (!int.TryParse(SetCountEntry.Text, out int setCount) || setCount <= 0)
        {
            ShowError(AppLanguage.NewWorkoutSetError);
            return;
        }

        int? defaultReps = int.TryParse(RepCountEntry.Text, out int r) && r > 0 ? r : null;
        setDetails = await Xaml.Controls.SetDetailsPopup.ShowAsync(this, setCount, defaultReps);
        if (setDetails is null)
        {
            // user cancelled — keep form intact so they can try again
            return;
        }
    }

    ExerciseEntry? entry = BuildExerciseEntryFromInputs(setDetails);
    if (entry is null)
    {
        return;
    }

    _exercises.Add(entry);

    ClearExerciseInputs();
    RefreshExercisesList();
}
```

- [ ] **Step 2: Replace `BuildExerciseEntryFromInputs` to accept pre-collected set details**

Locate the existing `BuildExerciseEntryFromInputs` method (around line 358). Replace the entire method (signature + body) with:

```csharp
private ExerciseEntry? BuildExerciseEntryFromInputs(List<SetDetail>? setDetails = null)
{
    if (_selectedExerciseItem is null)
    {
        ShowError(AppLanguage.NewWorkoutChooseFirst);
        return null;
    }

    if (_selectedExerciseItem.TrackingMode == ExerciseTrackingMode.Strength)
    {
        if (setDetails is null || setDetails.Count == 0)
        {
            ShowError(AppLanguage.NewWorkoutSetError);
            return null;
        }

        int? restSeconds = null;
        int? rir = null;

        if (!string.IsNullOrWhiteSpace(RirEntry.Text))
        {
            if (!int.TryParse(RirEntry.Text, out int parsedRir) || parsedRir < 0 || parsedRir > 5)
            {
                ShowError(AppLanguage.NewWorkoutRirError);
                return null;
            }
            rir = parsedRir;
        }

        if (!string.IsNullOrWhiteSpace(RestSecondsEntry.Text))
        {
            if (!int.TryParse(RestSecondsEntry.Text, out int parsedRest) || parsedRest <= 0)
            {
                ShowError(AppLanguage.NewWorkoutRestError);
                return null;
            }
            restSeconds = parsedRest;
        }

        double? concentricTime = null;
        if (!string.IsNullOrWhiteSpace(ConcentricTimeEntry.Text))
        {
            if (!MetricInput.TryParseFlexibleDouble(ConcentricTimeEntry.Text, out double parsedTime) || parsedTime <= 0)
            {
                ShowError(AppLanguage.NewWorkoutConcentricError);
                return null;
            }
            concentricTime = parsedTime;
        }

        var agg = SetDetailsAggregator.Aggregate(setDetails);

        return new ExerciseEntry
        {
            ExerciseName = _selectedExerciseItem.Name,
            ExerciseCategory = _selectedExerciseItem.Category,
            TrackingMode = nameof(ExerciseTrackingMode.Strength),
            Sets = agg.Sets,
            Reps = agg.Reps,
            RIR = rir,
            RestSeconds = restSeconds,
            ConcentricTimeSeconds = concentricTime,
            Metric1Value = agg.MaxWeight,
            Metric1Unit = agg.MaxWeight is null ? string.Empty : "kg"
        };
    }

    if (!MetricInput.TryParseFlexibleDouble(Metric1Entry.Text, out double metric1) || metric1 <= 0)
    {
        ShowError(AppLanguage.FormatMustBePositive(_selectedExerciseItem.PrimaryLabel));
        return null;
    }

    double? metric2 = null;
    if (_selectedExerciseItem.HasSecondaryMetric)
    {
        if (!MetricInput.TryParseFlexibleDouble(Metric2Entry.Text, out double parsedMetric2) || parsedMetric2 <= 0)
        {
            ShowError(AppLanguage.FormatMustBePositive(_selectedExerciseItem.SecondaryLabel));
            return null;
        }
        metric2 = parsedMetric2;
    }

    double? gct = null;
    if (_selectedExerciseItem.SupportsGroundContactTime && !string.IsNullOrWhiteSpace(GroundContactTimeEntry.Text))
    {
        if (!MetricInput.TryParseFlexibleDouble(GroundContactTimeEntry.Text, out double parsedGctSeconds) || parsedGctSeconds <= 0)
        {
            ShowError(AppLanguage.NewWorkoutGctError);
            return null;
        }
        gct = MetricInput.SecondsToMilliseconds(parsedGctSeconds);
    }

    return new ExerciseEntry
    {
        ExerciseName = _selectedExerciseItem.Name,
        ExerciseCategory = _selectedExerciseItem.Category,
        TrackingMode = nameof(ExerciseTrackingMode.Custom),
        Metric1Value = metric1,
        Metric1Unit = _selectedExerciseItem.PrimaryUnit,
        Metric2Value = metric2,
        Metric2Unit = _selectedExerciseItem.SecondaryUnit,
        GroundContactTimeMs = gct
    };
}
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build FreakLete.csproj -c Debug --nologo -v q`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add CodeBehind/NewWorkoutPage.xaml.cs
git commit -m "feat: open set details popup after set count entry"
```

---

## Task 7: Update session summary text to include max weight

**Files:**
- Modify: `CodeBehind/NewWorkoutPage.xaml.cs` (only the `FormatPrimarySummary` method)

**Context:** Each strength exercise now carries `Metric1Value` (max weight) after aggregation. The existing "Session Exercises" card summary should surface this so the user can verify their input at a glance.

- [ ] **Step 1: Replace `FormatPrimarySummary`**

Locate `FormatPrimarySummary` (around line 473). Replace the method body with:

```csharp
private static string FormatPrimarySummary(ExerciseEntry entry)
{
    if (entry.TrackingMode == nameof(ExerciseTrackingMode.Custom))
    {
        ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory(entry.ExerciseName, entry.ExerciseCategory);
        if (item is not null)
        {
            return $"{item.PrimaryLabel}: {entry.Metric1Value:0.##} {entry.Metric1Unit}";
        }
    }

    string core = entry.RIR.HasValue
        ? $"Sets x Reps: {entry.Sets} x {entry.Reps} (RIR{entry.RIR.Value})"
        : $"Sets x Reps: {entry.Sets} x {entry.Reps}";

    if (entry.Metric1Value is > 0 && !string.IsNullOrEmpty(entry.Metric1Unit))
    {
        core += $" @ {entry.Metric1Value:0.#} {entry.Metric1Unit}";
    }

    return core;
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.csproj -c Debug --nologo -v q`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add CodeBehind/NewWorkoutPage.xaml.cs
git commit -m "feat: show max weight in session exercises summary"
```

---

## Task 8: Manual verification on Android

**Files:** (no changes — runtime verification only)

- [ ] **Step 1: Deploy to Android device/emulator**

Run: `dotnet build FreakLete.csproj -t:Run -f net10.0-android -c Debug`
Expected: App launches on device.

- [ ] **Step 2: Verify the happy path**

1. Log in → tap "New Workout".
2. Enter a date and workout name → tap "Continue".
3. Tap "Browse" → pick "Bench Press".
4. Enter Set Count = `3`, Rep Count = `8`.
5. Tap "Add".
6. **Expect:** SetDetailsPopup opens with 3 rows; Reps columns are pre-filled with `8`; Weight columns are empty.
7. Enter weights `60`, `80`, `100` and reps `8`, `8`, `6` → tap "Kaydet".
8. **Expect:** Popup closes; "Session Exercises" card shows `Bench Press — Sets x Reps: 3 x 6 @ 100 kg`.

- [ ] **Step 3: Verify cancel path**

1. Tap "Browse" → pick another exercise.
2. Enter Set Count = `4`, Rep Count = `10` → tap "Add".
3. Popup opens → tap "İptal".
4. **Expect:** Popup closes; exercise is NOT added; form inputs remain populated so the user can try again.

- [ ] **Step 4: Verify validation**

1. Enter Set Count = `2`, leave Rep Count empty → tap "Add".
2. Popup opens with 2 rows; reps fields empty.
3. Leave weight and reps fields empty → tap "Kaydet".
4. **Expect:** Inline error `"Her set için tekrar sayısı girilmelidir."` appears above the buttons; popup stays open.

- [ ] **Step 5: Verify custom (non-strength) mode is untouched**

1. Tap "Browse" → pick "Vertical Jump" (Custom tracking).
2. **Expect:** Strength inputs hidden; Metric1 (Height) shown.
3. Enter `40` cm → tap "Add".
4. **Expect:** No popup opens; exercise is added directly to the list.

- [ ] **Step 6: No commit — verification-only step**

If all five verifications pass, move on to Task 9.

---

## Task 9: Run core test suite to confirm no regressions

**Files:** (no changes — verification only)

- [ ] **Step 1: Run all Core tests**

Run: `dotnet test FreakLete.Core.Tests --nologo -v q`
Expected: `Passed: 17+, Failed: 0` (12 existing `ExerciseCatalogTests` + 5 new `SetDetailsAggregatorTests`).

- [ ] **Step 2: No commit — verification-only step**

---

## Self-Review Results

**Spec coverage:**
- ✓ Popup opens after Set Count entered — Task 6, Step 1
- ✓ Each set has its own weight + reps input — Task 5, `BuildRows`
- ✓ Save/Cancel handling — Task 5, `OnSaveClicked` / `OnCancelClicked`
- ✓ Per-set data aggregated into existing `ExerciseEntry` — Task 2 (`SetDetailsAggregator`) + Task 6 (`BuildExerciseEntryFromInputs`)
- ✓ Custom tracking mode untouched — Task 6 `isStrength` branch
- ✓ Localization (TR/EN) — Task 3
- ✓ Session summary surfaces max weight — Task 7

**Placeholder scan:** None. Every step has the exact code or the exact command.

**Type consistency:**
- `SetDetail.SetNumber` / `Reps` / `Weight` — used consistently across Tasks 1, 2, 5, 6.
- `SetDetailsAggregator.AggregatedResult(Sets, Reps, MaxWeight)` — consumed in Task 6 via `agg.Sets` / `agg.Reps` / `agg.MaxWeight`.
- `SetDetailsPopup.ShowAsync(Page, int, int?)` — signature matches call site in Task 6.

**Known follow-up (out of scope for this plan):** Persisting per-set data end-to-end (new entity + migration + DTO list) would require its own plan. The current plan only aggregates on save — if the user ever wants per-set history preserved on the backend, treat that as a separate feature.
