using System.Text;
using FreakLete.Api.Data;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured. Set via appsettings or Jwt__Key env var.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddSingleton<TokenService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending migrations on startup
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health check — verifies DB connectivity
app.MapGet("/api/health", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var pending = await db.Database.GetPendingMigrationsAsync();
        var pendingList = pending.ToList();
        return Results.Ok(new
        {
            status = canConnect ? "healthy" : "unhealthy",
            database = canConnect,
            pendingMigrations = pendingList.Count,
            migrations = pendingList
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "unhealthy",
            database = false,
            error = ex.Message
        });
    }
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
