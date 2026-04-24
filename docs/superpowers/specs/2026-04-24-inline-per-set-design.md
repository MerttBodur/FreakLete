# Inline Per-Set Input — Design Spec

**Date:** 2026-04-24
**Status:** Design approved, awaiting plan generation
**Replaces:** `docs/superpowers/plans/2026-04-24-per-set-persist.md` (popup-based, abandoned)

---

## Problem

Mevcut akışta kullanıcı Set Count (örn. 3) girdikten sonra bir popup açılıyor ve her set için ağırlık/tekrar giriliyor. İki problem:

1. Popup, `ExerciseEntry` modelinde tek `Metric1Value` alanına sığdığı için sadece max weight saklanıyor. Setler farklı ağırlıktaysa kullanıcı doğru veriyi göremiyor.
2. Popup bağlamdan kopuk: kullanıcı form alanları (RIR, Concentric, Rest) ile set-başı girişi aynı ekranda göremiyor. Her set için de bu alanlar girilemiyor.

## Goal

Exercise Builder'da her set ayrı bir kart olarak inline render edilsin. Her set kendi `Weight`, `Reps`, `RIR`, `RestSeconds`, `ConcentricTime` alanlarını taşısın. Backend bu değerleri per-set olarak kalıcı sakla. Aynı component 4 sayfada kullanılsın.

---

## User Experience

### Exercise Builder Flow

1. Kullanıcı egzersiz seçer (Bench Press).
2. Otomatik olarak tek bir set kartı render edilir (Set 1).
3. Set kartı collapsed: sadece `Weight` + `Reps` görünür.
4. Kullanıcı `[▽]` ile detay açar: `RIR`, `Rest (s)`, `Concentric (s)` ek alanları görünür.
5. `+ Set Ekle` butonu → yeni set kartı eklenir. Yeni kartın tüm alanları önceki setten kopyalanır.
6. `− Set Çıkar` → son seti siler. 1 set kaldığında disabled.
7. `Add Exercise` → validation (her set Reps+Weight > 0) → `Session Exercises` listesine eklenir.

### Session Exercises List

Kart kapalı:
```
Bench Press
3 × 5
```
Sadece `SetsCount × Reps` (reps tüm setlerde aynıysa doğrudan, farklıysa last set'in reps'i).

Karta tıklanınca inline expand, tüm setler düzenlenebilir kartlar olarak açılır (aynı `SetCardView`, `IsReadOnly=false`). Değişiklik anında `ObservableCollection` üzerinden memory'ye yazılır.

### Live Session (StartWorkoutSessionPage)

Her set sırayla render edilir. Kullanıcı set'i tamamlar, "Complete" ikonuna basar, kart soluk-görünüm (faded) state'e geçer. Bir sonraki sete odaklanır. (Bu davranış Phase 4 planında detaylandırılır.)

---

## Architecture

### Phases

```
Phase 1 — Backend (entity + migration + controller + DTO)
  ↓
Phase 2 — SetCardView component + NewWorkoutPage inline
  ↓
Phase 3 — AddWorkoutFromProgramPage inline
  ↓
Phase 4 — StartWorkoutSessionPage + Session Exercises inline expand/edit
```

Her phase kendi spec/plan/PR döngüsüne sahip olur. Bu doc 4 phase'i yüksek-seviyeden tarif eder; her phase için ayrı implementation plan yazılır.

### Data Model

#### Backend Entity

`FreakLete.Api/Entities/ExerciseSet.cs`:
```csharp
public class ExerciseSet
{
    public int Id { get; set; }
    public int ExerciseEntryId { get; set; }
    public int SetNumber { get; set; }

    public int Reps { get; set; }
    public double? Weight { get; set; }
    public int? RIR { get; set; }
    public int? RestSeconds { get; set; }
    public double? ConcentricTimeSeconds { get; set; }

    public ExerciseEntry ExerciseEntry { get; set; } = null!;
}
```

#### `ExerciseEntry` Değişikliği

- `public int Sets { get; set; }` → `public int SetsCount { get; set; }` (EF mapping `.HasColumnName("Sets")` korur)
- Yeni navigation: `public List<ExerciseSet> Sets { get; set; } = new();`
- Legacy alanlar kalır: `Reps`, `Metric1Value`, `RIR`, `RestSeconds`, `ConcentricTimeSeconds` — per-set yazılırken türetilir:
  ```
  SetsCount            = sets.Count
  Reps                 = sets[^1].Reps
  Metric1Value         = sets.Max(s => s.Weight)   (null-safe)
  RIR                  = sets[^1].RIR
  RestSeconds          = sets[^1].RestSeconds
  ConcentricTimeSeconds = sets[^1].ConcentricTimeSeconds
  ```
