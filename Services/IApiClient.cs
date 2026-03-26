namespace FreakLete.Services;

/// <summary>
/// Surface of ApiClient used by ProfilePage. Enables page-behavior testing
/// without a real HTTP backend.
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
