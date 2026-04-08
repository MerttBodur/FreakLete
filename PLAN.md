# FFMI, Donate ve Subscription Delivery Plan

## Ozet
Bu is tek bir UI eklemesi degil; MAUI app, ASP.NET Core API, veri modeli, Google Play billing, entitlement yonetimi ve FreakAI quota enforcement birlikte ele alinmali.

Bu fazin hedefleri:
- Calculations sayfasina `FFMI` eklemek
- Android-first `Donate` akislarini acmak
- Android-first `Subscription` akislarini acmak
- FreakAI free/premium limitlerini backend source-of-truth olacak sekilde enforce etmek

Kararlar:
- Billing scope: Android first
- Donate: yalnizca sabit tutarlar (`$1`, `$5`, `$10`, `$20`)
- Subscription product: `freaklete_premium`
- Base planlar: `monthly = $3`, `annual = $30`
- Free limits:
  - ayda `1` program generate
  - ayda `1` program analyze
  - `14` gunde `1` nutrition guidance
  - gunde `3` general chat
- Premium hidden safety caps:
  - generate: `8/gun`, `60/ay`
  - analyze: `12/gun`, `120/ay`
  - nutrition: `8/gun`, `60/ay`
  - general chat: `150/gun`

## Faz 1 - FFMI
Amac: bagimsiz ve dusuk riskli kismi once ship etmek.

Yapilacaklar:
- `CalculationService` icine FFMI hesabi ekle
- `POST /api/Calculations/ffmi` endpoint'i ekle
- Calculations ekranina `1RM / RSI / FFMI` tab yapisi getir
- FFMI hesabini `WeightKg + HeightCm + BodyFatPercentage` ile calistir
- Profile verisi eksikse hesap yerine aciklayici empty-state + Profile CTA goster
- Sonucta en az su degerleri goster:
  - normalized FFMI
  - lean body mass
  - raw FFMI

Testler:
- core unit testleri
- API integration testleri
- Calculations UI logic testleri

## Faz 2 - Monetization Backend ve FreakAI Quota Katmani
Amac: store satin alimi gelmeden once entitlement ve limit omurgasini guvenli hale getirmek.

Yapilacaklar:
- `BillingPurchase` tablosu ekle
- `AiUsageRecord` tablosu ekle
- entitlement hesaplayan servis ekle
- FreakAI request'lerine `intent` alani ekle
- intent tipleri:
  - `program_generate`
  - `program_view`
  - `program_analyze`
  - `nutrition_guidance`
  - `general_chat`
- free/premium limit kontrolunu Gemini cagrisindan once yap
- quota asimlarinda localized `429` response don
- usage snapshot donen bir `billing status` response modeli ekle

Kurallar:
- `program_view` premium hakki sayilmaz, `general_chat` sayilir
- `create_program`, `adjust_program`, `swap_exercise` generate kotasina yazilir
- gunluk pencereler UTC day
- aylik pencereler UTC calendar month
- nutrition penceresi rolling 14 days

Testler:
- intent classification
- free quota exhaustion
- premium hidden cap exhaustion
- downgrade/expiry sonrasi free fallback

## Faz 3 - Android Google Play Billing
Amac: gercek satin alma ve restore akislarini Android'de acmak.

Yapilacaklar:
- MAUI tarafinda `IBillingService` ekle
- Android implementasyonunda Google Play Billing kullan
- urunler:
  - donate: `donate_1`, `donate_5`, `donate_10`, `donate_20`
  - subscription product: `freaklete_premium`
  - base plans: `monthly`, `annual`
- Settings sayfasindaki `Donate` ve `Subscribe` kartlarini gercek akisa cevir
- restore purchases ekle
- manage subscription deep link ekle
- satin alma sonrasi API'ye sync atip server-side verify et
- subscription icin acknowledge, donate icin consume uygula

Backend:
- `POST /api/billing/googleplay/sync`
- `GET /api/billing/status`

Config:
- `GooglePlay:PackageName`
- `GooglePlay:ServiceAccountJsonBase64`

Not:
- iOS/Mac/Windows canli billing bu fazin disinda kalir
- custom donation amount bu fazin disinda kalir

## Faz 4 - FreakAI Premium UX, Dokumantasyon ve Smoke
Amac: premium faydalarini gorunur, dogru ve test edilmis hale getirmek.

Yapilacaklar:
- FreakAI ekranina compact usage/paywall state kardi ekle
- quick action butonlari intent gonderir hale gelsin
- limit bitince upgrade CTA goster
- Settings ekraninda aktif plan, renewal/end date ve restore state goster
- `README.md` ve `PRD.md` shipped reality'ye gore guncellensin
- Play internal testing checklist'i guncellensin

## Public Interfaces
- `FreakAiUsageIntent`
- `BillingStatusResponse`
- `GooglePlayPurchaseSyncRequest`
- `FfmiRequest`
- `FfmiResponse`

