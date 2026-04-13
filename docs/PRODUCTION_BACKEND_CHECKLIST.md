# FreakLete — Production Backend Environment Checklist

**Purpose:** Required environment variables and readiness steps for the Railway production deployment.
**Status:** Manual — these must be set in Railway dashboard before the app can serve production traffic.

> **Security:** Do NOT commit actual secret values to this repository. This document lists variable names only.

---

## 1. Required Environment Variables

Set all of the following in the Railway project environment (Variables tab):

### Database

| Variable | Expected value | Notes |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Railway Postgres plugin provides this automatically if linked |

### JWT Authentication

| Variable | Expected value | Notes |
|---|---|---|
| `Jwt__Key` | Random secret, ≥ 32 UTF-8 bytes | **Must not be a placeholder.** `Program.cs` validates at startup and throws if value is a known placeholder or shorter than 32 bytes |
| `Jwt__Issuer` | `freaklete` (or your configured value) | Must match the value used when tokens were originally issued |
| `Jwt__Audience` | `freaklete-users` (or your configured value) | Must match Android client expectations |

> **Startup validation:** `Program.cs` rejects placeholder values `OVERRIDE_VIA_ENVIRONMENT_OR_APPSETTINGS` and `OVERRIDE_VIA_ENVIRONMENT_VARIABLE`, and rejects any key shorter than 32 bytes. A misconfigured `Jwt__Key` will crash the server at startup.

### Gemini AI

| Variable | Expected value | Notes |
|---|---|---|
| `Gemini__ApiKey` | Google AI Studio API key | Required; server throws `InvalidOperationException` at startup if absent |
| `Gemini__Model` | `gemini-2.5-flash-lite` | Optional; defaults to `gemini-2.5-flash-lite` if unset |

### Google Play Billing

| Variable | Expected value | Notes |
|---|---|---|
| `GooglePlay__PackageName` | `com.mert.freaklete` | Must match the Play Console app package exactly |
| `GooglePlay__ServiceAccountJsonBase64` | Base64-encoded service account JSON | Service account must have **Pub/Sub Viewer** + **Order Management** roles in Play Console |
| `GooglePlay__RealTimeDeveloperNotificationSecret` | Random secret ≥ 32 chars | RTDN push endpoint protection. Missing → endpoint returns 503. Generate with `openssl rand -hex 32`. |

> **How to generate `GooglePlay__ServiceAccountJsonBase64`:**
> 1. In Google Play Console: Setup > API access > Link to a Google Cloud project
> 2. Create a service account with required roles
> 3. Download the JSON key file
> 4. Run: `base64 -w 0 service-account.json` (Linux/Mac) or equivalent PowerShell command
> 5. Set the output as the env var value
> 6. Delete the local JSON file; never commit it

### Runtime Environment

| Variable | Expected value | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Controls exception handler, OpenAPI endpoint visibility, and health check detail level |

---

## 1b. Production Hosting Hardening (Phase 4)

### Forwarded Headers

`Program.cs` enables `UseForwardedHeaders` in all non-Testing environments:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

- **X-Forwarded-For**: Real client IP is reflected in `HttpContext.Connection.RemoteIpAddress`. Login attempt rate-limiting partitions on this value.
- **X-Forwarded-Proto**: Real scheme (`https`) is reflected in `HttpContext.Request.IsHttps`. HSTS and HTTPS redirect behave correctly.
- **KnownNetworks**: Default (loopback 127.0.0.0/8 trusted). Railway's internal proxy connects via loopback — no additional config required. If Railway uses a non-loopback proxy address, add it: `options.KnownProxies.Add(IPAddress.Parse("<ip>"))`. Clearing both lists trusts all proxies (acceptable behind Railway's controlled network, but less restrictive).

### HSTS

`UseHsts()` is enabled in all non-Development, non-Testing environments. This adds `Strict-Transport-Security` to HTTPS responses, instructing browsers to use HTTPS for all subsequent requests.

### RTDN Query Secret (Pub/Sub Direct Push)

The RTDN endpoint accepts the shared secret in two ways:

| Method | Header | Example |
|---|---|---|
| Header (preferred) | `X-FreakLete-RTDN-Secret: <secret>` | Cloud Run proxy or NGINX injects header |
| Query param (MVP) | `?secret=<secret>` | Configure Pub/Sub push URL directly |

**Pub/Sub push URL with query secret (MVP):**
```
https://freaklete-production.up.railway.app/api/billing/googleplay/rtdn?secret=<RTDN_SECRET>
```

> **Risk note:** Query params appear in access logs and server-side request logging. The header approach (via Cloud Run proxy) is more secure for production. Use query param only as MVP until a proxy is in place.

---

## 2. Auto-Migration Behavior

`Program.cs` runs `db.Database.MigrateAsync()` on startup for all non-Testing environments:

```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    ...
}
```

This means:
- All pending EF Core migrations are applied automatically when the server starts
- The `BillingRawPayloadMaxLength` migration and any subsequent migrations are applied without manual intervention
- If a migration fails, the server will fail to start — check Railway deployment logs

**Still recommended before each release:**
- Verify migration applied cleanly by checking `/api/health` after deploy
- Review Railway deployment logs for migration output
- If a migration contains destructive DDL (column drops, type changes), test against a staging DB first

---

## 3. Health Check Endpoint

```
GET https://freaklete-production.up.railway.app/api/health
```

**Expected response (production — ASPNETCORE_ENVIRONMENT=Production):**

```json
{ "status": "healthy" }
```

HTTP 200 = healthy. HTTP 503 = unhealthy (DB unreachable or pending migrations).

**Note:** In production, the health endpoint returns only `status`. Migration details and error messages are intentionally suppressed. This is correct behavior — do not add config or secret details to this response.

**Quick check command:**
```bash
curl https://freaklete-production.up.railway.app/api/health
```

---

## 4. Billing Verification Readiness

Google Play purchase verification requires:

1. `GooglePlay__PackageName` = `com.mert.freaklete` — exact match with Play Console
2. `GooglePlay__ServiceAccountJsonBase64` — valid, non-expired service account key
3. Service account has the correct Play Console roles (Order Management at minimum)
4. Products `freaklete_premium`, `donate_1/5/10/20` are **Active** in Play Console

> There is no diagnostic endpoint that exposes billing config status to external callers. Billing readiness is verified by completing a test purchase with a license tester account on the internal track.

---

## 5. Pre-Release Verification Sequence

Run this sequence before each production release:

- [ ] All required env vars set in Railway (including `GooglePlay__RealTimeDeveloperNotificationSecret`)
- [ ] `GET /api/health` returns `{ "status": "healthy" }` with HTTP 200
- [ ] Railway deployment logs show no migration errors
- [ ] Railway deployment logs show no startup exceptions (Jwt, Gemini, DB)
- [ ] `X-Forwarded-For` and `X-Forwarded-Proto` headers forwarded correctly by Railway proxy (verify via login attempt log or `/api/health` scheme)
- [ ] Test purchase with license tester confirms billing sync reaches backend
- [ ] Entitlement endpoint returns correct plan after sync

---

## 6. What Is NOT in This Checklist

- Release signing (Play App Signing — managed in Play Console, not Railway)
- `versionCode` / `versionName` bump (in `FreakLete.csproj` before AAB build)
- Play Console product creation (see `docs/PLAY_CONSOLE_SETUP.md`)
- RTDN / PubSub infrastructure setup (endpoint shipped; Pub/Sub topic/subscription must be wired in Google Cloud Console — see `docs/PLAY_CONSOLE_SETUP.md` §7)
