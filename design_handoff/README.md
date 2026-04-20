# Handoff: FreakLete Design System

## Overview
FreakLete; field athlete'ler ve gym odaklı sporcular için geliştirilmiş bir mobil performans takip uygulamasıdır (.NET MAUI, Android-first). Bu paket, uygulamanın tam görsel sistemini — renkler, tipografi, spacing, komponentler ve 4 ekranlı click-through prototype — içermektedir.

## Design Files Hakkında
Bu paketteki HTML dosyaları **tasarım referanslarıdır** — production'a doğrudan alınacak kod değil. Amaç, bu tasarımları mevcut .NET MAUI / XAML ortamında (veya başka bir hedef ortamda) yeniden uygulamaktır.

## Fidelity
**High-fidelity.** Renkler, tipografi, spacing ve interaction states birebir pixel-level tanımlanmıştır. Geliştiricinin bu tasarımları hedef ortamda pixel-perfect olarak uygulaması beklenmektedir.

---

## Design Tokens

### Renkler

| Token | Hex | Kullanım |
|---|---|---|
| `Background` | `#100D1A` | Sayfa canvas |
| `Surface` | `#171321` | Kart yüzeyi |
| `SurfaceRaised` | `#1D1828` | Yükseltilmiş kart, input arka plan |
| `SurfaceStrong` | `#251F33` | Secondary button arka plan |
| `TopBar` | `#161125` | Navigasyon / top bar tonu |
| `Accent` | `#8B5CF6` | Primary aksiyon, CTA, aktif tab |
| `AccentGlow` | `#A78BFA` | Metric değerler, aktif label, vurgu |
| `AccentSoft` | `#2F2346` | Accent kart arka planı, aktif tab bg |
| `SurfaceBorder` | `#342D46` | Varsayılan kart/input border |
| `BorderSubtle` | `#2A2437` | Daha hafif ayırıcı çizgiler |
| `TextPrimary` | `#F7F7FB` | Ana metin |
| `TextSecondary` | `#B3B2C5` | Açıklama, yardımcı metin |
| `TextMuted` | `#8A889B` | Metadata, caption, placeholder |
| `Success` | `#22C55E` / soft `#0D2818` | Başarı durumu |
| `Warning` | `#F59E0B` / soft `#2A1F06` | Uyarı durumu |
| `Danger` | `#DC2626` / soft `#3A1623` | Tehlikeli aksiyon |
| `Error` | `#EF4444` | Hata mesajı |
| `Info` | `#3B82F6` / soft `#0D1B2A` | Bilgi durumu |

**Gradient'ler:**
- Card: `linear-gradient(160deg, #1D1828 0%, #171321 100%)`
- Accent: `linear-gradient(160deg, #2F2346 0%, #171321 100%)`

### Tipografi

**Font:** Open Sans (Semibold 600 + Regular 400). Serif yok, monospace yok.

| Rol | Boyut | Ağırlık | Kullanım |
|---|---|---|---|
| Headline | 34px | 600 | Login/Register hero |
| SubHeadline | 24px | 600 | Section başlığı, metrik değer |
| HeaderTitle | 21px | 600 | Top bar başlığı |
| Card Title | 18–20px | 600 | Kart başlıkları |
| Label | 15px | 600 | Standart label, input text |
| Body | 14px | 400 | Açıklama, yardımcı metin |
| Eyebrow | 12px | 600 | ALL CAPS, `letter-spacing: 0.08em` |
| Caption | 11–12px | 400 | Metadata, tarih |
| Nav Label | 10px | 600 | Bottom nav tab etiketi |

### Spacing (4px baz ritmi)

| Token | Değer | Kullanım |
|---|---|---|
| sp-1 | 4px | Hairline |
| sp-2 | 8px | In-card tight stack |
| sp-3 | 12px | In-card stack |
| sp-4 | 16px | Standart gap |
| sp-5 | 18px | Card padding, section spacing |
| sp-6 | 20px | Sayfa dış gutter |
| sp-7 | 24px | Section header spacing |
| sp-8 | 32px | Major section break |

### Border Radius

| Token | Değer | Kullanım |
|---|---|---|
| radius-sm | 10px | Küçük interaktif element |
| radius-md | 14px | Compact button/badge |
| radius-base | 18px | Button, input |
| radius-card | 24px | Kart |
| radius-shell | 26px | Bottom nav container |

