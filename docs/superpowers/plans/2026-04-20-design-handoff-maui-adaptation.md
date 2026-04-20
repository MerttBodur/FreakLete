# Design Handoff → MAUI Adaptation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adapt the FreakLete design handoff prototype (4 screens + shared controls) into pixel-faithful .NET MAUI XAML, implementing reusable ContentViews and updating all four main pages.

**Architecture:** Foundation-first — shared Controls are built first so pages can consume them. Each Control lives in `Xaml/Controls/XYZ.xaml` + `CodeBehind/Controls/XYZ.xaml.cs`. Pages keep existing code-behind logic; only the XAML layout changes.

**Tech Stack:** .NET MAUI, C# 12, XAML, LinearGradientBrush, BindableProperty, ControlTemplate

---

## File Map

| Action | File | Responsibility |
|---|---|---|
| Modify | `Resources/Styles/Styles.xaml` | Add `MetricValueLabel`, `NavLabel` named styles |
| Modify | `Xaml/Controls/MetricTile.xaml` | Update visual to spec: compact size, UPPERCASE label |
| Modify | `CodeBehind/Controls/MetricTile.xaml.cs` | Add `ValueColor` BindableProperty |
| Create | `Xaml/Controls/ElevatedCard.xaml` | Gradient card wrapper (ControlTemplate) |
| Create | `CodeBehind/Controls/ElevatedCard.xaml.cs` | ContentView partial class |
| Create | `Xaml/Controls/AccentCard.xaml` | Accent gradient card wrapper (ControlTemplate) |
| Create | `CodeBehind/Controls/AccentCard.xaml.cs` | ContentView partial class |
| Create | `Xaml/Controls/QuickAccessTile.xaml` | Icon + title + subtitle tile |
| Create | `CodeBehind/Controls/QuickAccessTile.xaml.cs` | Title, Subtitle, IconSource, Command BindableProperties |
| Create | `Xaml/Controls/TabSwitcher.xaml` | Pill tab bar for Calculations (1RM/RSI/FFMI) |
| Create | `CodeBehind/Controls/TabSwitcher.xaml.cs` | Items, SelectedIndex BindableProperties + dynamic tab generation |
| Create | `Xaml/Controls/SectionTabs.xaml` | Pill tab bar for Profile (Overview/Performance/Goals) |
| Create | `CodeBehind/Controls/SectionTabs.xaml.cs` | Items, SelectedIndex BindableProperties + dynamic tab generation |
| Modify | `Xaml/HomePage.xaml` | Prototype Home layout: ElevatedCard + QuickAccessTile + AccentCard |
| Modify | `Xaml/WorkoutPage.xaml` | Prototype Workout layout: ElevatedCard + template cards |
| Modify | `Xaml/CalculationsPage.xaml` | Redesign: TabSwitcher + 1RM/RSI/FFMI panels |
| Modify | `Xaml/ProfilePage.xaml` | Prototype Profile layout: AccentCard hero + SectionTabs |

---

## Task 1: Add Missing Named Styles to Styles.xaml

**Files:**
- Modify: `Resources/Styles/Styles.xaml`

- [ ] **Step 1: Verify EyebrowLabel exists**

Open `Resources/Styles/Styles.xaml` and confirm `x:Key="Eyebrow"` exists around line 189. It does — no change needed.

- [ ] **Step 2: Add MetricValueLabel and NavLabel after the `HeaderTitle` style block**

In `Resources/Styles/Styles.xaml`, after the `<Style TargetType="Label" x:Key="HeaderTitle">` block, insert:

```xml
    <Style TargetType="Label" x:Key="MetricValueLabel">
        <Setter Property="TextColor" Value="{StaticResource AccentGlow}" />
        <Setter Property="FontFamily" Value="OpenSansSemibold" />
        <Setter Property="FontSize" Value="20" />
        <Setter Property="LineHeight" Value="1" />
    </Style>

    <Style TargetType="Label" x:Key="NavLabel">
        <Setter Property="TextColor" Value="{StaticResource TextMuted}" />
        <Setter Property="FontFamily" Value="OpenSansSemibold" />
        <Setter Property="FontSize" Value="10" />
        <Setter Property="HorizontalTextAlignment" Value="Center" />
    </Style>
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Resources/Styles/Styles.xaml
git commit -m "feat: add MetricValueLabel and NavLabel named styles"
```

---

## Task 2: Update MetricTile Visual + Add ValueColor

**Files:**
- Modify: `Xaml/Controls/MetricTile.xaml`
- Modify: `CodeBehind/Controls/MetricTile.xaml.cs`

- [ ] **Step 1: Replace MetricTile.xaml with spec-correct layout**

Full replacement of `Xaml/Controls/MetricTile.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.MetricTile">

    <Border BackgroundColor="{StaticResource SurfaceRaised}"
            Stroke="{StaticResource SurfaceBorder}"
            StrokeThickness="1"
            StrokeShape="RoundRectangle 14"
            Padding="12,10">
        <VerticalStackLayout Spacing="4">
            <Label x:Name="MetricLabel"
                   FontFamily="OpenSansSemibold"
                   FontSize="10"
                   TextColor="{StaticResource TextMuted}"
                   CharacterSpacing="0.8"
                   TextTransform="Uppercase" />
            <Label x:Name="ValueLabel"
                   Style="{StaticResource MetricValueLabel}" />
            <Label x:Name="UnitLabel"
                   FontFamily="OpenSansRegular"
                   FontSize="11"
                   TextColor="{StaticResource TextMuted}" />
        </VerticalStackLayout>
    </Border>

</ContentView>
```

- [ ] **Step 2: Replace MetricTile.xaml.cs with ValueColor support**

Full replacement of `CodeBehind/Controls/MetricTile.xaml.cs`:

```csharp
namespace FreakLete;

public partial class MetricTile : ContentView
{
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(MetricTile), string.Empty,
            propertyChanged: (b, o, n) => ((MetricTile)b).ApplyState());

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(MetricTile), "—",
            propertyChanged: (b, o, n) => ((MetricTile)b).ApplyState());

    public static readonly BindableProperty UnitProperty =
        BindableProperty.Create(nameof(Unit), typeof(string), typeof(MetricTile), string.Empty,
            propertyChanged: (b, o, n) => ((MetricTile)b).ApplyState());

    public static readonly BindableProperty ValueColorProperty =
        BindableProperty.Create(nameof(ValueColor), typeof(Color), typeof(MetricTile), null,
            propertyChanged: (b, o, n) => ((MetricTile)b).ApplyState());

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public Color ValueColor
    {
        get => (Color)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public MetricTile()
    {
        InitializeComponent();
        ApplyState();
    }

    private void ApplyState()
    {
        MetricLabel.Text = Label;
        ValueLabel.Text = Value;
        UnitLabel.Text = Unit;
        UnitLabel.IsVisible = !string.IsNullOrWhiteSpace(Unit);

        var color = ValueColor;
        ValueLabel.TextColor = color is not null
            ? color
            : (Color)Application.Current!.Resources["AccentGlow"];
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/MetricTile.xaml CodeBehind/Controls/MetricTile.xaml.cs
git commit -m "feat: update MetricTile to design spec — compact size, ValueColor support"
```

---

## Task 3: Create ElevatedCard

**Files:**
- Create: `Xaml/Controls/ElevatedCard.xaml`
- Create: `CodeBehind/Controls/ElevatedCard.xaml.cs`

- [ ] **Step 1: Create ElevatedCard.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.ElevatedCard">

    <ContentView.ControlTemplate>
        <ControlTemplate>
            <Border Stroke="{StaticResource SurfaceBorder}"
                    StrokeThickness="1"
                    StrokeShape="RoundRectangle 24"
                    Padding="18">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0.15,0" EndPoint="0.85,1">
                        <GradientStop Color="#1D1828" Offset="0.0" />
                        <GradientStop Color="#171321" Offset="1.0" />
                    </LinearGradientBrush>
                </Border.Background>
                <ContentPresenter />
            </Border>
        </ControlTemplate>
    </ContentView.ControlTemplate>

</ContentView>
```

- [ ] **Step 2: Create ElevatedCard.xaml.cs**

```csharp
namespace FreakLete;

public partial class ElevatedCard : ContentView
{
    public ElevatedCard() => InitializeComponent();
}
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/ElevatedCard.xaml CodeBehind/Controls/ElevatedCard.xaml.cs
git commit -m "feat: add ElevatedCard ContentView with gradient ControlTemplate"
```

---

## Task 4: Create AccentCard

**Files:**
- Create: `Xaml/Controls/AccentCard.xaml`
- Create: `CodeBehind/Controls/AccentCard.xaml.cs`

- [ ] **Step 1: Create AccentCard.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.AccentCard">

    <ContentView.ControlTemplate>
        <ControlTemplate>
            <Border Stroke="{StaticResource Accent}"
                    StrokeThickness="1"
                    StrokeShape="RoundRectangle 24"
                    Padding="18">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0.15,0" EndPoint="0.85,1">
                        <GradientStop Color="#2F2346" Offset="0.0" />
                        <GradientStop Color="#171321" Offset="1.0" />
                    </LinearGradientBrush>
                </Border.Background>
                <ContentPresenter />
            </Border>
        </ControlTemplate>
    </ContentView.ControlTemplate>

</ContentView>
```

- [ ] **Step 2: Create AccentCard.xaml.cs**

```csharp
namespace FreakLete;

public partial class AccentCard : ContentView
{
    public AccentCard() => InitializeComponent();
}
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/AccentCard.xaml CodeBehind/Controls/AccentCard.xaml.cs
git commit -m "feat: add AccentCard ContentView with accent gradient ControlTemplate"
```

---

## Task 5: Create QuickAccessTile

**Files:**
- Create: `Xaml/Controls/QuickAccessTile.xaml`
- Create: `CodeBehind/Controls/QuickAccessTile.xaml.cs`

- [ ] **Step 1: Create QuickAccessTile.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.QuickAccessTile"
             x:Name="self">

    <Border BackgroundColor="{StaticResource SurfaceRaised}"
            Stroke="{StaticResource SurfaceBorder}"
            StrokeThickness="1"
            StrokeShape="RoundRectangle 24"
            Padding="14">
        <Border.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding Command, Source={x:Reference self}}" />
        </Border.GestureRecognizers>
        <VerticalStackLayout Spacing="8">
            <Image x:Name="TileIcon"
                   WidthRequest="24"
                   HeightRequest="24"
                   HorizontalOptions="Start" />
            <VerticalStackLayout Spacing="2">
                <Label x:Name="TitleLabel"
                       FontFamily="OpenSansSemibold"
                       FontSize="13"
                       TextColor="{StaticResource TextPrimary}" />
                <Label x:Name="SubtitleLabel"
                       FontFamily="OpenSansRegular"
                       FontSize="11"
                       TextColor="{StaticResource TextMuted}" />
            </VerticalStackLayout>
        </VerticalStackLayout>
    </Border>

</ContentView>
```

- [ ] **Step 2: Create QuickAccessTile.xaml.cs**

```csharp
using System.Windows.Input;

namespace FreakLete;

