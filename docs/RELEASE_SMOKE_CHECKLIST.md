# Release Smoke Checklist

Run this checklist manually before each release to verify end-to-end behavior
on a real device or emulator. Automated tests cover API contracts and core logic;
this checklist covers UI integration and device-specific behavior.

## Prerequisites

- [ ] `dotnet test FreakLete.Core.Tests` — all pass
- [ ] `dotnet test FreakLete.Api.Tests` — all pass
- [ ] `dotnet build FreakLete.csproj -f net10.0-android` — no errors
- [ ] API running locally (`dotnet run --project FreakLete.Api`)
- [ ] Fresh test database (or run against staging)

---

## Auth

- [ ] Register a new account with valid email/password
- [ ] Log out
- [ ] Log back in with the same credentials
- [ ] Verify token persists across app restart (SecureStorage)
- [ ] Attempt login with wrong password — see error message

## Profile

- [ ] View profile after login — fields populated from registration
- [ ] Update first name, last name, save — changes persist
- [ ] Update physical stats (height, weight, body fat) — changes persist
- [ ] Update training preferences (experience level, goals) — changes persist
- [ ] Kill and reopen app — profile data still loaded

## Workouts

- [ ] Create a new workout with at least 2 exercises and multiple sets
- [ ] Verify workout appears in list with correct date
- [ ] Edit an existing workout — change sets/reps/weight
- [ ] Delete a workout — confirm it disappears from list
- [ ] Verify workout data does not leak between users (if multi-user testing)

## PR (Personal Records)

- [ ] Create a new PR entry (exercise, weight, reps, date)
- [ ] Verify PR appears in list
- [ ] Edit a PR — change weight or reps
- [ ] Delete a PR — confirm removal
- [ ] Verify PRs are scoped to the logged-in user

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
