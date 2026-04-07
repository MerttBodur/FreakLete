# Release Smoke Checklist

**Test layers before release:**
- Automated API tests (FreakLete.Api.Tests) - **Blocking**
- Automated ViewModel tests (FreakLete.Core.Tests) - **Blocking**
- Manual Android emulator smoke testing - **Blocking** (this is the real verification)

---

## Automated Test Prerequisites

- [ ] `dotnet test FreakLete.Api.Tests` — all pass (blocking)
- [ ] `dotnet test FreakLete.Core.Tests` — all pass (blocking)
- [ ] `dotnet build FreakLete.csproj -f net10.0-android` — no errors

---

## Android Emulator Smoke Tests (Primary Verification)

Run on Android emulator with a test user account.
This is the real Profile page verification.

### Test Setup

- [ ] Android emulator running with API 30+ (Pixel 5 or similar)
- [ ] Fresh test database or staging server
- [ ] Test account: `testprofile@example.com` / `TestPassword123!`

### Critical Profile Fields to Verify

These fields are the core Profile flow that users interact with:

**Athlete Profile:**
1. **DateOfBirth** - Date picker from selector
2. **WeightKg** - Manual entry (numeric input)
3. **BodyFatPercentage** - Manual entry (numeric input)
4. **HeightCm** - Manual entry (numeric input, 80-300 cm)
5. **Sex** - Selector (Male / Female)
6. **SportName** - Selector (Sport picker)
7. **Position** - Selector (Position picker, if sport has positions)
8. **GymExperienceLevel** - Selector (Experience level picker)

**Coach Profile:**
7. **TrainingDaysPerWeek** - Selector (1-7 days)
8. **PreferredSessionDurationMinutes** - Selector (30, 45, 60, 75, 90, 120)
9. **PrimaryTrainingGoal** - Selector (Strength, Hypertrophy, etc.)
10. **SecondaryTrainingGoal** - Selector (optional)
11. **DietaryPreference** - Selector (High Protein, Vegan, Keto, etc.)
12. **AvailableEquipment** - Editor (free text)
13. **PhysicalLimitations** - Editor (free text)
14. **InjuryHistory** - Editor (free text)
15. **CurrentPainPoints** - Editor (free text)

### Test Datasets

Execute the Profile flow with these three datasets:

#### Dataset 1: Low
```
DateOfBirth: 2000-01-01
WeightKg: 50
BodyFatPercentage: 5
HeightCm: 165
Sex: Male
SportName: Soccer
Position: Goalkeeper
GymExperienceLevel: < 1 year
TrainingDaysPerWeek: 1
SessionDurationMinutes: 30
PrimaryTrainingGoal: General Fitness
SecondaryTrainingGoal: (none)
DietaryPreference: No preference
AvailableEquipment: (blank)
PhysicalLimitations: (blank)
InjuryHistory: (blank)
CurrentPainPoints: (blank)
```

**Steps:**
- [ ] Form loads empty and renders correctly
- [ ] Enter minimal values as specified above
- [ ] Save successfully
- [ ] Verify toast/success message
- [ ] Restart app, re-open Profile — fields persist correctly

#### Dataset 2: Typical
```
DateOfBirth: 1995-06-15
WeightKg: 85
BodyFatPercentage: 15
HeightCm: 178
Sex: Female
SportName: Powerlifting
Position: (not applicable)
GymExperienceLevel: 3-4 years
TrainingDaysPerWeek: 4
SessionDurationMinutes: 60
PrimaryTrainingGoal: Strength
SecondaryTrainingGoal: Hypertrophy
DietaryPreference: High Protein
AvailableEquipment: Barbell, Dumbbells, Bench
PhysicalLimitations: None
InjuryHistory: None
CurrentPainPoints: (blank)
```

**Steps:**
- [ ] Load from Dataset 1 state
- [ ] Update all fields to Dataset 2 values
- [ ] Verify selectors show correct display values
- [ ] Save successfully
- [ ] Restart app — verify all fields persisted

