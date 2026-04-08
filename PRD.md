# FreakLete PRD

## 1. Urun Ozeti
FreakLete, field athletes ve gym odakli sporcular icin gelistirilmis bir mobil performans takip uygulamasidir. Uygulama; workout loglama, athletic performance tracking, movement goals, exercise discovery, training programs ve performance calculations gibi ozellikleri tek bir akista toplar.

Bu dokumanin amaci:
- tamamlanan urun temelini netlestirmek
- mevcut urun mimarisini ozetlemek
- shipped state ile roadmap state'i birbirinden ayirmak
- bundan sonraki roadmap'i mantikli fazlara bolmek

## 1.1 Guncel Durum Ozeti
7 Nisan 2026 itibariyla FreakLete artik sadece local-first bir MVP degildir.

Bugunku durum:
- mobile app production backend'e baglidir
- backend Railway uzerinde canlidir
- production PostgreSQL persistence aktiftir
- JWT auth ve SecureStorage tabanli session mantigi calismaktadir
- profile, workouts, PRs, athletic performance ve movement goals backend tarafinda tutulmaktadir
- app-wide `EN/TR` localization aktiftir
- runtime language refresh mevcuttur; sayfalar aktif dil degisimine tepki verebilir
- settings sayfasinda language switch mevcuttur
- secure change-password flow mevcuttur
- structured sport / position profile selection ve coach profile alanlari eklenmistir
- training program persistence katmani eklenmistir
- starter template browser + clone flow mevcuttur
- live workout v1 start / active session flow mevcuttur
- initial FreakAI MVP uygulama icinde erisilebilir durumdadir ve kullanicinin dilinde cevap verebilir
- production backend smoke test'i gecmistir
- Android signed AAB uretilmistir

Bu nedenle urun durumu:
- cloud-backed mobile product foundation tamamlanmistir
- artik shipped reality ile roadmap ayrimi daha net tutulmalidir
- aktif odak Android release / Play Store cikisi ve sonraki roadmap fazlaridir

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
- benchmark ve percentile mantigi ile performans seviyesini daha anlamli gosteren
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
- `1RM` calculation
- `RSI` calculation
- `FFMI` calculation (normalized, raw, lean body mass)
- Athletic performance tracking
- Movement goals
- Profile / body metrics
- Structured sport / position selection
- Coach profile fields
- Training program persistence and retrieval
- Starter template browser + clone flow
- Live workout v1 start / active session flow
- Settings page
- App-wide `EN/TR` localization
- Runtime language refresh
- Secure change-password flow
- Initial FreakAI chat and coaching flow
- FreakAI language mirroring
- CRUD for key records

### 5.2 Product and UX
- Text-based eski form hissinden uzaklasan ilk visual refresh tamamlandi
- Kategori tabanli exercise browser eklendi
- Strength ve athletic movement'lar ayni katalog yapisinda toplandi
- Modern custom dialogs eklendi
- Profile tarafinda native picker / date dialog'lari yerine custom selector akisi kullanilmaya baslandi
- Workout surface'lerinde starter template ve kullanici programlari ayni akis icinde gorulebilir hale geldi
- Canli workout baslatma ve aktif seans tutma davranisi shipped v1 olarak mevcuttur

Not:
- bu iyilestirmeler ilk gecis adimidir
- asil hedef halen dashboard-first, graph-first, scan-based UI V2 tasarimidir

### 5.3 Data and Quality
- Production PostgreSQL persistence
- Railway production deployment
- Exercise catalog JSON + backend persistence
- SecureStorage tabanli token saklama
- Automated tests
- `FreakLete.Core.Tests` ile core logic / calculations / parsing / rules coverage
- `FreakLete.Api.Tests` ile auth, profile, workouts, PRs, athletic performance, movement goals, exercise catalog, sport catalog, calculations, training programs ve FreakAI controller coverage
- Typed athlete/coach profile endpoint'leri, roundtrip persistence, invalid input rejection, cross-section isolation ve `DateOfBirth` date-only behavior dogrulanmis durumda
- API regression coverage backend persistence guveni sagliyor; real UI flows manual Android smoke testing ile dogrulanir
- Mevcut test stratejisi: `FreakLete.Api.Tests` ve `FreakLete.Core.Tests` (blocking), plus manual Android emulator smoke testing (real verification)

