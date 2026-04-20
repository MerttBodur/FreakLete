# Design Handoff → MAUI Adaptation

**Date:** 2026-04-20
**Source:** `design_handoff/` package (README.md + prototype/index.html)
**Scope:** 4 ekran + shared Controls foundation

---

## Kararlar

| Karar | Seçim |
|---|---|
| Ekranlar | Home, Workout, Calculations, Profile (tümü) |
| Component yaklaşımı | Reusable ContentViews (A) |
| Kart arka planı | LinearGradientBrush — prototype'a sadık (A) |
| Uygulama sırası | Foundation-first: Controls → Styles → Screens (A) |

---

## Mimari

### Yeni Dosyalar

```
Xaml/Controls/
  MetricTile.xaml + MetricTile.xaml.cs
  ElevatedCard.xaml + ElevatedCard.xaml.cs
  AccentCard.xaml + AccentCard.xaml.cs
  QuickAccessTile.xaml + QuickAccessTile.xaml.cs
  TabSwitcher.xaml + TabSwitcher.xaml.cs
  SectionTabs.xaml + SectionTabs.xaml.cs
```

### Güncellenen Dosyalar

```
Resources/Styles/Styles.xaml   ← eksik named style'lar eklenir
Xaml/HomePage.xaml             ← prototype Home layout'una uyarlanır
Xaml/WorkoutPage.xaml          ← prototype Workout layout'una uyarlanır
Xaml/CalculationsPage.xaml     ← prototype Calc layout'una uyarlanır
Xaml/ProfilePage.xaml          ← prototype Profile layout'una uyarlanır
```

### Dokunulmayan Katmanlar

CodeBehind logic, Services, ViewModels, API layer — yalnızca XAML/UI katmanı değişir.

---

## Shared Controls Spesifikasyonları

### MetricTile

**BindableProperty'ler:** `Label`, `Value`, `Unit`, `ValueColor`

```xml
<controls:MetricTile Label="This Week" Value="4" Unit="sessions" />
<controls:MetricTile Label="Streak" Value="12" Unit="days" ValueColor="{StaticResource Success}" />
```

| Token | Değer |
|---|---|
| Background | `SurfaceRaised` |
| Border | `SurfaceBorder`, 1px |
| Radius | 14 |
| Padding | 12, 14 |
| Label style | 10px / Semibold / TextMuted / UPPERCASE |
| Value style | 20px / Semibold / AccentGlow (override: ValueColor) |
| Unit style | 11px / Regular / TextMuted |

---

### ElevatedCard

Gradient arka planlı Content host.

```xml
<controls:ElevatedCard>
    <!-- içerik -->
</controls:ElevatedCard>
```

| Token | Değer |
|---|---|
| Background | `LinearGradientBrush(160°, #1D1828 → #171321)` |
| Border | `SurfaceBorder`, 1px |
| Radius | 24 |
| Padding | 18 |

---

### AccentCard

FreakAI ve accent vurgulu içerikler için.

```xml
<controls:AccentCard>
    <!-- içerik -->
</controls:AccentCard>
```

| Token | Değer |
|---|---|
| Background | `LinearGradientBrush(160°, #2F2346 → #171321)` |
| Border | `Accent (#8B5CF6)`, 1px |
| Radius | 24 |
| Padding | 18 |

---

### QuickAccessTile

**BindableProperty'ler:** `Title`, `Subtitle`, `IconSource`, `Command`

```xml
<controls:QuickAccessTile Title="Calculations" Subtitle="1RM · RSI · FFMI" IconSource="nav_plates.svg" />
```

| Token | Değer |
|---|---|
| Background | `SurfaceRaised` |
| Border | `SurfaceBorder`, 1px |
| Radius | 24 |
| Padding | 14 |
| Icon color | `AccentGlow` |

---

### TabSwitcher (Calculations)

**BindableProperty'ler:** `Items` (string listesi), `SelectedIndex`

```xml
<controls:TabSwitcher Items="{Binding CalcTabs}" SelectedIndex="{Binding ActiveTab}" />
```

| State | Style |
|---|---|
| Container | `SurfaceRaised` bg / `SurfaceBorder` border / radius 18 / padding 4 |
| Active tab | `Accent` bg / `TextPrimary` text |
| Inactive tab | Transparent bg / `TextMuted` text |

---

### SectionTabs (Profile)

Aynı mantık, pill-style. Overview / Performance / Goals.

| State | Style |
|---|---|
| Active | `AccentSoft` bg / `AccentGlow` text |
| Inactive | Transparent bg / `TextMuted` text |

---

### Styles.xaml Eklemeleri

| Key | Spec |
|---|---|
| `MetricValueLabel` | 20px / Semibold / AccentGlow |
| `NavLabel` | 10px / Semibold / TextMuted |
| `EyebrowLabel` | zaten var — kontrol et, yoksa ekle |

---

## Ekran Layoutları

### Home (Dashboard)

