using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FreakLete.Services;

public class ApiClient : IApiClient
{
	private readonly HttpClient _http;
	private readonly UserSession _session;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	public ApiClient(UserSession session)
	{
		_session = session;

		var handler = new HttpClientHandler();

#if DEBUG
		// Development: accept self-signed certificates
		handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif

		_http = new HttpClient(handler);
		_http.BaseAddress = new Uri(GetBaseUrl());
		_http.Timeout = TimeSpan.FromSeconds(45);
		_http.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));
	}

	private static string GetBaseUrl()
	{
		string? configured = AppConfig.ApiBaseUrl;
		if (!string.IsNullOrEmpty(configured))
			return configured;

#if DEBUG
		// Local development backend
#if ANDROID
		// Android emulator routes 10.0.2.2 to host machine's localhost.
		return "http://10.0.2.2:5131";
#else
		return "http://localhost:5131";
#endif
#else
		// Release / production
		return "https://freaklete-production.up.railway.app";
#endif
	}

	// ── Auth ────────────────────────────────────────────

	public Task<ApiResult<AuthResponse>> RegisterAsync(string firstName, string lastName, string email, string password)
	{
		var request = new { firstName, lastName, email, password };
		return PostAsync<AuthResponse>("api/auth/register", request);
	}

	public Task<ApiResult<AuthResponse>> LoginAsync(string email, string password)
	{
		var request = new { email, password };
		return PostAsync<AuthResponse>("api/auth/login", request);
	}

	public async Task<ApiResult<bool>> ChangePasswordAsync(string email, string currentPassword, string newPassword, string newPasswordRepeat)
	{
		try
		{
			AttachToken();
			var request = new { email, currentPassword, newPassword, newPasswordRepeat };
			var response = await _http.PostAsJsonAsync("api/auth/change-password", request, JsonOptions);

			if (response.IsSuccessStatusCode)
				return ApiResult<bool>.Ok(true);

			var error = await ReadError(response);
			return ApiResult<bool>.Fail(error, (int)response.StatusCode);
		}
		catch (Exception ex)
		{
			return ApiResult<bool>.Fail($"Bağlantı hatası: {ex.Message}");
		}
	}

	public Task<ApiResult<UserProfileResponse>> GetProfileAsync()
	{
		return GetAsync<UserProfileResponse>("api/auth/profile");
	}

	public Task<ApiResult<UserProfileResponse>> SaveAthleteProfileAsync(SaveAthleteProfileRequest request)
	{
		return PutWithResponseAsync<UserProfileResponse>("api/auth/profile/athlete", request);
	}

	public Task<ApiResult<UserProfileResponse>> SaveCoachProfileAsync(SaveCoachProfileRequest request)
	{
		return PutWithResponseAsync<UserProfileResponse>("api/auth/profile/coach", request);
	}

	public Task<ApiResult<bool>> DeleteAccountAsync()
	{
		return DeleteAsync("api/auth/account");
	}

	// ── Athletic Performance ───────────────────────────

	public Task<ApiResult<List<AthleticPerformanceResponse>>> GetAthleticPerformancesAsync()
	{
		return GetAsync<List<AthleticPerformanceResponse>>("api/athleticperformance");
	}

	public Task<ApiResult<AthleticPerformanceResponse>> CreateAthleticPerformanceAsync(object data)
	{
		return PostAsync<AthleticPerformanceResponse>("api/athleticperformance", data);
	}

	public Task<ApiResult<bool>> UpdateAthleticPerformanceAsync(int id, object data)
	{
		return PutAsync($"api/athleticperformance/{id}", data);
	}

	public Task<ApiResult<bool>> DeleteAthleticPerformanceAsync(int id)
	{
		return DeleteAsync($"api/athleticperformance/{id}");
	}

	// ── Movement Goals ─────────────────────────────────

	public Task<ApiResult<List<MovementGoalResponse>>> GetMovementGoalsAsync()
	{
		return GetAsync<List<MovementGoalResponse>>("api/movementgoals");
	}

	public Task<ApiResult<MovementGoalResponse>> CreateMovementGoalAsync(object data)
	{
		return PostAsync<MovementGoalResponse>("api/movementgoals", data);
	}

	public Task<ApiResult<bool>> UpdateMovementGoalAsync(int id, object data)
	{
		return PutAsync($"api/movementgoals/{id}", data);
	}

	public Task<ApiResult<bool>> DeleteMovementGoalAsync(int id)
	{
		return DeleteAsync($"api/movementgoals/{id}");
	}

	// ── Workouts ──────────────────────────────────────

	public Task<ApiResult<List<WorkoutResponse>>> GetWorkoutsAsync()
	{
		return GetAsync<List<WorkoutResponse>>("api/workouts");
	}

	public Task<ApiResult<WorkoutResponse>> GetWorkoutByIdAsync(int id)
	{
		return GetAsync<WorkoutResponse>($"api/workouts/{id}");
	}

	public Task<ApiResult<List<WorkoutResponse>>> GetWorkoutsByDateAsync(DateTime date)
	{
		return GetAsync<List<WorkoutResponse>>($"api/workouts/by-date/{date:yyyy-MM-dd}");
	}

	public Task<ApiResult<WorkoutResponse>> CreateWorkoutAsync(object data)
	{
		return PostAsync<WorkoutResponse>("api/workouts", data);
	}

	public Task<ApiResult<bool>> UpdateWorkoutAsync(int id, object data)
	{
		return PutAsync($"api/workouts/{id}", data);
	}

	public Task<ApiResult<bool>> DeleteWorkoutAsync(int id)
	{
		return DeleteAsync($"api/workouts/{id}");
	}

	// ── PR Entries ────────────────────────────────────

	public Task<ApiResult<List<PrEntryResponse>>> GetPrEntriesAsync()
	{
		return GetAsync<List<PrEntryResponse>>("api/pr-entries");
	}

	public Task<ApiResult<PrEntryResponse>> CreatePrEntryAsync(object data)
	{
		return PostAsync<PrEntryResponse>("api/pr-entries", data);
	}

	public Task<ApiResult<bool>> UpdatePrEntryAsync(int id, object data)
	{
		return PutAsync($"api/pr-entries/{id}", data);
	}

	public Task<ApiResult<bool>> DeletePrEntryAsync(int id)
	{
		return DeleteAsync($"api/pr-entries/{id}");
	}

	// ── Sport Catalog ────────────────────────────────────

	public Task<ApiResult<List<SportDefinitionResponse>>> GetSportCatalogAsync()
	{
		return GetAsync<List<SportDefinitionResponse>>("api/sportcatalog");
	}

	// ── FreakAI ──────────────────────────────────────────

	public Task<ApiResult<FreakAiChatResponse>> FreakAiChatAsync(string message, List<FreakAiChatMessage>? history)
	{
		var request = new { message, history };
		return PostAsync<FreakAiChatResponse>("api/freakai/chat", request);
	}

	// ── Training Programs ────────────────────────────────

	public Task<ApiResult<List<TrainingProgramListResponse>>> GetTrainingProgramsAsync()
	{
		return GetAsync<List<TrainingProgramListResponse>>("api/trainingprogram");
	}

	public Task<ApiResult<TrainingProgramResponse>> GetActiveProgramAsync()
	{
		return GetAsync<TrainingProgramResponse>("api/trainingprogram/active");
	}

	public Task<ApiResult<TrainingProgramResponse>> GetProgramByIdAsync(int id)
	{
		return GetAsync<TrainingProgramResponse>($"api/trainingprogram/{id}");
	}

	// ── Starter Templates ───────────────────────────────

	public Task<ApiResult<List<TrainingProgramListResponse>>> GetStarterTemplatesAsync()
	{
		return GetAsync<List<TrainingProgramListResponse>>("api/trainingprogram/starter");
	}

	public Task<ApiResult<TrainingProgramResponse>> GetStarterTemplateByIdAsync(int id)
	{
		return GetAsync<TrainingProgramResponse>($"api/trainingprogram/starter/{id}");
	}

	public Task<ApiResult<TrainingProgramResponse>> CloneStarterTemplateAsync(int id)
	{
		return PostAsync<TrainingProgramResponse>($"api/trainingprogram/starter/{id}/clone", new { });
	}

	// ── HTTP helpers ────────────────────────────────────

	private void AttachToken()
	{
		var token = _session.GetToken();
		if (!string.IsNullOrEmpty(token))
		{
			_http.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);
		}
	}

	private async Task<ApiResult<T>> GetAsync<T>(string endpoint)
	{
		try
		{
			AttachToken();
			var response = await _http.GetAsync(endpoint);
			return await HandleResponse<T>(response);
		}
		catch (Exception ex)
		{
			return ApiResult<T>.Fail($"Bağlantı hatası: {ex.Message}");
		}
	}

	private async Task<ApiResult<T>> PostAsync<T>(string endpoint, object data)
	{
		try
		{
			AttachToken();
			var response = await _http.PostAsJsonAsync(endpoint, data, JsonOptions);
			return await HandleResponse<T>(response);
		}
		catch (Exception ex)
		{
			return ApiResult<T>.Fail($"Bağlantı hatası: {ex.Message}");
		}
	}

	private async Task<ApiResult<T>> PutWithResponseAsync<T>(string endpoint, object data)
	{
		try
		{
			AttachToken();
			var response = await _http.PutAsJsonAsync(endpoint, data, JsonOptions);
			return await HandleResponse<T>(response);
		}
		catch (Exception ex)
		{
			return ApiResult<T>.Fail($"Bağlantı hatası: {ex.Message}");
		}
	}

	private async Task<ApiResult<bool>> PutAsync(string endpoint, object data)
	{
		try
		{
			AttachToken();
			var response = await _http.PutAsJsonAsync(endpoint, data, JsonOptions);

			if (response.IsSuccessStatusCode)
				return ApiResult<bool>.Ok(true);

			var error = await ReadError(response);
			return ApiResult<bool>.Fail(error);
		}
		catch (Exception ex)
		{
			return ApiResult<bool>.Fail($"Bağlantı hatası: {ex.Message}");
		}
	}

	private async Task<ApiResult<bool>> DeleteAsync(string endpoint)
	{
		try
		{
			AttachToken();
			var response = await _http.DeleteAsync(endpoint);

			if (response.IsSuccessStatusCode)
				return ApiResult<bool>.Ok(true);

			var error = await ReadError(response);
			return ApiResult<bool>.Fail(error);
		}
		catch (Exception ex)
		{
			return ApiResult<bool>.Fail($"Bağlantı hatası: {ex.Message}");
		}
	}

	private static async Task<ApiResult<T>> HandleResponse<T>(HttpResponseMessage response)
	{
		if (response.IsSuccessStatusCode)
		{
			var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
			return data is not null
				? ApiResult<T>.Ok(data)
				: ApiResult<T>.Fail("Boş yanıt alındı.");
		}

		var error = await ReadError(response);
		return ApiResult<T>.Fail(error, (int)response.StatusCode);
	}

	private static async Task<string> ReadError(HttpResponseMessage response)
	{
		try
		{
			var body = await response.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(body);
			if (doc.RootElement.TryGetProperty("message", out var msg))
				return msg.GetString() ?? "Bilinmeyen hata.";
			return body;
		}
		catch
		{
			return $"Hata kodu: {(int)response.StatusCode}";
		}
	}
}

