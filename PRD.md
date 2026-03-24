# FreakLete PRD

## 1. Urun Ozeti
FreakLete, field athletes ve gym odakli sporcular icin gelistirilmis bir mobil performans takip uygulamasidir. Uygulama; workout loglama, athletic performance tracking, movement goals, exercise discovery ve performance calculations gibi ozellikleri tek bir akista toplar.

Bu dokumanin amaci:
- tamamlanan MVP'yi netlestirmek
- mevcut urun mimarisini ozetlemek
- bundan sonraki roadmap'i mantikli fazlara ayirmak

## 1.1 Guncel Durum Ozeti
22 Mart 2026 itibariyla FreakLete artik sadece local-first bir MVP degildir.

Bugunku durum:
- Mobile app production backend'e baglidir
- Backend Railway uzerinde canlidir
- Production PostgreSQL persistence aktiftir
- JWT auth ve SecureStorage tabanli session mantigi calismaktadir
- Profile, workouts, PRs, athletic performance ve movement goals backend tarafinda tutulmaktadir
- Structured sport / position profile selection ve coach profile alanlari eklenmistir
- Training program persistence katmani eklenmistir
- Initial FreakAI MVP uygulama icinde erisilebilir durumdadir
- Production backend smoke test'i gecmistir
- Android signed AAB uretilmistir

Bu nedenle urun durumu:
- Phase 1 tamamlandi
- aktif odak Android release / Play Store cikisidir

## 2. Problem
Mevcut fitness uygulamalarinin cogu ya sadece klasik bodybuilding log mantigina odaklanir ya da atletik performans tarafini yuzeysel gecer. Field athletes icin gerekli olan seyler genelde ayni yerde bulunmaz:
- strength log
- sprint / jump / plyometric tracking
- movement-specific metrics
- weak point analizi
- sport ve position'a gore anlamli oneriler

FreakLete bu boslugu kapatmayi hedefler.

## 3. Hedef Kullanici
- Field athletes
- Strength and conditioning ile ilgilenen sporcular
- Gym + athletic performance'i birlikte takip etmek isteyen kullanicilar
- Kendi antrenmanlarini daha sistemli hale getirmek isteyen bireyler

## 4. Product Vision
FreakLete'in uzun vadeli hedefi:
- kullanicinin antrenman verilerini merkezi olarak saklayan
- veriyi metin yiginlari yerine dashboard, chart, metric tile ve progress card'larla taranabilir hale getiren
- zaman icinde trendleri grafiklerle gorunur hale getiren
- canli workout akisinda set bazli veri toplayan
- zayif noktalarini tespit eden
- hedefine, sporuna ve pozisyonuna gore akilli oneriler yapan
- zamanla FreakAI tarafindan guclendirilen bir performance companion'a donusen
bir urun olmaktir.

## 5. Tamamlanan Urun Temeli
Asagidaki basliklar artik roadmap maddesi degil, mevcut urunun parcasidir.

### 5.1 Core Features
- Register / Login
- JWT tabanli auth
- Workout olusturma
- Exercise browser
- Calendar tabanli workout history
- Calculations sayfasi
- 1RM calculation
- RSI calculation
- Athletic performance tracking
- Movement goals
- Profile / body metrics
- Structured sport / position selection
- Coach profile fields
- Initial FreakAI chat and coaching flow
- Training program persistence and retrieval
- CRUD for key records

### 5.2 Product and UX
- Text-based eski form hissinden uzaklasan ilk visual refresh tamamlandi
- Kategori tabanli exercise browser eklendi
- Strength ve athletic movement'lar ayni katalog yapisinda toplandi
- Modern custom dialogs eklendi
- Profile tarafinda native picker / date dialog'lari yerine custom selector akisi kullanilmaya baslandi

Not:
- Bu iyilestirmeler ilk gecis adimidir
- Asil hedef halen dashboard-first, graph-first, scan-based UI V2 tasarimidir

