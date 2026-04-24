# Phase 4 — StartWorkoutSessionPage + Session Exercises Inline Expand/Edit

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans.

**Goal:** `StartWorkoutSessionPage`'de canlı session akışını `SetCardView` tabanlı yap: her set'in kendi kartı, "Complete" butonu, tamamlanan set faded. Ayrıca `NewWorkoutPage` ve `WorkoutPreviewPage`'deki "Session Exercises" listesinde kart tıklaması inline expand yap ve `SetCardView` ile per-set inline edit yap. Kalan legacy `ExerciseInputRowBuilder.BuildLive` temizlenecek.

**Architecture:**
- `LiveSessionRowBuilder` (yeni helper) her egzersiz için card + `SetCardView` listesi + Complete state üretir.
- `StartWorkoutSessionPage`: exercise tap → expand; set "Complete" → opacity 0.5 + sonrakine focus.
- `NewWorkoutPage` Session Exercises ItemTemplate → tap handler → expanded panel içinde N `SetCardView` + save.
- `WorkoutPreviewPage` expanded display — read-only `SetCardView`.
- `WorkoutSessionState` değişmez (zaten `ExerciseEntry.Sets` taşıyor).

**Tech Stack:** .NET MAUI 10, C#, XAML. **Bağımlılık:** Phase 1-3 tamamlanmış olmalı.

---

## Task 1: `LiveSessionRowBuilder`

**Files:**
- Create: `Helpers/LiveSessionRowBuilder.cs`

- [ ] **Step 1: Builder**

```csharp
using FreakLete.Models;
using FreakLete.Services;
using FreakLete.Xaml.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Helpers;

public static class LiveSessionRowBuilder
{
    public sealed class RowData
    {
        public ExerciseEntry Entry { get; init; } = null!;
        public List<SetDetail> Sets { get; init; } = [];
        public List<SetCardView> Cards { get; init; } = [];
        public HashSet<int> CompletedIndexes { get; } = new();
        public VerticalStackLayout CardsContainer { get; init; } = null!;
        public Button RemoveSetButton { get; init; } = null!;
        public bool ShowConcentric { get; init; }
    }

    public static (View View, RowData Data) Build(ExerciseEntry entry)
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

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        headerGrid.Children.Add(new Label
        {
            Text = entry.ExerciseName,
            FontSize = 15,
            FontFamily = "OpenSansSemibold",
            TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB"),
            VerticalOptions = LayoutOptions.Center
        });
        var arrow = new Label
        {
            Text = "▼",
            FontSize = 12,
            FontFamily = "OpenSansSemibold",
            TextColor = ColorResources.GetColor("TextSecondary", "#B3B2C5"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };
        headerGrid.Children.Add(arrow);
        Grid.SetColumn(arrow, 1);
        content.Children.Add(headerGrid);

        var cardsContainer = new VerticalStackLayout { Spacing = 8 };
        content.Children.Add(cardsContainer);

        bool showConcentric = entry.TrackingMode == nameof(ExerciseTrackingMode.Strength);

        var data = new RowData
        {
            Entry = entry,
            Sets = entry.Sets.ToList(),
            CardsContainer = cardsContainer,
            RemoveSetButton = new Button
            {
                Text = AppLanguage.NewWorkoutRemoveSet,
                Style = Application.Current?.Resources["SecondaryButton"] as Style
            },
            ShowConcentric = showConcentric
        };

        if (data.Sets.Count == 0)
            data.Sets.Add(new SetDetail { SetNumber = 1, Reps = 0 });

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
            var prev = data.Sets[^1];
            var next = new SetDetail
            {
                SetNumber = data.Sets.Count + 1,
                Reps = prev.Reps,
                Weight = prev.Weight,
                Rir = prev.Rir,
                RestSeconds = prev.RestSeconds,
                ConcentricSeconds = prev.ConcentricSeconds
            };
            data.Sets.Add(next);
            AddCard(data, next);
            UpdateRemoveEnabled(data);
        };

        data.RemoveSetButton.Clicked += (_, _) =>
        {
            if (data.Sets.Count <= 1) return;
            int lastIndex = data.Sets.Count - 1;
            data.Sets.RemoveAt(lastIndex);
            data.CardsContainer.Children.Remove(data.Cards[lastIndex]);
            data.Cards.RemoveAt(lastIndex);
            data.CompletedIndexes.Remove(lastIndex);
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

        headerGrid.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                cardsContainer.IsVisible = !cardsContainer.IsVisible;
                btnGrid.IsVisible = cardsContainer.IsVisible;
                arrow.Text = cardsContainer.IsVisible ? "▼" : "▶";
            })
        });

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

        var completeBtn = new Button
        {
            Text = "✓",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            BackgroundColor = ColorResources.GetColor("Accent", "#7C3AED"),
            TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB"),
            Padding = 0
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        row.Children.Add(view);
        Grid.SetColumn(view, 0);
        row.Children.Add(completeBtn);
        Grid.SetColumn(completeBtn, 1);

        int index = data.Cards.Count;
        completeBtn.Clicked += (_, _) =>
        {
            data.CompletedIndexes.Add(index);
            row.Opacity = 0.5;
            completeBtn.IsEnabled = false;
            view.IsReadOnly = true;

            if (index + 1 < data.Cards.Count)
                Application.Current?.Dispatcher.Dispatch(() => data.Cards[index + 1].FocusWeight());
        };

        data.Cards.Add(view);
        data.CardsContainer.Children.Add(row);
    }

    private static void UpdateRemoveEnabled(RowData data) =>
        data.RemoveSetButton.IsEnabled = data.Sets.Count > 1;
}
```

