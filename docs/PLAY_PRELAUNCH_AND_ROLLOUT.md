# FreakLete — Play Pre-Launch Report & Rollout Guide

**Purpose:** How to interpret the Play Console pre-launch report and manage the production rollout.
**Status:** Reference — execute after uploading the signed AAB to the internal or closed testing track.

---

## 1. Pre-Launch Report Overview

After uploading an AAB to any testing track, Play Console runs the app automatically on a set of physical devices and generates a pre-launch report. The report takes 30–60 minutes to complete after upload.

Access: Play Console → Testing → (track) → the uploaded release → Pre-launch report tab

---

## 2. Blocker Categories

The following findings in the pre-launch report are **blocking** and must be resolved before promoting to a wider track or production:

| Finding | Why it blocks | Action |
|---|---|---|
| **Crash on launch** | App is non-functional for affected device configurations | Fix crash, bump versionCode, re-upload |
| **ANR on startup or main flow** | App freezes — will trigger Play Store quality degradation | Profile the main thread; fix the blocking call |
| **Policy violation flagged** | Google may reject the release or delist the app | Address the specific policy item; re-submit |
| **Billing API error on purchase attempt** | Purchase flow fails for all users | Check product IDs, backend sync, and Play App Signing |
| **Startup crash due to missing permissions** | App crashes before first use | Verify manifest permissions are declared |

---

## 3. Accepted Non-Blocking Warnings

The following findings are expected and do not block release:

| Finding | Why it is accepted |
|---|---|
| Accessibility warnings (color contrast, touch target size) | Known UI limitations; planned for Dashboard V2 |
| Pre-launch screenshots showing loading states | Emulators may capture loading frames — not a functional issue |
| NU1608 NuGet version constraint warnings | AndroidX minor version resolution above lower bounds; no runtime impact |
| Missing `android:exported` attribute on activities (if shown) | Review case by case; MAUI generates the manifest — verify it is not a real omission |
| Battery/performance warnings on low-end devices | Acceptable for v1; benchmark on target device class |

---

## 4. Evidence to Collect from Pre-Launch Report

Capture the following before promoting to production:

- [ ] Screenshot of pre-launch report summary (pass/warn/fail counts)
- [ ] Screenshot or export of crash-free sessions percentage
- [ ] Screenshot of any crash stack traces (anonymized — no user data)
- [ ] List of device/OS configurations that showed warnings
- [ ] Note of any accessibility warnings that will be addressed in a future release

> Do not include purchase tokens, API keys, Railway secrets, or raw log payloads in any captured evidence.

---

## 5. Post-Release Monitoring Checklist

Run these checks for 24–48 hours after each production rollout:

### Play Console Android Vitals

- [ ] Crash rate: baseline is < 1% crash-free sessions; halt if materially above baseline
- [ ] ANR rate: baseline is < 0.5% ANR-free sessions
- [ ] Battery/memory: no unexpected spikes on target device class

### Backend (Railway)

- [ ] `GET /api/health` continues to return `{"status":"healthy"}`
- [ ] Railway deployment logs: no spike in 5xx errors after rollout
- [ ] No increase in JWT authentication failures (401s)
- [ ] No increase in billing sync errors

### Billing

- [ ] Subscription purchase completion rate — compare to internal testing baseline
- [ ] Donation purchase completion rate
- [ ] Restore purchase success rate
- [ ] No spike in failed/pending purchase sync reports

### User Signals

- [ ] Play Console Reviews tab: monitor for early crash reports or billing issues
- [ ] No user reports of data loss or account inaccessibility

---

## 6. Rollback / Halt Rollout Steps

If any monitoring criteria trigger a halt:

1. **In Play Console:** go to the affected track → find the active rollout → tap **Halt rollout**
   - Halting pauses new installs/updates; existing installs are not reverted
2. **Diagnose:** check Railway logs, Android Vitals crash trace, and billing sync logs
3. **Fix:** apply fix on `main` branch
4. **Rebuild:**
   ```bash
   # Increment ApplicationVersion in FreakLete.csproj first
   dotnet publish .\FreakLete.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=aab \
     -p:AndroidKeyStore=true \
     -p:AndroidSigningKeyStore=<path> \
     -p:AndroidSigningStorePass=<pass> \
     -p:AndroidSigningKeyAlias=<alias> \
     -p:AndroidSigningKeyPass=<keypass>
   ```
5. **Re-test:** complete Section 5 of `docs/RELEASE_CANDIDATE_CHECKLIST.md` (internal track smoke)
6. **Re-promote:** upload to internal track → verify → promote to production

---

## 7. Staged Rollout Reference

If Play Console allows a staged rollout for this app:

| Stage | Rollout % | Wait before expanding |
|---|---|---|
| Initial | 10–20% | 24–48 hours — monitor crash rate and billing |
| Expand | 50% | 24 hours — confirm no new crash clusters |
| Full | 100% | After stability confirmed |

> Google Play may not allow staged rollout for brand-new apps (first release to a track). In that case, rollout is 100% immediately. Monitor Android Vitals closely for the first 48 hours.