// ── Result wrapper ──────────────────────────────────────

public class ApiResult<T>
{
	public bool Success { get; init; }
	public T? Data { get; init; }
	public string? Error { get; init; }
	public int StatusCode { get; init; }

	public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data, StatusCode = 200 };
	public static ApiResult<T> Fail(string error, int statusCode = 0) => new() { Success = false, Error = error, StatusCode = statusCode };
}

// ── Response DTOs ───────────────────────────────────────

public class AuthResponse
{
	public int UserId { get; set; }
	public string Email { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string Token { get; set; } = "";
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

	// Coach profile fields
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

public class MovementGoalResponse
{
	public int Id { get; set; }
	public string MovementName { get; set; } = "";
	public string MovementCategory { get; set; } = "";
	public string GoalMetricLabel { get; set; } = "";
	public double TargetValue { get; set; }
	public string Unit { get; set; } = "";
	public DateTime CreatedAt { get; set; }
}

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
	public int Sets { get; set; }
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

// ── Sport Catalog DTOs ──────────────────────────────────

public class SportDefinitionResponse
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Category { get; set; } = "";
	public bool HasPositions { get; set; }
	public List<string> Positions { get; set; } = [];
}

// ── Athlete Profile Save DTO ────────────────────────────

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

// ── Coach Profile Save DTO ──────────────────────────────

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

// ── FreakAI DTOs ────────────────────────────────────────

public class FreakAiChatResponse
{
	public string Reply { get; set; } = "";
}

public class FreakAiChatMessage
{
	public string Role { get; set; } = "user";
	public string Content { get; set; } = "";
}

// ── Training Program DTOs ───────────────────────────────

public class TrainingProgramListResponse
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Goal { get; set; } = "";
	public string Status { get; set; } = "";
	public int DaysPerWeek { get; set; }
	public DateTime CreatedAt { get; set; }
}

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