```
ScrollView
└── VStack (padding: 20, gap: 16)
    ├── ElevatedCard
    │   ├── HStack: Eyebrow "TODAY" + Premium badge (AccentSoft bg / AccentGlow text)
    │   ├── Label "Ready to train?" [CardTitle: 18px/Semibold]
    │   ├── HStack: 3× MetricTile
    │   │     This Week / Last 1RM (kg · exercise) / Streak (days, Success color)
    │   └── Button "Start Workout" [Primary, full-width]
    ├── VStack
    │   ├── Label "Quick Access" [15px/Semibold/TextPrimary]
    │   └── HStack: 2× QuickAccessTile
    │         Calculations (nav_plates.svg) / Calendar (calendar_icon.svg)
    └── AccentCard
        ├── HStack: BoltIcon + VStack(FreakAI label + kota metni)
        └── HStack: Entry "Ask your coach..." + ImageButton (icon_send.svg, Accent bg)
```

---

### Workout

**Landing state:**
```
ScrollView
└── VStack (padding: 20, gap: 16)
    ├── ElevatedCard
    │   ├── Label "Start a Session" [SubHeadline]
    │   └── HStack: Button "Quick Start" [Primary] + Button "From Program" [Secondary]
    └── CollectionView: program templates
        └── per item → CardBorder
            ├── Label: template adı [CardTitle]
            ├── HStack: Badge'ler (muscle group tags)
            └── Label: sessions/week [TextMuted]
```

**Active session state:** mevcut `StartWorkoutSessionPage` korunur. Set kartları styling güncellenir:
- Done: `SuccessSoft` (#0D2818) bg / `Success` (#22C55E) border / checkmark
- Undone: `SurfaceRaised` bg / `SurfaceBorder` border

---

### Calculations

```
VStack (padding: 20, gap: 16)
├── TabSwitcher ["1RM", "RSI", "FFMI"]
└── ContentPresenter (SelectedIndex'e göre)

    [1RM Tab]
    ├── Input: Weight (kg)
    ├── Input: Reps
    ├── Button "Calculate" [Primary]
    └── Result bölümü (hesaplama sonrası görünür)
        ├── Label: sonuç değeri [42px / Semibold / AccentGlow]
        └── HStack: 3× MetricTile (90% / 80% / 70%)

    [RSI Tab]
    ├── Input: Jump Height (cm)
    ├── Input: Ground Contact Time (ms)
    ├── Button "Calculate" [Primary]
    └── Result: RSI score + açıklama Label

    [FFMI Tab]
    └── Empty state (profil verisi yoksa CTA button)
```

---

### Profile

```
ScrollView
└── VStack
    ├── AccentCard [Hero bölümü]
    │   ├── HStack
    │   │   ├── Avatar: 56px daire (AccentSoft bg / AccentGlow initial label)
    │   │   └── VStack: ad Label + spor/pozisyon Label + Plan badge
    │   └── HStack: 3× MetricTile (Weight / Body Fat / FFMI)
    ├── SectionTabs ["Overview", "Performance", "Goals"]
    └── ContentPresenter

        [Overview]
        ├── CardBorder: Sport Profile → ListRow'lar
        └── CardBorder: Body Metrics → ListRow'lar

        [Performance]
        └── CollectionView: PR kayıtları
            └── per item → CardElevated
                ├── Label: hareket adı [15px/Semibold]
                ├── Label: tarih [Caption/TextMuted]
                └── Label: PR değeri [SubHeadline/AccentGlow]

        [Goals]
        └── CollectionView: hedefler
            └── per item → CardBorder
                ├── Label: hedef adı
                ├── ProgressBar (Accent fill / SurfaceStrong track)
                └── HStack: current/target labels + % badge
```

---

## Design Token Eşlemesi

Tüm tokenlar `Resources/Styles/Colors.xaml`'da mevcut.

| Handoff Token | MAUI Key |
|---|---|
| Background | `Background` |
| Surface | `Surface` |
| SurfaceRaised | `SurfaceRaised` |
| Accent | `Accent` |
| AccentGlow | `AccentGlow` |
| AccentSoft | `AccentSoft` |
| SurfaceBorder | `SurfaceBorder` |
| TextPrimary | `TextPrimary` |
| TextMuted | `TextMuted` |
| Success | `Success` |
| SuccessSoft | `SuccessSoft` |

---

## MAUI Kısıtlamaları & Çözümler

| Kısıtlama | Çözüm |
|---|---|
| `LinearGradientBrush` Styles.xaml'da global style tanımlanamaz | ContentView içinde inline `Border.Background` olarak tanımlanır |
| CSS `box-shadow` yok | `SurfaceBorder` border ile derinlik; bottom nav için `Shadow` element |
| SVG icon tint | `TintColor` property veya MAUI `Image` ColorFilters |
| CSS `letter-spacing` | MAUI `CharacterSpacing` (pt cinsinden) |
