// Minimal DTO stubs so AthleteProfileViewModel can compile in the test project
// without pulling in ApiClient.cs (which has MAUI dependencies).
// These mirror the shapes in Services/ApiClient.cs.

namespace FreakLete.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data, StatusCode = 200 };
    public static ApiResult<T> Fail(string error, int statusCode = 0) => new() { Success = false, Error = error, StatusCode = statusCode };
}

public class UserProfileResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public double? HeightCm { get; set; }
    public string Sex { get; set; } = "";
    public string SportName { get; set; } = "";
    public string Position { get; set; } = "";
    public string GymExperienceLevel { get; set; } = "";
    public int TotalWorkouts { get; set; }
    public int TotalPrs { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? TrainingDaysPerWeek { get; set; }
    public int? PreferredSessionDurationMinutes { get; set; }
    public string AvailableEquipment { get; set; } = "";
    public string PhysicalLimitations { get; set; } = "";
    public string InjuryHistory { get; set; } = "";
    public string CurrentPainPoints { get; set; } = "";
    public string PrimaryTrainingGoal { get; set; } = "";
    public string SecondaryTrainingGoal { get; set; } = "";
    public string DietaryPreference { get; set; } = "";
}

public class SportDefinitionResponse
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public bool HasPositions { get; set; }
    public List<string> Positions { get; set; } = [];
}

public class SaveAthleteProfileRequest
{
    public DateOnly? DateOfBirth { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercentage { get; set; }
    public double? HeightCm { get; set; }
    public string? Sex { get; set; }
    public string? SportName { get; set; }
    public string? Position { get; set; }
    public string? GymExperienceLevel { get; set; }
}

public class SaveCoachProfileRequest
{
    public int? TrainingDaysPerWeek { get; set; }
    public int? PreferredSessionDurationMinutes { get; set; }
    public string? PrimaryTrainingGoal { get; set; }
    public string? SecondaryTrainingGoal { get; set; }
    public string? DietaryPreference { get; set; }
    public string? AvailableEquipment { get; set; }
    public string? PhysicalLimitations { get; set; }
    public string? InjuryHistory { get; set; }
    public string? CurrentPainPoints { get; set; }
}

// ── Billing Sync DTOs (for SettingsBillingLogic tests) ──────

public class GooglePlaySyncRequest
{
    public string ProductId { get; set; } = "";
    public string? BasePlanId { get; set; }
    public string PurchaseToken { get; set; } = "";
    public string? OrderId { get; set; }
    public int PurchaseState { get; set; }
    public bool IsAcknowledged { get; set; }
    public string? RawPayloadJson { get; set; }
}

public class GooglePlaySyncResponse
{
    public string State { get; set; } = "";
    public DateTime? EntitlementEndsAtUtc { get; set; }
    public string Kind { get; set; } = "";
}


// ── Chart / Workout DTOs (for ChartDataHelper tests) ────────

public class WorkoutResponse
{
    public int Id { get; set; }
    public string WorkoutName { get; set; } = "";
    public DateTime WorkoutDate { get; set; }
    public List<ExerciseEntryDto> Exercises { get; set; } = [];
}

public class ExerciseEntryDto
{
    public string ExerciseName { get; set; } = "";
    public string ExerciseCategory { get; set; } = "";
    public string TrackingMode { get; set; } = "Strength";
    public int SetsCount { get; set; }
    public List<ApiExerciseSetDto> Sets { get; set; } = [];
    public int Reps { get; set; }
    public int? RIR { get; set; }
    public int? RestSeconds { get; set; }
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public double? Metric1Value { get; set; }
    public string Metric1Unit { get; set; } = "";
    public double? Metric2Value { get; set; }
    public string Metric2Unit { get; set; } = "";
}

public class ApiExerciseSetDto
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double? Weight { get; set; }
}

public class PrEntryResponse
{
    public int Id { get; set; }
    public string ExerciseName { get; set; } = "";
    public string ExerciseCategory { get; set; } = "";
    public string TrackingMode { get; set; } = "";
    public int Weight { get; set; }
    public int Reps { get; set; }
    public int? RIR { get; set; }
    public double? Metric1Value { get; set; }
    public string Metric1Unit { get; set; } = "";
    public double? Metric2Value { get; set; }
    public string Metric2Unit { get; set; } = "";
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AthleticPerformanceResponse
{
    public int Id { get; set; }
    public string MovementName { get; set; } = "";
    public string MovementCategory { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
    public double? SecondaryValue { get; set; }
    public string SecondaryUnit { get; set; } = "";
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public DateTime RecordedAt { get; set; }
}

// ── Training Program DTOs (for SessionPickerHelper tests) ───

public class TrainingProgramResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Goal { get; set; } = "";
    public int DaysPerWeek { get; set; }
    public int SessionDurationMinutes { get; set; }
    public string Status { get; set; } = "";
    public string Sport { get; set; } = "";
    public string Position { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsStarterTemplate { get; set; }
    public List<ProgramWeekResponse> Weeks { get; set; } = [];
}

public class ProgramWeekResponse
{
    public int Id { get; set; }
    public int WeekNumber { get; set; }
    public string Focus { get; set; } = "";
    public bool IsDeload { get; set; }
    public List<ProgramSessionResponse> Sessions { get; set; } = [];
}

public class ProgramSessionResponse
{
    public int Id { get; set; }
    public int DayNumber { get; set; }
    public string SessionName { get; set; } = "";
    public string Focus { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<ProgramExerciseResponse> Exercises { get; set; } = [];
}

public class ProgramExerciseResponse
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string ExerciseName { get; set; } = "";
    public string ExerciseCategory { get; set; } = "";
    public int Sets { get; set; }
    public string RepsOrDuration { get; set; } = "";
    public string IntensityGuidance { get; set; } = "";
    public int? RestSeconds { get; set; }
    public string Notes { get; set; } = "";
    public string SupersetGroup { get; set; } = "";
}

public class TrainingProgramListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Status { get; set; } = "";
    public int DaysPerWeek { get; set; }
    public DateTime CreatedAt { get; set; }
}
