# FreakLete Account and Data Deletion

**Prepared for:** Google Play Store compliance
**Status:** Draft — requires a live hosted web page before Play Console submission

> **Important:** Google Play requires apps that allow account creation to provide a publicly accessible web URL where users can request account and data deletion. The URL `https://freaklete.app/account-deletion` must be live and functional before this URL is entered into the Play Console app data deletion settings. This document describes what that page should contain and is **not a substitute** for the hosted web page.

---

## Deletion Options

### Option 1: Delete from within the app (recommended)

1. Open FreakLete.
2. Navigate to the **Profile** tab.
3. Scroll to the bottom and tap **Delete Account**.
4. Confirm the action when prompted.

Your account and all associated data are deleted immediately.

### Option 2: Request deletion by email or web form

If you cannot access the app or prefer a web-based method:

- **Web form (required for Play Store):** `https://freaklete.app/account-deletion`
  *(This URL must be published and functional before Play Console submission.)*
- **Email:** support@freaklete.app — include "Account Deletion Request" in the subject line and your registered email address in the body.

We will process deletion requests within **30 days** of receipt.

---

## What Data Is Deleted

When your account is deleted (via either method), the following data is permanently removed:

| Data category | Deleted? |
|---|---|
| Account credentials (email, hashed password) | Yes |
| Profile data (name, DOB, weight, height, sex, sport, experience level, coach fields) | Yes |
| Profile photo | Yes |
| Workout sessions and exercise entries | Yes |
| Personal records (PRs) | Yes |
| Athletic performance records | Yes |
| Movement goals | Yes |
| FreakAI chat history (if stored) | Yes |
| Training programs | Yes |

---

## Data That May Be Retained

A limited set of records may be retained after deletion as required by law or for legitimate business purposes:

| Data category | Retention reason | Retention period |
|---|---|---|
| Google Play purchase tokens and order IDs | Legal, tax, fraud prevention | As required by applicable law (typically 7 years) |
| Anonymised aggregate analytics (if any) | Product improvement — not linked to your account | Indefinitely (not personal data) |
| Security/abuse logs | Fraud prevention and compliance | Up to 90 days |

---

## Play Console Setup Reference

When submitting to Google Play, enter the following in the **App content > Data safety > Account deletion** section:

- **App supports account deletion:** Yes
- **In-app deletion:** Yes (Profile > Delete Account)
- **Web URL for deletion requests:** `https://freaklete.app/account-deletion`

Ensure the web URL is live and returns a functional deletion request form before entering it in Play Console.

---

## Contact

**Email:** support@freaklete.app
**Developer:** Mert Bodur
