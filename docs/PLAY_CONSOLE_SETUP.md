# FreakLete — Google Play Console Product Setup Checklist

**Purpose:** Step-by-step reference for creating Play Console products before the first release.
**Status:** Manual — these items must be completed in the Play Console UI. Nothing here is automated.

---

## 1. App Package Name

All products must be created under the correct application:

```
com.mert.freaklete
```

This value must match `GooglePlay__PackageName` in the production backend environment exactly.

---

## 2. Subscription Product

### Product ID

```
freaklete_premium
```

This ID is used in:
- Android client: billing SKU lookup and purchase initiation
- Backend allowlist: `GooglePlayBillingService.cs` and entitlement logic
- `PLAY_DATA_SAFETY.md` declarations

**Do not rename this ID.** Any mismatch between Play Console, Android client, and backend will break purchase verification.

### Base Plans

Create two base plans under `freaklete_premium`:

| Base Plan ID | Billing period | Price   | Notes                              |
|---|---|---|---|
| `monthly`    | 1 month        | 3.00 USD | Auto-renewing; cancellable anytime |
| `annual`     | 12 months      | 30.00 USD | ~17% saving vs. monthly            |

**Critical:** The base plan IDs `monthly` and `annual` must match exactly what is used in the Android client and any backend base plan verification logic. A mismatch results in purchase verification failure.

### Required Play Console steps (subscription)

- [ ] Navigate to **Monetize > Products > Subscriptions**
- [ ] Create subscription with product ID `freaklete_premium`
- [ ] Add base plan `monthly` — billing period: 1 month, price: 3.00 USD
- [ ] Add base plan `annual` — billing period: 12 months, price: 30.00 USD
- [ ] Set status to **Active** for both base plans
- [ ] Add product to the **internal testing track** before promoting to production
- [ ] Verify base plan IDs match Android client constants

---

## 3. One-Time Products (Consumable Donations)

### Product IDs and Prices

| Product ID  | Price    | Type        |
|---|---|---|
| `donate_1`  | 1.00 USD | Consumable  |
| `donate_5`  | 5.00 USD | Consumable  |
| `donate_10` | 10.00 USD | Consumable |
| `donate_20` | 20.00 USD | Consumable |

These products are consumed immediately after purchase. A user can purchase them multiple times.

### Required Play Console steps (one-time products)

- [ ] Navigate to **Monetize > Products > In-app products**
- [ ] Create each product with the exact product ID above
- [ ] Set type to **Managed product** (Google Play handles consumable lifecycle at the app level via `consumeAsync`)
- [ ] Set price for each product
- [ ] Activate each product

---

## 4. License Testers

For internal testing without real charges:

- [ ] Navigate to **Setup > License testing**
- [ ] Add all test accounts to the **License testers** list
- [ ] License testers receive PURCHASED responses without real billing
- [ ] Testers must use a Google account registered as a tester on the Play Console

---

## 5. Internal Testing Track

- [ ] Upload a signed AAB to the **Internal testing** track before testing purchases
- [ ] Add internal testers to the track
- [ ] Products must be **Active** for testers to see them in the purchase sheet
- [ ] Confirm purchase sheet shows correct prices before promoting to production

---

## 6. Contract Summary

The following IDs are load-bearing. Any rename requires a coordinated change across all three surfaces:

| Surface | Subscription ID | Base Plan IDs | Donation IDs |
|---|---|---|---|
| Play Console | `freaklete_premium` | `monthly`, `annual` | `donate_1/5/10/20` |
| Android client | `freaklete_premium` | `monthly`, `annual` | `donate_1/5/10/20` |
| Backend allowlist | `freaklete_premium` | `monthly`, `annual` | `donate_1/5/10/20` |

---

## 7. Real-Time Developer Notifications (RTDN) via Pub/Sub

RTDN delivers server-push subscription lifecycle events (renewal, cancellation, expiry, refund) to the backend without waiting for the next client sync.

### Google Cloud Pub/Sub setup

1. In [Google Cloud Console](https://console.cloud.google.com/):
   - Enable the **Pub/Sub API** for the project linked to Play Console
   - Create a **Topic** (e.g. `play-rtdn`)
   - Grant the Google Play service account `pubsub.topics.publish` permission on that topic
   - Create a **Push Subscription** on the topic:
     - Delivery type: **Push**
     - Endpoint URL: `https://freaklete-production.up.railway.app/api/billing/googleplay/rtdn`
     - Add custom attribute or configure the endpoint URL with a secret query param — however, FreakLete uses a **custom header** instead (see below)

2. Because Pub/Sub push does not natively support custom headers, use one of these approaches:
   - **Option A (recommended):** Append the secret as a URL query parameter and read it from `Request.Query` — update endpoint to check `?secret=...` instead of the header
   - **Option B:** Place a lightweight reverse proxy (Cloud Run, NGINX) in front that injects the `X-FreakLete-RTDN-Secret` header before forwarding to Railway
   - **Option C (current implementation):** Configure Pub/Sub to push to a URL that includes the secret in the path: `...rtdn?key=<secret>` — not supported in the current header-based implementation; choose Option A or B

   > The current implementation reads `X-FreakLete-RTDN-Secret` header. If Google Pub/Sub cannot inject headers, switch to query-parameter validation or use an intermediary.

3. In Play Console:
   - Navigate to **Monetize > Real-time developer notifications**
   - Set the **Pub/Sub topic** to the topic you created
   - Click **Send test notification** and verify the backend returns 200

### Railway environment variable

| Variable | Value |
|---|---|
| `GooglePlay__RealTimeDeveloperNotificationSecret` | Random secret string, ≥ 32 characters |

Generate a secret:
```bash
openssl rand -hex 32
```

Set in Railway dashboard → Variables. Never commit this value.

### Security notes

- Endpoint is public but requires the `X-FreakLete-RTDN-Secret` header to match the configured secret
- Missing secret config → 503 (endpoint disabled, not silently accepting)
- Wrong secret → 401
- Purchase tokens are **never logged** — only a SHA-256 fingerprint is stored in `GooglePlayRtdnEvents`
- Duplicate `messageId` values are idempotent — second call returns 200 without re-processing
- `oneTimeProductNotification` events are silently ignored (200 returned)

### Event table

RTDN events are stored in `GooglePlayRtdnEvents`:

| ProcessingState | Meaning |
|---|---|
| `processed` | Subscription verify succeeded, entitlement updated |
| `verification_failed` | Google Play verify returned null; state set to `verification_failed` |
| `ignored_unknown_token` | Purchase token not in DB; client sync creates the record on next open |
| `duplicate` | `messageId` already processed; no mutation |

---

## 8. After Setup

Once all products are active and testers are configured:

1. Upload signed AAB to internal testing track
2. Complete a test subscription purchase with a license tester account
3. Verify backend receives and processes the purchase sync
4. Confirm Settings shows Premium plan and renewal date
5. Test a donation purchase — confirm consume and success toast
6. Test Restore Purchases — re-grants entitlement from existing subscription