### 5.3 Data and Quality
- Production PostgreSQL persistence
- Railway production deployment
- Exercise catalog JSON + backend persistence
- SecureStorage tabanli token saklama
- Automated tests
- `FreakLete.Core.Tests` ile core logic / calculations / parsing / rules coverage
- `FreakLete.Api.Tests` ile auth/profile API regression coverage
- Auth/profile roundtrip persistence, partial update, invalid input rejection ve `DateOfBirth` date-only behavior dogrulanmis durumda
- API regression coverage backend persistence guveni sagliyor, ancak profile save sonrasi mobile UI state'in hemen dogru yenilendigini tek basina garanti etmiyor
- Full system regression coverage halen tamamlanmis degil; workouts, PRs, athletic performance, movement goals, training programs, FreakAI controller ve mobile profile state consistency coverage halen eksik

### 5.4 Production Validation
- Railway production backend canli
- Production PostgreSQL migration'lari uygulanmis
- Backend smoke test sonucu: 13/13 PASSED
- Register / login / profile / workouts / PR / athletic performance / movement goals / delete account production'da dogrulandi
- Signed Android AAB uretilmis durumda

## 6. Mevcut Mimari

### 6.1 Mobile App
- .NET MAUI
- C#
- XAML
- SecureStorage + backend API

### 6.2 Backend
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT authentication
- Dockerized Railway deployment

### 6.3 Data Model
- User
- Workout
- ExerciseEntry
- PrEntry
- AthleticPerformanceEntry
- MovementGoal
- ExerciseDefinition
- TrainingProgram
- ProgramWeek
- ProgramSession
- ProgramExercise

### 6.4 Current State
Bugunku uygulama cloud-backed mobile product foundation'ina ulasmistir.

Bu ne demek:
- kullanici hesabi backend tarafinda tutulur
- reinstall sonrasi login ve veri geri gelebilir
- production source of truth PostgreSQL'dir
- app debug modda local backend kullanabilir, release modda production backend kullanir
- structured athlete profile ve program verisi cloud-backed hale gelmistir
- initial FreakAI katmani backend uzerinden cagrilabilir durumdadir

## 7. Backend Durumu
Backend tarafi artik "direction" seviyesinde degil, aktif production katmanidir.

Mevcut backend durumu:
- Railway uzerinde deploy edilmis production API
- Railway PostgreSQL ile calisan production database
- Dockerfile tabanli deploy
- Health check / migration mantigi dogrulanmis production ortam

Bu katmanin mevcut rolu:
- gercek account system
- cloud persistence
- reinstall sonrasi account restore
- release build'lerde production source of truth olmak
- gelecekte recommendation ve AI sistemleri icin merkezi veri kaynagi saglamak

## 8. Product Principles
Roadmap boyunca su prensipler korunmali:
- once veri yapisi, sonra intelligence
- once kaliteli tracking, sonra akilli yorumlama
- once deterministic recommendation logic, sonra AI
- once structured metadata, sonra open-ended AI output
- mobile app offline hissini kaybetmeden cloud'a gecmeli
- UI; paragraf ve form yogun bir yardimci uygulama gibi degil, scan-based athletic dashboard gibi hissettirmelidir
- chart, metric tile, badge, weekly strip ve action card gibi gorsel sinyaller ana bilgi tasiyici olmali
- kullanicidan toplanan her detay kullaniciya gosterilmek zorunda degil; bazi skorlar ic kalite / coaching sinyali olarak arka planda kalabilir
- FreakAI, veri ve recommendation katmanlarinin ustune kurulan urunun ana intelligence omurgasi olmalidir

## 9. Roadmap

## Phase 1 - Real Account System and Cloud Sync
Durum: TAMAMLANDI

### Hedef
Kullanicinin hesabi ve verileri artik sadece cihaz icinde degil, backend tarafinda da tutulacak.

### Kapsam
- Backend auth'i tamamlama
- Cloud user account system
- PostgreSQL persistence
- JWT / token tabanli session management
- SecureStorage tabanli token saklama
- Login sonrasi cloud -> local cache sync
- Logout flow
- Reinstall sonrasi account restore

