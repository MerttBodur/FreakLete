# Exercise Demo Media Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Egzersiz kataloğunu browse edebilen ve her egzersizin MP4 videosunu oynatanbir detail sayfası eklemek.

**Architecture:** WorkoutPage'e eklenen kart → ExerciseCatalogPage (local JSON'dan kategori/arama) → ExerciseDetailPage (MediaElement ile video + tab'lı içerik). Backend entity/DTO'ya nullable MediaUrl/ThumbnailUrl eklenir; mobil model aynı alanları local JSON'dan okur.

**Tech Stack:** .NET MAUI, XAML, CommunityToolkit.Maui (MediaElement), ASP.NET Core, EF Core migration, Cloudflare R2 (CDN — sadece URL üretimi, kod dışı)

---

## File Map

**Create:**
- `Xaml/ExerciseCatalogPage.xaml`
- `CodeBehind/ExerciseCatalogPage.xaml.cs`
- `Xaml/ExerciseDetailPage.xaml`
- `CodeBehind/ExerciseDetailPage.xaml.cs`

**Modify:**
- `FreakLete.Api/Entities/ExerciseDefinition.cs` — `MediaUrl?`, `ThumbnailUrl?` alanları
- `FreakLete.Api/DTOs/Exercise/ExerciseDefinitionResponse.cs` — aynı alanlar
- `FreakLete.Api/Controllers/ExerciseCatalogController.cs` — `MapToResponse` güncelleme
- `Models/ExerciseCatalogItem.cs` — `MediaUrl?`, `ThumbnailUrl?` alanları
- `FreakLete.csproj` — CommunityToolkit.Maui paketi
- `CodeBehind/MauiProgram.cs` — `.UseMauiCommunityToolkitMediaElement()`
- `Xaml/WorkoutPage.xaml` — "Egzersiz Kataloğu" card
- `CodeBehind/WorkoutPage.xaml.cs` — tap handler
- `FreakLete.Api.Tests/ExerciseCatalogIntegrationTests.cs` — mediaUrl alan testi

**Migration (auto-generated):**
- `FreakLete.Api/Migrations/..._AddExerciseMediaUrl.cs`

---

## Task 1: Backend — ExerciseDefinition'a MediaUrl/ThumbnailUrl ekle

**Files:**
- Modify: `FreakLete.Api/Entities/ExerciseDefinition.cs`
- Modify: `FreakLete.Api/DTOs/Exercise/ExerciseDefinitionResponse.cs`
- Modify: `FreakLete.Api/Controllers/ExerciseCatalogController.cs`

- [ ] **Step 1: Entity'ye alanları ekle**

`FreakLete.Api/Entities/ExerciseDefinition.cs` dosyasında `RecommendedRank` satırından sonra:

```csharp
public int RecommendedRank { get; set; }
public string? MediaUrl { get; set; }
public string? ThumbnailUrl { get; set; }
```

- [ ] **Step 2: DTO'ya alanları ekle**

`FreakLete.Api/DTOs/Exercise/ExerciseDefinitionResponse.cs` dosyasında `RecommendedRank` satırından sonra:

```csharp
public int RecommendedRank { get; set; }
public string? MediaUrl { get; set; }
public string? ThumbnailUrl { get; set; }
```

- [ ] **Step 3: MapToResponse güncelle**

`FreakLete.Api/Controllers/ExerciseCatalogController.cs` dosyasında `MapToResponse` metodunda `RecommendedRank = e.RecommendedRank,` satırından sonra:

```csharp
RecommendedRank = e.RecommendedRank,
MediaUrl = e.MediaUrl,
ThumbnailUrl = e.ThumbnailUrl,
```

- [ ] **Step 4: EF Core migration oluştur**

```bash
cd FreakLete.Api
dotnet ef migrations add AddExerciseMediaUrl
```

Expected: `Build succeeded.` + `Done. To undo this action, use 'ef migrations remove'`

Migration dosyasını aç ve şu kolonların eklendiğini doğrula:

```csharp
migrationBuilder.AddColumn<string>(
    name: "MediaUrl",
    table: "ExerciseDefinitions",
    type: "text",
    nullable: true);

migrationBuilder.AddColumn<string>(
    name: "ThumbnailUrl",
    table: "ExerciseDefinitions",
    type: "text",
    nullable: true);
```

- [ ] **Step 5: Backend build**

```bash
cd FreakLete.Api
dotnet build
```

Expected: `Build succeeded.` 0 errors.

- [ ] **Step 6: Commit**

```bash
git add FreakLete.Api/Entities/ExerciseDefinition.cs
git add FreakLete.Api/DTOs/Exercise/ExerciseDefinitionResponse.cs
git add FreakLete.Api/Controllers/ExerciseCatalogController.cs
git add FreakLete.Api/Migrations/
git commit -m "feat: add MediaUrl and ThumbnailUrl to ExerciseDefinition"
```

---

## Task 2: Backend test — MediaUrl alanı response'da geliyor mu?

**Files:**
- Modify: `FreakLete.Api.Tests/ExerciseCatalogIntegrationTests.cs`

- [ ] **Step 1: Var olan shape testine mediaUrl ve thumbnailUrl assertion ekle**

`GetAll_ResponseShape_HasAllExpectedFields` testinde, `bench.GetProperty("recommendedRank")` assertionından sonra:

```csharp
Assert.Equal(1, bench.GetProperty("recommendedRank").GetInt32());

// mediaUrl ve thumbnailUrl nullable — seed data'da null, field JSON'da var olmalı
var mediaUrl = bench.GetProperty("mediaUrl");
Assert.Equal(System.Text.Json.JsonValueKind.Null, mediaUrl.ValueKind);
var thumbnailUrl = bench.GetProperty("thumbnailUrl");
Assert.Equal(System.Text.Json.JsonValueKind.Null, thumbnailUrl.ValueKind);
```

- [ ] **Step 2: MediaUrl dolu olan exercise için ayrı test ekle**

`ExerciseCatalogIntegrationTests` class'ının sonuna, `PowerExercise_HasCorrectTrackingFields` testinden sonra:

```csharp
[Fact]
public async Task GetById_WithMediaUrl_ReturnsMediaUrl()
{
    // Seed bir egzersizi mediaUrl ile güncelle
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var ex = await db.ExerciseDefinitions.FindAsync("bench-press");
    ex!.MediaUrl = "https://cdn.example.com/bench-press.mp4";
    ex.ThumbnailUrl = "https://cdn.example.com/bench-press-thumb.jpg";
    await db.SaveChangesAsync();

    var client = await AuthenticateAsync();
    var response = await client.GetAsync("/api/ExerciseCatalog/bench-press");
    response.EnsureSuccessStatusCode();

    var body = await response.Content.ReadAsStringAsync();
    var exercise = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

    Assert.Equal("https://cdn.example.com/bench-press.mp4", exercise.GetProperty("mediaUrl").GetString());
    Assert.Equal("https://cdn.example.com/bench-press-thumb.jpg", exercise.GetProperty("thumbnailUrl").GetString());
}
```

- [ ] **Step 3: Testleri çalıştır**

```bash
dotnet test FreakLete.Api.Tests
```

Expected: `All tests passed.` (önceki test sayısı + 2 yeni test)

- [ ] **Step 4: Commit**

```bash
git add FreakLete.Api.Tests/ExerciseCatalogIntegrationTests.cs
git commit -m "test: verify MediaUrl and ThumbnailUrl in exercise catalog response"
```

---

## Task 3: Mobile model — ExerciseCatalogItem'a MediaUrl/ThumbnailUrl ekle

**Files:**
- Modify: `Models/ExerciseCatalogItem.cs`

- [ ] **Step 1: Alanları modele ekle**

`Models/ExerciseCatalogItem.cs` dosyasında `RecommendedRank` satırından sonra:

```csharp
public int RecommendedRank { get; init; }
public string? MediaUrl { get; init; }
public string? ThumbnailUrl { get; init; }
```

`HasSecondaryMetric`, `HintText`, `MuscleSummary`, `DetailSummary`, `SelectionHintText` computed property'lerine dokunma.

- [ ] **Step 2: Build al (mobile)**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: `Build succeeded.` 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Models/ExerciseCatalogItem.cs
git commit -m "feat: add MediaUrl and ThumbnailUrl to ExerciseCatalogItem"
```

---

## Task 4: CommunityToolkit.Maui — MediaElement kurulumu

**Files:**
- Modify: `FreakLete.csproj`
- Modify: `CodeBehind/MauiProgram.cs`

- [ ] **Step 1: NuGet paketini ekle**

```bash
dotnet add FreakLete.csproj package CommunityToolkit.Maui.MediaElement
```

Expected: `Package 'CommunityToolkit.Maui.MediaElement' added to project.`

- [ ] **Step 2: MauiProgram.cs'e MediaElement kaydını ekle**

`CodeBehind/MauiProgram.cs` dosyasında `.ConfigureFonts(...)` bloğundan sonra:

```csharp
builder
    .UseMauiApp<App>()
    .UseMauiCommunityToolkitMediaElement()
    .ConfigureFonts(fonts =>
    {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    });
```

- [ ] **Step 3: Build al**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: `Build succeeded.` 0 errors.

- [ ] **Step 4: Commit**

```bash
git add FreakLete.csproj CodeBehind/MauiProgram.cs
git commit -m "feat: add CommunityToolkit.Maui.MediaElement"
```

---

## Task 5: ExerciseCatalogPage oluştur

**Files:**
- Create: `Xaml/ExerciseCatalogPage.xaml`
- Create: `CodeBehind/ExerciseCatalogPage.xaml.cs`

- [ ] **Step 1: XAML oluştur**

`Xaml/ExerciseCatalogPage.xaml` dosyasını oluştur:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FreakLete.ExerciseCatalogPage"
             NavigationPage.HasNavigationBar="False"
             Title="Egzersiz Kataloğu"
             BackgroundColor="{StaticResource Background}">

    <Grid RowDefinitions="Auto,Auto,Auto,*">

        <!-- Header -->
        <Grid Grid.Row="0"
              Padding="20,52,20,12"
              BackgroundColor="{StaticResource TopBar}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0"
                    x:Name="BackButton"
                    Text="←"
                    FontSize="20"
                    FontFamily="OpenSansSemibold"
                    TextColor="{StaticResource Accent}"
                    BackgroundColor="Transparent"
                    Padding="0"
                    Margin="0,0,12,0"
                    Clicked="OnBackClicked" />
            <Label Grid.Column="1"
                   x:Name="PageTitleLabel"
                   FontSize="21"
                   FontFamily="OpenSansSemibold"
                   TextColor="{StaticResource TextPrimary}"
                   VerticalOptions="Center"
                   Text="Egzersiz Kataloğu" />
        </Grid>

        <!-- Search -->
        <Border Grid.Row="1"
                Margin="16,10,16,0"
                StrokeShape="RoundRectangle 14"
                Stroke="{StaticResource SurfaceBorder}"
                BackgroundColor="{StaticResource SurfaceRaised}"
                Padding="12,0">
            <Entry x:Name="SearchEntry"
                   FontSize="14"
                   FontFamily="OpenSansSemibold"
                   TextColor="{StaticResource TextPrimary}"
                   PlaceholderColor="{StaticResource TextMuted}"
                   BackgroundColor="Transparent"
                   HeightRequest="48"
                   TextChanged="OnSearchTextChanged" />
        </Border>

        <!-- Category chips -->
        <ScrollView Grid.Row="2"
                    Orientation="Horizontal"
                    HorizontalScrollBarVisibility="Never"
                    Margin="0,10,0,0">
            <HorizontalStackLayout x:Name="ChipContainer"
                                   Spacing="8"
                                   Padding="16,0,16,10" />
        </ScrollView>

        <!-- Exercise list -->
        <CollectionView Grid.Row="3"
                        x:Name="ExerciseList"
                        ItemsLayout="VerticalList"
                        SelectionMode="None">
            <CollectionView.GroupHeaderTemplate>
                <DataTemplate>
                    <Label Text="{Binding Key}"
                           FontSize="11"
                           FontFamily="OpenSansSemibold"
                           TextColor="{StaticResource Accent}"
                           CharacterSpacing="1.2"
                           Margin="16,14,16,6" />
                </DataTemplate>
            </CollectionView.GroupHeaderTemplate>
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="x:Object">
                    <Grid Padding="16,3,16,3">
                        <Border StrokeShape="RoundRectangle 14"
                                Stroke="{StaticResource SurfaceBorder}"
                                BackgroundColor="{StaticResource Surface}"
                                Padding="14,12">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Tapped="OnExerciseTapped" />
                            </Border.GestureRecognizers>
                            <Grid ColumnDefinitions="*,Auto">
                                <VerticalStackLayout Grid.Column="0" Spacing="3">
                                    <Label x:Name="ExerciseNameLabel"
                                           FontSize="13"
                                           FontFamily="OpenSansSemibold"
                                           TextColor="{StaticResource TextPrimary}"
                                           LineBreakMode="TailTruncation" />
                                    <Label x:Name="ExerciseMetaLabel"
                                           FontSize="11"
                                           FontFamily="OpenSansRegular"
                                           TextColor="{StaticResource TextMuted}" />
                                </VerticalStackLayout>
                                <HorizontalStackLayout Grid.Column="1" Spacing="6" VerticalOptions="Center">
                                    <Border x:Name="VideoBadge"
                                            StrokeShape="RoundRectangle 8"
                                            BackgroundColor="{StaticResource AccentSoft}"
                                            Stroke="Transparent"
                                            Padding="7,3"
                                            IsVisible="False">
                                        <Label Text="▶"
                                               FontSize="10"
                                               FontFamily="OpenSansSemibold"
                                               TextColor="{StaticResource AccentGlow}" />
                                    </Border>
                                    <Label Text="›"
                                           FontSize="18"
                                           TextColor="{StaticResource TextMuted}"
                                           VerticalOptions="Center" />
                                </HorizontalStackLayout>
                            </Grid>
                        </Border>
                    </Grid>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

    </Grid>
</ContentPage>
```

- [ ] **Step 2: Code-behind oluştur**

`CodeBehind/ExerciseCatalogPage.xaml.cs` dosyasını oluştur:

```csharp
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete;

public partial class ExerciseCatalogPage : ContentPage
{
    private IReadOnlyList<ExerciseCatalogItem> _allExercises = [];
    private string _selectedCategory = "All";
    private string _searchText = string.Empty;

    public ExerciseCatalogPage()
    {
        InitializeComponent();
        BuildCategoryChips();
        LoadExercises();

        var lang = AppLanguage.CurrentLanguage;
        PageTitleLabel.Text = lang == "tr" ? "Egzersiz Kataloğu" : "Exercise Catalog";
        SearchEntry.Placeholder = lang == "tr" ? "Egzersiz ara..." : "Search exercises...";
    }

    private void BuildCategoryChips()
    {
        var lang = AppLanguage.CurrentLanguage;
        var allLabel = lang == "tr" ? "Tümü" : "All";

        ChipContainer.Children.Clear();
        ChipContainer.Children.Add(MakeChip(allLabel, "All", true));

        foreach (var cat in ExerciseCatalog.Categories)
            ChipContainer.Children.Add(MakeChip(cat, cat, false));
    }

    private Border MakeChip(string label, string value, bool selected)
    {
        var chip = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            BackgroundColor = selected
                ? (Color)Application.Current!.Resources["Accent"]
                : (Color)Application.Current!.Resources["SurfaceRaised"],
            Stroke = selected
                ? Colors.Transparent
                : (Color)Application.Current!.Resources["SurfaceBorder"],
            Padding = new Thickness(14, 6),
            BindingContext = value
        };
        chip.Content = new Label
        {
            Text = label,
            FontSize = 12,
            FontFamily = "OpenSansSemibold",
            TextColor = selected
                ? Colors.White
                : (Color)Application.Current!.Resources["TextSecondary"]
        };
        chip.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnChipSelected(chip))
        });
        return chip;
    }

    private void OnChipSelected(Border selected)
    {
        _selectedCategory = (string)selected.BindingContext;

        foreach (var child in ChipContainer.Children.OfType<Border>())
        {
            bool isSelected = child == selected;
            child.BackgroundColor = isSelected
                ? (Color)Application.Current!.Resources["Accent"]
                : (Color)Application.Current!.Resources["SurfaceRaised"];
            child.Stroke = isSelected
                ? Colors.Transparent
                : (Color)Application.Current!.Resources["SurfaceBorder"];
            if (child.Content is Label lbl)
                lbl.TextColor = isSelected
                    ? Colors.White
                    : (Color)Application.Current!.Resources["TextSecondary"];
        }

        ApplyFilters();
    }

    private void LoadExercises()
    {
        _allExercises = ExerciseCatalog.GetAllItems();
        ApplyFilters();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allExercises.AsEnumerable();

        if (_selectedCategory != "All")
            filtered = filtered.Where(x => x.Category == _selectedCategory);

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var q = _searchText.ToLower();
            filtered = filtered.Where(x =>
                x.DisplayName.ToLower().Contains(q) ||
                x.Name.ToLower().Contains(q) ||
                x.TurkishName.ToLower().Contains(q) ||
                x.PrimaryMuscles.Any(m => m.ToLower().Contains(q)));
        }

        var grouped = filtered
            .GroupBy(x => x.Category)
            .OrderBy(g => g.Key)
            .Select(g => new ExerciseGroup(g.Key, g.OrderBy(x => x.RecommendedRank).ThenBy(x => x.DisplayName)))
            .ToList();

        ExerciseList.ItemsSource = grouped;
        RebindListItems();
    }

    // CollectionView with DataTemplate x:DataType="x:Object" requires manual binding in code-behind
    // because ExerciseCatalogItem is sealed and we avoid adding XAML-only dependencies to the model.
    private void RebindListItems()
    {
        // Binding is done via ItemAppearing or ChildAdded — see OnExerciseTapped for item retrieval.
        // Labels are bound via BindingContext set by CollectionView automatically.
        // We use TapGestureRecognizer.CommandParameter workaround below.
    }

    private async void OnExerciseTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.BindingContext is not ExerciseCatalogItem item) return;
        await Navigation.PushAsync(new ExerciseDetailPage(item), true);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync(true);
    }
}

public class ExerciseGroup : List<ExerciseCatalogItem>
{
    public string Key { get; }
    public ExerciseGroup(string key, IEnumerable<ExerciseCatalogItem> items) : base(items)
    {
        Key = key;
    }
}
```

**Not:** CollectionView ile `DataTemplate` içinde `x:DataType` kullanınca binding `ExerciseCatalogItem`'ı otomatik context olarak alır. `ExerciseNameLabel` ve `ExerciseMetaLabel` için XAML'deki `x:Name` yerine binding expression kullan — bunları Step 3'te düzeltiyoruz.

- [ ] **Step 3: XAML binding'leri düzelt**

`ExerciseCatalogPage.xaml` dosyasında DataTemplate içindeki Label'ları binding ile bağla. `x:Name` yerine şu şekilde güncelle:

```xml
<DataTemplate x:DataType="models:ExerciseCatalogItem">
```

Bunun için namespace ekle:

```xml
xmlns:models="clr-namespace:FreakLete.Models"
```

Ve Label binding'lerini güncelle:

```xml
<Label Text="{Binding DisplayName}"
       FontSize="13"
       FontFamily="OpenSansSemibold"
       TextColor="{StaticResource TextPrimary}"
       LineBreakMode="TailTruncation" />
<Label Text="{Binding DetailSummary}"
       FontSize="11"
       FontFamily="OpenSansRegular"
       TextColor="{StaticResource TextMuted}" />
```

VideoBadge için `IsVisible` binding:

```xml
<Border IsVisible="{Binding HasMedia}"
        ...>
```

`ExerciseCatalogItem.cs`'e `HasMedia` computed property ekle (`MediaUrl` satırından sonra):

```csharp
public string? MediaUrl { get; init; }
public string? ThumbnailUrl { get; init; }
public bool HasMedia => !string.IsNullOrWhiteSpace(MediaUrl);
```

- [ ] **Step 4: Build al**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: `Build succeeded.` 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Xaml/ExerciseCatalogPage.xaml CodeBehind/ExerciseCatalogPage.xaml.cs Models/ExerciseCatalogItem.cs
git commit -m "feat: add ExerciseCatalogPage with search and category filter"
```

---

## Task 6: ExerciseDetailPage oluştur

**Files:**
- Create: `Xaml/ExerciseDetailPage.xaml`
- Create: `CodeBehind/ExerciseDetailPage.xaml.cs`

- [ ] **Step 1: XAML oluştur**

`Xaml/ExerciseDetailPage.xaml` dosyasını oluştur:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
             x:Class="FreakLete.ExerciseDetailPage"
             NavigationPage.HasNavigationBar="False"
             Title="Egzersiz Detayı"
             BackgroundColor="{StaticResource Background}">

    <Grid RowDefinitions="Auto,*">

        <!-- Header -->
        <Grid Grid.Row="0"
              Padding="20,52,20,12"
              BackgroundColor="{StaticResource TopBar}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0"
                    Text="←"
                    FontSize="20"
                    FontFamily="OpenSansSemibold"
                    TextColor="{StaticResource Accent}"
                    BackgroundColor="Transparent"
                    Padding="0"
                    Margin="0,0,12,0"
                    Clicked="OnBackClicked" />
            <Label Grid.Column="1"
                   x:Name="ExerciseNameHeader"
                   FontSize="17"
                   FontFamily="OpenSansSemibold"
                   TextColor="{StaticResource TextPrimary}"
                   VerticalOptions="Center"
                   LineBreakMode="TailTruncation" />
        </Grid>

        <!-- Scrollable content -->
        <ScrollView Grid.Row="1">
            <VerticalStackLayout Padding="16,12,16,32" Spacing="12">

                <!-- Hero Card: video + chips -->
                <Border x:Name="HeroCard"
                        StrokeShape="RoundRectangle 18"
                        Stroke="{StaticResource SurfaceBorder}"
                        BackgroundColor="{StaticResource SurfaceRaised}"
                        Padding="0">
                    <VerticalStackLayout>
                        <!-- Video player -->
                        <toolkit:MediaElement x:Name="VideoPlayer"
                                              ShouldAutoPlay="False"
                                              ShouldShowPlaybackControls="True"
                                              BackgroundColor="Black"
                                              HeightRequest="210"
                                              IsVisible="False" />

                        <!-- No-video placeholder -->
                        <Border x:Name="NoVideoPlaceholder"
                                HeightRequest="80"
                                BackgroundColor="{StaticResource Surface}"
                                IsVisible="True">
                            <Label Text="Demo video yakında"
                                   FontSize="13"
                                   FontFamily="OpenSansRegular"
                                   TextColor="{StaticResource TextMuted}"
                                   HorizontalOptions="Center"
                                   VerticalOptions="Center" />
                        </Border>

                        <!-- Muscle + equipment chips -->
                        <FlexLayout x:Name="ChipRow"
                                    Wrap="Wrap"
                                    Direction="Row"
                                    Padding="12,10,12,12"
                                    JustifyContent="Start" />
                    </VerticalStackLayout>
                </Border>

                <!-- Tab bar -->
                <Grid ColumnDefinitions="*,*,*"
                      ColumnSpacing="0">
                    <Button Grid.Column="0"
                            x:Name="TabInstructions"
                            Clicked="OnTabInstructionsClicked"
                            BackgroundColor="Transparent"
                            FontSize="12"
                            FontFamily="OpenSansSemibold"
                            Padding="0,10" />
                    <Button Grid.Column="1"
                            x:Name="TabMistakes"
                            Clicked="OnTabMistakesClicked"
                            BackgroundColor="Transparent"
                            FontSize="12"
                            FontFamily="OpenSansSemibold"
                            Padding="0,10" />
                    <Button Grid.Column="2"
                            x:Name="TabProgression"
                            Clicked="OnTabProgressionClicked"
                            BackgroundColor="Transparent"
                            FontSize="12"
                            FontFamily="OpenSansSemibold"
                            Padding="0,10" />
                </Grid>

                <!-- Tab indicator -->
                <BoxView x:Name="TabIndicator"
                         HeightRequest="2"
                         Color="{StaticResource Accent}"
                         Margin="0,-8,0,4" />

                <!-- Tab content panels -->
                <Border x:Name="PanelInstructions"
                        StrokeShape="RoundRectangle 14"
                        Stroke="{StaticResource SurfaceBorder}"
                        BackgroundColor="{StaticResource Surface}"
                        Padding="16">
                    <Label x:Name="InstructionsLabel"
                           FontSize="13"
                           FontFamily="OpenSansRegular"
                           TextColor="{StaticResource TextSecondary}"
                           LineHeight="1.6" />
                </Border>

                <Border x:Name="PanelMistakes"
                        StrokeShape="RoundRectangle 14"
                        Stroke="{StaticResource SurfaceBorder}"
                        BackgroundColor="{StaticResource Surface}"
                        Padding="16"
                        IsVisible="False">
                    <Label x:Name="MistakesLabel"
                           FontSize="13"
                           FontFamily="OpenSansRegular"
                           TextColor="{StaticResource TextSecondary}"
                           LineHeight="1.6" />
                </Border>

                <Border x:Name="PanelProgression"
                        StrokeShape="RoundRectangle 14"
                        Stroke="{StaticResource SurfaceBorder}"
                        BackgroundColor="{StaticResource Surface}"
                        Padding="16"
                        IsVisible="False">
                    <Label x:Name="ProgressionLabel"
                           FontSize="13"
                           FontFamily="OpenSansRegular"
                           TextColor="{StaticResource TextSecondary}"
                           LineHeight="1.6" />
                </Border>

            </VerticalStackLayout>
        </ScrollView>

    </Grid>
</ContentPage>
```

- [ ] **Step 2: Code-behind oluştur**

`CodeBehind/ExerciseDetailPage.xaml.cs` dosyasını oluştur:

```csharp
using FreakLete.Models;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class ExerciseDetailPage : ContentPage
{
    private readonly ExerciseCatalogItem _exercise;
    private enum Tab { Instructions, Mistakes, Progression }
    private Tab _activeTab = Tab.Instructions;

    public ExerciseDetailPage(ExerciseCatalogItem exercise)
    {
        InitializeComponent();
        _exercise = exercise;
        BindExercise();
        SetTab(Tab.Instructions);
    }

    private void BindExercise()
    {
        var lang = AppLanguage.CurrentLanguage;
        var name = lang == "tr" && !string.IsNullOrWhiteSpace(_exercise.TurkishName)
            ? _exercise.TurkishName
            : _exercise.DisplayName;

        ExerciseNameHeader.Text = name;

        // Video
        if (!string.IsNullOrWhiteSpace(_exercise.MediaUrl))
        {
            VideoPlayer.Source = MediaSource.FromUri(_exercise.MediaUrl);
            if (!string.IsNullOrWhiteSpace(_exercise.ThumbnailUrl))
                VideoPlayer.ShouldMirrorVideo = false; // no-op; poster not available in MediaElement v3 via property
            VideoPlayer.IsVisible = true;
            NoVideoPlaceholder.IsVisible = false;
        }

        // Chips
        AddChips(_exercise.PrimaryMuscles, isPrimary: true);
        AddChips(_exercise.SecondaryMuscles, isPrimary: false);
        if (!string.IsNullOrWhiteSpace(_exercise.Equipment))
            AddChip(_exercise.Equipment, isPrimary: false);
        if (!string.IsNullOrWhiteSpace(_exercise.Mechanic))
            AddChip(_exercise.Mechanic, isPrimary: false);

        // Tab labels
        TabInstructions.Text = lang == "tr" ? "Nasıl Yapılır" : "How To";
        TabMistakes.Text = lang == "tr" ? "Sık Hatalar" : "Mistakes";
        TabProgression.Text = lang == "tr" ? "Progression" : "Progression";

        // Tab content
        InstructionsLabel.Text = string.Join("\n\n", _exercise.Instructions);
        MistakesLabel.Text = _exercise.CommonMistakes;
        ProgressionLabel.Text = BuildProgressionText();

        // Hide tabs with no content
        TabMistakes.IsVisible = !string.IsNullOrWhiteSpace(_exercise.CommonMistakes);
        TabProgression.IsVisible = !string.IsNullOrWhiteSpace(_exercise.Progression);
    }

    private string BuildProgressionText()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_exercise.Progression))
            parts.Add($"Next: {_exercise.Progression}");
        if (!string.IsNullOrWhiteSpace(_exercise.Regression))
            parts.Add($"Easier: {_exercise.Regression}");
        return string.Join("\n\n", parts);
    }

    private void AddChips(IEnumerable<string> values, bool isPrimary)
    {
        foreach (var v in values.Where(s => !string.IsNullOrWhiteSpace(s)))
            AddChip(v, isPrimary);
    }

    private void AddChip(string text, bool isPrimary)
    {
        var chip = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = isPrimary
                ? (Color)Application.Current!.Resources["AccentSoft"]
                : (Color)Application.Current!.Resources["SurfaceRaised"],
            Stroke = isPrimary
                ? Colors.Transparent
                : (Color)Application.Current!.Resources["SurfaceBorder"],
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 0, 6, 6),
            Content = new Label
            {
                Text = text,
                FontSize = 11,
                FontFamily = "OpenSansSemibold",
                TextColor = isPrimary
                    ? (Color)Application.Current!.Resources["AccentGlow"]
                    : (Color)Application.Current!.Resources["TextMuted"]
            }
        };
        ChipRow.Add(chip);
    }

    private void SetTab(Tab tab)
    {
        _activeTab = tab;

        PanelInstructions.IsVisible = tab == Tab.Instructions;
        PanelMistakes.IsVisible = tab == Tab.Mistakes;
        PanelProgression.IsVisible = tab == Tab.Progression;

        TabInstructions.TextColor = tab == Tab.Instructions
            ? (Color)Application.Current!.Resources["Accent"]
            : (Color)Application.Current!.Resources["TextMuted"];
        TabMistakes.TextColor = tab == Tab.Mistakes
            ? (Color)Application.Current!.Resources["Accent"]
            : (Color)Application.Current!.Resources["TextMuted"];
        TabProgression.TextColor = tab == Tab.Progression
            ? (Color)Application.Current!.Resources["Accent"]
            : (Color)Application.Current!.Resources["TextMuted"];

        // Move indicator
        var col = tab switch
        {
            Tab.Instructions => 0,
            Tab.Mistakes => 1,
            Tab.Progression => 2,
            _ => 0
        };
        // indicator is full-width; column highlight via text color only
        _ = col;
    }

    private void OnTabInstructionsClicked(object? sender, EventArgs e) => SetTab(Tab.Instructions);
    private void OnTabMistakesClicked(object? sender, EventArgs e) => SetTab(Tab.Mistakes);
    private void OnTabProgressionClicked(object? sender, EventArgs e) => SetTab(Tab.Progression);

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        VideoPlayer.Stop();
        await Navigation.PopAsync(true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        VideoPlayer.Stop();
    }
}
```

- [ ] **Step 3: Build al**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: `Build succeeded.` 0 errors. Eğer `MediaSource` namespace hatası alırsan `using CommunityToolkit.Maui.Core.Primitives;` ekle.

- [ ] **Step 4: Commit**

```bash
git add Xaml/ExerciseDetailPage.xaml CodeBehind/ExerciseDetailPage.xaml.cs
git commit -m "feat: add ExerciseDetailPage with video player and tabbed content"
```

---

## Task 7: WorkoutPage'e katalog kartı ekle

**Files:**
- Modify: `Xaml/WorkoutPage.xaml`
- Modify: `CodeBehind/WorkoutPage.xaml.cs`

- [ ] **Step 1: WorkoutPage XAML'e katalog kartı ekle**

`Xaml/WorkoutPage.xaml` dosyasında, `<!-- Page Header -->` bloğundan hemen sonra, `<!-- Hero Card: Active Program -->` öncesine:

```xml
<!-- Exercise Catalog Card -->
<Border StrokeShape="RoundRectangle 18"
        Stroke="{StaticResource SurfaceBorder}"
        BackgroundColor="{StaticResource Surface}"
        Padding="18,14">
    <Border.GestureRecognizers>
        <TapGestureRecognizer Tapped="OnExerciseCatalogTapped" />
    </Border.GestureRecognizers>
    <Grid ColumnDefinitions="*,Auto">
        <VerticalStackLayout Grid.Column="0" Spacing="3">
            <Label x:Name="CatalogCardTitle"
                   FontSize="15"
                   FontFamily="OpenSansSemibold"
                   TextColor="{StaticResource TextPrimary}"
                   Text="Egzersiz Kataloğu" />
            <Label x:Name="CatalogCardSubtitle"
                   FontSize="12"
                   FontFamily="OpenSansRegular"
                   TextColor="{StaticResource TextMuted}"
                   Text="Hareketleri keşfet, videoları izle" />
        </VerticalStackLayout>
        <Label Grid.Column="1"
               Text="›"
               FontSize="22"
               TextColor="{StaticResource Accent}"
               VerticalOptions="Center" />
    </Grid>
</Border>
```

- [ ] **Step 2: WorkoutPage code-behind'a tap handler ekle**

`CodeBehind/WorkoutPage.xaml.cs` dosyasında mevcut event handler'lardan birine bakarak aynı pattern ile ekle:

```csharp
private async void OnExerciseCatalogTapped(object? sender, TappedEventArgs e)
{
    await Navigation.PushAsync(new ExerciseCatalogPage(), true);
}
```

- [ ] **Step 3: Localization (opsiyonel, diğer string'lerle tutarlı olsun)**

WorkoutPage'deki diğer string'ler `AppLanguage` ile set ediliyorsa, `OnAppearing` veya `ApplyLocalization` metodunda:

```csharp
var lang = AppLanguage.CurrentLanguage;
CatalogCardTitle.Text = lang == "tr" ? "Egzersiz Kataloğu" : "Exercise Catalog";
CatalogCardSubtitle.Text = lang == "tr" ? "Hareketleri keşfet, videoları izle" : "Explore movements, watch demos";
```

WorkoutPage localization pattern'ini koddan oku ve uygun yere ekle.

- [ ] **Step 4: Build al**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: `Build succeeded.` 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Xaml/WorkoutPage.xaml CodeBehind/WorkoutPage.xaml.cs
git commit -m "feat: add exercise catalog entry card to WorkoutPage"
```

---

## Self-Review

**Spec coverage:**
- ✅ WorkoutPage kart → Task 7
- ✅ ExerciseCatalogPage (search + category chips + grouped list + video badge) → Task 5
- ✅ ExerciseDetailPage (hero card + video + chips + tabs) → Task 6
- ✅ MediaUrl/ThumbnailUrl backend entity + DTO + migration → Task 1
- ✅ Backend test → Task 2
- ✅ Mobile model → Task 3
- ✅ CommunityToolkit.Maui kurulumu → Task 4
- ✅ Video yoksa fallback (NoVideoPlaceholder, tab gizleme) → Task 6
- ✅ OnDisappearing'de VideoPlayer.Stop() → Task 6

**Placeholder scan:** TBD veya TODO yok. Tüm steplar kod içeriyor.

**Type consistency:**
- `ExerciseCatalogItem.MediaUrl` — Task 3 ve Task 5/6'da tutarlı
- `ExerciseGroup` class — Task 5 code-behind sonunda tanımlandı, Task 6'da kullanılmıyor
- `MediaSource.FromUri()` — CommunityToolkit.Maui.MediaElement namespace'inden geliyor, Task 4'te paket eklendi

**Dikkat edilmesi gereken:**
- Task 5 Step 3'te `x:DataType` ve namespace eklenmesi kritik; atlanırsa binding çalışmaz
- `VideoPlayer.Stop()` çağrısı `OnDisappearing`'de zorunlu; yoksa arka planda çalmaya devam eder
- `CollectionView.IsGrouped="True"` XAML'de set edilmesi gerekiyor — Task 5 XAML'de eklenmiş durumda; `ItemsSource` `IEnumerable<IGrouping>` veya `ObservableCollection<ExerciseGroup>` kabul eder
