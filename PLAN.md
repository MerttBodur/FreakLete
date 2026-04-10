# GymTracker Immediate Roadmap

## Current Status

Current position: `Phase 2 complete`

Completed so far:
- `Phase 1` completed: calculations polish
- `Phase 2` completed: homepage comparison chart fix

Immediate work still pending:
- `Phase 3` pending: settings billing repair
- `Phase 4` pending: calculation insights v1

Backlog / noted items:
- `Phase 5` planned: quick workouts visuals + new ready-made programs
- `Phase 6` planned: leaderboard groundwork

Summary:
- total phases in this plan: `6`
- completed: `2`
- remaining: `4`

## Phase 1 — Calculations Polish
Status: `Completed`

Delivered:
- FFMI result caption changed from normalized wording to `Adjusted FFMI / Düzeltilmiş FFMI`
- FFMI secondary line moved to locale-safe formatting
- 1RM and RSI result surfaces aligned to the FFMI result card pattern
- result hierarchy now uses:
  - large primary value
  - small caption
  - one supporting secondary line

Acceptance reached:
- formatting compiles
- core tests passed

## Phase 2 — Homepage Comparison Chart Fix
Status: `Completed`

Delivered:
- homepage chart now merges:
  - workout history
  - PR entries
  - athletic performance records
- selectable time ranges added:
  - `14 Gün`
  - `1 Ay`
  - `3 Ay`
  - `6 Ay`
- bucket strategy implemented:
  - `14 Gün`: daily
  - `1 Ay`: daily
  - `3 Ay`: weekly
  - `6 Ay`: monthly
- chart subtitle and unit display made range-aware

Acceptance reached:
- chart helper covered by unit tests
- no blocker found in static review

Known minor note:
- `3 Ay` weekly labels may need device-level readability check

## Phase 3 — Settings Billing Repair
Status: `Pending`
Priority: `Immediate`

Goal:
- make `Bağış Yap` and `Abone Ol` flows truly reliable on Android + Railway/internal track

Required work:
- separate purchase success from backend sync success
- do not show false success when sync or verification fails
- handle these outcomes explicitly:
  - cancelled
  - unavailable
  - already owned
  - sync failed
  - verification failed
  - Play unavailable
  - backend verification/config missing
- refresh billing status only after a valid sync result
- keep donate / subscribe / restore flows consistent

Acceptance criteria:
- no fake success UX remains
- premium UI only updates after verified successful sync
- restore flow reports full / partial / empty outcomes correctly
- Android build passes
- directly affected tests pass

## Phase 4 — Calculation Insights V1
Status: `Pending`
Priority: `Immediate`

Goal:
- after `1RM`, `RSI`, and `FFMI` calculation, show a simple deterministic analysis band

Target labels:
- `Geliştirilmeli`
- `İdare Eder`
- `İyi`
- `Elit`

Rules:
- not only pro athletes
- use sport-specific context when supported
- also show global athlete context
- unsupported norm profiles must not get fake tiers

Scope:
- support only safe metrics / supported movements first
- preferred supported 1RM movements:
  - `Bench Press`
  - `Back Squat`
  - `Deadlift`
  - `Military/Overhead Press`
  - `Power Clean`
- `RSI` and `FFMI` should use cautious athlete-population-aware logic

Acceptance criteria:
- supported calculations show a clear analysis card
- unsupported cases fall back to raw result only
- logic is deterministic and test-covered

## Phase 5 — Home Content Pack
Status: `Planned`
Priority: `Backlog`

Contains noted items:
- add visuals for quick workouts
- add new ready-made programs

Expected scope:
- quick workout cards get real visual assets instead of fallback-only coverage
- starter template pool expands with new useful ready-made programs
- home/program surfaces stay aligned with current design language

## Phase 6 — Leaderboard Groundwork
Status: `Planned`
Priority: `Backlog`

Source note:
- requested after calculation insight layer

Goal:
- prepare a future leaderboard around supported performance metrics

Expected direction:
- build only after insight rules are stable
- should not rely only on pro-athlete thresholds
- should reuse the same supported benchmark/resolver logic as calculation insights

## Working Order

Recommended execution order from here:
1. `Phase 3` — Settings Billing Repair
2. `Phase 4` — Calculation Insights V1
3. `Phase 5` — Home Content Pack
4. `Phase 6` — Leaderboard Groundwork

## Verification Notes

Completed verification already reported:
- calculations polish tests/build passed
- chart helper tests passed

Still needs real runtime smoke on device:
- homepage chart range readability
- Android billing real purchase / restore behavior
- future insight card presentation
