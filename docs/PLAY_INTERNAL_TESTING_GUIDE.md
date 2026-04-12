# FreakLete — Google Play Internal Testing Execution Guide

**Purpose:** Step-by-step guide for running the first real-device internal/closed track smoke test.
**Status:** Guide prepared — manual execution required on a physical device or Play-supported emulator.

> This document is for the developer executing the internal testing run. Do not include screenshots, purchase tokens, service account credentials, or any secret values in version control.

---

## 1. Prerequisites

Before starting:

- [ ] Signed AAB built: `bin/Release/net10.0-android/publish/com.mert.freaklete-Signed.aab` (~31 MB)
- [ ] Railway production backend healthy: `curl https://freaklete-production.up.railway.app/api/health`
  Expected: `{"status":"healthy"}` with HTTP 200
- [ ] All Phase 4 Play Console products active (see `docs/PLAY_CONSOLE_SETUP.md`)
- [ ] All Phase 4 Railway env vars set (see `docs/PRODUCTION_BACKEND_CHECKLIST.md`)
- [ ] Physical Android device (API 26+) or Google Play–capable emulator
- [ ] Test Google account added as a license tester in Play Console under Setup > License testing
- [ ] Test Google account added as an internal tester on the internal testing track

---

## 2. Play Console Setup Verification

Before uploading the AAB, verify:

- [ ] App package: `com.mert.freaklete` — confirmed in Play Console app overview
- [ ] Subscription `freaklete_premium` active with base plans `monthly` and `annual`
- [ ] One-time products `donate_1`, `donate_5`, `donate_10`, `donate_20` all active
- [ ] License tester email confirmed under Setup > License testing

---

## 3. Railway Environment Verification

Before testing:

```bash
curl https://freaklete-production.up.railway.app/api/health
```

- Expected response: `{"status":"healthy"}`
- If HTTP 503: check Railway deployment logs for migration errors or startup exceptions
- Verify via Railway dashboard that all required env vars are set (see `docs/PRODUCTION_BACKEND_CHECKLIST.md`)

---

## 4. AAB Upload

1. Open Play Console → app → Testing → Internal testing
2. Create a new release
3. Upload: `bin/Release/net10.0-android/publish/com.mert.freaklete-Signed.aab`
4. Add release notes (optional for internal track)
5. Save and roll out to internal testing
6. Confirm `versionCode` and `versionName` match what is set in `FreakLete.csproj`

---

## 5. Tester Install Steps

1. On the tester's device: open Play Store, search for FreakLete
   (The app may not appear publicly — use the opt-in link from Play Console)
2. Navigate to: Play Console → Internal testing track → copy tester opt-in URL
3. Open the opt-in URL on the device, join testing, then install from Play Store
4. Verify the installed version code/name matches the uploaded AAB

---

## 6. Auth & Backend Connectivity

- [ ] Register a new account — confirm request hits production backend (`https://freaklete-production.up.railway.app`)
- [ ] Login with the new account
- [ ] Verify JWT token persists across app restart
- [ ] Open any data-loading screen (e.g., Profile, Workouts) — confirm no 401 or network errors

---

## 7. Billing Purchase Tests

Run all billing steps with the license tester account. License testers receive PURCHASED responses without real charges.

### 7.1 Billing Status Load

- [ ] Open Settings
- [ ] Current plan card loads without error
- [ ] Shows Free plan by default

### 7.2 Monthly Subscription

- [ ] Tap Subscribe → select Monthly plan
- [ ] Google Play purchase sheet appears with price: 3.00 USD/month
- [ ] Complete purchase
- [ ] **Expected:** purchase success toast shown
- [ ] **Expected:** Settings plan card updates to Premium with renewal date

### 7.3 Annual Subscription

- [ ] Cancel the monthly subscription (or use a separate test account)
- [ ] Tap Subscribe → select Annual plan
- [ ] Google Play purchase sheet appears with price: 30.00 USD/year
- [ ] Complete purchase
- [ ] **Expected:** purchase success toast shown
- [ ] **Expected:** Settings plan card updates to Premium with renewal date

