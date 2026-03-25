using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Auth;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

public class CoachProfileService
{
    private readonly AppDbContext _db;

    private static readonly HashSet<string> AllowedTrainingGoals = new(StringComparer.OrdinalIgnoreCase)
    {
        "Strength", "Hypertrophy", "Athletic Performance", "Fat Loss",
        "General Fitness", "Sport-Specific", "Powerlifting", "Olympic Weightlifting",
        "Rehab / Return to Training", "Body Recomposition"
    };

    private static readonly HashSet<string> AllowedDietaryPreferences = new(StringComparer.OrdinalIgnoreCase)
    {
        "No preference", "Standard / Balanced", "High Protein", "Vegetarian",
        "Vegan", "Pescatarian", "Keto / Low Carb", "Mediterranean",
        "Intermittent Fasting", "Halal", "Kosher"
    };

    private static readonly HashSet<int> AllowedSessionDurations = [30, 45, 60, 75, 90, 120];

    public CoachProfileService(AppDbContext db)
    {
        _db = db;
    }

    public record SaveResult(bool Success, string? Error, User? User);

    public async Task<SaveResult> SaveAsync(int userId, SaveCoachProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return new SaveResult(false, null, null);

        // ── Validate training days ───────────────────────
        if (request.TrainingDaysPerWeek.HasValue &&
            (request.TrainingDaysPerWeek.Value < 1 || request.TrainingDaysPerWeek.Value > 7))
            return new SaveResult(false, "Training days must be between 1 and 7.", user);

        // ── Validate session duration ────────────────────
        if (request.PreferredSessionDurationMinutes.HasValue &&
            !AllowedSessionDurations.Contains(request.PreferredSessionDurationMinutes.Value))
            return new SaveResult(false,
                $"Invalid session duration. Allowed values: {string.Join(", ", AllowedSessionDurations.Order())}.", user);

        // ── Validate training goals ──────────────────────
        var primaryGoal = NormalizeEmpty(request.PrimaryTrainingGoal);
        if (primaryGoal is not null && !AllowedTrainingGoals.Contains(primaryGoal))
            return new SaveResult(false, $"Invalid primary training goal: '{primaryGoal}'.", user);

        var secondaryGoal = NormalizeEmpty(request.SecondaryTrainingGoal);
        if (secondaryGoal is not null && !AllowedTrainingGoals.Contains(secondaryGoal))
            return new SaveResult(false, $"Invalid secondary training goal: '{secondaryGoal}'.", user);

        // ── Validate dietary preference ──────────────────
        var dietary = NormalizeEmpty(request.DietaryPreference);
        if (dietary is not null && !AllowedDietaryPreferences.Contains(dietary))
            return new SaveResult(false, $"Invalid dietary preference: '{dietary}'.", user);

        // ── Apply fields ─────────────────────────────────
        user.TrainingDaysPerWeek = request.TrainingDaysPerWeek;
        user.PreferredSessionDurationMinutes = request.PreferredSessionDurationMinutes;
        user.PrimaryTrainingGoal = primaryGoal ?? string.Empty;
        user.SecondaryTrainingGoal = secondaryGoal ?? string.Empty;
        user.DietaryPreference = dietary ?? string.Empty;
        user.AvailableEquipment = NormalizeEmpty(request.AvailableEquipment) ?? string.Empty;
        user.PhysicalLimitations = NormalizeEmpty(request.PhysicalLimitations) ?? string.Empty;
        user.InjuryHistory = NormalizeEmpty(request.InjuryHistory) ?? string.Empty;
        user.CurrentPainPoints = NormalizeEmpty(request.CurrentPainPoints) ?? string.Empty;

        await _db.SaveChangesAsync();
        return new SaveResult(true, null, user);
    }

    public async Task<UserProfileResponse> BuildProfileResponseAsync(int userId, User user)
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