### Gölge
- Bottom nav: `box-shadow: 0 4px 20px rgba(0,0,0,0.35)`
- Diğer yerlerde shadow kullanılmaz — derinlik için border + gradient tercih edilir.

### Touch Target
Minimum: **44×44px**

---

## Komponent Spesifikasyonları

### Primary Button
```
background: #8B5CF6
color: #F7F7FB
font: 15px / 600 / Open Sans
border-radius: 18px
padding: 13px 22px
min-height: 44px
hover: opacity 0.88
active: opacity 0.75 + scale(0.98)
```

### Secondary Button
```
background: #251F33
color: #F7F7FB
border: 1px solid #342D46
font: 15px / 600 / Open Sans
border-radius: 18px
padding: 13px 22px
min-height: 44px
```

### Destructive Button
```
background: #3A1623
color: #DC2626
border: 1px solid #DC2626
font: 15px / 600 / Open Sans
border-radius: 18px
```

### Card (Standard)
```
background: #171321
border: 1px solid #342D46
border-radius: 24px
padding: 18px
```

### Card (Elevated)
```
background: linear-gradient(160deg, #1D1828, #171321)
border: 1px solid #342D46
border-radius: 24px
padding: 18px
```

### Card (Accent)
```
background: linear-gradient(160deg, #2F2346, #171321)
border: 1px solid #8B5CF6
border-radius: 24px
padding: 18px
```

### Input / Entry
```
background: #1D1828
color: #F7F7FB
font: 15px / 600 / Open Sans
placeholder color: #8A889B
border: 1px solid #342D46
border-radius: 18px
padding: 13px 16px
min-height: 52px
focus border: #8B5CF6
```

### Bottom Navigation
```
container:
  background: #171321
  border: 1px solid #342D46
  border-radius: 26px
  box-shadow: 0 4px 20px rgba(0,0,0,0.35)
  padding: 6px 8px

tab (inactive):
  icon stroke: #8A889B
  label: 10px / 600 / #8A889B

tab (active):
  background: #2F2346
  border-radius: 18px
  icon stroke: #A78BFA
  label: 10px / 600 / #A78BFA
```

### MetricTile
```
background: #1D1828
border: 1px solid #342D46
border-radius: 14px
padding: 12px 14px

label: 10px / 600 / #8A889B / UPPERCASE
value: 20px / 600 / #A78BFA
unit: 11px / regular / #8A889B
```

---

## Ekranlar

### 1. Home (Dashboard)
**Amaç:** Günlük antrenman akışına giriş noktası.

**Layout:**
- `TopBar` — "FreakLete" başlığı, sağda EN/TR dil badge'i
- `HeroCard` (Elevated) — "Today" eyebrow, "Ready to train?" başlık, 3 MetricTile (This Week / Last 1RM / Streak), "Start Workout" primary button
- `QuickAccess` — 2 tile: Calculations + Calendar (SurfaceRaised bg, 24px radius)
- `FreakAICard` (Accent) — "FreakAI" başlık, kota bilgisi, chat input + send button

**State:**
- FreakAI input: controlled text field
- Kota: free user için "N messages remaining", premium için "Unlimited"

---

### 2. Workout
**Amaç:** Antrenman başlatma + aktif seans yönetimi.

**Landing state:**
- HeroCard: "Quick Start" + "From Program" butonları
- Starter Templates listesi: kart per template, name + tag badge'leri + sessions/wk metriği

