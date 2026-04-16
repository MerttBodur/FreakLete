# Exercise Tier System — Design Spec
**Date:** 2026-04-16
**Status:** Approved

---

## Overview

Her scoreable egzersiz için kullanıcıya 6 seviyeli bir tier sistemi sunulur:
`Need Improvement → Beginner → Intermediate → Advanced → Elite → Freak`

- **Need Improvement:** Hayatında hiç spor yapmamış kişiler
- **Freak:** Genetik olarak gifted veya anabolik kullanan insanların ulaşabileceği seviye

---

## 1. Scoreable Exercises

### Kural
```
TrackingMode == "Strength" AND Mechanic != "isolation"  →  Strength Tier
TrackingMode == "Athletic"                               →  Athletic Tier
```

Lateral Raise, Bicep Curl, Leg Extension gibi isolation hareketler puanlanmaz.
Power Clean, Olympic lifts dahil tüm compound ve full-body hareketler puanlanır.

### İki Katman

**Tier-1 — Ana hareketler** (~15-20 egzersiz)
Kendi unique threshold array'lerine sahip. Araştırma tabanlı, cinsiyet bazlı.

Örnek: Bench Press, Back Squat, Conventional Deadlift, OHP, Power Clean,
Romanian Deadlift, Front Squat, Barbell Row, Chin-up, vb.

**Tier-2 — Varyasyon/aksesuar hareketler**
Parent hareketten türetilir + scaling faktörü ile.

```json
"tierParent": "conventionaldeadlift",
"tierScale": 1.1
```

Örnekler:
- Rack Pull → Deadlift × 1.1 (mekanik avantaj)
- Paused Deadlift → Deadlift × 0.90
- Block Pull → Deadlift × 1.05
- Deficit Deadlift → Deadlift × 0.92

---

## 2. Tier Thresholds

### Strength Exercises — Bw Multiplier (catalog JSON'da, per-exercise)

```json
{
  "id": "benchpress",
  "tierType": "StrengthRatio",
  "tierThresholds": {
    "male":   [0.5, 1.0, 1.25, 1.5,  1.75],
    "female": [0.35, 0.7, 0.9, 1.1,  1.35]
  }
}
```

Array 5 eleman — 6 tier sınırı tanımlar:
- `< thresholds[0]` → Need Improvement
- `< thresholds[1]` → Beginner
- `< thresholds[2]` → Intermediate
- `< thresholds[3]` → Advanced
- `< thresholds[4]` → Elite
- `>= thresholds[4]` → Freak

Hesaplama: `ratio = 1RM / bodyweightKg`

### Athletic Exercises — Absolute Value (catalog JSON'da, per-exercise)

```json
{
  "id": "verticaljump",
  "tierType": "AthleticAbsolute",
  "tierThresholds": {
    "male":   [30, 45, 55, 65, 75],
    "female": [20, 32, 42, 52, 60]
  }
}
```

Cinsiyet farkı atletik performansta anlamlıysa ayrı array, değilse aynı değerler kullanılır.

Sprint gibi ters metrikler (düşük = iyi) için `tierType: "AthleticInverse"` kullanılır.

---

## 3. Data Model

### Yeni Entity: `UserExerciseTier`

```csharp
public class UserExerciseTier
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CatalogId { get; set; }
    public string ExerciseName { get; set; }     // denormalized, display için
    public string TierLevel { get; set; }        // "NeedImprovement" | "Beginner" | ... | "Freak"
    public double RawValue { get; set; }         // Strength: 1RM kg, Athletic: raw metric
    public double? BasisValue { get; set; }      // Strength: bodyweightKg, Athletic: null
    public double? Ratio { get; set; }           // Strength: RawValue/BasisValue, Athletic: null
    public DateTime CalculatedAt { get; set; }

    public User User { get; set; } = null!;
}
```

**Migration:** `UserExerciseTiers` tablosu, `(UserId, CatalogId)` unique index ile.

### ExerciseDefinition'a eklenen alanlar (catalog JSON + entity)

```csharp
public string TierType { get; set; }             // "StrengthRatio" | "AthleticAbsolute" | "AthleticInverse" | ""
public string TierThresholdsMale { get; set; }   // JSON array string: "[0.5,1.0,1.25,1.5,1.75]"
public string TierThresholdsFemale { get; set; } // JSON array string
public string? TierParentId { get; set; }        // Tier-2 için parent CatalogId
public double? TierScale { get; set; }           // Tier-2 için scaling faktörü
```

---

## 4. Core Logic (FreakLete.Core)

### TierLevel Enum

```csharp
public enum TierLevel
{
    NeedImprovement,
    Beginner,
    Intermediate,
    Advanced,
    Elite,
    Freak
}
```

### TierResolver (saf fonksiyon, test edilebilir)

