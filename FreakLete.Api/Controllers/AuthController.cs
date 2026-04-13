using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Auth;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using FreakLete.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly AthleteProfileService _athleteProfileService;
    private readonly CoachProfileService _coachProfileService;

    public AuthController(AppDbContext db, TokenService tokenService,
        AthleteProfileService athleteProfileService, CoachProfileService coachProfileService)
    {
        _db = db;
        _tokenService = tokenService;
        _athleteProfileService = athleteProfileService;
        _coachProfileService = coachProfileService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!request.Password.Any(char.IsUpper))
            return BadRequest(new { message = "Şifre en az 1 büyük harf içermelidir." });
        if (!request.Password.Any(c => !char.IsLetterOrDigit(c)))
            return BadRequest(new { message = "Şifre en az 1 özel karakter içermelidir." });

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return Conflict(new { message = "Bu e-posta adresi zaten kayıtlı." });

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            Token = _tokenService.GenerateToken(user)
        });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Opportunistic cleanup: purge records older than 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        await _db.AuthLoginAttempts
            .Where(a => a.OccurredAtUtc < thirtyDaysAgo)
            .ExecuteDeleteAsync();

        // Account-targeted brute-force check: 5 failed attempts / 15 min per email+IP
        var windowStart = DateTime.UtcNow.AddMinutes(-15);
        var recentFailedCount = await _db.AuthLoginAttempts
            .CountAsync(a => a.NormalizedEmail == normalizedEmail
                          && a.IpAddress == ipAddress
                          && !a.WasSuccessful
                          && a.OccurredAtUtc >= windowStart);

        if (recentFailedCount >= 5)
            return StatusCode(429, new { message = "E-posta veya şifre hatalı." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        var passwordValid = user is not null && PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);

        _db.AuthLoginAttempts.Add(new AuthLoginAttempt
        {
            NormalizedEmail = normalizedEmail,
            IpAddress = ipAddress,
            OccurredAtUtc = DateTime.UtcNow,
            WasSuccessful = passwordValid
        });
        await _db.SaveChangesAsync();

        if (!passwordValid)
            return Unauthorized(new { message = "E-posta veya şifre hatalı." });

        return Ok(new AuthResponse
        {
            UserId = user!.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            Token = _tokenService.GenerateToken(user)
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting("auth-change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal))
            return BadRequest(new { message = "E-posta adresi hesabınızla eşleşmiyor." });

        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mevcut şifre hatalı." });

        if (request.NewPassword != request.NewPasswordRepeat)
            return BadRequest(new { message = "Yeni şifreler eşleşmiyor." });

        if (request.NewPassword.Length < 8)
            return BadRequest(new { message = "Şifre en az 8 karakter olmalıdır." });

        if (!request.NewPassword.Any(char.IsUpper))
            return BadRequest(new { message = "Şifre en az 1 büyük harf içermelidir." });

        if (!request.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
            return BadRequest(new { message = "Şifre en az 1 özel karakter içermelidir." });

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.TokenVersion += 1;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Şifre başarıyla değiştirildi." });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        var workoutCount = await _db.Workouts.CountAsync(w => w.UserId == userId);
        var prCount = await _db.PrEntries.CountAsync(p => p.UserId == userId);

        return Ok(new UserProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            WeightKg = user.WeightKg,
            BodyFatPercentage = user.BodyFatPercentage,
            HeightCm = user.HeightCm,
            Sex = user.Sex,
            SportName = user.SportName,
            Position = user.Position,
            GymExperienceLevel = user.GymExperienceLevel,
            TotalWorkouts = workoutCount,
            TotalPrs = prCount,
            CreatedAt = user.CreatedAt,
            TrainingDaysPerWeek = user.TrainingDaysPerWeek,
            PreferredSessionDurationMinutes = user.PreferredSessionDurationMinutes,
            AvailableEquipment = user.AvailableEquipment,
            PhysicalLimitations = user.PhysicalLimitations,
            InjuryHistory = user.InjuryHistory,
            CurrentPainPoints = user.CurrentPainPoints,
            PrimaryTrainingGoal = user.PrimaryTrainingGoal,
            SecondaryTrainingGoal = user.SecondaryTrainingGoal,
            DietaryPreference = user.DietaryPreference
        });
    }

    [Authorize]
    [HttpPut("profile/athlete")]
    public async Task<ActionResult<UserProfileResponse>> SaveAthleteProfile(SaveAthleteProfileRequest request)
    {
        var userId = User.GetUserId();
        var result = await _athleteProfileService.SaveAsync(userId, request);

        if (result.User is null)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        var response = await _athleteProfileService.BuildProfileResponseAsync(userId, result.User);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("profile/coach")]
    public async Task<ActionResult<UserProfileResponse>> SaveCoachProfile(SaveCoachProfileRequest request)
    {
        var userId = User.GetUserId();
        var result = await _coachProfileService.SaveAsync(userId, request);

        if (result.User is null)
            return NotFound();

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        var response = await _coachProfileService.BuildProfileResponseAsync(userId, result.User);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Şifre doğrulaması başarısız." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
