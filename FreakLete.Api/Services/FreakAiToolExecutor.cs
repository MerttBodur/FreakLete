using System.Text.Json;
using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using FreakLete.Api.Services.Embeddings;
using FreakLete.Services;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Services;

public class FreakAiToolExecutor
{
    private readonly AppDbContext _db;
    private readonly TrainingSummaryService _summaryService;
    private readonly IUserSnapshotEventSink _snapshotSink;
    private readonly ILogger<FreakAiToolExecutor> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Tool names executed during the current request scope.
    /// Used by controller to promote intent when mutating tools are called.
    /// </summary>
    public HashSet<string> ExecutedTools { get; } = [];

    private static readonly HashSet<string> ProgramMutatingTools =
        ["create_program", "adjust_program", "swap_exercise"];

    /// <summary>
    /// Returns true if any program-mutating tool was called during this request.
    /// </summary>
    public bool DidMutateProgramGenerate => ExecutedTools.Overlaps(ProgramMutatingTools);

    public FreakAiToolExecutor(
        AppDbContext db,
        TrainingSummaryService summaryService,
        IUserSnapshotEventSink snapshotSink,
        ILogger<FreakAiToolExecutor> logger)
    {
        _db = db;
        _summaryService = summaryService;
        _snapshotSink = snapshotSink;
        _logger = logger;
    }

