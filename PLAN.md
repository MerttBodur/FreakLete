# GymTracker Immediate Roadmap

## Current Status

Current position: `Phase 3 complete`

Completed so far:
- `Phase 1` completed: calculations polish
- `Phase 2` completed: homepage comparison chart fix
- `Phase 3` completed: settings billing repair

Immediate work still pending:
- `Phase 4` pending: calculation insights v1

Backlog / noted items:
- `Phase 5` planned: quick workouts visuals + new ready-made programs
- `Phase 6` planned: leaderboard groundwork

Summary:
- total phases in this plan: `6`
- completed: `3`
- remaining: `3`

## Phase 1 - Calculations Polish
Status: `Completed`

Delivered:
- FFMI result caption changed from normalized wording to `Adjusted FFMI / Duzeltilmis FFMI`
- FFMI secondary line moved to locale-safe formatting
- 1RM and RSI result surfaces aligned to the FFMI result card pattern
- result hierarchy now uses:
  - large primary value
  - small caption
  - one supporting secondary line

Acceptance reached:
- formatting compiles
- core tests passed

## Phase 2 - Homepage Comparison Chart Fix
Status: `Completed`

Delivered:
- homepage chart now merges:
  - workout history
  - PR entries
  - athletic performance records
- selectable time ranges added:
  - `14 Gun`
  - `1 Ay`
  - `3 Ay`
  - `6 Ay`
- bucket strategy implemented:
  - `14 Gun`: daily
  - `1 Ay`: daily
  - `3 Ay`: weekly
  - `6 Ay`: monthly
- chart subtitle and unit display made range-aware

Acceptance reached:
- chart helper covered by unit tests
- no blocker found in static review

Known minor note:
- `3 Ay` weekly labels may need device-level readability check

## Phase 3 - Settings Billing Repair
Status: `Completed`
Priority: `Immediate`

Goal:
- make `Bagis Yap` and `Abone Ol` flows reliable on Android + Railway/internal track

Delivered:
- purchase success and backend sync success are handled separately
- fake success toasts for sync or verification failures were removed
- explicit outcome handling now exists for:
  - cancelled
  - unavailable
  - already owned
  - sync failed
  - verification failed
  - restore empty
  - restore partial success
  - restore full success
- premium UI refresh is gated on verified sync only
- donate / subscribe / restore flows use testable billing outcome logic
- donate follow-up fix completed:
  - backend donate state `completed` now maps to success without relaxing subscription security

Acceptance reached:
- no fake success UX remains in logic layer
- premium UI updates only after verified successful sync
- restore flow reports full / partial / empty outcomes correctly
- directly affected billing logic tests pass

Known runtime note:
- real Play internal track verification still depends on Android device + Railway Google Play configuration

## Phase 4 - Calculation Insights V1
Status: `Pending`
Priority: `Immediate`

Goal:
- after `1RM`, `RSI`, and `FFMI` calculation, show a simple deterministic analysis band

Target labels:
- `Gelistirilmeli`
- `Idare Eder`
- `Iyi`
- `Advanced`
- `Elit`

Rules:
- not only pro athletes
- use sport-specific context when supported
- also show global athlete context
- unsupported norm profiles must not get fake tiers

Planned follow-up note:
- the long-term band schema should be:
  - `NeedsWork`
  - `Adequate`
  - `Good`
  - `Advanced`
  - `Elite`
- `Advanced` should sit between `Good` and `Elite`
- `Elite` should represent more extreme top-end thresholds than the first simple version
- example future directions:
  - bench press around `1.75x BW`
  - power clean around `1.50x BW`
  - back squat around `2.25x BW`
- these exact elite thresholds are **not** part of the current implementation phase
- they require a separate benchmark research pass before being shipped

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

## Phase 5 - Home Content Pack
Status: `Planned`
Priority: `Backlog`

Contains noted items:
- add visuals for quick workouts
- add new ready-made programs

Expected scope:
- quick workout cards get real visual assets instead of fallback-only coverage
- starter template pool expands with new useful ready-made programs
- home/program surfaces stay aligned with current design language

## Phase 6 - Leaderboard Groundwork
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
1. `Phase 4` - Calculation Insights V1
2. `Phase 5` - Home Content Pack
3. `Phase 6` - Leaderboard Groundwork

## Verification Notes

Completed verification already reported:
- calculations polish tests/build passed
- chart helper tests passed
- settings billing outcome logic tests passed
- donate sync classification follow-up tests passed

Still needs real runtime smoke on device:
- homepage chart range readability
- Android billing real purchase / restore behavior
- future insight card presentation