- [ ] **Step 2: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Commit**

```bash
git add Helpers/LiveSessionRowBuilder.cs
git commit -m "feat: add LiveSessionRowBuilder for live session per-set cards"
```

---

## Task 2: `StartWorkoutSessionPage` — yeni builder'a geç

**Files:**
- Modify: `CodeBehind/StartWorkoutSessionPage.xaml.cs`

- [ ] **Step 1: Field tipini değiştir**

```csharp
private readonly List<LiveSessionRowBuilder.RowData> _rowData = [];
```

- [ ] **Step 2: İnit methodunda yeni builder**

```csharp
foreach (var entry in _state.Exercises)
{
    var (view, data) = LiveSessionRowBuilder.Build(entry);
    _rowData.Add(data);
    ExercisesContainer.Children.Add(view);
}
```

> **NOT:** Mevcut kod `ExerciseInputRowBuilder.BuildLive` çağırıyordu. Çağrıyı değiştir. `WorkoutSessionState.FromTemplate` Phase 3 sonrası zaten `entry.Sets` dolu egzersizler üretmeli; eğer eski akış bırakılmışsa `ProgramExerciseConverter.Convert` ile üret.

- [ ] **Step 3: Save akışı — per-set POST body**

```csharp
var exercises = _rowData.Select(r =>
{
    var legacy = Helpers.ExerciseEntryLegacyDeriver.Derive(r.Sets);
    return new
    {
        exerciseName = r.Entry.ExerciseName,
        exerciseCategory = r.Entry.ExerciseCategory,
        trackingMode = r.Entry.TrackingMode,
        setsCount = r.Sets.Count,
        sets = r.Sets.Select(s => new
        {
            setNumber = s.SetNumber,
            reps = s.Reps,
            weight = s.Weight,
            rir = s.Rir,
            restSeconds = s.RestSeconds,
            concentricTimeSeconds = s.ConcentricSeconds
        }).ToList(),
        reps = legacy.Reps,
        rir = legacy.Rir,
        restSeconds = legacy.RestSeconds,
        concentricTimeSeconds = legacy.ConcentricSeconds,
        metric1Value = legacy.MaxWeight,
        metric1Unit = r.Entry.Metric1Unit,
        metric2Value = r.Entry.Metric2Value,
        metric2Unit = r.Entry.Metric2Unit
    };
}).ToList();
```

- [ ] **Step 4: Build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 5: Commit**

```bash
git add CodeBehind/StartWorkoutSessionPage.xaml.cs
git commit -m "feat: inline per-set cards on StartWorkoutSessionPage"
```

