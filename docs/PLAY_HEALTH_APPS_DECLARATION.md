# FreakLete — Google Play Health Apps Declaration Worksheet

**Purpose:** Reference answers for the Google Play Console Health apps policy declaration.
**Status:** Draft — verify against the live Play Console UI before submission, as Google periodically updates declaration options and requirements.

---

## 1. App Health and Fitness Scope

FreakLete is a fitness tracking and athletic performance application. Its health-related functionality includes:

| Feature | Description |
|---|---|
| Workout logging | Users log gym sessions with exercises, sets, reps, weight, and effort metrics |
| Strength calculations | 1RM estimation, rep-range output based on user-input loads |
| Athletic performance tracking | Jump height, ground contact time (RSI), sprint metrics, and other movement-specific values |
| Body composition input | User-entered weight (kg), height (cm), body fat percentage — used for FFMI calculation and AI context |
| Movement goals | User-defined targets for specific movements |
| Nutrition/dietary preference | Dietary preference (e.g., vegetarian) collected in the coach profile form |
| AI fitness coaching | FreakAI generates personalised fitness and nutrition guidance based on user profile and chat |

---

## 2. Medical Disclaimer

FreakLete does **not** make medical diagnoses, provide medical treatment recommendations, or claim to replace professional medical or clinical advice.

Recommended disclaimer copy (suitable for use in app store description, settings page, or onboarding):

> *FreakLete is for fitness and wellness guidance only and is not medical advice. Consult a qualified healthcare professional before beginning any exercise programme or making changes to your nutrition.*

This disclaimer should appear in:
- Google Play Store long description
- Potentially in the app's onboarding or settings if health-specific features (body fat, injury history) are prominent

---

## 3. No Clinical or Therapeutic Claims

The app does not:
- Diagnose medical conditions
- Recommend medications or clinical treatments
- Process clinical health data (e.g., blood glucose, ECG, blood pressure)
- Connect to medical devices or health sensors
- Make claims about treating or curing any disease or condition

---

## 4. Suggested Play Console Health Declaration Selections

> **Important:** The exact options available in Play Console may differ from those listed here. The following are based on standard Play Store policy categories as of early 2026. Verify each selection against the live form.

### Likely applicable categories:

| Category | Applicable? | Rationale |
|---|---|---|
| **Fitness** | Yes | Core feature: workout logging, strength calculations, RSI, athletic performance |
| **Nutrition and weight management** | Yes (partial) | Dietary preference collected; FFMI uses body weight + body fat; AI provides nutrition guidance |
| **Health-related information provided by the user** | Yes | Body weight, height, body fat, injury history, physical limitations entered by user |
| **Mental health** | No | No mental health features |
| **Medications** | No | No medication tracking |
| **Medical devices** | No | No sensor or device integration |

### Policy compliance checkpoints:

- [ ] App does not make disease diagnosis claims
- [ ] App does not claim to treat or cure any medical condition
- [ ] Medical disclaimer included in store description
- [ ] User-provided health data is not shared beyond what is declared in Data Safety
- [ ] AI coaching responses do not substitute for medical advice (disclaim in UI if needed)

---

## 5. Sensitive Data Handling Notes

The following data fields are health-adjacent and should be handled carefully:

| Field | Sensitivity | Handling |
|---|---|---|
| Body fat percentage | Sensitive | User-entered; used for FFMI and AI context; not displayed publicly |
| Injury history / physical limitations | Sensitive | User-entered in coach profile; used only for AI coaching context; not shared beyond Gemini API |
| Sex | Sensitive | Used for FFMI normalisation and AI coaching; not shared beyond Gemini API |
| Date of birth / age | Moderate | Used for profile context; not displayed publicly |

---

## 6. Recommended Store Description Medical Disclaimer

Add the following line to the Play Store long description (suggested placement: end of description, before feature list):

> *FreakLete is a fitness tracking app and is not a medical device. It is not intended to diagnose, treat, cure, or prevent any disease or medical condition. Always consult a healthcare professional before starting a new exercise or nutrition programme.*