### Basari Kriterleri
- Kullanici uygulamayi silip yeniden yuklediginde tekrar login olabilir
- Workout, goals, PR ve athletic performance datasi geri gelir
- Session sadece local Preferences'e degil, gercek auth mantigina dayanir

### Sonuc
Bu faz tamamlanmistir.

Tamamlananlar:
- production backend deployment
- PostgreSQL persistence
- JWT auth
- SecureStorage token handling
- production smoke test
- mobile app'in production backend'e baglanmasi

## Phase 2 - Structured Athlete Profile
Recommendation sisteminden once kullanici profilini daha anlamli ve secilebilir hale getirmek gerekir.

Not:
- sport / position selection ve coach profile alanlarinin ilk versiyonu eklenmistir
- bu faz artik "tamamen sifirdan baslama" degil, kalan structured profile genislemesidir

### Hedef
Handwritten profile alanlarini structured browser / picker sistemine cevirmek.

### Kapsam
- Sport browser
- Position browser
- Goal metric browser
- Target quality selection
- Target body area selection
- Structured athlete profile model

### Ornek Alanlar
- Sport: Football, Basketball, Volleyball, Track and Field vb.
- Position: QB, RB, WR, OL, DL vb.
- Target quality: explosiveness, max strength, reactive ability, acceleration vb.
- Target metric: squat, vertical jump, broad jump, 40y dash vb.

## Phase 2A - Live Workout and Tracking Analytics
Bu faz, urunun sadece "log kaydi" degil, canli antrenman akisini ve zaman icindeki degisimi anlayan bir tracking katmanina donusmesini hedefler.

### Hedef
- Workout'lari canli sekilde baslatip yonetebilmek
- Set bazli daha iyi sinyal toplayabilmek
- Kullaniciya trendleri grafiklerle gostermek
- Ileride recommendation ve FreakAI tarafinda kullanilacak bir internal fatigue sinyali uretmek

### Kapsam
- Live workout mode
- Workout start / active session flow
- Global workout timer
- Set timer
- Rest timer
- Per-set reps girisi
- Per-set RPE girisi
- Opsiyonel concentric phase time girisi
- Set tamamlandiginda otomatik rest baslatma
- Session sonu total fatigue hesaplamasi
- Fatigue'yi kullaniciya ham skor olarak gostermeme
- Internal fatigue bucket:
  - low
  - intermediate
  - high

### Analytics Kapsami
- PR analysis line chart
- Bodyweight analysis line chart
- Workout count / consistency line chart
- Ileride genisletilebilir trend kartlari

### Urun Davranisi
- Kullanici Start Workout'a tiklar
- Workout session aktif hale gelir
- Kullanici egzersiz secer (ornegin Back Squat)
- Set suresi baslar
- Kullanici seti bitirince ilgili set tamamlanir
- Sonrasinda reps, RPE ve opsiyonel concentric phase time girilir
- Set bitiminden hemen sonra dinlenme suresi baslar
- Session boyunca tum parametrelerden total fatigue hesaplanir
- Bu skor ham deger olarak gosterilmez; sistem icinde yorumlanir

### Neden Onemli
- Daha zengin workout context'i verir
- Recommendation engine icin daha iyi veri tabani olusturur
- FreakAI'in recovery, load, yorgunluk ve progression kararlarini destekler
- Kullanicinin ilerlemeyi sadece liste degil trend olarak gormesini saglar

## Phase 2B - Dashboard-First UI V2
Bu faz, mevcut visual refresh'i daha ileri tasiyip urunu text-heavy utility app gorunumunden scan-based performance dashboard deneyimine donusturur.

### Hedef
- Ana ekranlari daha gorsel, daha taranabilir ve daha az yorucu hale getirmek
- Grafik, metric tile, progress card ve action surface'leri urunun ana dili yapmak
- Kullaniciya paragraf okumadan "bugun ne var", "nasil gidiyor", "sirada ne var" sorularinin cevabini vermek

### UI V2 Phase 1 - Shared Design System
- HeroPanel
- MetricTile
- TrendCard
- ActionTile
- WeeklyStrip
- ProgressRingCard
- WorkoutExerciseCard
- BadgeChip
- EmptyStateCard
- SelectionGridCard

