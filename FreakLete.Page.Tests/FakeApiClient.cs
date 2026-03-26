using FreakLete.Services;

namespace FreakLete.Page.Tests;

/// <summary>
/// In-memory IApiClient fake that stores profile state and returns it on load.
/// Athlete save mutates only athlete fields; coach save mutates only coach fields.
/// Mirrors the real server endpoint behavior.
/// </summary>
internal class FakeApiClient : IApiClient
{
	public UserProfileResponse Profile { get; set; } = new()
	{
		Id = 1,
		FirstName = "Test",
		LastName = "User",
		Email = "test@test.com"
	};

	public SaveAthleteProfileRequest? LastAthleteSave { get; private set; }
	public SaveCoachProfileRequest? LastCoachSave { get; private set; }

	private readonly List<SportDefinitionResponse> _sports =
	[
		new() { Id = "powerlifting", Name = "Powerlifting", Category = "Strength", HasPositions = false, Positions = [] },
		new() { Id = "basketball", Name = "Basketball", Category = "Team", HasPositions = true, Positions = ["Point Guard", "Shooting Guard", "Small Forward", "Power Forward", "Center"] },
		new() { Id = "soccer", Name = "Soccer", Category = "Team", HasPositions = true, Positions = ["Goalkeeper", "Defender", "Midfielder", "Striker"] },
	];

	public Task<ApiResult<UserProfileResponse>> GetProfileAsync()
		=> Task.FromResult(ApiResult<UserProfileResponse>.Ok(Profile));

	public Task<ApiResult<UserProfileResponse>> SaveAthleteProfileAsync(SaveAthleteProfileRequest request)
	{
		LastAthleteSave = request;
		Profile.DateOfBirth = request.DateOfBirth;
		Profile.WeightKg = request.WeightKg;
		Profile.BodyFatPercentage = request.BodyFatPercentage;
		Profile.SportName = request.SportName ?? "";
		Profile.Position = request.Position ?? "";
		Profile.GymExperienceLevel = request.GymExperienceLevel ?? "";
		return Task.FromResult(ApiResult<UserProfileResponse>.Ok(Profile));
	}

	public Task<ApiResult<UserProfileResponse>> SaveCoachProfileAsync(SaveCoachProfileRequest request)
	{
		LastCoachSave = request;
		Profile.TrainingDaysPerWeek = request.TrainingDaysPerWeek;
		Profile.PreferredSessionDurationMinutes = request.PreferredSessionDurationMinutes;
		Profile.PrimaryTrainingGoal = request.PrimaryTrainingGoal ?? "";
		Profile.SecondaryTrainingGoal = request.SecondaryTrainingGoal ?? "";
		Profile.DietaryPreference = request.DietaryPreference ?? "";
		Profile.AvailableEquipment = request.AvailableEquipment ?? "";
		Profile.PhysicalLimitations = request.PhysicalLimitations ?? "";
		Profile.InjuryHistory = request.InjuryHistory ?? "";
		Profile.CurrentPainPoints = request.CurrentPainPoints ?? "";
		return Task.FromResult(ApiResult<UserProfileResponse>.Ok(Profile));
	}

	public Task<ApiResult<List<SportDefinitionResponse>>> GetSportCatalogAsync()
		=> Task.FromResult(ApiResult<List<SportDefinitionResponse>>.Ok(_sports));

	public Task<ApiResult<List<AthleticPerformanceResponse>>> GetAthleticPerformancesAsync()
		=> Task.FromResult(ApiResult<List<AthleticPerformanceResponse>>.Ok([]));

	public Task<ApiResult<AthleticPerformanceResponse>> CreateAthleticPerformanceAsync(object data)
		=> Task.FromResult(ApiResult<AthleticPerformanceResponse>.Fail("not implemented"));

	public Task<ApiResult<bool>> UpdateAthleticPerformanceAsync(int id, object data)
		=> Task.FromResult(ApiResult<bool>.Fail("not implemented"));

	public Task<ApiResult<bool>> DeleteAthleticPerformanceAsync(int id)
		=> Task.FromResult(ApiResult<bool>.Fail("not implemented"));

	public Task<ApiResult<List<MovementGoalResponse>>> GetMovementGoalsAsync()
		=> Task.FromResult(ApiResult<List<MovementGoalResponse>>.Ok([]));

	public Task<ApiResult<MovementGoalResponse>> CreateMovementGoalAsync(object data)
		=> Task.FromResult(ApiResult<MovementGoalResponse>.Fail("not implemented"));

	public Task<ApiResult<bool>> UpdateMovementGoalAsync(int id, object data)
		=> Task.FromResult(ApiResult<bool>.Fail("not implemented"));

	public Task<ApiResult<bool>> DeleteMovementGoalAsync(int id)
		=> Task.FromResult(ApiResult<bool>.Fail("not implemented"));

	public Task<ApiResult<bool>> DeleteAccountAsync()
		=> Task.FromResult(ApiResult<bool>.Fail("not implemented"));
}