    public async Task<string> ExecuteToolAsync(int userId, string toolName, JsonElement? args)
    {
        _logger.LogInformation("Executing tool {Tool} for user {UserId}", toolName, userId);
        ExecutedTools.Add(toolName);

        return toolName switch
        {
            // ── Read / Context tools ──
            "get_user_profile" => await GetUserProfile(userId),
            "get_training_preferences" => await GetTrainingPreferences(userId),
            "get_equipment_profile" => await GetEquipmentProfile(userId),
            "get_physical_limitations" => await GetPhysicalLimitations(userId),
            "get_injury_context" => await GetInjuryContext(userId),
            "get_training_summary" => await GetTrainingSummary(userId, args),
            "get_recent_workouts" => await GetRecentWorkouts(userId, args),
            "get_pr_history" => await GetPrHistory(userId, args),
            "get_athletic_performance_history" => await GetAthleticPerformanceHistory(userId, args),
            "get_movement_goals" => await GetMovementGoals(userId),
            "get_current_program" => await GetCurrentProgram(userId),
            "get_program_list" => await GetProgramList(userId),
            "search_exercises" => await SearchExercises(args),
            "calculate_one_rm" => CalculateOneRm(args),
            "calculate_rsi" => CalculateRsi(args),

            // ── Coach / Write tools ──
            "create_program" => await CreateProgram(userId, args),
            "adjust_program" => await AdjustProgram(userId, args),
            "swap_exercise" => await SwapExercise(userId, args),
            "set_program_status" => await SetProgramStatus(userId, args),

            _ => Err($"Unknown tool: {toolName}")
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  READ / CONTEXT TOOLS
    // ════════════════════════════════════════════════════════════════

    private async Task<string> GetUserProfile(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Err("User not found");

        var workoutCount = await _db.Workouts.CountAsync(w => w.UserId == userId);
        var prCount = await _db.PrEntries.CountAsync(p => p.UserId == userId);

        return Json(new
        {
            user.FirstName,
            user.LastName,
            user.DateOfBirth,
            user.WeightKg,
            user.BodyFatPercentage,
            user.SportName,
            user.Position,
            user.GymExperienceLevel,
            user.PrimaryTrainingGoal,
            user.SecondaryTrainingGoal,
            user.DietaryPreference,
            totalWorkouts = workoutCount,
            totalPrs = prCount,
            memberSince = user.CreatedAt
        });
    }

    private async Task<string> GetTrainingPreferences(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Err("User not found");

        return Json(new
        {
            user.TrainingDaysPerWeek,
            user.PreferredSessionDurationMinutes,
            user.PrimaryTrainingGoal,
            user.SecondaryTrainingGoal,
            user.SportName,
            user.Position,
            user.GymExperienceLevel
        });
    }

    private async Task<string> GetEquipmentProfile(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Err("User not found");

        var equipmentType = user.AvailableEquipment?.Trim() ?? "";
        var mapping = MapEquipmentAccess(equipmentType);

        return Json(new
        {
            equipmentType,
            hasEquipmentInfo = !string.IsNullOrWhiteSpace(equipmentType),
            accessLevel = mapping.AccessLevel,
            inferredEquipmentTags = mapping.EquipmentTags,
            inferredCapabilities = mapping.Capabilities
        });
    }

    /// <summary>
    /// Maps canonical equipment access types to structured equipment capabilities.
    /// Helps FreakAI understand what exercises and methodologies are feasible.
    /// </summary>
    private (string AccessLevel, string[] EquipmentTags, string[] Capabilities) MapEquipmentAccess(string equipmentType)
    {
        return equipmentType switch
        {
            "Home" => (
                "home_setup",
                new[] { "dumbbells", "resistance_bands", "bodyweight", "mat" },
                new[] { "bodyweight_strength", "dumbbell_work", "mobility", "core_training" }
            ),
            "Local Gym" => (
                "basic_gym",
                new[] { "dumbbells", "barbells", "benches", "squat_racks", "cable_machine", "cardio" },
                new[] { "barbell_strength", "full_body_training", "heavy_lifting", "progressive_overload", "athletic_performance" }
            ),
            "Commercial Gym" => (
                "full_commercial",
                new[] { "barbells", "dumbbells", "benches", "squat_racks", "cable_machine", "smith_machine", "leg_press", "cardio", "accessories" },
                new[] { "comprehensive_strength", "hypertrophy_training", "power_development", "athletic_conditioning", "sport_specific_prep" }
            ),
            "Powerlifting Gym" => (
                "powerlifting_focused",
                new[] { "competition_barbells", "competition_plates", "squat_rack", "bench_press", "deadlift_platform", "specialty_barbells", "chains", "bands" },
                new[] { "competition_lift_training", "max_strength_development", "load_progression", "technique_refinement", "meet_prep" }
            ),
            "CrossFit Gym" => (
                "crossfit_facility",
                new[] { "barbells", "kettlebells", "dumbbells", "wall_balls", "rowing_machine", "rig", "plyo_boxes", "assault_bike", "rower", "pull_up_bar" },
                new[] { "metabolic_conditioning", "functional_fitness", "power_development", "high_intensity", "gymnastics_skills", "olympic_lift_variations" }
            ),
            "Weightlifting Gym" => (
                "olympic_focused",
                new[] { "competition_barbells", "bumper_plates", "lifting_platform", "power_rack", "blocks", "pulleys" },
                new[] { "olympic_lift_training", "technical_perfection", "power_development", "speed_strength", "snatch_clean_jerk_training" }
            ),
            "Athlete Performance Gym" => (
                "performance_facility",
                new[] { "barbells", "dumbbells", "plyo_boxes", "sport_equipment", "force_plate", "agility_ladder", "timing_gates", "sled_push", "medicine_balls", "pull_up_bar" },
                new[] { "athletic_development", "power_training", "sport_specific_skills", "speed_agility", "explosive_strength", "performance_metrics" }
            ),
            _ => (
                "unspecified",
                new[] { "basic_equipment" },
                new[] { "general_training" }
            )
        };
    }

    private async Task<string> GetPhysicalLimitations(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Err("User not found");

        return Json(new
        {
            user.PhysicalLimitations,
            user.CurrentPainPoints,
            hasLimitations = !string.IsNullOrWhiteSpace(user.PhysicalLimitations),
            hasPainPoints = !string.IsNullOrWhiteSpace(user.CurrentPainPoints)
        });
    }

    private async Task<string> GetInjuryContext(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Err("User not found");

        return Json(new
        {
            user.InjuryHistory,
            user.CurrentPainPoints,
            user.PhysicalLimitations,
            hasInjuryHistory = !string.IsNullOrWhiteSpace(user.InjuryHistory),
            hasCurrentPain = !string.IsNullOrWhiteSpace(user.CurrentPainPoints)
        });
    }

    private async Task<string> GetTrainingSummary(int userId, JsonElement? args)
    {
        int days = 30;
        if (args.HasValue && args.Value.TryGetProperty("days", out var daysEl))
            days = daysEl.GetInt32();

        var summary = await _summaryService.GetSummaryAsync(userId, days);
        return Json(summary);
    }

    private async Task<string> GetRecentWorkouts(int userId, JsonElement? args)
    {
        int limit = 10;
        if (args.HasValue && args.Value.TryGetProperty("limit", out var limitEl))
            limit = Math.Clamp(limitEl.GetInt32(), 1, 30);

        var workouts = await _db.Workouts
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.WorkoutDate)
            .Take(limit)
            .Include(w => w.ExerciseEntries)
            .Select(w => new
            {
                w.WorkoutName,
                w.WorkoutDate,
                exercises = w.ExerciseEntries.Select(e => new
                {
                    e.ExerciseName,
                    e.ExerciseCategory,
                    Sets = e.SetsCount,
                    e.Reps,
                    e.RIR,
                    e.Metric1Value,
                    e.Metric1Unit,
                    e.Metric2Value,
                    e.Metric2Unit
                })
            })
            .ToListAsync();

        return Json(workouts);
    }

    private async Task<string> GetPrHistory(int userId, JsonElement? args)
    {
        int limit = 20;
        string? exerciseName = null;

        if (args.HasValue)
        {
            if (args.Value.TryGetProperty("limit", out var limitEl))
                limit = Math.Clamp(limitEl.GetInt32(), 1, 50);
            if (args.Value.TryGetProperty("exerciseName", out var nameEl))
                exerciseName = nameEl.GetString();
        }

        var query = _db.PrEntries.Where(p => p.UserId == userId);

        if (!string.IsNullOrEmpty(exerciseName))
            query = query.Where(p => p.ExerciseName == exerciseName);

        var prs = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new
            {
                p.ExerciseName,
                p.ExerciseCategory,
                p.Weight,
                p.Reps,
                p.RIR,
                p.Metric1Value,
                p.Metric1Unit,
                p.Metric2Value,
                p.Metric2Unit,
                p.CreatedAt
            })
            .ToListAsync();

        return Json(prs);
    }