### UI V2 Phase 2 - Analytics and Chart Infrastructure
- GraphicsView tabanli custom line chart
- mini sparkline
- progress ring
- mini bar trend
- chart empty / single-point / multi-point state handling
- bodyweight history icin historical measurement model

### UI V2 Phase 3 - Dashboard Surfaces
- Home redesign
- Workout landing redesign
- Calendar redesign
- daha kisa hero copy
- daha guclu action tiles
- weekly strip ve summary cards

### UI V2 Phase 4 - Deep Workflow Surfaces
- Calculations redesign
- Profile'i Overview / Coach / Performance / Goals segmentlerine ayirma
- NewWorkout'i step-based flow'a donusturme
- FreakAI'i coach dashboard-first yuzeye tasima

### UI V2 Phase 5 - Visual Selection and Onboarding Surfaces
- Equipment selection grid
- goal/focus/equipment gibi secimlerde visual card kullanimi
- browser-backed selector akisini daha gorsel ve hizli hale getirme

### Basari Kriterleri
- Ana ekranlarda kullanici uzun aciklama paragraflari okumadan yonunu bulabilir
- Home gercek bir dashboard hissi verir
- Profile uzun tek parca form gibi hissettirmez
- Chart'lar dekoratif degil, ana bilgi tasiyici olur
- Workout ve program akislarinda image-backed / status-aware kartlar kullanilir
- Uygulama "tool collection" degil, "training system" gibi hissedilir

## Phase 2C - Regression Safety Expansion
Bu fazin amaci, uygulamanin yalnizca build alan degil, regression'a dayanikli bir urun haline gelmesini saglamaktir.

### Hedef
- Auth/profile tarafinda baslayan API regression coverage'i urunun diger kritik alanlarina yaymak
- Mobile profile state tutarsizliklarini yakalayacak testable logic ve regression coverage eklemek
- FreakAI ve tracked-data endpoint'lerinde korkmadan iterasyon yapilabilecek bir safety net kurmak

### Kapsam
- Workouts API regression tests
- PR entries API regression tests
- Athletic performance API regression tests
- Movement goals API regression tests
- Training program endpoint regression tests
- FreakAI controller / error-path tests
- Mobile profile save/state consistency tests

### Neden Onemli
- API roundtrip coverage tek basina yeterli degil; kullanicinin "saved" gorup sonra eski state'e donmesi gibi mobile-state bug'lari ayrica ele alinmali
- Bu faz correctness ve risk reduction fazidir
- UI V2 ve yeni feature development'i daha guvenli hale getirir

## Phase 3 - Exercise Metadata Engine
Bu faz roadmap'in en kritik katmanlarindan biridir.

### Hedef
Tum egzersizleri akilli recommendation sistemine uygun metadata ile etiketlemek.

### Kapsam
- Exercise catalog expansion
- Medball movements ekleme
- Existing catalog growth
- Tag system
- Attribute scoring system

### Metadata Ornekleri
- primary muscles
- secondary muscles
- movement pattern
- athletic quality
- nervous system load
- sport transfer relevance
- equipment
- force / mechanic / level

### Scoring Ornekleri
Her exercise icin su eksenlerde puanlama:
- strength
- hypertrophy
- explosiveness
- speed
- reactivity
- deceleration
- stability

Bu puanlama recommendation engine'in temel girdilerinden biri olacak.

## Phase 4 - Rule-Based Recommendation Engine
AI'dan once test edilebilir ve deterministic bir recommendation engine kurulmali.

### Hedef
Kullanicinin mevcut verisine gore mantiksal ve aciklanabilir oneriler sunmak.

### Kapsam
- Workout history analizi
- Volume / frequency pattern analizi
- Weak point detection
- Goal-based movement suggestions
- Target body area + target quality uyum skorlama
- Sport / position bazli rule-based recommendation

### Ornek Use Case'ler
- Kullanici sprint odakli ama posterior chain volumu dusuk
- Kullanici broad jump gelistirmek istiyor ama relevant power movement exposure zayif
- Kullanici RB olarak acceleration odakli bir profile sahip

