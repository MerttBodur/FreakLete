# FreakLete Privacy Policy

**Last updated:** April 2026

> **Note:** This document is a draft prepared for Google Play release compliance. It must be hosted at a publicly accessible URL (e.g., `https://freaklete.app/privacy`) before being submitted to Play Console. This file is not a substitute for a hosted web page.

---

## 1. Introduction

FreakLete ("we", "our", "the app") is a fitness and athletic performance tracking application developed by Mert Bodur. This Privacy Policy explains what data we collect, how we use it, who we share it with, and your rights regarding your personal information.

We do not sell personal data to third parties.

---

## 2. Data We Collect

### 2.1 Account Data
- **First name, last name, email address** — collected at registration to create and identify your account.
- **Encrypted password** — stored as a hashed credential; we never store or transmit plaintext passwords.

### 2.2 Profile and Fitness Data
When you complete your athlete or coach profile, you may provide:
- Date of birth
- Body weight (kg)
- Height (cm)
- Body fat percentage
- Sex
- Sport and position
- Gym experience level
- Training preferences (days/week, session duration, equipment, goals, dietary preference)
- Physical limitations or injury history (coach profile fields)

All of this data is provided voluntarily and is used solely to personalise your in-app experience and AI coaching responses.

### 2.3 Workout and Performance Data
- Workout sessions: name, date, exercises, sets, reps, weight, RIR, rest periods, and tracking metrics
- Personal records (PRs): exercise name, category, weight, reps, and related metrics
- Athletic performance records: jump height, ground contact time, sprint data, and other movement-specific values
- Movement goals: target values per movement

### 2.4 AI and Chat Data
- Messages you send to FreakAI are processed through Google Gemini API to generate responses.
- Relevant profile and performance context (fitness metrics, sport background, workout history) may be included in the prompt sent to Gemini to improve response quality.
- We do not train AI models on your personal data.

### 2.5 Nutrition and Lifestyle Data
- Dietary preference (e.g., vegetarian, no preference) and primary/secondary training goals, if provided via the coach profile form.

### 2.6 Billing and Purchase Metadata
- Google Play handles all payment processing. We receive only the purchase token, order ID, product ID, and purchase state from Google Play Billing — we do not receive or store card numbers or full payment details.
- This metadata is used to verify your subscription or donation and to grant access to premium features.

### 2.7 Device and Diagnostic Data
- We do not intentionally collect device identifiers (GAID/advertising ID, IMEI).
- Standard HTTP request logs on the backend (Railway) may include your IP address and timestamps for security and stability purposes. These are not correlated to your fitness data.

---

## 3. How We Use Your Data

| Purpose | Data used |
|---|---|
| Provide the core app features | Account, profile, workouts, PRs, athletic performance, movement goals |
| Personalise AI coaching responses | Profile, sport, goals, fitness data, chat messages |
| Verify purchases and grant premium access | Google Play purchase token / order metadata |
| Account security and authentication | Email, hashed password, JWT tokens |
| Diagnose backend errors and maintain uptime | Request logs (IP, timestamps) |

---

## 4. Third Parties We Share Data With

We share data only with the following service providers, strictly to operate the app:

| Provider | Purpose | Data shared |
|---|---|---|
| **Google Play Billing** | In-app purchase processing | Purchase token, product ID, order ID (returned to our backend) |
| **Google Gemini API** | AI coaching and guidance responses | Profile context, user chat messages |
| **Railway** | Backend API hosting | All server-side data at rest and in transit |
| **PostgreSQL (Railway-hosted)** | Database persistence | All user and app data |

We do not share your data with advertisers, data brokers, or analytics platforms.

---

## 5. Data Security

- All communication between the app and our backend uses HTTPS (TLS).
- Passwords are stored as bcrypt hashes.
- JWT tokens are stored in secure device storage (Android Keystore-backed).
- We apply standard access controls on our backend infrastructure.

---

## 6. Data Retention

We retain your data as long as your account is active. If you delete your account:
- Your profile, workouts, PRs, athletic performance records, movement goals, and AI chat history are deleted.
- Billing and transaction records (purchase tokens, order IDs) may be retained for a limited period as required for legal, tax, fraud prevention, or compliance purposes.

---

## 7. Your Rights

You have the right to:
- **Access** the data we hold about you.
- **Correct** inaccurate profile data at any time within the app.
- **Delete** your account and associated data (see Section 8).
- **Object** to certain uses of your data.

To exercise any of these rights, contact us at **support@freaklete.app**.

---

## 8. Account and Data Deletion

You can delete your account in two ways:

1. **In-app:** Profile tab > Delete Account.
2. **Web form:** Submit a deletion request at `https://freaklete.app/account-deletion` (must be live before Play Store submission).

Upon deletion, all personal data listed in Section 6 is removed. Retained billing/legal records are purged after the applicable retention period.

---

## 9. Children's Privacy

FreakLete is not directed at children under the age of 13 (or the applicable age of digital consent in your jurisdiction). We do not knowingly collect personal data from children.

---

## 10. Changes to This Policy

We may update this policy periodically. Material changes will be communicated via in-app notice or email. Continued use of the app after changes constitutes acceptance.

---

## 11. Contact

For privacy questions or data requests:

**Email:** support@freaklete.app
**Developer:** Mert Bodur
**GitHub:** https://github.com/MerttBodur

---

*This policy should be reviewed by a qualified legal professional before production use. It is provided as a compliance preparation artifact.*
