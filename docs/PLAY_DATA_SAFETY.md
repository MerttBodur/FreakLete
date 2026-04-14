# FreakLete — Google Play Data Safety Worksheet

**Purpose:** Reference answers for the Google Play Console Data Safety form.
**Status:** Draft — verify final answers against the live Play Console UI before submission, as Google may update form options.

All production API traffic uses HTTPS. "Encrypted in transit: Yes" applies to all categories below.

---

## Section 1: Does your app collect or share any of the required user data types?

**Yes** — the app collects and shares data as described below.

---

## Section 2: Is all of the user data collected by your app encrypted in transit?

**Yes.** The production API endpoint (`https://freaklete-production.up.railway.app`) uses HTTPS/TLS for all traffic.

---

## Section 3: Do you provide a way for users to request that their data is deleted?

**Yes.** Users can delete their account in-app (Profile > Delete Account) and via a web form at `https://freaklete.app/account-deletion`.

---

## Section 4: Data Types Collected

### 4.1 Personal Info

| Field | Value |
|---|---|
| **Data type** | Name, Email address, Profile photo (user-provided image) |
| **Collected?** | Yes |
| **Shared?** | No (not shared with third parties; stored in our backend only) |
| **Purpose** | Account creation and authentication |
| **Encrypted in transit** | Yes |
| **User can request deletion** | Yes |

### 4.2 Health and Fitness

| Field | Value |
|---|---|
| **Data type** | Fitness info (workouts, exercises, PRs, athletic performance, movement goals), Health info (body weight, height, body fat percentage, date of birth, sex) |
| **Collected?** | Yes |
| **Shared?** | Shared with Google Gemini API (service provider) for AI coaching responses |
| **Purpose** | Core app functionality (workout tracking, calculations); personalising AI coaching |
| **Encrypted in transit** | Yes |
| **User can request deletion** | Yes |

> **Note on Gemini sharing:** Profile metrics (weight, height, body fat, sex, sport, goals) and workout context are included in prompts sent to Google Gemini API to generate AI coaching responses. In the Play Console form, select "Shared with service provider" for this category.

### 4.3 App Activity

| Field | Value |
|---|---|
| **Data type** | App interactions (in-app navigation, feature usage), User-generated content (FreakAI chat messages) |
| **Collected?** | Yes |
| **Shared?** | FreakAI chat messages shared with Google Gemini API (service provider) |
| **Purpose** | Core app functionality; AI response generation |
| **Encrypted in transit** | Yes |
| **User can request deletion** | Yes |

### 4.4 Financial Info

| Field | Value |
|---|---|
| **Data type** | Purchase history (Google Play in-app purchases and subscriptions) |
| **Collected?** | Yes — we receive purchase token, order ID, product ID, and purchase state from Google Play |
| **Shared?** | Not shared externally (Google Play handles payment processing) |
| **Purpose** | Verifying subscription/donation status and granting premium access |
| **Encrypted in transit** | Yes |
| **User can request deletion** | Billing records may be retained as required by law (see Account Deletion doc) |

> **Note:** The app uses Google Play Billing. Actual payment processing (card numbers, bank details) is handled entirely by Google and never passes through our servers.

### 4.5 Device or Other IDs

| Field | Value |
|---|---|
| **Data type** | Device or other identifiers |
| **Collected?** | Not intentionally collected. We do not collect GAID, IMEI, or advertising IDs. Standard server-side HTTP logs may temporarily capture IP addresses for security purposes. |
| **Shared?** | No |
| **Purpose** | N/A |

---

## Section 5: Data Safety Summary Table

| Category | Collected | Shared | Purpose | Encrypted in Transit | Deletable |
|---|---|---|---|---|---|
| Name | Yes | No | Account auth | Yes | Yes |
| Email | Yes | No | Account auth | Yes | Yes |
| Profile photo (user-provided image) | Yes | No | Profile avatar personalisation | Yes | Yes |
| Body metrics (weight, height, body fat, sex, DOB) | Yes | With Gemini (service provider) | App features + AI coaching | Yes | Yes |
| Fitness data (workouts, exercises, PRs, athletic perf) | Yes | With Gemini (service provider) | App features + AI coaching | Yes | Yes |
| Training preferences/goals | Yes | With Gemini (service provider) | AI coaching personalisation | Yes | Yes |
| AI chat messages | Yes | With Gemini (service provider) | AI response generation | Yes | Yes |
| Purchase token / order metadata | Yes | No | Subscription/donation verification | Yes | Retained per legal obligation |
| Device IDs | No | No | — | — | — |

---

## Notes for Play Console Submission

1. When selecting "Shared with third party", choose "Service provider" (not "Other") for Google Gemini API — we share data to provide a feature, not for independent use by the provider.
2. For health/fitness data, you may need to select the Health and fitness > Fitness info and Health and fitness > Health info sub-categories separately depending on the form version.
3. The Data Safety section is public-facing. Verify all answers are consistent with what the app actually collects before publishing.
4. Account deletion web URL must be live before declaring "user can request deletion" via web.