- Geri uyum: `ChartDataHelper`, `CalculationsPage`, `FreakAiToolExecutor`, `PrEntry` akışları legacy alanlardan okumaya devam.

#### DbContext Config

```csharp
modelBuilder.Entity<ExerciseEntry>(e =>
{
    // mevcut config
    e.Property(x => x.SetsCount).HasColumnName("Sets");
});

modelBuilder.Entity<ExerciseSet>(e =>
{
    e.HasKey(s => s.Id);
    e.HasIndex(s => s.ExerciseEntryId);
    e.HasOne(s => s.ExerciseEntry)
     .WithMany(x => x.Sets)
     .HasForeignKey(s => s.ExerciseEntryId)
     .OnDelete(DeleteBehavior.Cascade);
});
```

#### DTO

`FreakLete.Api/DTOs/Workout/ExerciseSetDto.cs`:
```csharp
public class ExerciseSetDto
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double? Weight { get; set; }
    public int? RIR { get; set; }
    public int? RestSeconds { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
}
```

`ExerciseEntryDto`:
- `public int Sets` → `public int SetsCount`
- Yeni: `public List<ExerciseSetDto> Sets { get; set; } = [];`

#### Mobile Model

`Models/SetDetail.cs` (mevcut, genişletilir):
```csharp
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

`Models/ExerciseEntry.cs`:
- `public int Sets` → `public int SetsCount`
- Yeni: `public List<SetDetail> Sets { get; set; } = new();`

### Migration

Migration: `AddExerciseSets`.

Seed:
```sql
INSERT INTO "ExerciseSets"
  ("ExerciseEntryId", "SetNumber", "Reps", "Weight", "RIR", "RestSeconds", "ConcentricTimeSeconds")
