# Exercise Demo Media — Design Spec
Date: 2026-04-15

## Overview
Kullanıcılar egzersizlerin nasıl yapıldığını video ile izleyebilecek. WorkoutPage üzerinden erişilebilen bağımsız bir Exercise Catalog sayfası ve egzersiz başına bir Detail sayfası ekleniyor.

## User Flow
```
WorkoutPage
  └── "Egzersiz Kataloğu" kartı
        └── ExerciseCatalogPage (YENİ)
              └── egzersize tap
                    └── ExerciseDetailPage (YENİ)
```

Picker akışına (ExercisePickerPage) dokunulmaz. Bu özellik, kullanıcının workout oluştururken egzersiz seçmesini değil; egzersizleri browse edip nasıl yapıldığını öğrenmesini hedefler.

## Architecture
**Yaklaşım:** İki yeni sayfa — ExerciseCatalogPage + ExerciseDetailPage.

Seçilme sebebi: Sorumluluklar net ayrılmış, picker akışı kirlenmiyor, her sayfa bağımsız geliştirilebilir.

Reddedilen alternatifler:
- ExercisePickerPage dual-mode: picker mantığı karışıyor
- Bottom sheet modal: MAUI'de karmaşık, video + full content sıkışıyor

## Pages

### ExerciseCatalogPage
**Amaç:** Tüm egzersiz kataloğunu browse etmek, filtrelemek, detaya geçmek.

**Bileşenler:**
- Üstte sabit arama kutusu (`Entry`, SurfaceRaised, placeholder "Egzersiz ara...")
- Yatay scroll kategori chip'leri (Tümü / Chest / Back / Legs / Shoulders / Arms / Core / Athletic / ...)
- Seçili chip: Accent arka plan. Seçisiz: SurfaceBorder kenarlıklı, muted
- Kategori grup başlıkları (TextSecondary, uppercase, eyebrow style)
- Egzersiz listesi: her satır CardBorder, `14px radius`
  - Sol: egzersiz adı (TextPrimary, Semibold 13px) + ekipman · tip (TextMuted 9px)
  - Sağ: video rozeti (▶, AccentSoft bg, AccentGlow text) varsa + şevron (›)
- Videosu olmayan egzersizler rozetsiz gösterilir, detay sayfasına yine gider

**Filtreleme mantığı:** Kategori chip seçilince liste o kategoriye göre filtrelenir. Arama kutusu aktifken chip filtresi de korunur (AND mantığı).

### ExerciseDetailPage
**Amaç:** Egzersizin video demosu ve tam bilgisi.

**Layout:** ScrollView içinde tek sütun.

**Bileşenler (yukarıdan aşağı):**
1. Header bar: geri butonu + egzersiz adı (HeaderTitle style)
2. Hero Card (CardBorder, 16px radius):
   - 16:9 `MediaElement` — `ShouldAutoPlay="False"`, `ShouldShowPlaybackControls="True"`, `Poster` thumbnail URL
   - Video yoksa hero card collapse edilir (IsVisible = false)
   - Alt kısım: kas grubu chip'leri (AccentSoft bg, AccentGlow text) + ekipman/tip chip'leri (SurfaceBorder, TextMuted)
3. Tab bar: **Nasıl Yapılır** / **Sık Hatalar** / **Progression** (Regression 4. tab olarak opsiyonel)
4. Tab içeriği: ScrollView, TextSecondary, 13px Regular, line-height 1.7

**Fallback davranışı:**
- `MediaUrl` null → hero card gizlenir, chip'ler + tab'lar normal görünür
- Tab alanı boşsa (JSON'da yok) o tab gizlenir
- Minimum: egzersiz adı + en az bir tab içeriği her zaman gösterilir

## Data Changes

### Backend — ExerciseDefinition entity
```csharp
public string? MediaUrl { get; set; }      // Cloudflare R2 CDN URL, nullable
public string? ThumbnailUrl { get; set; }  // Poster image URL, nullable
```
- EF Core migration gerekir
- Mevcut endpoint'e bu alanlar eklenir (breaking change yok, nullable)

### Mobile — ExerciseDefinition model
```csharp
public string? MediaUrl { get; set; }
public string? ThumbnailUrl { get; set; }
```

### WorkoutPage
- "Egzersiz Kataloğu" action card/tile eklenir
- Tıklanınca `ExerciseCatalogPage`'e navigate eder

## Media Hosting

**Platform:** Cloudflare R2
- Storage: ilk aşama ~20-30 video × 5MB ≈ 150MB → ücretsiz tier (10GB/ay)
- Egress: R2'de ücretsiz (S3'ten farklı)
- Format: MP4 (H.264), max 720p, ~15-30 saniye

**İlk aşama kapsam (~20-30 egzersiz):**
Bench Press, Incline DB Press, Back Squat, Front Squat, Deadlift, Romanian Deadlift, Overhead Press, Pull-Up, Barbell Row, Dumbbell Row, Power Clean, Hang Clean, Box Jump, Broad Jump, Vertical Jump, Hip Thrust, Leg Press, Dumbbell Curl, Tricep Pushdown, Plank ve benzeri popüler hareketler.

**Genişleme:** İlk aşama oturulduktan sonra tüm 251 egzersiz kapsamına geçilebilir.

## Tech Dependencies

| Bağımlılık | Kullanım |
|---|---|
| `CommunityToolkit.Maui` | `MediaElement` video playback |
| Cloudflare R2 | MP4 + thumbnail CDN hosting |
| EF Core migration | `MediaUrl`, `ThumbnailUrl` alanları |

`CommunityToolkit.Maui` zaten projede varsa sadece `MediaElement` kullanımı eklenir. Yoksa NuGet paketi eklenir ve `MauiProgram.cs`'de `.UseMauiCommunityToolkitMediaElement()` çağrısı yapılır.

## Performance

| Risk | Önlem |
|---|---|
| Katalog sayfasında video yükleme | Video sadece DetailPage'de yüklenir |
| Otomatik oynatma bant tüketimi | `ShouldAutoPlay="False"` |
| Tam dosya indirme | MediaElement streaming destekler |
| Büyük liste render | CollectionView + virtualization (MAUI default) |

## Out of Scope
- ExercisePickerPage'e dokunulmaz
- Bottom navigation değişmez
- iOS/Mac/Windows billing değişmez
- Sosyal özellik, beğeni, yorum yok
- Tüm 251 egzersiz için video (Phase 1)
- Offline video cache

## Success Criteria
- WorkoutPage'den kataloga, katalogdan detaya geçiş çalışır
- Video olan egzersizde MediaElement oynatılabilir
- Video olmayan egzersizde hero card gizlenir, içerik normal görünür
- Arama + kategori filtresi birlikte çalışır
- `MediaUrl` null olan satırlarda rozet görünmez
- `FreakLete.Api.Tests` migration + endpoint coverage
- `FreakLete.Core.Tests` etkilenen alan yoksa dokunulmaz
