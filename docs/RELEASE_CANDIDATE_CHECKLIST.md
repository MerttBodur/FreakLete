# FreakLete — Release Candidate Checklist

**Purpose:** Final blocking gate list before any track promotion (internal → closed → production).
**Status:** Manual — all items must be verified by the developer before promoting a release.

Complete sections in order. Do not proceed to the next section if any blocking item is unresolved.

---

## Section 1: Automated Test Gates (Blocking)

Run before every release candidate build:

- [ ] `dotnet test .\FreakLete.Api.Tests\FreakLete.Api.Tests.csproj -v:m` — **all pass** (last known: 333/333)
- [ ] `dotnet test .\FreakLete.Core.Tests\FreakLete.Core.Tests.csproj -v:m` — **all pass** (last known: 341/341)
- [ ] `dotnet publish .\FreakLete.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=aab` — **succeeds, 0 errors**

---

## Section 2: Signing & Version (Blocking)

- [ ] AAB signed with upload key (not debug keystore) — see `docs/RELEASE_SIGNING_CHECKLIST.md`
- [ ] Upload certificate fingerprint matches Play Console App integrity page
- [ ] `ApplicationVersion` (versionCode) is correct and **not previously used** for this package
- [ ] `ApplicationDisplayVersion` (versionName) reflects the intended user-visible version
- [ ] AAB file size within expected range (~31 MB ± 5 MB)

> **Note:** First Play Console upload uses versionCode 1. Every subsequent upload to any track (internal, closed, or production) must increment `ApplicationVersion` in `FreakLete.csproj`.

---

## Section 3: Policy & Compliance (Blocking for Store Submission)

- [ ] Privacy policy hosted at a public URL (e.g., `https://freaklete.app/privacy`)
- [ ] Account deletion form live at `https://freaklete.app/account-deletion`
- [ ] Play Console Data Safety form completed and published (reference: `docs/PLAY_DATA_SAFETY.md`)
- [ ] Play Console Health Apps declaration completed (reference: `docs/PLAY_HEALTH_APPS_DECLARATION.md`)
- [ ] Medical disclaimer added to Play Store long description (reference: `docs/PLAY_HEALTH_APPS_DECLARATION.md`)

---

## Section 4: Play Console Products & Backend (Blocking)

- [ ] Subscription `freaklete_premium` active with base plans `monthly` and `annual` (reference: `docs/PLAY_CONSOLE_SETUP.md`)
- [ ] One-time products `donate_1`, `donate_5`, `donate_10`, `donate_20` all active
- [ ] All Railway env vars set (reference: `docs/PRODUCTION_BACKEND_CHECKLIST.md`)
- [ ] `GET https://freaklete-production.up.railway.app/api/health` → `{"status":"healthy"}` HTTP 200
- [ ] Railway deployment logs show no migration errors or startup exceptions

---

## Section 5: Internal Track Smoke (Blocking)

- [ ] Signed AAB uploaded to internal testing track
- [ ] All billing smoke tests passed with license tester account (reference: `docs/PLAY_INTERNAL_TESTING_GUIDE.md`)
- [ ] Monthly subscription purchase → backend sync → Settings shows Premium
- [ ] Annual subscription purchase → backend sync → Settings shows Premium
- [ ] Restore purchases → Premium re-granted
- [ ] All four donation denominations tested
- [ ] Cancel/failed purchase — no crash, graceful return
- [ ] FreakAI quota card reflects Premium entitlement after subscription
- [ ] Logout/login preserves Premium status

---

## Section 6: Play Pre-Launch Report (Blocking)

- [ ] Pre-launch report completed in Play Console (no blocking crashes or ANRs)
- [ ] No policy violations flagged in pre-launch report
- [ ] Non-blocking accessibility warnings reviewed (see `docs/PLAY_PRELAUNCH_AND_ROLLOUT.md`)
- [ ] No sensitive data or credentials visible in pre-launch screenshots

---

## Section 7: Release Notes

- [ ] Release notes prepared for this version (English minimum; Turkish recommended for TR market)
- [ ] Notes accurately describe what is new/changed (do not overstate features)

---

## Rollout Strategy

**Do not go directly to 100% production rollout for the first release.**

### Recommended rollout sequence:

1. **Internal testing** (current) — license testers only; billing smoke
2. **Closed testing** (alpha/beta) — broader tester group; extended smoke
3. **Production — staged rollout** — start at 10–20% if Play Console allows staged rollout for new apps
   - Monitor crash rate, ANRs, and billing failures for 24–48 hours before expanding
4. **Production — full rollout** — only after staged rollout period is stable

### Halt rollout criteria:

Stop and halt rollout immediately if any of the following appear after promotion:

- Crash rate above baseline (Play Console Vitals > Android Vitals)
- Billing purchase failure rate > 5% for any product
- Login/auth failures appearing in backend logs
- User reports of data loss or account inaccessibility

### Rollback steps:

1. In Play Console: go to the affected track > Managed publishing or Rollout
2. Halt the rollout (does not remove the app from already-installed devices)
3. Fix the issue on the `main` branch
4. Bump `ApplicationVersion` (versionCode)
5. Rebuild, re-sign, re-upload, and re-test the new release candidate
