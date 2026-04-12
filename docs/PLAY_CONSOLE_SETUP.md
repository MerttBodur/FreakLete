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

## 7. After Setup

Once all products are active and testers are configured:

1. Upload signed AAB to internal testing track
2. Complete a test subscription purchase with a license tester account
3. Verify backend receives and processes the purchase sync
4. Confirm Settings shows Premium plan and renewal date
5. Test a donation purchase — confirm consume and success toast
6. Test Restore Purchases — re-grants entitlement from existing subscription