public partial class QuickAccessTile : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(QuickAccessTile), string.Empty,
            propertyChanged: (b, o, n) => ((QuickAccessTile)b).TitleLabel.Text = (string)n);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(QuickAccessTile), string.Empty,
            propertyChanged: (b, o, n) => ((QuickAccessTile)b).SubtitleLabel.Text = (string)n);

    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(nameof(IconSource), typeof(string), typeof(QuickAccessTile), string.Empty,
            propertyChanged: (b, o, n) => ((QuickAccessTile)b).TileIcon.Source = ImageSource.FromFile((string)n));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(QuickAccessTile), null);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string IconSource
    {
        get => (string)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public QuickAccessTile() => InitializeComponent();
}
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/QuickAccessTile.xaml CodeBehind/Controls/QuickAccessTile.xaml.cs
git commit -m "feat: add QuickAccessTile ContentView with icon, title, subtitle, command"
```

---

## Task 6: Create TabSwitcher

**Files:**
- Create: `Xaml/Controls/TabSwitcher.xaml`
- Create: `CodeBehind/Controls/TabSwitcher.xaml.cs`

- [ ] **Step 1: Create TabSwitcher.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.TabSwitcher">

    <Border BackgroundColor="{StaticResource SurfaceRaised}"
            Stroke="{StaticResource SurfaceBorder}"
            StrokeThickness="1"
            StrokeShape="RoundRectangle 18"
            Padding="4">
        <Grid x:Name="TabsGrid" ColumnSpacing="0" />
    </Border>

</ContentView>
```

- [ ] **Step 2: Create TabSwitcher.xaml.cs**

```csharp
namespace FreakLete;

public partial class TabSwitcher : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<string>), typeof(TabSwitcher), null,
            propertyChanged: (b, o, n) => ((TabSwitcher)b).RebuildTabs());

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(TabSwitcher), 0,
            propertyChanged: (b, o, n) => ((TabSwitcher)b).UpdateSelection((int)n));

    public IList<string> Items
    {
        get => (IList<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public event EventHandler<int>? TabSelected;

    public TabSwitcher() => InitializeComponent();

    private void RebuildTabs()
    {
        TabsGrid.Children.Clear();
        TabsGrid.ColumnDefinitions.Clear();
        if (Items is null) return;

        for (int i = 0; i < Items.Count; i++)
        {
            TabsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var idx = i;
            var label = new Label
            {
                Text = Items[i],
                FontFamily = "OpenSansSemibold",
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = (Color)Application.Current!.Resources["TextMuted"],
            };

            var pill = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                Padding = new Thickness(0, 9),
                BackgroundColor = Colors.Transparent,
                Content = label,
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                SelectedIndex = idx;
                TabSelected?.Invoke(this, idx);
            };
            pill.GestureRecognizers.Add(tap);

            Grid.SetColumn(pill, i);
            TabsGrid.Children.Add(pill);
        }

        UpdateSelection(SelectedIndex);
    }

    private void UpdateSelection(int selected)
    {
        var accent = (Color)Application.Current!.Resources["Accent"];
        var textPrimary = (Color)Application.Current!.Resources["TextPrimary"];
        var textMuted = (Color)Application.Current!.Resources["TextMuted"];

        for (int i = 0; i < TabsGrid.Children.Count; i++)
        {
            if (TabsGrid.Children[i] is not Border pill) continue;
            if (pill.Content is not Label label) continue;

            if (i == selected)
            {
                pill.BackgroundColor = accent;
                label.TextColor = textPrimary;
            }
            else
            {
                pill.BackgroundColor = Colors.Transparent;
                label.TextColor = textMuted;
            }
        }
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/TabSwitcher.xaml CodeBehind/Controls/TabSwitcher.xaml.cs
git commit -m "feat: add TabSwitcher ContentView with dynamic equal-width pill tabs"
```

---

## Task 7: Create SectionTabs

**Files:**
- Create: `Xaml/Controls/SectionTabs.xaml`
- Create: `CodeBehind/Controls/SectionTabs.xaml.cs`

- [ ] **Step 1: Create SectionTabs.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.SectionTabs">

    <HorizontalStackLayout x:Name="TabsContainer" Spacing="8" />

</ContentView>
```

- [ ] **Step 2: Create SectionTabs.xaml.cs**

```csharp
namespace FreakLete;