## Test Plan
- `FreakLete.Core.Tests`
- `FreakLete.Api.Tests`
- Android build
- Play internal testing:
  - monthly subscribe
  - annual subscribe
  - donate SKU purchase
  - restore
  - subscription management deep link
  - free limit exhaustion
  - premium unlock
  - FFMI missing-data ve happy-path

## Phase Analizi
Evet, bu is fazlara bolunmeli.

Gerekceler:
- `FFMI` teknik olarak bagimsiz ve store bagimliligi olmadan tamamlanabiliyor.
- Billing tarafi dis bagimli; Play Console urunleri, servis hesabi ve internal testing yuzunden tum isi tek blokta tutmak riskli.
- Subscription satmadan once backend entitlement ve quota enforcement'in saglam olmasi gerekiyor.
- Review ve rollback maliyeti daha dusuk olur; `FFMI` bozulursa billing'i beklemez, billing bozulursa calculations tarafini bloke etmez.

Onerilen teslim sekli:
- `PR 1`: Faz 1
- `PR 2`: Faz 2
- `PR 3`: Faz 3
- `PR 4`: Faz 4

Tek branch ile gidilecekse bile execution sirasi yine bu olmali.

## Varsayimlar
- Android billing ilk surum icin yeterli.
- Donate custom amount gercek odeme olarak bu fazda yapilmayacak.
- Premium "limitsiz" pazarlansa da backend hidden caps enforce edecek.
- RTDN / PubSub bu ilk surumde yok; sync ve status fetch ile entitlement guncellenecek.

## Claude Prompt
Asagidaki prompt, fazlara bolunmesi gerektigi icin yalnizca bir sonraki faz olan Faz 1 icindir:

```text
Source of truth:
- CODEX.md
- README.md
- PRD.md
- mevcut MAUI app + ASP.NET Core API yapisi

Current problem:
Calculations roadmap'inde gorunen FFMI isi henuz shipped degil. Mevcut uygulamada 1RM ve RSI var, fakat FFMI yok. Profile tarafinda FFMI icin gereken WeightKg, HeightCm ve BodyFatPercentage verileri mevcut. Ama Calculations UI, core math, API endpoint ve test coverage henuz tamamlanmis degil.

Scope:
- FFMI hesap mantigini ekle
- API parity icin FFMI endpoint'i ekle
- Calculations sayfasina FFMI tab/tool ekle
- Eksik profile verisinde dogru empty state ve yonlendirme goster
- Ilgili testleri ekle/guncelle
- README.md ve PRD.md icindeki shipped/roadmap gercekligini FFMI acisindan hizala

Out of scope:
- Donate
- Subscription
- Google Play Billing
- FreakAI quota/entitlement enforcement
- benchmark tier sistemi
- tooltip sistemi
- 1RM/bodyweight ratio veya diger roadmap maddeleri

Required implementation tasks:
1. Mevcut CalculationService yapisini incele ve `CalculateFfmi` benzeri bir core method ekle.
2. FFMI hesabinda en az su ciktilari uret:
   - normalized FFMI
   - raw FFMI
   - lean body mass
3. Input validation ekle:
   - weight > 0
   - height > 0
   - body fat 0 ile 100 arasinda
4. API tarafinda `POST /api/Calculations/ffmi` endpoint'i ekle.
5. Calculations UI'da `1RM / RSI / FFMI` arasinda gecis yapilabilsin.
6. FFMI ekrani profile verisini kullansin; `WeightKg`, `HeightCm` veya `BodyFatPercentage` eksikse hesap alani yerine aciklayici empty-state goster ve kullaniciyi profile yonlendiren CTA sun.
7. Sonuc kartinda normalized FFMI'yi ana cikti olarak goster; raw FFMI ve lean body mass ikincil bilgi olarak goster.
8. AppLanguage icindeki gerekli tum kullanici metinlerini ekle.
9. README.md ve PRD.md icinde FFMI artik shipped ise bunu dogru sekilde guncelle; shipped olmayan benchmark/tier kisimlarini shipped gibi yazma.

Tests/verification required:
- FreakLete.Core.Tests icine FFMI math ve invalid input testleri ekle
- FreakLete.Api.Tests icine auth, valid request, invalid request coverage ekle
- gerekiyorsa calculations page logic/UI-facing testlerini guncelle
- ilgili build/test komutlarini calistir ve exact sonucu raporla
- warning varsa gizleme; warning count ve error count'i durustce yaz

Expected output:
- kucuk ve kontrollu bir diff
- yeni FFMI math + API + UI calisir durumda
- testler guncellenmis
- README.md / PRD.md gercek duruma hizalanmis
- hangi dosyalarin degistigi, hangi testlerin kostugu ve exact sonuclarin ne oldugu net raporlanmis

Important constraints:
- gereksiz architectural churn yapma
- mevcut 1RM/RSI yapisini bozmadan ayni pattern'i takip et
- partial progress'i done gibi raporlama
- yalnizca bu fazi bitir; Donate/Subscription tarafina gecme
```