Bu dokuman guncellemesi icin mevcut verification note:
- `FreakLete.Core.Tests` bu oturumda calisti ve `158/158` gecti
- `FreakLete.Api.Tests` bu oturumda restore cikisindan sonra `exit code 1` ile sessiz kapandi
- bu nedenle `FreakLete.Api.Tests` bu oturum icin green olarak belgelenmedi

### 5.4 Production Validation
- Railway production backend canli
- Production PostgreSQL migration'lari uygulanmis
- Backend smoke test sonucu: `13/13 PASSED`
- Register / login / profile / workouts / PR / athletic performance / movement goals / delete account production'da dogrulandi
- Signed Android AAB uretilmis durumda

## 6. Mevcut Mimari

### 6.1 Mobile App
- .NET MAUI
- C#
- XAML
- SecureStorage + backend API
- AppLanguage tabanli client-side localization

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
- starter template akisi public browse + authenticated clone mantigi ile kurulmustur
- live workout v1 aktif seans mantigi repo icinde mevcuttur
- initial FreakAI katmani backend uzerinden cagrilabilir ve kullanicinin diliyle eslesecek sekilde yonlendirilebilir durumdadir

## 7. Backend Durumu
Backend tarafi artik "direction" seviyesinde degil, aktif production katmanidir.

Mevcut backend durumu:
- Railway uzerinde deploy edilmis production API
- Railway PostgreSQL ile calisan production database
- Dockerfile tabanli deploy
- Health check / migration mantigi dogrulanmis production ortam
- Training program starter template endpoint'leri ve seeding mantigi mevcuttur
- Auth tarafinda secure change-password endpoint'i mevcuttur
- FreakAI tarafinda language detection + response guard mantigi mevcuttur

Bu katmanin mevcut rolu:
- gercek account system
- cloud persistence
- reinstall sonrasi account restore
- release build'lerde production source of truth olmak
- gelecekte recommendation, benchmark ve AI sistemleri icin merkezi veri kaynagi saglamak

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
- benchmark dili "global kabul gormus tek oran" gibi yazilmamali; dogru ifade `public benchmark tables / competition-derived percentiles` olmalidir
- norm profili desteklenmiyorsa uygulama raw value + raw ratio gosterebilir, ama sahte tier uretmemelidir
- shipped olmayan roadmap basliklari mevcut ozellikmis gibi yazilmamalidir

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
- kullanici uygulamayi silip yeniden yuklediginde tekrar login olabilir
- workout, goals, PR ve athletic performance datasi geri gelir
- session sadece local Preferences'e degil, gercek auth mantigina dayanir

### Sonuc
Bu faz tamamlanmistir.

Tamamlananlar:
- production backend deployment
- PostgreSQL persistence
- JWT auth
- SecureStorage token handling
- production smoke test
- mobile app'in production backend'e baglanmasi

## Phase 2 - Profile Expansion for Benchmarking and Guidance
Shipped athlete/coach selector surfaces'in ustune, benchmark ve guidance icin eksik profile alanlarini tamamlamak gerekir.

Not:
- sport / position selection ve coach profile selector alanlari shipped durumdadir
- training days, session duration, goal ve dietary selector akislarinin ilk versiyonu da shipped durumdadir
- bu faz artik tamamen sifirdan baslama degil, kalan profile genislemesidir
- `FFMI` ve benchmark norm gating icin profile `HeightCm` ve `Sex` alanlari eklenmelidir

### Hedef
Ileride benchmark-driven calculations ve guidance icin gerekli temel profile alanlarini tamamlamak.

### Kapsam
- `HeightCm` alani
- `Sex` alani
- Goal metric browser
- Target quality selection
- Target body area selection