public partial class SectionTabs : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<string>), typeof(SectionTabs), null,
            propertyChanged: (b, o, n) => ((SectionTabs)b).RebuildTabs());

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(SectionTabs), 0,
            propertyChanged: (b, o, n) => ((SectionTabs)b).UpdateSelection((int)n));

    public IList<string> Items
    {
        get => (IList<string>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public event EventHandler<int>? TabSelected;

    public SectionTabs() => InitializeComponent();

    private void RebuildTabs()
    {
        TabsContainer.Children.Clear();
        if (Items is null) return;

        for (int i = 0; i < Items.Count; i++)
        {
            var idx = i;
            var label = new Label
            {
                Text = Items[i],
                FontFamily = "OpenSansSemibold",
                FontSize = 14,
                TextColor = (Color)Application.Current!.Resources["TextMuted"],
            };

            var pill = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
                Padding = new Thickness(16, 8),
                BackgroundColor = Colors.Transparent,
                Content = label,
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                SelectedIndex = idx;
                TabSelected?.Invoke(this, idx);
            };
            pill.GestureRecognizers.Add(tap);
            TabsContainer.Children.Add(pill);
        }

        UpdateSelection(SelectedIndex);
    }

    private void UpdateSelection(int selected)
    {
        var accentSoft = (Color)Application.Current!.Resources["AccentSoft"];
        var accentGlow = (Color)Application.Current!.Resources["AccentGlow"];
        var textMuted = (Color)Application.Current!.Resources["TextMuted"];

        for (int i = 0; i < TabsContainer.Children.Count; i++)
        {
            if (TabsContainer.Children[i] is not Border pill) continue;
            if (pill.Content is not Label label) continue;

            if (i == selected)
            {
                pill.BackgroundColor = accentSoft;
                label.TextColor = accentGlow;
            }
            else
            {
                pill.BackgroundColor = Colors.Transparent;
                label.TextColor = textMuted;
            }
        }
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/Controls/SectionTabs.xaml CodeBehind/Controls/SectionTabs.xaml.cs
git commit -m "feat: add SectionTabs ContentView for profile section switching"
```

---

## Task 8: Update HomePage.xaml

**Files:**
- Modify: `Xaml/HomePage.xaml`

**Note:** Check `CodeBehind/HomePage.xaml.cs` for `x:Name` references before replacing — any names used in code-behind must be preserved in the new layout.

- [ ] **Step 1: Check code-behind names**

```bash
grep -oP "(?<=\b)[A-Z][a-zA-Z]+(?=\b)" CodeBehind/HomePage.xaml.cs | sort -u | head -30
```

Note which names map to XAML elements and preserve them.

- [ ] **Step 2: Replace HomePage.xaml**

Full replacement of `Xaml/HomePage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:FreakLete"
             x:Class="FreakLete.HomePage"
             NavigationPage.HasNavigationBar="False"
             Title=""
             BackgroundColor="{StaticResource Background}">

    <Grid RowDefinitions="*,Auto">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout Padding="20,16,20,24" Spacing="16">

                <!-- Hero Card -->
                <local:ElevatedCard>
                    <VerticalStackLayout Spacing="14">
                        <Grid ColumnDefinitions="*,Auto">
                            <VerticalStackLayout Grid.Column="0" Spacing="2">
                                <Label Text="TODAY"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="10"
                                       TextColor="{StaticResource TextMuted}"
                                       CharacterSpacing="0.8"
                                       TextTransform="Uppercase" />
                                <Label Text="Ready to train?"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="18"
                                       TextColor="{StaticResource TextPrimary}" />
                            </VerticalStackLayout>
                            <Border Grid.Column="1"
                                    x:Name="PlanBadge"
                                    BackgroundColor="{StaticResource AccentSoft}"
                                    Stroke="{StaticResource Accent}"
                                    StrokeThickness="1"
                                    StrokeShape="RoundRectangle 12"
                                    Padding="10,5"
                                    VerticalOptions="Start">
                                <Label x:Name="PlanBadgeLabel"
                                       Text="Free"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="11"
                                       TextColor="{StaticResource AccentGlow}" />
                            </Border>
                        </Grid>

                        <Grid ColumnDefinitions="*,*,*" ColumnSpacing="10">
                            <local:MetricTile Grid.Column="0"
                                             x:Name="WeekSessionsTile"
                                             Label="This Week"
                                             Value="0"
                                             Unit="sessions" />
                            <local:MetricTile Grid.Column="1"
                                             x:Name="LastOnermTile"
                                             Label="Last 1RM"
                                             Value="—"
                                             Unit="" />
                            <local:MetricTile Grid.Column="2"
                                             x:Name="StreakTile"
                                             Label="Streak"
                                             Value="0"
                                             Unit="days"
                                             ValueColor="{StaticResource Success}" />
                        </Grid>

                        <Button x:Name="StartButton"
                                Text="Start Workout"
                                HorizontalOptions="Fill" />
                    </VerticalStackLayout>
                </local:ElevatedCard>

                <!-- Quick Access -->
                <VerticalStackLayout Spacing="10">
                    <Label Text="Quick Access"
                           FontFamily="OpenSansSemibold"
                           FontSize="15"
                           TextColor="{StaticResource TextPrimary}" />
                    <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                        <local:QuickAccessTile Grid.Column="0"
                                              x:Name="CalcTile"
                                              Title="Calculations"
                                              Subtitle="1RM · RSI · FFMI"
                                              IconSource="nav_plates.svg" />
                        <local:QuickAccessTile Grid.Column="1"
                                              x:Name="CalendarTile"
                                              Title="Calendar"
                                              Subtitle="Workout history"
                                              IconSource="calendar_icon.svg" />
                    </Grid>
                </VerticalStackLayout>

                <!-- FreakAI Card -->
                <local:AccentCard>
                    <VerticalStackLayout Spacing="12">
                        <VerticalStackLayout Spacing="2">
                            <Label Text="FreakAI"
                                   FontFamily="OpenSansSemibold"
                                   FontSize="15"
                                   TextColor="{StaticResource TextPrimary}" />
                            <Label x:Name="AiQuotaLabel"
                                   Text="3 messages remaining today"
                                   FontFamily="OpenSansRegular"
                                   FontSize="11"
                                   TextColor="{StaticResource TextMuted}" />
                        </VerticalStackLayout>
                        <Grid ColumnDefinitions="*,44" ColumnSpacing="8">
                            <Border Grid.Column="0"
                                    BackgroundColor="{StaticResource SurfaceRaised}"
                                    Stroke="{StaticResource SurfaceBorder}"
                                    StrokeThickness="1"
                                    StrokeShape="RoundRectangle 18"
                                    Padding="14,0">
                                <Entry x:Name="AiEntry"
                                       Placeholder="Ask your coach..."
                                       PlaceholderColor="{StaticResource TextMuted}"
                                       TextColor="{StaticResource TextPrimary}"
                                       FontFamily="OpenSansRegular"
                                       FontSize="14"
                                       BackgroundColor="Transparent"
                                       MinimumHeightRequest="44" />
                            </Border>
                            <Border Grid.Column="1"
                                    BackgroundColor="{StaticResource Accent}"
                                    StrokeThickness="0"
                                    StrokeShape="RoundRectangle 18"
                                    WidthRequest="44"
                                    HeightRequest="44">
                                <Border.GestureRecognizers>
                                    <TapGestureRecognizer x:Name="AiSendTap" />
                                </Border.GestureRecognizers>
                                <Image Source="icon_send.svg"
                                       WidthRequest="20"
                                       HeightRequest="20"
                                       HorizontalOptions="Center"
                                       VerticalOptions="Center" />
                            </Border>
                        </Grid>
                    </VerticalStackLayout>
                </local:AccentCard>

            </VerticalStackLayout>
        </ScrollView>

        <local:BottomNavBar Grid.Row="1" x:Name="BottomNav" />

    </Grid>

</ContentPage>
```

- [ ] **Step 3: Build — fix any missing x:Name references**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

If code-behind references a name not in the new XAML, add the element back with the required `x:Name`. Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/HomePage.xaml
git commit -m "feat: update HomePage to design spec — hero card, quick access tiles, FreakAI"
```

---

## Task 9: Update WorkoutPage.xaml

**Files:**
- Modify: `Xaml/WorkoutPage.xaml`

- [ ] **Step 1: Check code-behind names**

```bash
grep "x:Name\|QuickStart\|FromProgram\|Templates" CodeBehind/WorkoutPage.xaml.cs | head -20
```

- [ ] **Step 2: Replace WorkoutPage.xaml**

Full replacement of `Xaml/WorkoutPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:FreakLete"
             x:Class="FreakLete.WorkoutPage"
             NavigationPage.HasNavigationBar="False"
             Title="Workout"
             BackgroundColor="{StaticResource Background}">

    <Grid RowDefinitions="*,Auto">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout Padding="20,16,20,24" Spacing="16">

                <!-- Session Start Card -->
                <local:ElevatedCard>
                    <VerticalStackLayout Spacing="16">
                        <Label Text="Start a Session"
                               FontFamily="OpenSansSemibold"
                               FontSize="20"
                               TextColor="{StaticResource TextPrimary}" />
                        <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                            <Button Grid.Column="0"
                                    x:Name="QuickStartButton"
                                    Text="Quick Start"
                                    HorizontalOptions="Fill" />
                            <Button Grid.Column="1"
                                    x:Name="FromProgramButton"
                                    Text="From Program"
                                    Style="{StaticResource SecondaryButton}"
                                    HorizontalOptions="Fill" />
                        </Grid>
                    </VerticalStackLayout>
                </local:ElevatedCard>

                <!-- Program Templates -->
                <VerticalStackLayout Spacing="10">
                    <Label Text="Starter Templates"
                           FontFamily="OpenSansSemibold"
                           FontSize="15"
                           TextColor="{StaticResource TextPrimary}" />
                    <CollectionView x:Name="TemplatesCollection"
                                    ItemsLayout="VerticalList"
                                    SelectionMode="None">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Border Style="{StaticResource CardBorder}"
                                        Margin="0,0,0,10">
                                    <VerticalStackLayout Spacing="8">
                                        <Label Text="{Binding Name}"
                                               FontFamily="OpenSansSemibold"
                                               FontSize="15"
                                               TextColor="{StaticResource TextPrimary}" />
                                        <Label Text="{Binding SessionsPerWeek, StringFormat='{0} sessions/wk'}"
                                               FontFamily="OpenSansRegular"
                                               FontSize="12"
                                               TextColor="{StaticResource TextMuted}" />
                                    </VerticalStackLayout>
                                </Border>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <local:BottomNavBar Grid.Row="1" x:Name="BottomNav" />

    </Grid>

</ContentPage>
```

- [ ] **Step 3: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Xaml/WorkoutPage.xaml
git commit -m "feat: update WorkoutPage to design spec — ElevatedCard session start, template list"
```

---

## Task 10: Update CalculationsPage.xaml

**Files:**
- Modify: `Xaml/CalculationsPage.xaml`
- Modify: `CodeBehind/CalculationsPage.xaml.cs`

**Note:** Existing CalculationsPage shows PR records. This task replaces it with the 1RM/RSI/FFMI calculator layout from the prototype.

- [ ] **Step 1: Check existing code-behind for reusable logic**

```bash
grep -n "Epley\|OneRm\|Calculate\|Weight\|Reps\|RSI\|FFMI" CodeBehind/CalculationsPage.xaml.cs | head -20
```

Preserve any existing calculation methods; remove PR-list references.

- [ ] **Step 2: Replace CalculationsPage.xaml**

Full replacement of `Xaml/CalculationsPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:FreakLete"
             x:Class="FreakLete.CalculationsPage"
             NavigationPage.HasNavigationBar="False"
             Title="Calculations"
             BackgroundColor="{StaticResource Background}">

    <Grid RowDefinitions="*,Auto">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout Padding="20,16,20,24" Spacing="16">

                <local:TabSwitcher x:Name="CalcTabSwitcher"
                                   Items="{x:Static local:CalculationsPage.CalcTabItems}" />

                <!-- 1RM Panel -->
                <VerticalStackLayout x:Name="OnermPanel" Spacing="14">
                    <Border Style="{StaticResource CardBorder}">
                        <VerticalStackLayout Spacing="12">
                            <Label Text="1RM CALCULATOR"
                                   FontFamily="OpenSansSemibold"
                                   FontSize="10"
                                   TextColor="{StaticResource TextMuted}"
                                   CharacterSpacing="0.8"
                                   TextTransform="Uppercase" />
                            <Entry x:Name="OnermWeightEntry"
                                   Placeholder="Weight (kg)"
                                   Keyboard="Numeric"
                                   ReturnType="Next" />
                            <Entry x:Name="OnermRepsEntry"
                                   Placeholder="Reps"
                                   Keyboard="Numeric"
                                   ReturnType="Done" />
                            <Button Text="Calculate"
                                    HorizontalOptions="Fill"
                                    Clicked="OnCalculate1RM" />
                        </VerticalStackLayout>
                    </Border>
                    <VerticalStackLayout x:Name="OnermResultPanel" IsVisible="False" Spacing="12">
                        <Border Style="{StaticResource CardElevated}">
                            <VerticalStackLayout Spacing="6" HorizontalOptions="Center">
                                <Label Text="ESTIMATED 1RM"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="10"
                                       TextColor="{StaticResource TextMuted}"
                                       CharacterSpacing="0.8"
                                       TextTransform="Uppercase"
                                       HorizontalTextAlignment="Center" />
                                <Label x:Name="OnermResultLabel"
                                       Text="—"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="42"
                                       TextColor="{StaticResource AccentGlow}"
                                       HorizontalTextAlignment="Center" />
                                <Label Text="kg"
                                       FontFamily="OpenSansRegular"
                                       FontSize="14"
                                       TextColor="{StaticResource TextMuted}"
                                       HorizontalTextAlignment="Center" />
                            </VerticalStackLayout>
                        </Border>
                        <Grid ColumnDefinitions="*,*,*" ColumnSpacing="10">
                            <local:MetricTile Grid.Column="0" x:Name="Pct90Tile"
                                             Label="90%" Value="—" Unit="kg" />
                            <local:MetricTile Grid.Column="1" x:Name="Pct80Tile"
                                             Label="80%" Value="—" Unit="kg" />
                            <local:MetricTile Grid.Column="2" x:Name="Pct70Tile"
                                             Label="70%" Value="—" Unit="kg" />
                        </Grid>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- RSI Panel -->
                <VerticalStackLayout x:Name="RsiPanel" IsVisible="False" Spacing="14">
                    <Border Style="{StaticResource CardBorder}">
                        <VerticalStackLayout Spacing="12">
                            <Label Text="RSI CALCULATOR"
                                   FontFamily="OpenSansSemibold"
                                   FontSize="10"
                                   TextColor="{StaticResource TextMuted}"
                                   CharacterSpacing="0.8"
                                   TextTransform="Uppercase" />
                            <Entry x:Name="RsiJumpEntry"
                                   Placeholder="Jump Height (cm)"
                                   Keyboard="Numeric"
                                   ReturnType="Next" />
                            <Entry x:Name="RsiGctEntry"
                                   Placeholder="Ground Contact Time (ms)"
                                   Keyboard="Numeric"
                                   ReturnType="Done" />
                            <Button Text="Calculate"
                                    HorizontalOptions="Fill"
                                    Clicked="OnCalculateRSI" />
                        </VerticalStackLayout>
                    </Border>
                    <VerticalStackLayout x:Name="RsiResultPanel" IsVisible="False" Spacing="12">
                        <Border Style="{StaticResource CardElevated}">
                            <VerticalStackLayout Spacing="6" HorizontalOptions="Center">
                                <Label Text="RSI SCORE"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="10"
                                       TextColor="{StaticResource TextMuted}"
                                       CharacterSpacing="0.8"
                                       TextTransform="Uppercase"
                                       HorizontalTextAlignment="Center" />
                                <Label x:Name="RsiResultLabel"
                                       Text="—"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="42"
                                       TextColor="{StaticResource AccentGlow}"
                                       HorizontalTextAlignment="Center" />
                                <Label x:Name="RsiDescLabel"
                                       Text=""
                                       FontFamily="OpenSansRegular"
                                       FontSize="13"
                                       TextColor="{StaticResource TextSecondary}"
                                       HorizontalTextAlignment="Center" />
                            </VerticalStackLayout>
                        </Border>
                    </VerticalStackLayout>
                </VerticalStackLayout>

                <!-- FFMI Panel -->
                <VerticalStackLayout x:Name="FfmiPanel" IsVisible="False" Spacing="14">
                    <Border Style="{StaticResource CardBorder}">
                        <VerticalStackLayout Spacing="12" HorizontalOptions="Center">
                            <Label Text="Profile data required"
                                   FontFamily="OpenSansSemibold"
                                   FontSize="15"
                                   TextColor="{StaticResource TextPrimary}"
                                   HorizontalTextAlignment="Center" />
                            <Label Text="Fill in your weight, height and body fat % in Profile to calculate FFMI."
                                   FontFamily="OpenSansRegular"
                                   FontSize="13"
                                   TextColor="{StaticResource TextMuted}"
                                   HorizontalTextAlignment="Center" />
                            <Button x:Name="FfmiGoToProfileButton"
                                    Text="Go to Profile"
                                    Style="{StaticResource SecondaryButton}"
                                    HorizontalOptions="Center" />
                        </VerticalStackLayout>
                    </Border>
                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <local:BottomNavBar Grid.Row="1" x:Name="BottomNav" />

    </Grid>

</ContentPage>
```

- [ ] **Step 3: Add calc logic to CalculationsPage.xaml.cs**

In `CodeBehind/CalculationsPage.xaml.cs`, add at the class level and wire in constructor/OnAppearing:

```csharp
// Static tab items (referenced from XAML via x:Static)
public static readonly IList<string> CalcTabItems = new[] { "1RM", "RSI", "FFMI" };

// Call this from constructor or OnAppearing:
private void WireTabSwitcher()
{
    CalcTabSwitcher.TabSelected += (_, idx) => ShowPanel(idx);
}

private void ShowPanel(int idx)
{
    OnermPanel.IsVisible = idx == 0;
    RsiPanel.IsVisible   = idx == 1;
    FfmiPanel.IsVisible  = idx == 2;
}

private void OnCalculate1RM(object sender, EventArgs e)
{
    if (!double.TryParse(OnermWeightEntry.Text, out var w)) return;
    if (!double.TryParse(OnermRepsEntry.Text, out var r) || r < 1) return;

    // Epley formula
    var oneRm = w * (1 + r / 30.0);
    OnermResultLabel.Text = oneRm.ToString("F1");
    Pct90Tile.Value = (oneRm * 0.9).ToString("F1");
    Pct80Tile.Value = (oneRm * 0.8).ToString("F1");
    Pct70Tile.Value = (oneRm * 0.7).ToString("F1");
    OnermResultPanel.IsVisible = true;
}

private void OnCalculateRSI(object sender, EventArgs e)
{
    if (!double.TryParse(RsiJumpEntry.Text, out var h)) return;
    if (!double.TryParse(RsiGctEntry.Text, out var gct) || gct <= 0) return;

    // RSI = jump height (m) / ground contact time (s)
    var rsi = (h / 100.0) / (gct / 1000.0);
    RsiResultLabel.Text = rsi.ToString("F2");
    RsiDescLabel.Text = rsi switch
    {
        >= 2.0 => "Elite — exceptional reactive strength",
        >= 1.5 => "Advanced — very good reactive strength",
        >= 1.0 => "Intermediate — good reactive strength",
        _ => "Developing — focus on plyometric training",
    };
    RsiResultPanel.IsVisible = true;
}
```

- [ ] **Step 4: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add Xaml/CalculationsPage.xaml CodeBehind/CalculationsPage.xaml.cs
git commit -m "feat: redesign CalculationsPage — 1RM/RSI/FFMI TabSwitcher with Epley formula"
```

---

## Task 11: Update ProfilePage.xaml

**Files:**
- Modify: `Xaml/ProfilePage.xaml`
- Modify: `CodeBehind/ProfilePage.xaml.cs`

- [ ] **Step 1: Check code-behind names**

```bash
grep "OnAvatarTapped\|InitialsLabel\|NameLabel\|SportLabel\|x:Name" CodeBehind/ProfilePage.xaml.cs | head -20
```

- [ ] **Step 2: Replace ProfilePage.xaml**

Full replacement of `Xaml/ProfilePage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:FreakLete"
             x:Class="FreakLete.ProfilePage"
             x:Name="ProfilePageRoot"
             NavigationPage.HasNavigationBar="False"
             Title="Profile"
             BackgroundColor="{StaticResource Background}">

    <Grid RowDefinitions="*,Auto">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout Padding="20,16,20,24" Spacing="16">

                <!-- Hero Card -->
                <local:AccentCard>
                    <VerticalStackLayout Spacing="14">
                        <Grid ColumnDefinitions="56,*" ColumnSpacing="14">
                            <Border Grid.Column="0"
                                    BackgroundColor="{StaticResource AccentSoft}"
                                    StrokeThickness="0"
                                    StrokeShape="RoundRectangle 28"
                                    WidthRequest="56"
                                    HeightRequest="56"
                                    HorizontalOptions="Start">
                                <Border.GestureRecognizers>
                                    <TapGestureRecognizer Tapped="OnAvatarTapped" />
                                </Border.GestureRecognizers>
                                <Label x:Name="InitialsLabel"
                                       Text="?"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="20"
                                       TextColor="{StaticResource AccentGlow}"
                                       HorizontalTextAlignment="Center"
                                       VerticalTextAlignment="Center" />
                            </Border>
                            <VerticalStackLayout Grid.Column="1" Spacing="4" VerticalOptions="Center">
                                <Label x:Name="NameLabel"
                                       Text="Athlete"
                                       FontFamily="OpenSansSemibold"
                                       FontSize="17"
                                       TextColor="{StaticResource TextPrimary}" />
                                <Label x:Name="SportLabel"
                                       Text="Sport / Position"
                                       FontFamily="OpenSansRegular"
                                       FontSize="12"
                                       TextColor="{StaticResource TextMuted}" />
                                <Border x:Name="PlanBadge"
                                        BackgroundColor="{StaticResource AccentSoft}"
                                        Stroke="{StaticResource Accent}"
                                        StrokeThickness="1"
                                        StrokeShape="RoundRectangle 10"
                                        Padding="8,3"
                                        HorizontalOptions="Start">
                                    <Label x:Name="PlanLabel"
                                           Text="Free Plan"
                                           FontFamily="OpenSansSemibold"
                                           FontSize="10"
                                           TextColor="{StaticResource AccentGlow}" />
                                </Border>
                            </VerticalStackLayout>
                        </Grid>
                        <Grid ColumnDefinitions="*,*,*" ColumnSpacing="10">
                            <local:MetricTile Grid.Column="0" x:Name="WeightTile"
                                             Label="Weight" Value="—" Unit="kg" />
                            <local:MetricTile Grid.Column="1" x:Name="BodyFatTile"
                                             Label="Body Fat" Value="—" Unit="%" />
                            <local:MetricTile Grid.Column="2" x:Name="FfmiTile"
                                             Label="FFMI" Value="—" Unit="" />
                        </Grid>
                    </VerticalStackLayout>
                </local:AccentCard>

                <!-- Section tabs -->
                <local:SectionTabs x:Name="ProfileSectionTabs"
                                   Items="{x:Static local:ProfilePage.SectionTabItems}" />

                <!-- Overview Panel -->
                <VerticalStackLayout x:Name="OverviewPanel" Spacing="12">
                    <Border Style="{StaticResource CardBorder}">
                        <VerticalStackLayout Spacing="0">
                            <Label Text="Sport Profile"
                                   FontFamily="OpenSansSemibold"
                                   FontSize="15"
                                   TextColor="{StaticResource TextPrimary}"
                                   Margin="0,0,0,10" />
                            <VerticalStackLayout x:Name="SportProfileRows" Spacing="0" />
                        </VerticalStackLayout>
                    </Border>
                    <Border Style="{StaticResource CardBorder}">
                        <VerticalStackLayout Spacing="0">
                            <Label Text="Body Metrics"
                                   FontFamily="OpenSansSemibold"
                                   FontSize="15"
                                   TextColor="{StaticResource TextPrimary}"
                                   Margin="0,0,0,10" />
                            <VerticalStackLayout x:Name="BodyMetricRows" Spacing="0" />
                        </VerticalStackLayout>
                    </Border>
                </VerticalStackLayout>

                <!-- Performance Panel -->
                <VerticalStackLayout x:Name="PerformancePanel" IsVisible="False" Spacing="12">
                    <CollectionView x:Name="PrCollection" SelectionMode="None">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Border Style="{StaticResource CardElevated}" Margin="0,0,0,10">
                                    <Grid ColumnDefinitions="*,Auto">
                                        <VerticalStackLayout Grid.Column="0" Spacing="2">
                                            <Label Text="{Binding ExerciseName}"
                                                   FontFamily="OpenSansSemibold"
                                                   FontSize="15"
                                                   TextColor="{StaticResource TextPrimary}" />
                                            <Label Text="{Binding Date, StringFormat='{0:MMM d, yyyy}'}"
                                                   FontFamily="OpenSansRegular"
                                                   FontSize="11"
                                                   TextColor="{StaticResource TextMuted}" />
                                        </VerticalStackLayout>
                                        <Label Grid.Column="1"
                                               Text="{Binding Value, StringFormat='{0} kg'}"
                                               FontFamily="OpenSansSemibold"
                                               FontSize="22"
                                               TextColor="{StaticResource AccentGlow}"
                                               VerticalTextAlignment="Center" />
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>

                <!-- Goals Panel -->
                <VerticalStackLayout x:Name="GoalsPanel" IsVisible="False" Spacing="12">
                    <CollectionView x:Name="GoalsCollection" SelectionMode="None">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Border Style="{StaticResource CardBorder}" Margin="0,0,0,10">
                                    <VerticalStackLayout Spacing="10">
                                        <Grid ColumnDefinitions="*,Auto">
                                            <Label Grid.Column="0"
                                                   Text="{Binding Name}"
                                                   FontFamily="OpenSansSemibold"
                                                   FontSize="14"
                                                   TextColor="{StaticResource TextPrimary}" />
                                            <Border Grid.Column="1"
                                                    BackgroundColor="{StaticResource AccentSoft}"
                                                    StrokeThickness="0"
                                                    StrokeShape="RoundRectangle 10"
                                                    Padding="8,3">
                                                <Label Text="{Binding Percentage, StringFormat='{0}%'}"
                                                       FontFamily="OpenSansSemibold"
                                                       FontSize="11"
                                                       TextColor="{StaticResource AccentGlow}" />
                                            </Border>
                                        </Grid>
                                        <ProgressBar Progress="{Binding Progress}"
                                                     ProgressColor="{StaticResource Accent}"
                                                     BackgroundColor="{StaticResource SurfaceStrong}"
                                                     HeightRequest="6" />
                                        <Grid ColumnDefinitions="*,*">
                                            <Label Grid.Column="0"
                                                   Text="{Binding Current, StringFormat='Current: {0}'}"
                                                   FontFamily="OpenSansRegular"
                                                   FontSize="11"
                                                   TextColor="{StaticResource TextMuted}" />
                                            <Label Grid.Column="1"
                                                   Text="{Binding Target, StringFormat='Target: {0}'}"
                                                   FontFamily="OpenSansRegular"
                                                   FontSize="11"
                                                   TextColor="{StaticResource TextMuted}"
                                                   HorizontalTextAlignment="End" />
                                        </Grid>
                                    </VerticalStackLayout>
                                </Border>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <local:BottomNavBar Grid.Row="1" x:Name="BottomNav" />

    </Grid>

</ContentPage>
```

- [ ] **Step 3: Add SectionTabs wiring to ProfilePage.xaml.cs**

In `CodeBehind/ProfilePage.xaml.cs`, add at class level and call from constructor/OnAppearing:

```csharp
public static readonly IList<string> SectionTabItems = new[] { "Overview", "Performance", "Goals" };

private void WireSectionTabs()
{
    ProfileSectionTabs.TabSelected += (_, idx) => ShowSection(idx);
}

private void ShowSection(int idx)
{
    OverviewPanel.IsVisible    = idx == 0;
    PerformancePanel.IsVisible = idx == 1;
    GoalsPanel.IsVisible       = idx == 2;
}
```

- [ ] **Step 4: Build**

```
dotnet build FreakLete.csproj -f net10.0-android -c Debug
```

Expected: `Build succeeded. 0 Error(s)`. Fix any `x:Name` mismatches from old code-behind.

- [ ] **Step 5: Commit**

```bash
git add Xaml/ProfilePage.xaml CodeBehind/ProfilePage.xaml.cs
git commit -m "feat: update ProfilePage — AccentCard hero, SectionTabs, PR and Goals panels"
```

---

## Self-Review

**Spec coverage:**
- ✅ All 4 screens updated (Home, Workout, Calculations, Profile)
- ✅ MetricTile updated: compact 14px/20px/11px sizes, UPPERCASE label, ValueColor
- ✅ ElevatedCard + AccentCard with LinearGradientBrush via ControlTemplate
- ✅ QuickAccessTile: Title, Subtitle, IconSource, Command BindableProperties
- ✅ TabSwitcher: Items/SelectedIndex, equal-width Grid columns, Accent active state
- ✅ SectionTabs: Items/SelectedIndex, AccentSoft/AccentGlow active state
- ✅ Styles.xaml: MetricValueLabel (20px/AccentGlow), NavLabel (10px/TextMuted)
- ✅ Epley 1RM formula in CalculationsPage
- ✅ RSI formula (jump height m / GCT s) in CalculationsPage
- ✅ FFMI empty state CTA in CalculationsPage
- ✅ Profile: avatar circle, initials, name, sport, plan badge, body metric tiles
- ✅ Profile Performance: PR list with ExerciseName/Date/Value bindings
- ✅ Profile Goals: progress bar + current/target + percentage badge

**Type consistency:**
- `TabSwitcher.TabSelected` event → defined Task 6, used Task 10 ✅
- `SectionTabs.TabSelected` event → defined Task 7, used Task 11 ✅
- `MetricTile.ValueColor` property → defined Task 2, used Task 8 ✅
- `CalcTabSwitcher` x:Name → declared Task 10 XAML, referenced Task 10 code-behind ✅
- `ProfileSectionTabs` x:Name → declared Task 11 XAML, referenced Task 11 code-behind ✅
- `CalculationsPage.CalcTabItems` static → declared Task 10 code-behind, bound Task 10 XAML ✅
- `ProfilePage.SectionTabItems` static → declared Task 11 code-behind, bound Task 11 XAML ✅
- `OnermResultPanel`, `Pct90Tile`, `Pct80Tile`, `Pct70Tile` → declared Task 10 XAML, set Task 10 code-behind ✅
- `RsiResultPanel`, `RsiResultLabel`, `RsiDescLabel` → declared Task 10 XAML, set Task 10 code-behind ✅