---

## Task 3: Eski `ExerciseInputRowBuilder.BuildLive` kaldır

**Files:**
- Modify: `Helpers/ExerciseInputRowBuilder.cs`

- [ ] **Step 1: `BuildLive`, `BuildSetRow`, `AddLiveHeader`, `SetData` record'unu sil**

Kalan dosya tamamen boşsa git rm.

- [ ] **Step 2: Build → 0 error**

- [ ] **Step 3: Commit**

```bash
git rm Helpers/ExerciseInputRowBuilder.cs
git commit -m "chore: remove legacy ExerciseInputRowBuilder"
```

(Dosyada başka metot kaldıysa `git rm` yerine `git add` + "chore: remove legacy BuildLive" commit'i.)

---

## Task 4: Session Exercises inline expand + edit (NewWorkoutPage)

**Files:**
- Modify: `Xaml/NewWorkoutPage.xaml`
- Modify: `CodeBehind/NewWorkoutPage.xaml.cs`

- [ ] **Step 1: `SessionExerciseRow` wrapper sınıfı**

```csharp
using System.ComponentModel;

public class SessionExerciseRow : INotifyPropertyChanged
{
    public ExerciseEntry Entry { get; init; } = null!;
    public string SetRepText { get; set; } = string.Empty;
    public string RestText { get; set; } = string.Empty;

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; PropertyChanged?.Invoke(this, new(nameof(IsExpanded))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

- [ ] **Step 2: ItemTemplate genişlet**

Mevcut Border içine (Grid'in altına):
```xml
<VerticalStackLayout StyleId="ExpandedPanel"
                     IsVisible="{Binding IsExpanded}"
                     Spacing="8"
                     Margin="0,8,0,0" />
```

Border'a tap:
```xml
<Border.GestureRecognizers>
    <TapGestureRecognizer Tapped="OnSessionExerciseTapped" />
</Border.GestureRecognizers>
```

- [ ] **Step 3: `RefreshExercisesList` — `SessionExerciseRow` üret**

Mevcut `ObservableCollection<ExerciseEntry>` yerine `ObservableCollection<SessionExerciseRow>` kullanırsan en temiz olur. Alternatif: mevcut listeyi wrap eden yardımcı ViewModel.

Her entry için:
```csharp
new SessionExerciseRow
{
    Entry = entry,
    SetRepText = Helpers.ExerciseSummaryFormatter.FormatStrength(entry),
    RestText = entry.RestSeconds is > 0 ? $"Rest {entry.RestSeconds}s" : string.Empty
}
```

- [ ] **Step 4: `OnSessionExerciseTapped` — expand + inject cards**

```csharp
private void OnSessionExerciseTapped(object? sender, TappedEventArgs e)
{
    if (sender is not Border border) return;
    if (border.BindingContext is not SessionExerciseRow row) return;

    row.IsExpanded = !row.IsExpanded;

    var expandPanel = FindExpandedPanel(border);
    if (expandPanel is null) return;

    if (row.IsExpanded && expandPanel.Children.Count == 0)
    {
        foreach (var s in row.Entry.Sets)
        {
            var card = new Xaml.Controls.SetCardView
            {
                SetNumber = s.SetNumber,
                Weight = s.Weight,
                Reps = s.Reps > 0 ? s.Reps : null,
                Rir = s.Rir,
                RestSeconds = s.RestSeconds,
                ConcentricSeconds = s.ConcentricSeconds,
                IsExpanded = true,
                ShowConcentric = row.Entry.TrackingMode == nameof(ExerciseTrackingMode.Strength)
            };
            card.ValueChanged += (_, _) =>
            {
                s.Weight = card.Weight;
                s.Reps = card.Reps ?? 0;
                s.Rir = card.Rir;
                s.RestSeconds = card.RestSeconds;
                s.ConcentricSeconds = card.ConcentricSeconds;

                var legacy = Helpers.ExerciseEntryLegacyDeriver.Derive(row.Entry.Sets);
                row.Entry.Reps = legacy.Reps;
                row.Entry.Metric1Value = legacy.MaxWeight;
                row.Entry.RIR = legacy.Rir;
                row.Entry.RestSeconds = legacy.RestSeconds;
                row.Entry.ConcentricTimeSeconds = legacy.ConcentricSeconds;

                row.SetRepText = Helpers.ExerciseSummaryFormatter.FormatStrength(row.Entry);
            };
            expandPanel.Children.Add(card);
        }
    }
}

private static VerticalStackLayout? FindExpandedPanel(Element root)
{
    foreach (var c in root.GetVisualTreeDescendants())
    {
        if (c is VerticalStackLayout vsl && vsl.StyleId == "ExpandedPanel")
            return vsl;
    }
    return null;
}
```

- [ ] **Step 5: Build → 0 error**

- [ ] **Step 6: Commit**

```bash
git add Xaml/NewWorkoutPage.xaml CodeBehind/NewWorkoutPage.xaml.cs
git commit -m "feat: inline expand + edit for Session Exercises in NewWorkoutPage"
```

---

## Task 5: `WorkoutPreviewPage` — expanded read-only görünüm

**Files:**
- Modify: `CodeBehind/WorkoutPreviewPage.xaml.cs`

- [ ] **Step 1: Her exercise kartına expand panel ekle**

```csharp
var expandPanel = new VerticalStackLayout { Spacing = 6, IsVisible = false };
foreach (var s in entry.Sets)
{
    var card = new Xaml.Controls.SetCardView
    {
        SetNumber = s.SetNumber,
        Weight = s.Weight,
        Reps = s.Reps,
        Rir = s.Rir,
        RestSeconds = s.RestSeconds,
        ConcentricSeconds = s.ConcentricSeconds,
        IsExpanded = true,
        IsReadOnly = true,
        ShowConcentric = entry.TrackingMode == nameof(ExerciseTrackingMode.Strength)
    };
    expandPanel.Children.Add(card);
}

cardBorder.GestureRecognizers.Add(new TapGestureRecognizer
{
    Command = new Command(() => expandPanel.IsVisible = !expandPanel.IsVisible)
});
```

- [ ] **Step 2: Build → 0 error**

- [ ] **Step 3: Commit**

```bash
git add CodeBehind/WorkoutPreviewPage.xaml.cs
git commit -m "feat: expanded read-only per-set view in WorkoutPreviewPage"
```

---

## Task 6: Regression + manuel

- [ ] **Step 1: Tüm unit testler**

Run: `dotnet test FreakLete.Core.Tests`
Expected: PASS.

- [ ] **Step 2: Mobile build**

Run: `dotnet build FreakLete.csproj -f net10.0-android`
Expected: 0 error.

- [ ] **Step 3: Manuel — StartWorkoutSessionPage**

- Program şablonundan "Start Session" → N adet set kartı.
- Weight gir → "✓" → kart faded, sonraki Weight focus.
- Add Set / Remove Set çalışıyor.
- Tüm setler ✓ → "Save Session" → workout kalıcı.

- [ ] **Step 4: Manuel — Session Exercises expand/edit**

- NewWorkoutPage'de egzersiz ekle → listede `3 × 5`.
- Listeye tıkla → 3 set kartı expand.
- Bir set'in ağırlığını değiştir → özet güncellenir.
- Tekrar tıkla → gizlenir.

- [ ] **Step 5: Manuel — WorkoutPreviewPage**

- Geçmiş workout aç → exercise kartına tıkla → set detayları (read-only).

---

## Risks

- **`GetVisualTreeDescendants` performansı:** Küçük set sayılarında sorun yok.
- **ValueChanged tetiklenme sıklığı:** Her harf yazımında legacy türetim çalışır — tipik set sayılarında ucuz.
- **Complete state kalıcılığı:** Memory'de; sayfa navigasyonunda kaybolur. `WorkoutSessionState`'e eklenmedi — session interrupt/resume ileriye.
- **`ExerciseInputRowBuilder` kaldırma sırası:** Task 3 Task 2'den sonra; aksi halde build kırılır.
- **AppLanguage:** Yeni string yok; Phase 2'dekiler reuse.
