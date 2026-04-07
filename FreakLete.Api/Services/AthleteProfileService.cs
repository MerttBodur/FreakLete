using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Auth;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

public class AthleteProfileService
{
    private readonly AppDbContext _db;

    private static readonly HashSet<string> AllowedGymExperienceLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "< 1 year", "1-2 years", "3-4 years", "5+ years"
    };

    private static readonly HashSet<string> AllowedSexValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "Male", "Female"
    };

    public AthleteProfileService(AppDbContext db)
    {
        _db = db;
    }

    public record SaveResult(bool Success, string? Error, User? User);

    public async Task<SaveResult> SaveAsync(int userId, SaveAthleteProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return new SaveResult(false, null, null); // caller returns 404

        // ── Validate date of birth ───────────────────────
        if (request.DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.DateOfBirth.Value > today)
                return new SaveResult(false, "Date of birth cannot be in the future.", user);
            if (request.DateOfBirth.Value < new DateOnly(1900, 1, 1))
                return new SaveResult(false, "Date of birth is unreasonably old.", user);
        }

        // ── Validate weight ──────────────────────────────
        if (request.WeightKg.HasValue && (request.WeightKg.Value < 20 || request.WeightKg.Value > 400))
            return new SaveResult(false, "Weight must be between 20 and 400 kg.", user);

        // ── Validate body fat ────────────────────────────
        if (request.BodyFatPercentage.HasValue &&
            (request.BodyFatPercentage.Value < 0 || request.BodyFatPercentage.Value > 100))
            return new SaveResult(false, "Body fat must be between 0 and 100%.", user);

        // ── Validate height ─────────────────────────────
        if (request.HeightCm.HasValue && (request.HeightCm.Value < 80 || request.HeightCm.Value > 300))
            return new SaveResult(false, "Height must be between 80 and 300 cm.", user);

        // ── Validate sex ────────────────────────────────
        var normalizedSex = NormalizeEmpty(request.Sex);
        if (normalizedSex is not null && !AllowedSexValues.Contains(normalizedSex))
            return new SaveResult(false,
                $"Invalid sex value: '{normalizedSex}'. Allowed values: {string.Join(", ", AllowedSexValues)}.", user);

        // ── Validate gym experience level ────────────────
        var normalizedGym = NormalizeEmpty(request.GymExperienceLevel);
        if (normalizedGym is not null && !AllowedGymExperienceLevels.Contains(normalizedGym))
            return new SaveResult(false,
                $"Invalid gym experience level: '{normalizedGym}'. Allowed values: {string.Join(", ", AllowedGymExperienceLevels)}.", user);

        // ── Validate sport / position coherence ──────────
        string? resolvedSport = NormalizeEmpty(request.SportName);
        string? resolvedPosition = NormalizeEmpty(request.Position);

        if (resolvedSport is not null)
        {
            var sportDef = SportCatalog.All.FirstOrDefault(s =>
                string.Equals(s.Name, resolvedSport, StringComparison.OrdinalIgnoreCase));

            if (sportDef is null)
                return new SaveResult(false, $"Unknown sport: {resolvedSport}", user);

            if (!sportDef.HasPositions || sportDef.Positions.Count == 0)
            {
                // Sport has no positions → force-clear position
                resolvedPosition = null;
            }
            else if (resolvedPosition is not null)
            {
                // Sport has positions and a position was sent — validate it
                bool valid = sportDef.Positions.Any(p =>
                    string.Equals(p, resolvedPosition, StringComparison.OrdinalIgnoreCase));
                if (!valid)
                    return new SaveResult(false,
                        $"Invalid position '{resolvedPosition}' for sport '{sportDef.Name}'.", user);
            }
            // else: sport has positions but none sent → clear position
        }
        else
        {
            // No sport → position must also be cleared
            resolvedPosition = null;
        }

        // ── Resolve canonical sex value ─────────────────
        string? resolvedSex = normalizedSex is not null
            ? AllowedSexValues.First(v => string.Equals(v, normalizedSex, StringComparison.OrdinalIgnoreCase))
            : null;

        // ── Apply fields ─────────────────────────────────
        user.DateOfBirth = request.DateOfBirth;
        user.WeightKg = request.WeightKg;
        user.BodyFatPercentage = request.BodyFatPercentage;
        user.HeightCm = request.HeightCm;
        user.Sex = resolvedSex ?? string.Empty;
        user.SportName = resolvedSport ?? string.Empty;
        user.Position = resolvedPosition ?? string.Empty;
        user.GymExperienceLevel = normalizedGym ?? string.Empty;

        await _db.SaveChangesAsync();
        return new SaveResult(true, null, user);
    }

    public async Task<UserProfileResponse?> BuildProfileResponseAsync(int userId, User user)
    {
        var workoutCount = await _db.Workouts.CountAsync(w => w.UserId == userId);
        var prCount = await _db.PrEntries.CountAsync(p => p.UserId == userId);

        return new UserProfileResponse
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
        };
    }

    private static string? NormalizeEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