#### Dataset 3: High
```
DateOfBirth: 1970-12-31
WeightKg: 150
BodyFatPercentage: 45
HeightCm: 198
Sex: Male
SportName: Basketball
Position: Center
GymExperienceLevel: 5+ years
TrainingDaysPerWeek: 6
SessionDurationMinutes: 120
PrimaryTrainingGoal: Athletic Performance
SecondaryTrainingGoal: Strength
DietaryPreference: Vegetarian
AvailableEquipment: Full range: Barbells, Dumbbells, Benches, Cable Machine, Smith Machine, Adjustable Squat Rack
PhysicalLimitations: Previous knee injury during basketball
InjuryHistory: Torn ACL (left knee, 2015); recovered fully with rehab
CurrentPainPoints: Mild lower back discomfort from previous deadlift injury
```

**Steps:**
- [ ] Load from Dataset 2 state
- [ ] Update all fields, including long text in editors
- [ ] Verify UI handles long text (proper wrapping, readability)
- [ ] Save successfully
- [ ] Restart app — verify all fields persisted exactly

---

## Other Profile-Related Flows

- [ ] Register new account → default profile state
- [ ] Login → profile loads correctly
- [ ] Update profile → changes persist
- [ ] Logout/login → profile still present

---

## Existing Manual Smoke Checklist

Run this checklist manually on Android emulator after the critical Profile flows above.

---

## Auth

- [ ] Register a new account with valid email/password
- [ ] Log out
- [ ] Log back in with the same credentials
- [ ] Verify token persists across app restart (SecureStorage)
- [ ] Attempt login with wrong password — see error message

## Workouts

- [ ] Create a new workout with at least 2 exercises and multiple sets
- [ ] Verify workout appears in list with correct date
- [ ] Edit an existing workout — change sets/reps/weight
- [ ] Delete a workout — confirm it disappears from list

## PR (Personal Records)

- [ ] Create a new PR entry (exercise, weight, reps, date)
- [ ] Verify PR appears in list
- [ ] Edit a PR — change weight or reps
- [ ] Delete a PR — confirm removal

## Athletic Performance

- [ ] Create a performance entry (movement, value, unit, date)
- [ ] Verify it appears in list
- [ ] Filter by movement — only matching entries shown
- [ ] Edit an entry — change value
- [ ] Delete an entry — confirm removal

## Movement Goals

- [ ] Create a movement goal (movement, target value, deadline)
- [ ] Verify it appears in goals list
- [ ] Edit a goal — change target or deadline
- [ ] Delete a goal — confirm removal
- [ ] Verify goals are user-scoped

## FreakAI

- [ ] Open FreakAI chat screen
- [ ] Send a simple message — receive a coherent response
- [ ] Send a Turkish message — response should be in Turkish
- [ ] Send a follow-up message — conversation context maintained
- [ ] Verify error state if network is off — user sees friendly error, not crash
- [ ] Verify long messages (near 2000 chars) are accepted

## Exercise Catalog

- [ ] Browse exercise catalog — categories load
- [ ] Search for an exercise by name — results appear
- [ ] Filter by category — only matching exercises shown

## Account

- [ ] Delete account — confirm prompt shown
- [ ] After deletion, app returns to login screen
- [ ] Attempting to use old token fails (401)

---

## Automated Coverage Summary

| Area | Automated Tests | What's NOT automated |
|------|----------------|---------------------|
| Auth (register/login/401s) | 11 API tests | UI flow, SecureStorage persistence |
| Athlete Profile (typed endpoint, validation) | 17 API tests | UI binding, reload on app restart |
| Coach Profile (typed endpoint, validation) | 9 API tests | UI binding, reload on app restart |
| Workouts (CRUD, isolation) | 16 API tests | UI exercise picker, set editing UX |
| PRs (CRUD, isolation) | 13 API tests | UI date picker, list sorting |
| Athletic Performance | 14 API tests | UI filtering UX |
| Movement Goals | 12 API tests | UI deadline display |
| FreakAI (chat, errors, tools) | 31 API tests | Real Gemini responses, UI chat scroll |
| Exercise Catalog | 14 API tests | UI browsing, image loading |
| Sport Catalog | 11 API tests | UI display |
| Calculations | 22 API + 16 Core | UI input validation UX |
| Core logic | 106 Core tests | — |
| **Total** | **297 tests** | |

## What Remains Unautomated

1. **MAUI UI behavior** — navigation, data binding, visual rendering, gesture handling
2. **SecureStorage** — token persistence across app restart
3. **Real Gemini API** — actual model responses, latency, rate limits
4. **Device-specific** — Android permissions, status bar, keyboard behavior
5. **Network edge cases** — slow connections, intermittent failures on device
6. **Multi-device sync** — same account on different devices
