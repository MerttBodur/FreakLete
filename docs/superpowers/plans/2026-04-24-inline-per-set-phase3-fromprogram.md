# Phase 3 — AddWorkoutFromProgramPage Inline Per-Set

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans.

**Goal:** `AddWorkoutFromProgramPage`'de program şablonundan gelen her egzersizi inline `SetCardView` listesi ile render et. Kullanıcı her set'in ağırlık/tekrar/RIR/Rest/Concentric alanlarını editleyebilsin. `ExerciseInputRowBuilder.Build` (legacy single-row) akışı kaldırılacak.

**Architecture:** `ProgramExerciseConverter` artık `SetDetail` listesi üretir (N set, aynı Reps ve RestSeconds template'ten). `ProgramExerciseRowBuilder` (yeni helper) her egzersiz için bir card + SetCardView listesi + Add/Remove Set butonları render eder. `AddWorkoutFromProgramPage.OnSaveClicked` her row'un güncel `List<SetDetail>`'ini okur, `sets` alanı ile POST eder. Legacy tek satır form silinir.

**Tech Stack:** .NET MAUI 10, C#, XAML. **Bağımlılık:** Phase 2 tamamlanmış olmalı (SetCardView + SetDetail extended + ExerciseEntryLegacyDeriver).

---

## Task 1: `ProgramExerciseConverter` — per-set list üret

**Files:**
- Modify: `Helpers/ProgramExerciseConverter.cs`

- [ ] **Step 1: `Convert` metodunun return'ünü Sets listesi doldurulmuş haliyle değiştir**

```csharp
public static ExerciseEntry Convert(ProgramExerciseResponse pe)
{
    var catalogItem = ExerciseCatalog.GetByName(pe.ExerciseName);
    string category = !string.IsNullOrWhiteSpace(pe.ExerciseCategory)
        ? pe.ExerciseCategory
        : catalogItem?.Category ?? "Push";

    string trackingMode = catalogItem is not null
        ? catalogItem.TrackingMode.ToString()
        : nameof(ExerciseTrackingMode.Strength);

    int reps = ParseReps(pe.RepsOrDuration);
    int setCount = pe.Sets > 0 ? pe.Sets : 1;

    var sets = new List<SetDetail>();
    if (trackingMode == nameof(ExerciseTrackingMode.Strength))
    {
        for (int i = 1; i <= setCount; i++)
        {
            sets.Add(new SetDetail
            {
                SetNumber = i,
                Reps = reps,
                Weight = null,
                Rir = null,
                RestSeconds = pe.RestSeconds,
                ConcentricSeconds = null
            });
        }
    }

    return new ExerciseEntry
    {
        ExerciseName = pe.ExerciseName,
        ExerciseCategory = category,
        TrackingMode = trackingMode,
        SetsCount = setCount,
        Reps = reps,
        RestSeconds = pe.RestSeconds,
        RIR = null,
        Sets = sets
    };
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Helpers/ProgramExerciseConverter.cs
git commit -m "feat: ProgramExerciseConverter produces SetDetail list"
```

---

## Task 2: Unit test for converter

**Files:**
- Create: `FreakLete.Core.Tests/ProgramExerciseConverterPerSetTests.cs`
- Modify: `FreakLete.Core.Tests/FreakLete.Core.Tests.csproj`

- [ ] **Step 1: csproj'a link (yoksa)**

```xml
<Compile Include="..\Helpers\ProgramExerciseConverter.cs" Link="Linked\ProgramExerciseConverter.cs" />
```

- [ ] **Step 2: Test**

```csharp
using FreakLete.Helpers;

namespace FreakLete.Core.Tests;

public class ProgramExerciseConverterPerSetTests
{
    [Fact]
    public void Convert_Strength_BuildsSetDetailsFromSetsCount()
    {
        var pe = new ProgramExerciseResponse
        {
            ExerciseName = "Bench Press",
            ExerciseCategory = "Push",
            Sets = 3,
            RepsOrDuration = "5",
            RestSeconds = 120
        };

        var entry = ProgramExerciseConverter.Convert(pe);

        Assert.Equal(3, entry.Sets.Count);
        Assert.Equal(1, entry.Sets[0].SetNumber);
        Assert.Equal(3, entry.Sets[2].SetNumber);
        Assert.All(entry.Sets, s => Assert.Equal(5, s.Reps));
        Assert.All(entry.Sets, s => Assert.Equal(120, s.RestSeconds));
        Assert.All(entry.Sets, s => Assert.Null(s.Weight));
    }

    [Fact]
    public void Convert_ZeroSetsCount_DefaultsToOne()
    {
        var pe = new ProgramExerciseResponse
        {
            ExerciseName = "Bench Press",
            Sets = 0,
            RepsOrDuration = "8"
        };

        var entry = ProgramExerciseConverter.Convert(pe);

        Assert.Equal(1, entry.SetsCount);
        Assert.Single(entry.Sets);
    }
}
```

> **NOT:** Mevcut `ProgramExerciseResponse` tipi `FreakLete.Core.Tests/TestDtos.cs`'te link'li veya reuse edilebiliyor. Mevcut test'leri inceleyerek doğru tip yolunu kullan.

- [ ] **Step 3: Test koş**

Run: `dotnet test FreakLete.Core.Tests --filter "FullyQualifiedName~ProgramExerciseConverterPerSetTests"`
Expected: 2/2 PASS.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Core.Tests/ProgramExerciseConverterPerSetTests.cs FreakLete.Core.Tests/FreakLete.Core.Tests.csproj
git commit -m "test: ProgramExerciseConverter produces per-set list"
```

---

## Task 3: `ProgramExerciseRowBuilder` — inline card + SetCardView listesi

**Files:**
- Create: `Helpers/ProgramExerciseRowBuilder.cs`

- [ ] **Step 1: Yeni builder**

```csharp
using FreakLete.Models;
using FreakLete.Services;
using FreakLete.Xaml.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Helpers;

public static class ProgramExerciseRowBuilder
{
    public sealed class RowData
    {
        public ExerciseEntry Entry { get; init; } = null!;
        public ProgramExerciseResponse TemplateExercise { get; init; } = null!;
        public List<SetDetail> Sets { get; init; } = [];
        public List<SetCardView> Cards { get; init; } = [];
        public VerticalStackLayout CardsContainer { get; init; } = null!;
        public Button RemoveSetButton { get; init; } = null!;
        public bool ShowConcentric { get; init; }
    }

    public static (View View, RowData Data) Build(
        ProgramExerciseResponse template,
        ExerciseEntry prefilled)
    {
        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            BackgroundColor = ColorResources.GetColor("SurfaceRaised", "#1D1828"),
            Stroke = new SolidColorBrush(ColorResources.GetColor("SurfaceBorder", "#342D46")),
            StrokeThickness = 1,
            Padding = new Thickness(16, 14)
        };

        var content = new VerticalStackLayout { Spacing = 10 };

        content.Children.Add(new Label
        {
            Text = template.ExerciseName,
            FontSize = 15,
            FontFamily = "OpenSansSemibold",
            TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB")
        });

        string hint = ProgramExerciseConverter.BuildTemplateHint(template);
        if (!string.IsNullOrEmpty(hint))
        {
            content.Children.Add(new Label
            {
                Text = hint,
                FontSize = 11,
                FontFamily = "OpenSansRegular",
                TextColor = ColorResources.GetColor("AccentGlow", "#A78BFA")
            });
        }

        var cardsContainer = new VerticalStackLayout { Spacing = 8 };
        content.Children.Add(cardsContainer);

        bool showConcentric = prefilled.TrackingMode == nameof(ExerciseTrackingMode.Strength);

        var data = new RowData
        {
            Entry = prefilled,
            TemplateExercise = template,
            Sets = prefilled.Sets.ToList(),
            CardsContainer = cardsContainer,
            RemoveSetButton = new Button
            {
                Text = AppLanguage.NewWorkoutRemoveSet,
                Style = Application.Current?.Resources["SecondaryButton"] as Style
            },
            ShowConcentric = showConcentric
        };

        foreach (var s in data.Sets)
            AddCard(data, s);
        UpdateRemoveEnabled(data);

        var addBtn = new Button
        {
            Text = AppLanguage.NewWorkoutAddSet,
            Style = Application.Current?.Resources["SecondaryButton"] as Style
        };
        addBtn.Clicked += (_, _) =>
        {
            var prev = data.Sets.Count > 0 ? data.Sets[^1] : null;
            var next = new SetDetail
            {
                SetNumber = data.Sets.Count + 1,
                Reps = prev?.Reps ?? 0,
                Weight = prev?.Weight,
                Rir = prev?.Rir,
                RestSeconds = prev?.RestSeconds,
                ConcentricSeconds = prev?.ConcentricSeconds
            };
            data.Sets.Add(next);
            AddCard(data, next);
            UpdateRemoveEnabled(data);
        };

        data.RemoveSetButton.Clicked += (_, _) =>
        {
            if (data.Sets.Count <= 1) return;
            data.Sets.RemoveAt(data.Sets.Count - 1);
            var lastCard = data.Cards[^1];
            data.CardsContainer.Children.Remove(lastCard);
            data.Cards.RemoveAt(data.Cards.Count - 1);
            UpdateRemoveEnabled(data);
        };

        var btnGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        btnGrid.Children.Add(data.RemoveSetButton);
        Grid.SetColumn(data.RemoveSetButton, 0);
        btnGrid.Children.Add(addBtn);
        Grid.SetColumn(addBtn, 1);
        content.Children.Add(btnGrid);

        card.Content = content;
        return (card, data);
    }

    private static void AddCard(RowData data, SetDetail set)
    {
        var view = new SetCardView
        {
            SetNumber = set.SetNumber,
            Weight = set.Weight,
            Reps = set.Reps > 0 ? set.Reps : null,
            Rir = set.Rir,
            RestSeconds = set.RestSeconds,
            ConcentricSeconds = set.ConcentricSeconds,
            ShowConcentric = data.ShowConcentric
        };
        view.ValueChanged += (_, _) =>
        {
            set.Weight = view.Weight;
            set.Reps = view.Reps ?? 0;
            set.Rir = view.Rir;
            set.RestSeconds = view.RestSeconds;
            set.ConcentricSeconds = view.ConcentricSeconds;
        };
        data.Cards.Add(view);
        data.CardsContainer.Children.Add(view);
    }

    private static void UpdateRemoveEnabled(RowData data) =>
        data.RemoveSetButton.IsEnabled = data.Sets.Count > 1;

    public static ExerciseEntry ReadValues(RowData data)
    {
        var entry = data.Entry;
        if (data.Sets.Count == 0)
        {
            entry.SetsCount = 0;
            entry.Sets = [];
            return entry;
        }

        for (int i = 0; i < data.Sets.Count; i++)
            data.Sets[i].SetNumber = i + 1;

        entry.Sets = data.Sets.ToList();

        if (entry.TrackingMode == nameof(ExerciseTrackingMode.Strength))
        {
            var legacy = ExerciseEntryLegacyDeriver.Derive(entry.Sets);
            entry.SetsCount = legacy.SetsCount;
            entry.Reps = legacy.Reps;
            entry.Metric1Value = legacy.MaxWeight;
            entry.Metric1Unit = legacy.MaxWeight.HasValue ? "kg" : string.Empty;
            entry.RIR = legacy.Rir;
            entry.RestSeconds = legacy.RestSeconds;
            entry.ConcentricTimeSeconds = legacy.ConcentricSeconds;
        }

        return entry;
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Helpers/ProgramExerciseRowBuilder.cs
git commit -m "feat: add ProgramExerciseRowBuilder with inline SetCardView list"
```

---

## Task 4: `AddWorkoutFromProgramPage` — yeni builder'a geç

**Files:**
- Modify: `CodeBehind/AddWorkoutFromProgramPage.xaml.cs`

- [ ] **Step 1: Field tipi değişikliği**

```csharp
private readonly List<ProgramExerciseRowBuilder.RowData> _rowData = [];
```

- [ ] **Step 2: `BuildExerciseRows`**

```csharp
private void BuildExerciseRows()
{
    ExercisesContainer.Children.Clear();
    _rowData.Clear();

    var exercises = _session.Exercises ?? [];
    foreach (var pe in exercises.OrderBy(e => e.Order))
    {
        var prefilled = ProgramExerciseConverter.Convert(pe);
        var (view, data) = ProgramExerciseRowBuilder.Build(pe, prefilled);
        _rowData.Add(data);
        ExercisesContainer.Children.Add(view);
    }
}
```

- [ ] **Step 3: `OnSaveClicked` — per-set POST body + validation**

```csharp
private async void OnSaveClicked(object? sender, EventArgs e)
{
    ErrorLabel.IsVisible = false;

    var exercises = new List<object>();
    foreach (var row in _rowData)
    {
        var entry = ProgramExerciseRowBuilder.ReadValues(row);

        if (entry.TrackingMode == nameof(ExerciseTrackingMode.Strength))
        {
            if (entry.Sets.Count == 0)
            {
                ErrorLabel.Text = $"{entry.ExerciseName}: {AppLanguage.AddFromProgramSetRequired}";
                ErrorLabel.IsVisible = true;
                return;
            }

            for (int i = 0; i < entry.Sets.Count; i++)
            {
                var s = entry.Sets[i];
                if (s.Reps <= 0)
                {
                    ErrorLabel.Text = $"{entry.ExerciseName}: {AppLanguage.NewWorkoutSetDetailsRepsRequired}";
                    ErrorLabel.IsVisible = true;
                    row.Cards[i].IsExpanded = true;
                    return;
                }
                if (!s.Weight.HasValue || s.Weight.Value <= 0)
                {
                    ErrorLabel.Text = $"{entry.ExerciseName}: {AppLanguage.NewWorkoutSetDetailsWeightRequired}";
                    ErrorLabel.IsVisible = true;
                    row.Cards[i].IsExpanded = true;
                    return;
                }
            }
        }

        exercises.Add(new
        {
            exerciseName = entry.ExerciseName,
            exerciseCategory = entry.ExerciseCategory,
            trackingMode = entry.TrackingMode,
            setsCount = entry.SetsCount,
            sets = entry.Sets.Select(s => new
            {
                setNumber = s.SetNumber,
                reps = s.Reps,
                weight = s.Weight,
                rir = s.Rir,
                restSeconds = s.RestSeconds,
                concentricTimeSeconds = s.ConcentricSeconds
            }).ToList(),
            reps = entry.Reps,
            rir = entry.RIR,
            restSeconds = entry.RestSeconds,
            groundContactTimeMs = entry.GroundContactTimeMs,
            concentricTimeSeconds = entry.ConcentricTimeSeconds,
            metric1Value = entry.Metric1Value,
            metric1Unit = entry.Metric1Unit,
            metric2Value = entry.Metric2Value,
            metric2Unit = entry.Metric2Unit
        });
    }

    if (exercises.Count == 0)
    {
        ErrorLabel.Text = AppLanguage.AddFromProgramNeedExercise;
        ErrorLabel.IsVisible = true;
        return;
    }

    SaveButton.IsEnabled = false;
    SaveButton.Text = AppLanguage.AddFromProgramSaving;

    var workoutData = new
    {
        workoutName = _workoutName,
        workoutDate = $"{WorkoutDatePicker.Date:yyyy-MM-dd}",
        exercises
    };

    var result = await _api.CreateWorkoutAsync(workoutData);
    if (result.Success)
    {
        await Navigation.PopAsync(true);
    }
    else
    {
        SaveButton.IsEnabled = true;
        SaveButton.Text = AppLanguage.AddFromProgramSave;
        ErrorLabel.Text = result.Error ?? AppLanguage.AddFromProgramFailed;
        ErrorLabel.IsVisible = true;
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 5: Commit**

```bash
git add CodeBehind/AddWorkoutFromProgramPage.xaml.cs
git commit -m "feat: inline per-set input on AddWorkoutFromProgramPage"
```

---

## Task 5: Eski `ExerciseInputRowBuilder.Build` (legacy single-row) kaldır

**Files:**
- Modify: `Helpers/ExerciseInputRowBuilder.cs`

- [ ] **Step 1: Sil**

- `Build(ProgramExerciseResponse, ExerciseEntry)` metodunu sil.
- `ExerciseRowData` içindeki legacy alanları (`SetsEntry`, `RepsEntry`, `RirEntry`, `RestEntry`) sil.
- `ReadValues` içindeki "Legacy mode" bloğunu sil.

> **NOT:** `BuildLive` `StartWorkoutSessionPage` tarafından kullanılıyor. Dokunma — Phase 4'te değiştirilecek.

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Helpers/ExerciseInputRowBuilder.cs
git commit -m "chore: remove legacy single-row ExerciseInputRowBuilder.Build"
```

---

## Task 6: Regression + manuel

- [ ] **Step 1: Tüm unit testler**

Run: `dotnet test FreakLete.Core.Tests`
Expected: PASS.

- [ ] **Step 2: Mobile build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Manuel Android**

- Program seç → "Add Workout From Session" → her egzersiz için N adet set kartı görünür, Reps template'ten dolu.
- Her set'in Weight'ını gir → Save.
- Add Set / Remove Set çalışıyor, min 1 set.
- Kaydedilen workout geçmişte per-set değerleri doğru.

---

## Risks

- **`ProgramExerciseResponse.Sets = 0` edge:** `Convert` `setCount=1` varsayılır. Kullanıcı manuel Add Set yapabilir.
- **Duration/AMRAP template (reps = 0):** Set kartı reps=0 ile render edilir; validation Reps>0 dayatır.
- **`ExerciseInputRowBuilder.BuildLive` bozulmamalı:** Phase 4'te değişecek; Phase 3 sonunda `StartWorkoutSessionPage` çalışır durumda.
- **AppLanguage stringleri:** `NewWorkoutAddSet`, `NewWorkoutRemoveSet`, `NewWorkoutSetDetailsWeightRequired` Phase 2'de eklendi — reuse.
