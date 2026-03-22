# FreakLete PRD

## 1. Urun Ozeti
FreakLete, field athletes ve gym odakli sporcular icin gelistirilmis bir mobil performans takip uygulamasidir. Uygulama; workout loglama, athletic performance tracking, movement goals, exercise discovery ve performance calculations gibi ozellikleri tek bir akista toplar.

Bu dokumanin amaci:
- tamamlanan MVP'yi netlestirmek
- mevcut urun mimarisini ozetlemek
- bundan sonraki roadmap'i mantikli fazlara ayirmak

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
- zayif noktalarini tespit eden
- hedefine, sporuna ve pozisyonuna gore akilli oneriler yapan
- zamanla AI destekli bir performance companion'a donusen
bir urun olmaktir.

## 5. Tamamlanan MVP
Asagidaki basliklar artik roadmap maddesi degil, mevcut urunun parcasidir.

### 5.1 Core Features
- Register / Login
- Local hashed auth
- Workout olusturma
- Exercise browser
- Calendar tabanli workout history
- Calculations sayfasi
- 1RM calculation
- RSI calculation
- Athletic performance tracking
- Movement goals
- Profile / body metrics
- CRUD for key records

### 5.2 Product and UX
- Text-based eski form hissinden cikilip modern visual UI'a gecildi
- Kategori tabanli exercise browser eklendi
- Strength ve athletic movement'lar ayni katalog yapisinda toplandi
- Modern custom dialogs eklendi

### 5.3 Data and Quality
- Local SQLite persistence
- Exercise catalog JSON + local DB seed yapisi
- Automated tests
- Core logic, calculations, catalog ve DB integration test coverage

## 6. Mevcut Mimari

### 6.1 Mobile App
- .NET MAUI
- C#
- XAML
- Local SQLite

### 6.2 Local Data Model
- User
- Workout
- ExerciseEntry
- PrEntry
- AthleticPerformanceEntry
- MovementGoal
- ExerciseDefinition

### 6.3 Current State
Bugunku uygulama polished bir local-first MVP'dir.

Bu ne demek:
- veriler cihaz icinde tutulur
- reinstall sonrasi local data kaybolur
- cok cihazli hesap sistemi yoktur
- merkezi veri kaynagi yoktur

## 7. Mevcut Backend Yonu
Projede backend tarafina gecis baslamistir.

Mevcut backend izi:
- ASP.NET Core Web API
- JWT auth direction
- PostgreSQL / EF Core direction

Bu katmanin amaci:
- gercek account system
- cloud persistence
- reinstall sonrasi account restore
- gelecekte recommendation ve AI sistemleri icin merkezi veri kaynagi saglamak

## 8. Product Principles
Roadmap boyunca su prensipler korunmali:
- once veri yapisi, sonra intelligence
- once deterministic recommendation logic, sonra AI
- once structured metadata, sonra open-ended AI output
- mobile app offline hissini kaybetmeden cloud'a gecmeli

## 9. Roadmap

## Phase 1 - Real Account System and Cloud Sync
Bu faz su an en yuksek onceliktir.

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

## Phase 2 - Structured Athlete Profile
Recommendation sisteminden once kullanici profilini daha anlamli ve secilebilir hale getirmek gerekir.

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

## Phase 5 - AI Recommendation Layer
AI bu urunde temel veri yapisinin yerine gecmeyecek; onun ustune oturacak.

### Hedef
Kullaniciya daha akilli, daha aciklayici ve daha context-aware oneriler sunmak.

### Kapsam
- AI assisted recommendation summaries
- Weak point explanation
- Sport + position specific guidance
- Goal-oriented movement suggestions
- Program idea generation

### Onemli Ilke
AI output:
- Phase 2'deki structured profile
- Phase 3'teki exercise metadata
- Phase 4'teki deterministic scoring
ustune kurulacak.

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
- visual UI redesign
- exercise browser
- 1RM calculation
- RSI calculation
- movement goals
- athletic performance tracking
- local catalog seeding
- automated tests

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

## 13. Next Recommended Execution Order
Urunu saglam buyutmek icin onerilen sira:

1. Real account system / backend persistence
2. Structured athlete profile
3. Exercise metadata engine
4. Rule-based recommendation engine
5. AI recommendation layer
6. Program builder

## 14. Net Product Direction
FreakLete'in bundan sonraki asil hedefi sadece "log tutan bir app" olmak degil;
athlete-specific, structured-data-driven, recommendation-capable bir performance platform'una donusmektir.
