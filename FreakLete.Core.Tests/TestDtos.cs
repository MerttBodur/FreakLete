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
    public string? SportName { get; set; }
    public string? Position { get; set; }
    public string? GymExperienceLevel { get; set; }
}