```csharp
public static class TierResolver
{
    public static TierLevel Resolve(double value, double[] thresholds)
    {
        for (int i = 0; i < thresholds.Length; i++)
            if (value < thresholds[i]) return (TierLevel)i;
        return TierLevel.Freak;
    }

    public static double[] GetThresholds(ExerciseTierConfig config, string sex,
        Dictionary<string, ExerciseTierConfig> allConfigs)
    {
        // Tier-2: parent threshold'larını scale et
        if (config.TierParentId != null && config.TierScale.HasValue)
        {
            var parent = allConfigs[config.TierParentId];
            var raw = sex == "Female" ? parent.ThresholdsFemale : parent.ThresholdsMale;
            return raw.Select(t => t * config.TierScale.Value).ToArray();
        }
        return sex == "Female" ? config.ThresholdsFemale : config.ThresholdsMale;
    }
}
```

---

## 5. API Layer

### Tier Hesaplama Akışı (PR kaydedilince)

```
POST /api/pr
    ↓ PR kaydedilir
    ↓ ExerciseTierService.RecalculateTierAsync(userId, catalogId)
        1. ExerciseDefinition → tierType, thresholds, tierParent, tierScale
        2. User.WeightKg + User.Sex
        3. 1RM = CalculationService.CalculateOneRm(weight, reps, rir)
        4. thresholds = TierResolver.GetThresholds(config, sex, allConfigs)
        5. tier = TierResolver.Resolve(ratio, thresholds)
        6. UserExerciseTier UPSERT
    ↓ PR response'a tier bilgisi eklenir
```

### PR Response (eklenen alan)

```json
{
  "prEntry": { "..." },
  "tier": {
    "exerciseId": "benchpress",
    "tierLevel": "Advanced",
    "previousTierLevel": "Intermediate",
    "leveledUp": true
  }
}
```

### Yeni Endpoint

```
GET /api/profile/tiers
→ ExerciseTierDto[]
```

```csharp
public class ExerciseTierDto
{
    public string CatalogId { get; set; }
    public string ExerciseName { get; set; }
    public string TierLevel { get; set; }
    public double RawValue { get; set; }
    public double? Ratio { get; set; }
    public DateTime CalculatedAt { get; set; }
}
```

---

## 6. Mobile Integration

### Tier veri çekme

| Durum | Akış |
|---|---|
| Profil sayfası açılır | `GET /api/profile/tiers` → tüm snapshot |
| PR kaydedilir | Response'daki `tier` objesi işlenir |
| `leveledUp == true` | Tier-up dialog/animasyon tetiklenir |

### Profil sayfası UI

```
Bench Press      ████░░  Advanced
Back Squat       ██░░░░  Beginner
Power Clean      █████░  Elite
Vertical Jump    ███░░░  Intermediate
```

---

## 7. Tier-1 Ana Hareketler (threshold araştırması gerekli)

**Strength (StrengthRatio) — Bw multiplier, cinsiyet bazlı:**
- Bench Press
- Back Squat
- Conventional Deadlift
- Sumo Deadlift
- Overhead Press / Military Press
- Power Clean
- Power Snatch
- Front Squat
- Romanian Deadlift
- Barbell Row
- Chin-up / Pull-up (toplam kaldırılan ağırlık: BW + ek)
- Trap Bar Deadlift
- Hip Thrust
- Push Press

**Athletic (AthleticAbsolute) — absolute value, cinsiyet bazlı:**
- Vertical Jump (cm)
- Standing Broad Jump (cm)
- RSI (value)
- 40-yard Dash (saniye — AthleticInverse)
- 10m Sprint (saniye — AthleticInverse)

Referans kaynaklar:
- Güç standartları: OpenPowerlifting, IPF GL Formula
- Atletik standartlar: sport-science literature, combine norms

---

## 8. Required Tests

- `TierResolver.Resolve` unit testleri (tüm tier sınır değerleri)
- `TierResolver.GetThresholds` Tier-2 scaling testi
- `ExerciseTierService` integration testi (PR save → tier upsert)
- `GET /api/profile/tiers` endpoint testi
- PR response'da `tier` alanı testi
- `User.WeightKg` null ise graceful skip testi
- `AthleticInverse` ters eşik davranış testi

---

## 9. Edge Cases

| Durum | Davranış |
|---|---|
| `User.WeightKg` null | Tier hesaplanmaz, `UserExerciseTier` oluşturulmaz |
| `User.Sex` boş | Male threshold default |
| Egzersiz scoreable değil | Tier hesaplanmaz, response'da `tier: null` |
| Tier-2 parent bulunamaz | Log + skip, exception fırlatma |
| Athletic ters metrik (sprint süresi) | `tierType: "AthleticInverse"` — düşük değer daha iyi |