    private async Task<string> GetAthleticPerformanceHistory(int userId, JsonElement? args)
    {
        int limit = 20;
        string? movementName = null;

        if (args.HasValue)
        {
            if (args.Value.TryGetProperty("limit", out var limitEl))
                limit = Math.Clamp(limitEl.GetInt32(), 1, 50);
            if (args.Value.TryGetProperty("movementName", out var nameEl))
                movementName = nameEl.GetString();
        }

        var query = _db.AthleticPerformanceEntries.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(movementName))
            query = query.Where(a => a.MovementName == movementName);

        var entries = await query
            .OrderByDescending(a => a.RecordedAt)
            .Take(limit)
            .Select(a => new
            {
                a.MovementName,
                a.MovementCategory,
                a.Value,
                a.Unit,
                a.SecondaryValue,
                a.SecondaryUnit,
                a.GroundContactTimeMs,
                a.ConcentricTimeSeconds,
                a.RecordedAt
            })
            .ToListAsync();

        return Json(entries);
    }

    private async Task<string> GetMovementGoals(int userId)
    {
        var goals = await _db.MovementGoals
            .Where(g => g.UserId == userId)
            .Select(g => new
            {
                g.MovementName,
                g.MovementCategory,
                g.GoalMetricLabel,
                g.TargetValue,
                g.Unit,
                g.CreatedAt
            })
            .ToListAsync();

        return Json(goals);
    }

    private async Task<string> GetCurrentProgram(int userId)
    {
        var program = await _db.TrainingPrograms
            .Where(p => p.UserId == userId && p.Status == "active")
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        if (program is null)
            return Json(new { hasActiveProgram = false, message = "No active training program found." });

        return Json(new
        {
            hasActiveProgram = true,
            program = MapProgramToResponse(program)
        });
    }

    private async Task<string> GetProgramList(int userId)
    {
        var programs = await _db.TrainingPrograms
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(10)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Goal,
                p.Status,
                p.DaysPerWeek,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Json(programs);
    }

    private async Task<string> SearchExercises(JsonElement? args)
    {
        string query = "";
        string? category = null;
        int limit = 10;

        if (args.HasValue)
        {
            if (args.Value.TryGetProperty("query", out var qEl))
                query = qEl.GetString() ?? "";
            if (args.Value.TryGetProperty("category", out var catEl))
                category = catEl.GetString();
            if (args.Value.TryGetProperty("limit", out var limitEl))
                limit = Math.Clamp(limitEl.GetInt32(), 1, 20);
        }

        var dbQuery = _db.ExerciseDefinitions.AsQueryable();

        if (!string.IsNullOrEmpty(category))
            dbQuery = dbQuery.Where(e => e.Category == category);

        if (!string.IsNullOrEmpty(query))
            dbQuery = dbQuery.Where(e =>
                e.Name.Contains(query) ||
                e.DisplayName.Contains(query) ||
                e.TurkishName.Contains(query) ||
                e.EnglishName.Contains(query));

        var results = await dbQuery
            .OrderBy(e => e.RecommendedRank)
            .Take(limit)
            .Select(e => new
            {
                e.Name,
                e.Category,
                e.DisplayName,
                e.TrackingMode,
                e.PrimaryLabel,
                e.PrimaryUnit,
                e.SecondaryLabel,
                e.SecondaryUnit,
                e.MovementPattern,
                e.AthleticQuality,
                e.Progression,
                e.Regression
            })
            .ToListAsync();

        return Json(results);
    }

    private static string CalculateOneRm(JsonElement? args)
    {
        if (!args.HasValue) return Err("Missing arguments");

        if (!args.Value.TryGetProperty("weightKg", out var wEl) ||
            !args.Value.TryGetProperty("reps", out var rEl))
            return Err("weightKg and reps are required");

        int weight = wEl.GetInt32();
        int reps = rEl.GetInt32();
        int rir = 0;
        if (args.Value.TryGetProperty("rir", out var rirEl))
            rir = rirEl.GetInt32();

        try
        {
            double oneRm = CalculationService.CalculateOneRm(weight, reps, rir);
            var rmTable = CalculationService.BuildRmTable(oneRm);

            return JsonSerializer.Serialize(new
            {
                oneRm = Math.Round(oneRm, 1),
                rmTable = rmTable.Select((v, i) => new { rm = i + 1, weight = Math.Round(v, 1) })
            }, JsonOpts);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Err(ex.Message);
        }
    }

    private static string CalculateRsi(JsonElement? args)
    {
        if (!args.HasValue) return Err("Missing arguments");

        if (!args.Value.TryGetProperty("jumpHeightCm", out var hEl) ||
            !args.Value.TryGetProperty("groundContactTimeSeconds", out var gEl))
            return Err("jumpHeightCm and groundContactTimeSeconds are required");

        double height = hEl.GetDouble();
        double gct = gEl.GetDouble();

        try
        {
            double rsi = CalculationService.CalculateRsi(height, gct);
            return JsonSerializer.Serialize(new { rsi = Math.Round(rsi, 3) }, JsonOpts);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Err(ex.Message);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  COACH / WRITE TOOLS
    // ════════════════════════════════════════════════════════════════

    private async Task<string> CreateProgram(int userId, JsonElement? args)
    {
        if (!args.HasValue) return Err("Missing program data");

        string name = GetString(args.Value, "name") ?? "Training Program";
        string description = GetString(args.Value, "description") ?? "";
        string goal = GetString(args.Value, "goal") ?? "";
        int daysPerWeek = GetInt(args.Value, "daysPerWeek") ?? 4;
        int sessionDuration = GetInt(args.Value, "sessionDurationMinutes") ?? 60;
        string notes = GetString(args.Value, "notes") ?? "";

        // Get user context for sport/position
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Err("User not found");

        // Archive any current active program
        var activeProgram = await _db.TrainingPrograms
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == "active");
        if (activeProgram is not null)
        {
            activeProgram.Status = "archived";
            activeProgram.UpdatedAt = DateTime.UtcNow;
        }

        var program = new TrainingProgram
        {
            UserId = userId,
            Name = name,
            Description = description,
            Goal = goal,
            DaysPerWeek = daysPerWeek,
            SessionDurationMinutes = sessionDuration,
            Status = "active",
            Sport = user.SportName,
            Position = user.Position,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Parse weeks from args
        if (args.Value.TryGetProperty("weeks", out var weeksEl) && weeksEl.ValueKind == JsonValueKind.Array)
        {
            int weekNum = 1;
            foreach (var weekEl in weeksEl.EnumerateArray())
            {
                var week = new ProgramWeek
                {
                    WeekNumber = weekNum++,
                    Focus = GetString(weekEl, "focus") ?? "",
                    IsDeload = GetBool(weekEl, "isDeload")
                };

                if (weekEl.TryGetProperty("sessions", out var sessionsEl) && sessionsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sessionEl in sessionsEl.EnumerateArray())
                    {
                        var session = new ProgramSession
                        {
                            DayNumber = GetInt(sessionEl, "dayNumber") ?? 1,
                            SessionName = GetString(sessionEl, "sessionName") ?? "",
                            Focus = GetString(sessionEl, "focus") ?? "",
                            Notes = GetString(sessionEl, "notes") ?? ""
                        };

                        if (sessionEl.TryGetProperty("exercises", out var exercisesEl) && exercisesEl.ValueKind == JsonValueKind.Array)
                        {
                            int order = 1;
                            foreach (var exEl in exercisesEl.EnumerateArray())
                            {
                                session.Exercises.Add(new ProgramExercise
                                {
                                    Order = order++,
                                    ExerciseName = GetString(exEl, "exerciseName") ?? "",
                                    ExerciseCategory = GetString(exEl, "exerciseCategory") ?? "",
                                    Sets = GetInt(exEl, "sets") ?? 3,
                                    RepsOrDuration = GetString(exEl, "repsOrDuration") ?? "",
                                    IntensityGuidance = GetString(exEl, "intensityGuidance") ?? "",
                                    RestSeconds = GetInt(exEl, "restSeconds"),
                                    Notes = GetString(exEl, "notes") ?? "",
                                    SupersetGroup = GetString(exEl, "supersetGroup") ?? ""
                                });
                            }
                        }

                        week.Sessions.Add(session);
                    }
                }

                program.Weeks.Add(week);
            }
        }

        _db.TrainingPrograms.Add(program);
        await _db.SaveChangesAsync();
        _snapshotSink.OnUserUpdated(userId);

        _logger.LogInformation("Created training program {ProgramId} for user {UserId}", program.Id, userId);

        return Json(new
        {
            success = true,
            programId = program.Id,
            name = program.Name,
            status = program.Status,
            weekCount = program.Weeks.Count,
            message = "Training program created and set as active."
        });
    }

    private async Task<string> AdjustProgram(int userId, JsonElement? args)
    {
        if (!args.HasValue) return Err("Missing adjustment data");

        var program = await _db.TrainingPrograms
            .Where(p => p.UserId == userId && p.Status == "active")
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .FirstOrDefaultAsync();

        if (program is null)
            return Err("No active program found. Create a program first.");

        string adjustmentType = GetString(args.Value, "adjustmentType") ?? "";
        string reason = GetString(args.Value, "reason") ?? "";

        // Apply notes about the adjustment
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string adjustmentNote = $"[{timestamp}] {adjustmentType}: {reason}";
        program.Notes = string.IsNullOrWhiteSpace(program.Notes)
            ? adjustmentNote
            : $"{program.Notes}\n{adjustmentNote}";
        program.UpdatedAt = DateTime.UtcNow;

        // If updated weeks are provided, replace them
        if (args.Value.TryGetProperty("updatedWeeks", out var weeksEl) && weeksEl.ValueKind == JsonValueKind.Array)
        {
            // Remove old weeks (cascade deletes sessions and exercises)
            _db.ProgramWeeks.RemoveRange(program.Weeks);

            int weekNum = 1;
            foreach (var weekEl in weeksEl.EnumerateArray())
            {
                var week = new ProgramWeek
                {
                    TrainingProgramId = program.Id,
                    WeekNumber = weekNum++,
                    Focus = GetString(weekEl, "focus") ?? "",
                    IsDeload = GetBool(weekEl, "isDeload")
                };

                if (weekEl.TryGetProperty("sessions", out var sessionsEl) && sessionsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sessionEl in sessionsEl.EnumerateArray())
                    {
                        var session = new ProgramSession
                        {
                            DayNumber = GetInt(sessionEl, "dayNumber") ?? 1,
                            SessionName = GetString(sessionEl, "sessionName") ?? "",
                            Focus = GetString(sessionEl, "focus") ?? "",
                            Notes = GetString(sessionEl, "notes") ?? ""
                        };

                        if (sessionEl.TryGetProperty("exercises", out var exercisesEl) && exercisesEl.ValueKind == JsonValueKind.Array)
                        {
                            int order = 1;
                            foreach (var exEl in exercisesEl.EnumerateArray())
                            {
                                session.Exercises.Add(new ProgramExercise
                                {
                                    Order = order++,
                                    ExerciseName = GetString(exEl, "exerciseName") ?? "",
                                    ExerciseCategory = GetString(exEl, "exerciseCategory") ?? "",
                                    Sets = GetInt(exEl, "sets") ?? 3,
                                    RepsOrDuration = GetString(exEl, "repsOrDuration") ?? "",
                                    IntensityGuidance = GetString(exEl, "intensityGuidance") ?? "",
                                    RestSeconds = GetInt(exEl, "restSeconds"),
                                    Notes = GetString(exEl, "notes") ?? "",
                                    SupersetGroup = GetString(exEl, "supersetGroup") ?? ""
                                });
                            }
                        }

                        week.Sessions.Add(session);
                    }
                }

                _db.ProgramWeeks.Add(week);
            }
        }

        // If volume/intensity adjustment
        if (args.Value.TryGetProperty("volumeMultiplier", out var volEl))
        {
            double multiplier = volEl.GetDouble();
            foreach (var week in program.Weeks)
            foreach (var session in week.Sessions)
            foreach (var exercise in session.Exercises)
            {
                exercise.Sets = Math.Max(1, (int)Math.Round(exercise.Sets * multiplier));
            }
        }

        await _db.SaveChangesAsync();
        _snapshotSink.OnUserUpdated(userId);

        _logger.LogInformation("Adjusted program {ProgramId} for user {UserId}: {Type}", program.Id, userId, adjustmentType);

        return Json(new
        {
            success = true,
            programId = program.Id,
            adjustmentType,
            reason,
            message = $"Program adjusted: {adjustmentType}"
        });
    }

    private async Task<string> SwapExercise(int userId, JsonElement? args)
    {
        if (!args.HasValue) return Err("Missing swap data");

        string oldExercise = GetString(args.Value, "oldExercise") ?? "";
        string newExercise = GetString(args.Value, "newExercise") ?? "";
        string newCategory = GetString(args.Value, "newCategory") ?? "";
        string reason = GetString(args.Value, "reason") ?? "";

        if (string.IsNullOrWhiteSpace(oldExercise) || string.IsNullOrWhiteSpace(newExercise))
            return Err("oldExercise and newExercise are required");

        var program = await _db.TrainingPrograms
            .Where(p => p.UserId == userId && p.Status == "active")
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .FirstOrDefaultAsync();

        if (program is null)
            return Err("No active program found.");

        int swapCount = 0;
        foreach (var week in program.Weeks)
        foreach (var session in week.Sessions)
        foreach (var exercise in session.Exercises)
        {
            if (string.Equals(exercise.ExerciseName, oldExercise, StringComparison.OrdinalIgnoreCase))
            {
                exercise.ExerciseName = newExercise;
                if (!string.IsNullOrWhiteSpace(newCategory))
                    exercise.ExerciseCategory = newCategory;
                if (!string.IsNullOrWhiteSpace(reason))
                    exercise.Notes = string.IsNullOrWhiteSpace(exercise.Notes)
                        ? $"Swapped from {oldExercise}: {reason}"
                        : $"{exercise.Notes} | Swapped from {oldExercise}: {reason}";
                swapCount++;
            }
        }

        if (swapCount == 0)
            return Json(new { success = false, message = $"Exercise '{oldExercise}' not found in active program." });

        program.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Swapped {Old} → {New} ({Count}x) in program {ProgramId}", oldExercise, newExercise, swapCount, program.Id);

        return Json(new
        {
            success = true,
            swappedCount = swapCount,
            oldExercise,
            newExercise,
            reason,
            message = $"Swapped {oldExercise} → {newExercise} in {swapCount} place(s)."
        });
    }

    private async Task<string> SetProgramStatus(int userId, JsonElement? args)
    {
        if (!args.HasValue) return Err("Missing arguments");

        int? programId = GetInt(args.Value, "programId");
        string newStatus = GetString(args.Value, "status") ?? "";

        if (string.IsNullOrWhiteSpace(newStatus))
            return Err("status is required (active, completed, archived)");

        if (newStatus is not ("active" or "completed" or "archived" or "draft"))
            return Err("Invalid status. Must be: active, completed, archived, or draft.");

        TrainingProgram? program;
        if (programId.HasValue)
        {
            program = await _db.TrainingPrograms
                .FirstOrDefaultAsync(p => p.Id == programId.Value && p.UserId == userId);
        }
        else
        {
            program = await _db.TrainingPrograms
                .Where(p => p.UserId == userId && p.Status == "active")
                .FirstOrDefaultAsync();
        }

        if (program is null)
            return Err("Program not found.");

        // If activating, archive any other active program
        if (newStatus == "active")
        {
            var otherActive = await _db.TrainingPrograms
                .Where(p => p.UserId == userId && p.Status == "active" && p.Id != program.Id)
                .ToListAsync();
            foreach (var other in otherActive)
            {
                other.Status = "archived";
                other.UpdatedAt = DateTime.UtcNow;
            }
        }

        program.Status = newStatus;
        program.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _snapshotSink.OnUserUpdated(userId);

        return Json(new
        {
            success = true,
            programId = program.Id,
            newStatus,
            message = $"Program '{program.Name}' status changed to {newStatus}."
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════

    private static object MapProgramToResponse(TrainingProgram p) => new
    {
        p.Id,
        p.Name,
        p.Description,
        p.Goal,
        p.DaysPerWeek,
        p.SessionDurationMinutes,
        p.Status,
        p.Sport,
        p.Position,
        p.Notes,
        p.CreatedAt,
        p.UpdatedAt,
        weeks = p.Weeks.OrderBy(w => w.WeekNumber).Select(w => new
        {
            w.WeekNumber,
            w.Focus,
            w.IsDeload,
            sessions = w.Sessions.OrderBy(s => s.DayNumber).Select(s => new
            {
                s.DayNumber,
                s.SessionName,
                s.Focus,
                s.Notes,
                exercises = s.Exercises.OrderBy(x => x.Order).Select(x => new
                {
                    x.Order,
                    x.ExerciseName,
                    x.ExerciseCategory,
                    x.Sets,
                    x.RepsOrDuration,
                    x.IntensityGuidance,
                    x.RestSeconds,
                    x.Notes,
                    x.SupersetGroup
                })
            })
        })
    };

    private static string? GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String ? val.GetString() : null;

    private static int? GetInt(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetInt32() : null;

    private static bool GetBool(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var val) && val.ValueKind is JsonValueKind.True or JsonValueKind.False && val.GetBoolean();

    private static string Json(object obj) => JsonSerializer.Serialize(obj, JsonOpts);

    private static string Err(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOpts);
}
