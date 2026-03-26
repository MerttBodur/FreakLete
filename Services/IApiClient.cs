namespace FreakLete.Services;

/// <summary>
/// API client abstraction for profile and performance data operations.
/// Decouples ProfilePage and other clients from direct HTTP backend dependency.
/// </summary>
public interface IApiClient
{
	Task<ApiResult<UserProfileResponse>> GetProfileAsync();
	Task<ApiResult<UserProfileResponse>> SaveAthleteProfileAsync(SaveAthleteProfileRequest request);
	Task<ApiResult<UserProfileResponse>> SaveCoachProfileAsync(SaveCoachProfileRequest request);
	Task<ApiResult<List<SportDefinitionResponse>>> GetSportCatalogAsync();
	Task<ApiResult<List<AthleticPerformanceResponse>>> GetAthleticPerformancesAsync();
	Task<ApiResult<AthleticPerformanceResponse>> CreateAthleticPerformanceAsync(object data);
	Task<ApiResult<bool>> UpdateAthleticPerformanceAsync(int id, object data);
	Task<ApiResult<bool>> DeleteAthleticPerformanceAsync(int id);
	Task<ApiResult<List<MovementGoalResponse>>> GetMovementGoalsAsync();
	Task<ApiResult<MovementGoalResponse>> CreateMovementGoalAsync(object data);
	Task<ApiResult<bool>> UpdateMovementGoalAsync(int id, object data);
	Task<ApiResult<bool>> DeleteMovementGoalAsync(int id);
	Task<ApiResult<bool>> DeleteAccountAsync();
}