SELECT e."Id", gs, e."Reps", e."Metric1Value", e."RIR", e."RestSeconds", e."ConcentricTimeSeconds"
FROM "ExerciseEntries" e
CROSS JOIN LATERAL generate_series(1, GREATEST(e."Sets", 1)) AS gs
WHERE e."TrackingMode" = 'Strength';
```

`Down` metodu: sadece `DropTable("ExerciseSets")`. Legacy alanlar dolu kaldığı için veri kaybı yok.

### Controller

`WorkoutsController`:
- **POST:** `ExerciseEntryDto.Sets` list doluysa → `entity.Sets` navigation doldurulur, legacy alanlar türetilir. Liste boşsa eski DTO davranışı (legacy `SetsCount/Reps/Metric1Value`).
- **GET:** `Include(w => w.ExerciseEntries).ThenInclude(x => x.Sets)`; response DTO'da `Sets` ordered by `SetNumber`.
- **PUT:** Mevcut "eski entries sil, yeni ekle" pattern cascade ile sets'i de temizler.

---

## UI Component: `SetCardView`

`Xaml/Controls/SetCardView.xaml` + `.xaml.cs` — 4 sayfada reuse edilecek ContentView.

### Bindable Properties

| Property | Type | Purpose |
|---|---|---|
| `SetNumber` | int | Kart başlığında gösterilen numara |
| `Weight` | double? | kg cinsinden ağırlık |
| `Reps` | int? | Tekrar sayısı |
| `Rir` | int? | Reps in Reserve (optional) |
| `RestSeconds` | int? | Dinlenme süresi (optional) |
| `ConcentricSeconds` | double? | Konsantrik süre (optional) |
| `IsExpanded` | bool | Detay alanları açık mı (default: false) |
| `IsReadOnly` | bool | Entry → Label render (Session Exercises expand için) |
| `ShowConcentric` | bool | Egzersiz tier'ine göre concentric alanı gizle/göster |

### Events

- `ValueChanged` — herhangi alanda değişim olunca fire (container `ObservableCollection<SetDetail>` günceller).

### Layout

Collapsed:
- Başlık: `Set N` + `[▽]` toggle (sağda)
- 2-kolon grid: Weight Entry, Reps Entry (her ikisi Numeric klavye)

Expanded (toggle = `[△]`):
- Collapsed içerik + alt 3-kolon grid: RIR, Rest (s), Concentric (s) — 3 Entry, hepsi optional placeholder

IsReadOnly=true:
- Her Entry, `Label` olarak render; background SurfaceRaised; ek "edit" ikonu yok — hosting page inline-edit'e geçmek isterse `IsReadOnly=false` yapar.

### AppLanguage Stringleri

Mevcut `AppLanguage.cs`'ye eklenecek:
```csharp
NewWorkoutSetDetailsWeightRequired   // TR/EN
NewWorkoutAddSet                      // "+ Set Ekle" / "+ Add Set"
NewWorkoutRemoveSet                   // "− Set Çıkar" / "− Remove Set"
NewWorkoutSetNumberFormat             // "Set {0}"
NewWorkoutAdvancedDetails             // "Detaylar" / "Details"
```

Mevcut `NewWorkoutSetCount` ve `NewWorkoutSetDetailsRepsRequired` korunur.

---

## Validation

### Add Exercise
- Her set için `Reps > 0` (zorunlu)
- Her set için `Weight > 0` (zorunlu)
- `RIR`, `RestSeconds`, `ConcentricTime` optional
- Minimum 1 set

Hata durumu: İlk hatalı set expand edilir, `ErrorLabel` kırmızı (o set'in kartı altında), sayfa o karta scroll.

### Save Workout
- En az 1 exercise
- Her exercise'de en az 1 set (UI zaten garanti eder)
- Sayfa seviyesinde `ErrorLabel` üst kısımda kalır

---

## Edge Cases

1. **Custom tracking:** `TrackingMode == Custom` olan egzersizler (Vertical Jump vb.) set kartı göstermez. Mevcut `CustomInputsSection` korunur.

2. **Concentric visibility:** Mevcut `StrengthTimingContainer.IsVisible` mantığı egzersiz tier'ine göre. Bu bilgi `SetCardView.ShowConcentric` bindable property'sine geçer; `NewWorkoutPage` `_selectedExerciseItem.Tier` vb.'e göre değerini belirler.

3. **Remove Set minimum:** 1 set kaldığında `Remove Set` disabled. Delete onay yok. Silinen setten sonra `SetNumber`'lar yeniden 1..N'ye atanır.

4. **Add Set focus:** Yeni set kartında Weight Entry otomatik focus.

5. **Legacy kayıt okuma:** Sunucudan `ExerciseEntryDto.Sets` boş gelirse (migration sonrası bile olmamış eski kayıt), `ExerciseSummaryFormatter` legacy `SetsCount × Reps @ Metric1Value` fallback üretir.

6. **AddWorkoutFromProgram pre-fill:** Program şablonundaki `RepsOrDuration`, `IntensityGuidance` vb.'den N adet set kartı hazırlanır. Kullanıcı değerleri editler veya direkt onaylar.

7. **StartWorkoutSessionPage live complete:** Set kartına "Complete" butonu. Complete edilen set faded (opacity 0.5), sonraki sete scroll + focus. Durum Phase 4 plan detayında.

---

## Testing Strategy

### Unit (FreakLete.Core.Tests)

- `ExerciseSummaryFormatterTests`:
  - Tüm setler aynı reps → `3 × 5`
  - Farklı reps → `3 sets` (reps varies)
  - Legacy entry (Sets list boş, SetsCount+Reps+Metric1Value dolu) → `3 × 5 @ 90 kg`
  - RIR dolu → `(RIR 2)` ekler

- `ExerciseEntryLegacyDerivationTests`:
  - Per-set list'ten `Metric1Value = Max(Weight)` doğru
  - `Reps = Sets[^1].Reps`
  - `RIR`, `RestSeconds`, `ConcentricTimeSeconds` → last set'ten kopyalanır
  - Weight'ler null ise `Metric1Value` null

### Integration (FreakLete.Api.Tests)

- `WorkoutsControllerPerSetTests`:
  - POST Sets list'i DB'ye yazar; `ExerciseSets` tablosunda N satır
  - GET response'ta `Sets` ordered (`SetNumber` ASC)
  - PUT overwrite: eski `ExerciseSets` cascade silinir, yenileri eklenir
  - Migration data seed: mevcut bir `ExerciseEntry` Fixture → migration sonrası `Sets.Count == SetsCount`

### Manuel (Android)

Her phase sonunda:
- Happy path (3 set, farklı weight/reps)
- Cancel/back davranışı
- Validation (empty weight → inline error)
- Legacy kayıt görüntüleme (migration sonrası)
- Custom tracking bozulmadı

---

## File Inventory

### Phase 1 — Backend

**Create:**
- `FreakLete.Api/Entities/ExerciseSet.cs`
- `FreakLete.Api/DTOs/Workout/ExerciseSetDto.cs`
- `FreakLete.Api/Migrations/<timestamp>_AddExerciseSets.cs`
- `FreakLete.Api.Tests/WorkoutsControllerPerSetTests.cs`

**Modify:**
- `FreakLete.Api/Entities/ExerciseEntry.cs`
- `FreakLete.Api/Data/AppDbContext.cs`
- `FreakLete.Api/Controllers/WorkoutsController.cs`
- `FreakLete.Api/DTOs/Workout/WorkoutRequest.cs`

### Phase 2 — NewWorkoutPage Inline

**Create:**
- `Xaml/Controls/SetCardView.xaml`
- `Xaml/Controls/SetCardView.xaml.cs`
- `Helpers/ExerciseSummaryFormatter.cs`
- `FreakLete.Core.Tests/ExerciseSummaryFormatterTests.cs`
- `FreakLete.Core.Tests/ExerciseEntryLegacyDerivationTests.cs`

**Modify:**
- `Models/SetDetail.cs`
- `Models/ExerciseEntry.cs`
- `Services/ApiClient.cs`
- `Services/AppLanguage.cs`
- `Xaml/NewWorkoutPage.xaml`
- `CodeBehind/NewWorkoutPage.xaml.cs`
- `FreakLete.Core.Tests/FreakLete.Core.Tests.csproj`

**Delete:**
- `Services/SetDetailsAggregator.cs`
- `Xaml/Controls/SetDetailsPopup.xaml` + `.xaml.cs`
- `FreakLete.Core.Tests/SetDetailsAggregatorTests.cs`

### Phase 3 — AddWorkoutFromProgramPage Inline

**Modify:**
- `Xaml/AddWorkoutFromProgramPage.xaml`
- `CodeBehind/AddWorkoutFromProgramPage.xaml.cs`
- `Helpers/ProgramExerciseConverter.cs`

### Phase 4 — StartWorkoutSessionPage + Session Exercises Expand

**Modify:**
- `Xaml/StartWorkoutSessionPage.xaml`
- `CodeBehind/StartWorkoutSessionPage.xaml.cs`
- `Models/WorkoutSessionState.cs`
- `Xaml/NewWorkoutPage.xaml` (Session Exercises ItemTemplate expand)
- `CodeBehind/NewWorkoutPage.xaml.cs`
- `CodeBehind/WorkoutPreviewPage.xaml.cs`

---

## Dependency Order

```
Phase 1 → Phase 2 → Phase 3 ─┐
                   ↓          ├─→ Phase 4
                   SetCardView
                   (Phase 2'de oluşur, 3 ve 4 reuse eder)
```

---

## Risks

- **Migration veri kaybı:** `generate_series` PostgreSQL-only. CLAUDE.md PostgreSQL. Rollback'te sadece tablo drop edilir, legacy alanlar hâlâ dolu olduğu için veri kayıp olmaz.
- **3 sayfa × SetCardView reuse:** SetCardView API'si Phase 2'de donar. Phase 3/4'te bindable property ekleme gerekirse geri dönüş olur — erken dondurmak yerine Phase 2'de genişletilebilir tasarla.
- **Legacy türetim kayması:** Legacy alanlar her POST/PUT'ta türetilmeli. Controller unit testi bu kaymayı yakalar.
- **Scope:** 4 phase toplam ~40-50 dosya. Her phase ayrı PR + test döngüsü zorunlu.

---

## Decisions Summary

| Karar | Seçim |
|---|---|
| UI layout | Expand/collapse per-card |
| Initial state | 1 set + Add/Remove butonları |
| New set defaults | Önceki setten tüm alanları kopyala |
| Backend field scope | Hepsi per-set + legacy alanlar korunur |
| Session Exercises summary | Collapsed: `N × Reps`; tıkla → inline expand |
| Popup fate | Sil (SetDetailsPopup + SetDetailsAggregator) |
| Scope | 3 sayfa (NewWorkout + FromProgram + SessionLive) + Session Exercises edit |
| Detail tap behavior | Inline expand |
| Session Exercises expand edit | Inline edit |
| Validation | Reps + Weight zorunlu per-set |
| Plan structure | 4 phase × spec + plan + PR |
| Min set | 1 |