**Active session state (Quick Start'a basınca):**
- Header: "Live Session" eyebrow + canlı timer (mm:ss) + "End" button
- Exercise listesi: her set için kart — toggle ile done/undone
  - Done: `#0D2818` bg, `#22C55E` border, checkmark circle
  - Undone: SurfaceRaised bg
- "Add Exercise" dashed border button

---

### 3. Calculations
**Amaç:** 1RM, RSI, FFMI hesaplama araçları.

**Tab switcher:** 3 sekme — 1RM | RSI | FFMI
```
tab container:
  background: #1D1828
  border: 1px solid #342D46
  border-radius: 18px
  padding: 4px

active tab: background #8B5CF6, color #F7F7FB
inactive tab: color #8A889B
```

**1RM:** Weight (kg) + Reps input → Calculate → Epley result (42px / AccentGlow) + 90%/80%/70% MetricTile'ları

**RSI:** Jump Height (cm) + GCT (ms) input → Calculate → RSI score + açıklama metni

**FFMI:** Profil verisi gerektiriyor — empty state CTA

---

### 4. Profile
**Amaç:** Sporcu profili, vücut metrikleri, atletik performans, hareket hedefleri.

**Hero bölümü (Accent gradient bg):**
- Avatar (56px daire, AccentSoft bg, AccentGlow initial)
- Ad + Pozisyon/Spor + Plan badge
- 3 MetricTile: Weight / Body Fat / FFMI

**Section tabs:** Overview | Performance | Goals (pill-style, aynı bottom nav mantığı)

**Overview:** Sport Profile card + Body Metrics card (ListRow pattern: icon + title + subtitle + sağ değer)

**Performance:** PR listesi — her kayıt için SurfaceRaised kart: hareket adı + tarih + büyük değer (AccentGlow)

**Goals:** Her hedef için card: progress bar (`#8B5CF6` fill, SurfaceStrong track) + current/target labels + yüzde badge

---

## İkonografi

**Stil:** 2px stroke, round linecap + linejoin, 24×24px viewBox. Fill yok (logo hariç).

**Renkler:**
- Aktif nav: `#A78BFA`
- Pasif nav: `#8A889B`
- Destructive: `#EF4444`
- Default: metin hiyerarşisine göre

**Icon listesi:**
- `nav_home` — ev ikonu
- `nav_dumbbell` — dumbbell (barbell)
- `nav_notebook` — not defteri
- `nav_plates` — plaka / hedef
- `nav_profile` — kullanıcı silüeti
- `calendar_icon` — takvim
- `icon_edit` — kalem
- `icon_delete` — çöp kutusu
- `icon_send` — gönd ok (FreakAI)

CDN alternatif: [Lucide Icons](https://unpkg.com/lucide@latest) — aynı 2px stroke stili.

---

## App Logo / Brand Mark

İki katmanlı SVG — `logo-bg.svg` (koyu mor gradient + glow) üzerine `logo-fg.svg` (atlet figürü + şimşek boltu). Her ikisi de aynı 512×512 viewBox. MAUI uygulamasında `AppIcon` olarak `freaklete.svg` (bg) + `freakletefg.svg` (fg) şeklinde kullanılmaktadır.

---

## Referans Dosyalar

| Dosya | İçerik |
|---|---|
| `colors_and_type.css` | Tüm CSS token'ları + semantic class'lar |
| `preview/colors-brand.html` | Accent renk paleti |
| `preview/colors-surfaces.html` | Surface renk skalası |
| `preview/colors-text.html` | Metin renkleri |
| `preview/colors-semantic.html` | Semantic durum renkleri |
| `preview/type-scale.html` | Tipografi ölçeği |
| `preview/spacing-tokens.html` | Spacing token'ları |
| `preview/radius-elevation.html` | Radius + elevation sistemi |
| `preview/buttons.html` | Button varyantları |
| `preview/cards.html` | Kart varyantları |
| `preview/inputs.html` | Input durumları |
| `preview/nav-bar.html` | Bottom navigation |
| `preview/icons.html` | İkon seti |
| `ui_kits/mobile_app/index.html` | **Click-through prototype (ana referans)** |

---

## Kaynak Kod

Orijinal repo: https://github.com/MerttBodur/FreakLete (`main` branch)
- `DESIGN.md` — görsel sistem spesifikasyonu
- `Resources/Styles/Colors.xaml` — MAUI renk token'ları
- `Resources/Styles/Styles.xaml` — MAUI komponent stilleri
- `Xaml/*.xaml` — ekran implementasyonları

---

## Claude Code'a Nasıl Kullanacaksın

1. Bu zip'i bir klasöre aç
2. Claude Code'u o klasörde aç (veya projenin root'unda aç ve klasörü göster)
3. Şunu yaz:

```
Bu klasörde FreakLete uygulamasının design system handoff'u var.
ui_kits/mobile_app/index.html dosyasındaki prototype'ı referans alarak
[hedef: MAUI XAML / React Native / vs.] ortamında implement et.
README.md'deki token ve komponent spesifikasyonlarını kullan.
```
