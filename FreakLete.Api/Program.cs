using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FreakLete.Api.Data;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication — validate key strength at startup
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured. Set via appsettings or Jwt__Key env var.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("Jwt:Issuer is not configured.");
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Audience is not configured.");

var placeholderKeys = new[]
{
    "OVERRIDE_VIA_ENVIRONMENT_OR_APPSETTINGS",
    "OVERRIDE_VIA_ENVIRONMENT_VARIABLE"
};
if (placeholderKeys.Contains(jwtKey, StringComparer.OrdinalIgnoreCase))
    throw new InvalidOperationException("Jwt:Key is still a placeholder value. Set a real secret.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 UTF-8 bytes for HS256.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdStr = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var versionStr = context.Principal?.FindFirstValue("token_version");

                if (!int.TryParse(userIdStr, out var userId) ||
                    !int.TryParse(versionStr, out var tokenVersion))
                {
                    context.Fail("Missing or unparseable token claims.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<FreakLete.Api.Data.AppDbContext>();

                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.TokenVersion })
                    .FirstOrDefaultAsync();

                if (user is null || user.TokenVersion != tokenVersion)
                    context.Fail("Token has been invalidated.");
            }
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<TrainingSummaryService>();
builder.Services.AddScoped<FreakAiToolExecutor>();
builder.Services.AddScoped<FreakAiOrchestrator>();
builder.Services.AddScoped<AthleteProfileService>();
builder.Services.AddScoped<CoachProfileService>();
builder.Services.AddScoped<StarterTemplateSeedService>();
builder.Services.AddScoped<EntitlementService>();
builder.Services.AddScoped<QuotaService>();
builder.Services.AddHttpClient<GooglePlayVerificationService>();

// Gemini AI — key from user-secrets (local) or env var Gemini__ApiKey (Railway)
var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
    ?? throw new InvalidOperationException(
        "Gemini:ApiKey is not configured. " +
        "Local: dotnet user-secrets set \"Gemini:ApiKey\" \"your-key\" --project FreakLete.Api  " +
        "Railway: set Gemini__ApiKey environment variable.");
var geminiOptions = new GeminiOptions
{
    ApiKey = geminiApiKey,
    Model = builder.Configuration["Gemini:Model"] ?? "gemini-2.5-flash-lite"
};
builder.Services.AddSingleton(geminiOptions);
builder.Services.AddHttpClient<GeminiClient>()
    .ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Rate limiting — brute force / credential stuffing protection
// Disabled in Testing environment so integration tests are not throttled.
var isTestEnvironment = builder.Environment.IsEnvironment("Testing");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (ctx, _) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Çok fazla istek gönderildi. Lütfen biraz bekleyin." });
    };

    // login: 5 per minute, partitioned by IP
    options.AddPolicy("auth-login", httpCtx =>
    {
        if (isTestEnvironment)
            return RateLimitPartition.GetNoLimiter("test");

        var ip = httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"login:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // register: 3 per 10 minutes by IP
    options.AddPolicy("auth-register", httpCtx =>
    {
        if (isTestEnvironment)
            return RateLimitPartition.GetNoLimiter("test");

        var ip = httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"register:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // change-password: 5 per 10 minutes by authenticated user id + IP
    options.AddPolicy("auth-change-password", httpCtx =>
    {
        if (isTestEnvironment)
            return RateLimitPartition.GetNoLimiter("test");

        var ip = httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = httpCtx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"change-password:{userId}:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Apply pending migrations on startup (skip in Testing — test fixture owns the lifecycle)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<StarterTemplateSeedService>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { status = "error", message = "An unexpected error occurred." });
    }));
}

// Health check — verifies DB connectivity
app.MapGet("/api/health", async (AppDbContext db, IWebHostEnvironment env) =>
{
    var isDevelopment = env.IsDevelopment();
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var pendingList = (await db.Database.GetPendingMigrationsAsync()).ToList();
        var healthy = canConnect && pendingList.Count == 0;

        if (isDevelopment)
        {
            var body = new
            {
                status = healthy ? "healthy" : "unhealthy",
                database = canConnect,
                pendingMigrations = pendingList.Count,
                migrations = pendingList
            };
            return healthy ? Results.Ok(body) : Results.Json(body, statusCode: 503);
        }
        else
        {
            var body = new { status = healthy ? "healthy" : "unhealthy" };
            return healthy ? Results.Ok(body) : Results.Json(body, statusCode: 503);
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Health check failed");

        if (isDevelopment)
            return Results.Json(new { status = "unhealthy", error = ex.Message }, statusCode: 503);

        return Results.Json(new { status = "unhealthy" }, statusCode: 503);
    }
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Marker class for WebApplicationFactory<Program> in integration tests
public partial class Program { }