### 7.4 Restore Purchases

- [ ] Uninstall and reinstall the app (or clear app data)
- [ ] Login with the same account that purchased a subscription
- [ ] Tap Restore Purchases in Settings
- [ ] **Expected:** entitlement restored; plan card shows Premium

### 7.5 Manage Subscription Deep Link

- [ ] While on Premium, tap Manage Subscription in Settings
- [ ] **Expected:** Google Play subscriptions page opens in external browser
- [ ] **Expected:** freaklete_premium subscription visible

### 7.6 Donation Purchases

Test each denomination with a fresh license tester account or after confirming consume completed:

| Product     | Expected price | Steps |
|---|---|---|
| `donate_1`  | 1.00 USD | Tap Donate → $1 → complete → success toast |
| `donate_5`  | 5.00 USD | Tap Donate → $5 → complete → success toast |
| `donate_10` | 10.00 USD | Tap Donate → $10 → complete → success toast |
| `donate_20` | 20.00 USD | Tap Donate → $20 → complete → success toast |

For each:
- [ ] Purchase sheet shows correct price
- [ ] Donation completes successfully
- [ ] Success toast appears
- [ ] No crash; app remains usable after donation

### 7.7 Cancelled / Failed Purchase

- [ ] Initiate a subscription or donation purchase
- [ ] Tap back or cancel before confirming
- [ ] **Expected:** no crash; graceful "cancelled" or silent return to Settings
- [ ] No stuck loading state

### 7.8 Billing Unavailable

- [ ] Disable network or test on a non-Android platform path if available
- [ ] **Expected:** billing-dependent buttons show a graceful unavailable message
- [ ] **Expected:** no crash

---

## 8. Post-Purchase Backend Sync Verification

After each subscription purchase:

- [ ] Backend sync endpoint is called automatically by the app
- [ ] Settings plan card shows correct plan and renewal date — confirms backend sync returned `verified`/`completed`
- [ ] FreakAI usage card shows Premium plan (Unlimited) after subscription
- [ ] FreakAI quota card shows Free plan with quota limits before subscription

Verifying backend sync without exposing secrets:
- Check Railway deployment logs for the billing sync request (no purchase token needed in logs, just HTTP status)
- Confirm Settings UI reflects the correct plan state after sync

---

## 9. Rollback Plan

If a critical issue is found during internal testing:

1. Do not promote the release to closed/open testing
2. Fix the issue on the `main` branch
3. Bump `versionCode` (required for a new upload to Play Console)
4. Rebuild the AAB: `dotnet publish ./FreakLete.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=aab`
5. Upload the new AAB to a new internal testing release
6. Re-run this checklist

---

## 10. Evidence to Capture

Capture the following during the internal testing run. Do not include purchase tokens, service account JSON, or Railway secrets.

| Evidence item | Why |
|---|---|
| Screenshot: Settings plan card showing **Free** before any purchase | Pre-purchase baseline |
| Screenshot: Google Play purchase sheet with correct product name and price | Confirms product is active and price is correct |
| Screenshot: Settings plan card showing **Premium** with renewal date after subscription | Confirms backend sync worked |
| Screenshot: Donate purchase sheet showing each denomination's price | Confirms donation products are active |
| Screenshot: Donate success toast | Confirms consume completed |
| Screenshot: Restore purchases → plan card update | Confirms restore flow works |
| Railway log snippet: billing sync HTTP 200 (no secrets visible) | Confirms server-side verification |
| Play Console: license tester order history showing test purchases | Confirms purchases registered in Play Console |
| Screen recording: cancel flow — no crash, no stuck state | Confirms graceful cancellation handling |

---

## 11. Exit Criteria

Internal testing is considered complete when:

- [ ] All billing purchase tests passed with license tester account
- [ ] Backend sync confirmed for at least one subscription purchase
- [ ] Restore purchases works
- [ ] All donation denominations tested
- [ ] No crashes observed in any tested flow
- [ ] Settings plan card correctly reflects plan state after each purchase and restore
- [ ] FreakAI usage card reflects premium entitlement after subscription
