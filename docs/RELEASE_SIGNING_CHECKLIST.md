# FreakLete — Release Signing Checklist

**Purpose:** Verify the signed AAB is production-ready before uploading to Play Console.
**Status:** Manual — signing config requires secrets stored outside the repository.

> **Security:** Do NOT commit keystore files, `.jks`, `.pfx`, `.pem`, passwords, or key aliases to this repository. `.gitignore` already covers these patterns.

---

## 1. Upload Key vs. Play App Signing

Google Play requires **Play App Signing** for all new apps:

| Concept | Description |
|---|---|
| **App signing key** | Google-managed; used to sign the APK delivered to devices; stored by Google |
| **Upload key** | Your key; used to sign the AAB you upload to Play Console; Google re-signs with the app signing key |
| **Keystore file** | The file containing your upload key; must be stored securely outside the repo |

- [ ] Play App Signing is enabled in Play Console (Dashboard > App signing)
- [ ] Upload certificate fingerprint confirmed in Play Console (SHA-256 shown under App integrity)

---

## 2. Keystore Storage

- [ ] Keystore file is stored in a secure location **outside the repository** (e.g., local machine path not under `c:\Users\mert_\source\GymTracker\`, or a password manager / secure vault)
- [ ] Keystore is **not tracked** by git: `git ls-files | grep -iE "(jks|keystore|pfx|pem)"` returns no results
- [ ] `.gitignore` covers signing artifacts — confirmed: `*.keystore`, `*.jks`, `*.pfx`, `*.pem`, `android-keystore.properties` are all excluded

---

## 3. Signing Risk — Current AAB

> **Important:** The AAB currently produced by `dotnet publish` without explicit signing parameters is signed with the **debug/default keystore** (`.android/debug.keystore`). A debug-signed AAB will be rejected by Play Console.
>
> Before uploading to Play Console, the AAB must be signed with your upload key using the signing parameters below.

---

## 4. Producing a Release-Signed AAB

Add the following MSBuild properties to the publish command. **Replace placeholders with actual values; never commit actual values.**

```bash
dotnet publish .\FreakLete.csproj \
  -f net10.0-android \
  -c Release \
  -p:AndroidPackageFormat=aab \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=<path-to-your-keystore.jks> \
  -p:AndroidSigningStorePass=<keystore-password> \
  -p:AndroidSigningKeyAlias=<key-alias> \
  -p:AndroidSigningKeyPass=<key-password> \
  -v:m
```

Alternatively, set these via environment variables or a properties file that is excluded from git (e.g., `android-keystore.properties` — already in `.gitignore`).

**Expected output path:**
```
bin/Release/net10.0-android/publish/com.mert.freaklete-Signed.aab
```

- [ ] Signed AAB produced with upload key (not debug keystore)
- [ ] AAB size is approximately 31 MB (within ±5 MB of reference build)

---

## 5. Verifying the Signature

After producing the signed AAB, verify it was signed with the correct key:

```bash
# Extract and inspect signing certificate from AAB
# (AAB is a zip — inspect META-INF/ entries)
unzip -p com.mert.freaklete-Signed.aab META-INF/*.RSA | keytool -printcert
```

- [ ] Certificate fingerprint matches the upload certificate registered in Play Console
- [ ] Certificate is NOT the debug certificate (`CN=Android Debug, O=Android, C=US`)

---

## 6. Play Console Upload Verification

- [ ] AAB uploaded to internal testing track without error
- [ ] Play Console shows the correct `versionCode` and `versionName` for the uploaded build
- [ ] No signing-related rejection from Play Console (wrong certificate, duplicate versionCode, etc.)

---

## 7. Disaster Recovery

If the upload key is lost:

1. Google Play allows requesting a key reset once per lifetime — go to Play Console > App integrity > Request key upgrade
2. The process requires identity verification and may take several days
3. Until resolved, no new releases can be uploaded

**Mitigations:**
- Store the keystore file in at least two secure locations (e.g., encrypted cloud storage + local encrypted backup)
- Record the key alias and store the keystore password in a password manager
- Do not rely on the `.android/debug.keystore` — it is not suitable for production

---

## 8. Version Metadata Reference

Current values (`FreakLete.csproj`):

| Property | Value | Notes |
|---|---|---|
| `ApplicationDisplayVersion` | `1.0` | Shown to users in Play Store |
| `ApplicationVersion` | `1` | Maps to Android `versionCode`; **must increment on every Play Console upload** |

> First upload uses `versionCode 1`. Every subsequent upload to any track (internal, closed, production) requires a higher `versionCode`. Duplicate `versionCode` uploads are rejected by Play Console.