## Phase 5 - FreakAI Intelligence Layer
FreakAI bu urunde tek basina bir "chat feature" degil, uzun vadede urunun intelligence backbone'u olacak katmandir.

Not:
- initial FreakAI MVP ve training-program-aware coach flow baslatilmistir
- bu faz, mevcut MVP'nin ustune daha derin reasoning, recommendation ve orchestration katmanini ifade eder

### Hedef
Kullaniciya daha akilli, daha aciklayici ve daha context-aware oneriler sunan; recommendation, aciklama ve guidance sistemlerini merkezi olarak tasiyan bir FreakAI katmani kurmak.

### Kapsam
- FreakAI assisted recommendation summaries
- Weak point explanation
- Sport + position specific guidance
- Goal-oriented movement suggestions
- Program idea generation
- User context aware coaching style explanations

### Onemli Ilke
FreakAI output:
- Phase 2'deki structured profile
- Phase 3'teki exercise metadata
- Phase 4'teki deterministic scoring
ustune kurulacak.

### Product Rolu
FreakAI zamanla su alanlarin ana orkestrasyon katmani olacaktir:
- recommendation explanation
- athlete-specific insight generation
- goal-driven movement reasoning
- future program builder intelligence
- kullanicinin tum performans verisini anlamlandiran ust katman

## Phase 6 - Program Builder
Bu fazda urun tek tek movement onerisi veren bir uygulamadan daha ileri gider.

### Hedef
Kullaniciya mini block, microcycle veya direction-level program onerileri sunmak.

### Kapsam
- Weekly structure suggestions
- Goal-based exercise grouping
- Session templates
- Progression logic
- Load / frequency recommendations

## 10. Yapilmis Islerin Roadmapten Cikarilmasi
Asagidaki basliklar artik "gelecek is" degil:
- exercise browser
- 1RM calculation
- RSI calculation
- movement goals
- athletic performance tracking
- local catalog seeding
- automated tests
- backend auth
- production PostgreSQL persistence
- Railway deployment
- production smoke testing

## 11. Product Risks
- Cloud migration sirasinda local SQLite ile backend verisinin cakisabilmesi
- Structured metadata kurulmadan AI feature'a erken gecilmesi
- Sport / position recommendation logic'inin veri olmadan varsayimla yazilmasi
- Catalog expansion sirasinda data quality problemleri

## 12. Out of Scope for Current Next Phase
Su an bir sonraki faz icin odak disi sayilabilecek basliklar:
- leaderboard
- social feed
- friends system
- monetization
- subscription flow
- coaching marketplace
- AI recommendation implementation
- iOS release execution
- advanced infrastructure migration

## 13. Store Readiness - Current Focus
Aktif odak product roadmap fazlarindan bagimsiz olarak Android release execution'dir.

### Hedef
FreakLete'i Google Play'e cikabilecek seviyeye getirmek.

### Tamamlananlar
- production backend canli
- release build production backend'e bagli
- Android signed AAB uretilmis

### Kalanlar
- dogru signed AAB ile Play Console upload
- internal testing / closed testing acilisi
- privacy policy URL
- Play Store listing assets
- final Android release smoke test on real device

### Basari Kriteri
- signed AAB Play Console'a yuklenmis olacak
- test track acilmis olacak
- production backend ile release build gercek cihazda dogrulanmis olacak

## 14. Next Recommended Execution Order
Urunu saglam buyutmek icin onerilen sira:

1. Android release / Play Store cikisi
2. iOS release hazirligi
3. Regression safety expansion
4. Dashboard-first UI V2
5. Structured athlete profile
6. Live workout and tracking analytics
7. Exercise metadata engine
8. Rule-based recommendation engine
9. FreakAI intelligence layer
10. Program builder

## 15. Net Product Direction
FreakLete'in bundan sonraki asil hedefi sadece "log tutan bir app" olmak degil;
athlete-specific, dashboard-first, structured-data-driven, trend-aware, recommendation-capable ve uzun vadede FreakAI tarafindan guclendirilen bir performance platformuna donusmektir.