### Ornek Alanlar
- Target quality: explosiveness, max strength, reactive ability, acceleration vb.
- Target metric: squat, vertical jump, broad jump, 40y dash vb.

## Phase 2A - Tracking Analytics and Live Workout Depth
Bu faz, shipped live workout v1'in ustune daha zengin tracking ve analytics katmani kurmayi hedefler.

Not:
- live workout v1 start / active session flow zaten mevcuttur
- bu faz basic "start workout"u degil, daha derin instrumentation ve analytics kapsamini ifade eder

### Hedef
- set bazli workout sinyallerini zenginlestirmek
- canli workout akisina daha fazla rehberlik eklemek
- kullaniciya trendleri grafiklerle gostermek
- ileride recommendation ve FreakAI tarafinda kullanilacak bir internal fatigue sinyali uretmek

### Kapsam
- guided set progression
- auto-rest behavior ve daha net set transition mantigi
- richer per-set capture
- `RPE` girisi
- opsiyonel concentric phase time girisi
- session sonu total fatigue hesaplamasi
- fatigue'yi kullaniciya ham skor olarak gostermeme
- internal fatigue bucket: `low`, `intermediate`, `high`

### Analytics Kapsami
- PR analysis line chart
- bodyweight analysis line chart
- workout count / consistency line chart
- historical body measurement tracking to support bodyweight/body fat charts
- ileride genisletilebilir trend kartlari

### Neden Onemli
- daha zengin workout context'i verir
- recommendation engine icin daha iyi veri tabani olusturur
- FreakAI'in recovery, load, yorgunluk ve progression kararlarini destekler
- kullanicinin ilerlemeyi sadece liste degil trend olarak gormesini saglar

## Phase 2B - Dashboard-First UI V2
Bu faz, mevcut visual refresh'i daha ileri tasiyip urunu text-heavy utility app gorunumunden scan-based performance dashboard deneyimine donusturur.

### Hedef
- ana ekranlari daha gorsel, daha taranabilir ve daha az yorucu hale getirmek
- grafik, metric tile, progress card ve action surface'leri urunun ana dili yapmak
- kullaniciya paragraf okumadan "bugun ne var", "nasil gidiyor", "sirada ne var" sorularinin cevabini vermek

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
- Profile'i `Overview / Coach / Performance / Goals` segmentlerine ayirma
- NewWorkout'i step-based flow'a donusturme
- FreakAI'i coach dashboard-first yuzeye tasima

### UI V2 Phase 5 - Visual Selection and Onboarding Surfaces
- Equipment selection grid
- goal / focus / equipment gibi secimlerde visual card kullanimi
- browser-backed selector akisini daha gorsel ve hizli hale getirme

### Basari Kriterleri
- ana ekranlarda kullanici uzun aciklama paragraflari okumadan yonunu bulabilir
- Home gercek bir dashboard hissi verir
- Profile uzun tek parca form gibi hissettirmez
- chart'lar dekoratif degil, ana bilgi tasiyici olur
- workout ve program akislarinda image-backed / status-aware kartlar kullanilir
- uygulama "tool collection" degil, "training system" gibi hissedilir

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

## Phase 3A - Performance Standards and Exercise Guidance
Bu faz, bugunku `1RM + RSI` calculations surface'ini daha akilli ama halen deterministic bir performance standards katmanina genisletir.

Onemli not:
- `FFMI` hesaplama shipped durumdadir (normalized, raw, lean body mass); profilde `WeightKg + HeightCm + BodyFatPercentage` varsa hesaplanir, yoksa empty-state gosterilir
- lift seviyeleri, kompozit atlet lakaplari ve egzersiz demo medyasi bugunku shipped urunde mevcut degildir
- benchmark dili `public benchmark tables / competition-derived percentiles` olarak yazilmalidir; evrensel tek tablo varsayimi yapilmamalidir

### Column 1 - Calculations Intelligence
Bu sutun, mevcut calculation surface'ini genisletir ama yalnizca veri ve benchmark mantigi guvenli oldugunda kullaniciya tier gosterir.

Kapsam:
- `FFMI` hesaplama: SHIPPED (`HeightCm + WeightKg + BodyFatPercentage` birlikte varsa hesaplanir, yoksa empty-state + profile CTA gosterilir)
- profile once `HeightCm` ve `Sex` alanlarini ekleme
- powerlifting liftleri icin UI'da `1RM / bodyweight` oranini gosterme
- lift-tier mantigini sabit kaba oranlarla degil, data-driven percentile yaklasimiyla kurma
- powerlifting omurgasinda `IPF GL` ve `OpenPowerlifting` benzeri competition-derived yaklasimlari referans alma
- `RSI`, vertical jump ve standing broad jump benchmark'larini sex ve mumkun oldugunda sport / athlete population'a gore ele alma
- desteklenmeyen norm profillerinde `raw value + raw ratio` gosterme, ama tier uretmeme
- air-time -> vertical jump hesabini opsiyonel ek hesap yolu olarak planlama

### Column 2 - Profile Level System
Bu sutun, profile yuzeyinde seviye etiketleri ve aciklayici guidance dilini tanimlar.

Kapsam:
- v1 benchmarked movement scope: `Bench Press`, `Back Squat`, `Deadlift`, `Military/Overhead Press`, `Power Clean`, `Vertical Jump`, `Single/Standing Broad Jump`
- v1 seviye etiketleri: `Beginner`, `Intermediate`, `Advanced`, `Freak`
- kompozit lakap mantigini minimum kapsama kuraliyla calistirma
- athletic cluster guclu ise `Athlete`
- yalniz power cluster guclu ise `Powerlifter`
- ikisi de kismi ise `Hybrid`
- veri yetersizse lakap gostermeme
- `1RM`, `RSI`, `FFMI` ve benchmark terimleri icin kucuk `?` tooltip aciklamalari planlama

### Column 3 - Exercise Demo Media
Bu sutun, metin tabanli egzersiz rehberligini medya ile zenginlestirir ama kapsami kontrollu tutar.

Kapsam:
- v1'de yalnizca Tier-1 hareketler icin demo medya hedefleme
- ilk fazda tum `251` hareket icin medya kapsamayi hedeflememe
- demo medya metadata'sini catalog'da opsiyonel alan olarak tasarlama
- medya yoksa mevcut `Instructions / CommonMistakes / Progression / Regression` metnini fallback olarak kullanma
- bu fazi mevcut urunde `GIF support already shipped` gibi anlatmama

### Required Validation
Bu faz implementasyonunda su test basliklari zorunlu olacaktir:
- profile alan migration testi (`HeightCm`, `Sex`)
- `FFMI` hesap testi
- benchmark tier resolver testleri
- athlete title resolver testleri
- tooltip rendering testi
- media fallback davranis testi

### Source Direction
Bu fazin veri dili ve benchmark omurgasi icin kullanilacak referans yonu:
- `FFMI` height-normalized temel: VanItallie 1990 https://pubmed.ncbi.nlm.nih.gov/2239792/
- `FFMI` historical/application context: Kouri 1995 https://pubmed.ncbi.nlm.nih.gov/7496846/
- `FFMI`'nin athlete population icinde sex/sport farklari gosterebildigine dair dayanak: https://pubmed.ncbi.nlm.nih.gov/37815277/
- ek athlete-population FFMI referansi: https://pubmed.ncbi.nlm.nih.gov/30985525/
- `RSI` benchmark dili icin `preliminary, athlete-population-specific` yaklasim: https://www.mdpi.com/2075-4663/6/4/133
- powerlifting relatif kiyas omurgasi: `IPF GL Formula` https://www.powerlifting.sport/rules/codes/info/ipf-formula
- competition-derived percentile data backbone yonu: `OpenPowerlifting Data Service` https://openpowerlifting.gitlab.io/opl-csv/introduction.html

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
- kullanici sprint odakli ama posterior chain volumu dusuk
- kullanici broad jump gelistirmek istiyor ama relevant power movement exposure zayif
- kullanici RB olarak acceleration odakli bir profile sahip

## Phase 5 - FreakAI Intelligence Layer
FreakAI bu urunde tek basina bir "chat feature" degil, uzun vadede urunun intelligence backbone'u olacak katmandir.

Not:
- initial FreakAI MVP, language-aware behavior, training-program-aware coach flow ve ilk program generation akisi baslatilmistir
- bu faz, mevcut MVP'nin ustune daha derin reasoning, recommendation ve orchestration katmanini ifade eder

### Hedef
Kullaniciya daha akilli, daha aciklayici ve daha context-aware oneriler sunan; recommendation, aciklama ve guidance sistemlerini merkezi olarak tasiyan bir FreakAI katmani kurmak.

### Kapsam
- FreakAI assisted recommendation summaries
- Weak point explanation
- Sport + position specific guidance
- Goal-oriented movement suggestions
- User context aware coaching style explanations

### Onemli Ilke
FreakAI output:
- Phase 2'deki structured profile
- Phase 3'teki exercise metadata
- Phase 3A'daki performance standards
- Phase 4'teki deterministic scoring
ustune kurulacak.

### Product Rolu
FreakAI zamanla su alanlarin ana orkestrasyon katmani olacaktir:
- recommendation explanation
- athlete-specific insight generation
- goal-driven movement reasoning
- future program builder intelligence
- kullanicinin tum performans verisini anlamlandiran ust katman

## Phase 6 - Advanced Program Builder
Bu faz, mevcut starter template ve ilk AI program generation katmaninin ustune daha guclu bir builder deneyimi kurar.

### Hedef
Kullaniciya mini block, microcycle veya direction-level programlari daha kontrollu sekilde olusturma ve yonetme kabiliyeti sunmak.

### Kapsam
- Weekly structure suggestions
- Goal-based exercise grouping
- Progression logic
- Load / frequency recommendations

## 10. Yapilmis Islerin Roadmapten Cikarilmasi
Asagidaki basliklar artik "gelecek is" degil:
- exercise browser
- `1RM` calculation
- `RSI` calculation
- `FFMI` calculation
- movement goals
- athletic performance tracking
- local catalog seeding
- automated tests
- backend auth
- production PostgreSQL persistence
- Railway deployment
- production smoke testing
- app-wide `EN/TR` localization
- runtime language refresh
- settings / language switch
- secure change-password flow
- starter template browse + clone flow
- live workout v1
- FreakAI language mirroring

## 11. Product Risks
- cloud migration sirasinda local SQLite ile backend verisinin cakisabilmesi
- structured metadata kurulmadan AI feature'a erken gecilmesi
- sport / position recommendation logic'inin veri olmadan varsayimla yazilmasi
- catalog expansion sirasinda data quality problemleri
- benchmark sisteminin "evrensel tek tablo" varsayimiyla hatali yazilmasi
- desteklenmeyen norm profillerinde sahte tier uretilmesi
- Tier-1 kapsami yerine tum katalog icin bir anda medya beklenmesi

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
- desteklenmeyen norm profillerine zorla tier uretmek
- ilk fazda tum katalog icin demo medya zorunlulugu

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
3. Dashboard-first UI V2
4. Profile expansion for benchmarking and guidance
5. Tracking analytics and live workout depth
6. Exercise metadata engine
7. Performance standards and exercise guidance
8. Rule-based recommendation engine
9. FreakAI intelligence layer
10. Advanced program builder

## 15. Net Product Direction
FreakLete'in bundan sonraki asil hedefi sadece "log tutan bir app" olmak degil;
multilingual, athlete-specific, dashboard-first, structured-data-driven, benchmark-aware, trend-aware, recommendation-capable ve uzun vadede FreakAI tarafindan guclendirilen bir performance platformuna donusmektir.
