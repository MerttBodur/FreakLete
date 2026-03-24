using System.Diagnostics;
using System.Text.Json;

namespace FreakLete.Api.Services;

public class FreakAiOrchestrator
{
    private readonly GeminiClient _gemini;
    private readonly FreakAiToolExecutor _toolExecutor;
    private readonly ILogger<FreakAiOrchestrator> _logger;

    private const int MaxToolRounds = 5;
    private const int MaxHistoryMessages = 20;
    private static readonly TimeSpan MaxChatDuration = TimeSpan.FromSeconds(40);

    public FreakAiOrchestrator(
        GeminiClient gemini,
        FreakAiToolExecutor toolExecutor,
        ILogger<FreakAiOrchestrator> logger)
    {
        _gemini = gemini;
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task<string> ChatAsync(
        int userId,
        string userMessage,
        List<ChatMessage>? history,
        CancellationToken cancellationToken = default)
    {
        var totalSw = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(MaxChatDuration);

        // ── Language detection ──────────────────────────────────
        string detectedLang = LanguageDetector.Detect(userMessage);
        string langName = LanguageDetector.GetLanguageName(detectedLang);

        _logger.LogInformation(
            "FreakAI chat for user {UserId}: detected language={Lang} ({LangName}), messageLen={Len}",
            userId, detectedLang, langName, userMessage.Length);

        // ── Build request with language-aware system prompt ─────
        var contents = BuildContents(userMessage, history);
        var tools = BuildToolDeclarations();
        var systemPrompt = BuildLanguageAwarePrompt(detectedLang, langName);

        var request = new GeminiRequest
        {
            SystemInstruction = new GeminiSystemInstruction
            {
                Parts = [new GeminiPart { Text = systemPrompt }]
            },
            Contents = contents,
            Tools = [new GeminiTool { FunctionDeclarations = tools }],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                MaxOutputTokens = 2048
            }
        };

        // ── Tool-calling loop with per-round timing ─────────────
        for (int round = 0; round < MaxToolRounds; round++)
        {
            var roundSw = Stopwatch.StartNew();

            var response = await _gemini.GenerateContentAsync(request, timeoutCts.Token);

            roundSw.Stop();
            _logger.LogInformation(
                "FreakAI round {Round}/{Max} for user {UserId}: Gemini call took {ElapsedMs}ms",
                round + 1, MaxToolRounds, userId, roundSw.ElapsedMilliseconds);

            var candidate = response.Candidates?.FirstOrDefault();
            if (candidate?.Content is null)
            {
                _logger.LogWarning("FreakAI: empty candidate in round {Round} for user {UserId}", round + 1, userId);
                return GetLocalizedErrorMessage(detectedLang, "empty_response");
            }

            var parts = candidate.Content.Parts;

            // Check if model wants to call functions
            var functionCalls = parts.Where(p => p.FunctionCall is not null).ToList();

            if (functionCalls.Count == 0)
            {
                // No function calls — return text response
                var text = string.Join("\n", parts
                    .Where(p => !string.IsNullOrWhiteSpace(p.Text))
                    .Select(p => p.Text));

                totalSw.Stop();
                _logger.LogInformation(
                    "FreakAI completed for user {UserId}: {Rounds} round(s), total {TotalMs}ms, lang={Lang}",
                    userId, round + 1, totalSw.ElapsedMilliseconds, detectedLang);

                if (string.IsNullOrWhiteSpace(text))
                    return GetLocalizedErrorMessage(detectedLang, "no_data");

                return text;
            }

            // Add model's response to contents
            request.Contents.Add(new GeminiContent
            {
                Role = "model",
                Parts = parts
            });

            // Execute each function call and add results
            var functionResponseParts = new List<GeminiPart>();
            foreach (var fc in functionCalls)
            {
                var call = fc.FunctionCall!;
                var toolSw = Stopwatch.StartNew();

                _logger.LogInformation(
                    "FreakAI tool call round {Round}: {Tool} for user {UserId}",
                    round + 1, call.Name, userId);

                var result = await _toolExecutor.ExecuteToolAsync(userId, call.Name, call.Args);

                toolSw.Stop();
                _logger.LogInformation(
                    "FreakAI tool {Tool} completed in {ElapsedMs}ms for user {UserId}",
                    call.Name, toolSw.ElapsedMilliseconds, userId);

                // Gemini requires functionResponse.response to be a JSON object (Struct)
                var parsed = JsonSerializer.Deserialize<JsonElement>(result);
                var responseElement = parsed.ValueKind == JsonValueKind.Object
                    ? parsed
                    : JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { result = parsed }));

                functionResponseParts.Add(new GeminiPart
                {
                    FunctionResponse = new GeminiFunctionResponse
                    {
                        Name = call.Name,
                        Response = responseElement
                    }
                });
            }

            request.Contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = functionResponseParts
            });
        }

        totalSw.Stop();
        _logger.LogWarning(
            "FreakAI hit max tool rounds ({Max}) for user {UserId}, total {TotalMs}ms",
            MaxToolRounds, userId, totalSw.ElapsedMilliseconds);

        return GetLocalizedErrorMessage(detectedLang, "too_complex");
    }

    // ── Language-aware system prompt ────────────────────────────

    private static string BuildLanguageAwarePrompt(string langCode, string langName)
    {
        var basePrompt = FreakAiSystemPrompt.Build();

        // Prepend a hard language directive that the model sees first
        string langDirective = $"""
            ## MANDATORY RESPONSE LANGUAGE: {langName} ({langCode})
            The user's latest message is in {langName}. You MUST write your ENTIRE response in {langName}.
            This includes: explanations, coaching cues, program names, session names, notes, and all text output.
            Tool results are in English — translate/adapt them naturally into {langName}.
            Technical exercise names (Bench Press, Squat, Deadlift) may stay in English only if that is natural usage in {langName}.
            DO NOT switch to English unless the detected language IS English.

            """;

        return langDirective + basePrompt;
    }

    // ── Localized error messages ────────────────────────────────

    private static string GetLocalizedErrorMessage(string langCode, string errorType)
    {
        return (langCode, errorType) switch
        {
            ("tr", "empty_response") => "Yanıt oluşturulamadı. Lütfen tekrar deneyin.",
            ("tr", "no_data") => "Yeterli veri yok. Lütfen profilinizi ve antrenman tercihlerinizi doldurun.",
            ("tr", "too_complex") => "İsteğinizi işlerken sorun oluştu. Lütfen daha kısa veya basit bir mesaj deneyin.",
            ("tr", "timeout") => "İstek zaman aşımına uğradı. Lütfen daha kısa bir mesaj deneyin.",
            ("tr", "ai_error") => "Yapay zeka servisi geçici olarak kullanılamıyor. Lütfen tekrar deneyin.",
            ("tr", "network_error") => "Bağlantı hatası. Lütfen internet bağlantınızı kontrol edip tekrar deneyin.",

            ("de", "empty_response") => "Antwort konnte nicht generiert werden. Bitte versuchen Sie es erneut.",
            ("de", "no_data") => "Nicht genügend Daten. Bitte füllen Sie Ihr Profil aus.",
            ("de", "too_complex") => "Verarbeitungsproblem. Bitte versuchen Sie eine einfachere Frage.",

            ("fr", "empty_response") => "Impossible de générer une réponse. Veuillez réessayer.",
            ("fr", "no_data") => "Données insuffisantes. Veuillez compléter votre profil.",
            ("fr", "too_complex") => "Problème de traitement. Essayez une question plus simple.",

            ("es", "empty_response") => "No se pudo generar una respuesta. Inténtelo de nuevo.",
            ("es", "no_data") => "Datos insuficientes. Complete su perfil primero.",
            ("es", "too_complex") => "Problema de procesamiento. Intente una pregunta más simple.",

            // Default: English fallback for all other languages/error types
            (_, "empty_response") => "I couldn't generate a response. Please try again.",
            (_, "no_data") => "I don't have enough data to help with that yet. Please fill out your profile and coach preferences first.",
            (_, "too_complex") => "I'm having trouble processing your request. Please try a shorter or simpler message.",
            (_, "timeout") => "AI request timed out. Try a shorter or simpler message.",
            (_, "ai_error") => "AI service returned an error. Please try again in a moment.",
            (_, "network_error") => "Could not reach AI service. Please check your connection and try again.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    /// <summary>
    /// Public accessor so the controller can get localized error messages too.
    /// </summary>
    public static string GetLocalizedError(string userMessage, string errorType)
    {
        string lang = LanguageDetector.Detect(userMessage);
        return GetLocalizedErrorMessage(lang, errorType);
    }

    // ── Build contents ──────────────────────────────────────────

    private static List<GeminiContent> BuildContents(string userMessage, List<ChatMessage>? history)
    {
        var contents = new List<GeminiContent>();

        if (history is not null)
        {
            var bounded = history.Count > MaxHistoryMessages
                ? history.Skip(history.Count - MaxHistoryMessages).ToList()
                : history;

            foreach (var msg in bounded)
            {
                contents.Add(new GeminiContent
                {
                    Role = msg.Role == "user" ? "user" : "model",
                    Parts = [new GeminiPart { Text = msg.Content }]
                });
            }
        }

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = [new GeminiPart { Text = userMessage }]
        });

        return contents;
    }

    private static List<GeminiFunctionDeclaration> BuildToolDeclarations() =>
    [
        // ── Read / Context tools ──────────────────────────────────

        new()
        {
            Name = "get_user_profile",
            Description = "Get the user's profile: sport, position, body metrics, experience, goals, dietary preference, and account stats."
        },
        new()
        {
            Name = "get_training_preferences",
            Description = "Get the user's training preferences: days per week, session duration, goals, sport, position, experience level."
        },
        new()
        {
            Name = "get_equipment_profile",
            Description = "Get the user's available equipment list. Essential before writing a program to avoid prescribing exercises the user can't perform."
        },
        new()
        {
            Name = "get_physical_limitations",
            Description = "Get the user's physical limitations and current pain points. Must check before writing any program or exercise recommendation."
        },
        new()
        {
            Name = "get_injury_context",
            Description = "Get the user's injury history, current pain points, and physical limitations. Use when user mentions pain, injury, or discomfort."
        },
        new()
        {
            Name = "get_training_summary",
            Description = "Get a statistical summary of the user's training over a period: workout frequency, volume, exercise distribution, PR count.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["days"] = new() { Type = "integer", Description = "Number of days to look back. Default 30." }
                }
            }
        },
        new()
        {
            Name = "get_recent_workouts",
            Description = "Get the user's most recent workouts with exercise details (sets, reps, weight, metrics).",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["limit"] = new() { Type = "integer", Description = "Number of workouts to return. Default 10, max 30." }
                }
            }
        },
        new()
        {
            Name = "get_pr_history",
            Description = "Get the user's personal record entries. Can filter by exercise name.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["exerciseName"] = new() { Type = "string", Description = "Filter PRs by exercise name." },
                    ["limit"] = new() { Type = "integer", Description = "Number of PRs to return. Default 20, max 50." }
                }
            }
        },
        new()
        {
            Name = "get_athletic_performance_history",
            Description = "Get the user's athletic performance entries (sprint times, jump heights, etc). Can filter by movement name.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["movementName"] = new() { Type = "string", Description = "Filter by movement name." },
                    ["limit"] = new() { Type = "integer", Description = "Number of entries to return. Default 20, max 50." }
                }
            }
        },
        new()
        {
            Name = "get_movement_goals",
            Description = "Get all of the user's active movement goals with target values."
        },
        new()
        {
            Name = "get_current_program",
            Description = "Get the user's current active training program with all weeks, sessions, and exercises. Use before adjusting or discussing the program."
        },
        new()
        {
            Name = "get_program_list",
            Description = "List all of the user's training programs (active, completed, archived)."
        },
        new()
        {
            Name = "search_exercises",
            Description = "Search the exercise catalog by name or category. Returns exercise details including progression/regression paths.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["query"] = new() { Type = "string", Description = "Search term for exercise name." },
                    ["category"] = new() { Type = "string", Description = "Filter by category (Push, Pull, SquatVariation, DeadliftVariation, Sprint, Jumps, Plyometrics, OlympicLifts)." },
                    ["limit"] = new() { Type = "integer", Description = "Max results. Default 10, max 20." }
                }
            }
        },
        new()
        {
            Name = "calculate_one_rm",
            Description = "Calculate estimated 1 rep max from weight, reps, and RIR. Returns 1RM and a rep max table.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["weightKg"] = new() { Type = "integer", Description = "Weight lifted in kg." },
                    ["reps"] = new() { Type = "integer", Description = "Reps performed." },
                    ["rir"] = new() { Type = "integer", Description = "Reps in reserve. Default 0." }
                },
                Required = ["weightKg", "reps"]
            }
        },
        new()
        {
            Name = "calculate_rsi",
            Description = "Calculate Reactive Strength Index from jump height and ground contact time.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["jumpHeightCm"] = new() { Type = "number", Description = "Jump height in centimeters." },
                    ["groundContactTimeSeconds"] = new() { Type = "number", Description = "Ground contact time in seconds." }
                },
                Required = ["jumpHeightCm", "groundContactTimeSeconds"]
            }
        },

        // ── Coach / Write tools ──────────────────────────────────

        new()
        {
            Name = "create_program",
            Description = "Create a new training program for the user. Automatically becomes the active program (previous active program is archived). Structure: weeks → sessions → exercises. ALWAYS fetch user profile, training preferences, equipment, and limitations BEFORE calling this tool.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["name"] = new() { Type = "string", Description = "Program name, e.g. 'Strength & Power Block 1'" },
                    ["description"] = new() { Type = "string", Description = "Brief program description and philosophy" },
                    ["goal"] = new() { Type = "string", Description = "Primary goal of the program" },
                    ["daysPerWeek"] = new() { Type = "integer", Description = "Training days per week" },
                    ["sessionDurationMinutes"] = new() { Type = "integer", Description = "Target session duration in minutes" },
                    ["notes"] = new() { Type = "string", Description = "Coach notes about the program design rationale" },
                    ["weeks"] = new()
                    {
                        Type = "array",
                        Description = "Array of week objects",
                        Items = new GeminiSchemaProperty
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiSchemaProperty>
                            {
                                ["focus"] = new() { Type = "string", Description = "Week focus, e.g. 'Hypertrophy' or 'Strength'" },
                                ["isDeload"] = new() { Type = "boolean", Description = "Whether this is a deload week" },
                                ["sessions"] = new()
                                {
                                    Type = "array",
                                    Description = "Array of session objects for this week",
                                    Items = new GeminiSchemaProperty
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, GeminiSchemaProperty>
                                        {
                                            ["dayNumber"] = new() { Type = "integer", Description = "Day number 1-7" },
                                            ["sessionName"] = new() { Type = "string", Description = "Session name, e.g. 'Upper Body A'" },
                                            ["focus"] = new() { Type = "string", Description = "Session focus" },
                                            ["notes"] = new() { Type = "string", Description = "Coach notes for this session" },
                                            ["exercises"] = new()
                                            {
                                                Type = "array",
                                                Description = "Array of exercises for this session",
                                                Items = new GeminiSchemaProperty
                                                {
                                                    Type = "object",
                                                    Properties = new Dictionary<string, GeminiSchemaProperty>
                                                    {
                                                        ["exerciseName"] = new() { Type = "string", Description = "Exercise name" },
                                                        ["exerciseCategory"] = new() { Type = "string", Description = "Category: Push, Pull, SquatVariation, etc." },
                                                        ["sets"] = new() { Type = "integer", Description = "Number of sets" },
                                                        ["repsOrDuration"] = new() { Type = "string", Description = "Reps or duration, e.g. '8-10' or '30s'" },
                                                        ["intensityGuidance"] = new() { Type = "string", Description = "e.g. '70% 1RM' or 'RPE 7'" },
                                                        ["restSeconds"] = new() { Type = "integer", Description = "Rest between sets in seconds" },
                                                        ["notes"] = new() { Type = "string", Description = "Exercise-specific notes" },
                                                        ["supersetGroup"] = new() { Type = "string", Description = "Superset group letter, e.g. 'A'" }
                                                    },
                                                    Required = ["exerciseName", "sets", "repsOrDuration"]
                                                }
                                            }
                                        },
                                        Required = ["dayNumber", "sessionName", "exercises"]
                                    }
                                }
                            },
                            Required = ["sessions"]
                        }
                    }
                },
                Required = ["name", "goal", "daysPerWeek", "weeks"]
            }
        },
        new()
        {
            Name = "adjust_program",
            Description = "Adjust the user's active training program. Can modify volume, replace weeks, or add notes. Use when user gives feedback about pain, fatigue, time constraints, or exercise preferences. ALWAYS fetch the current program first before adjusting.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["adjustmentType"] = new() { Type = "string", Description = "Type: volume_reduction, volume_increase, deload_insertion, pain_modification, schedule_change, exercise_swap, intensity_adjustment, rehab_modification" },
                    ["reason"] = new() { Type = "string", Description = "Why this adjustment is being made" },
                    ["volumeMultiplier"] = new() { Type = "number", Description = "Optional: multiply all sets by this factor (e.g. 0.6 for deload, 1.2 for volume increase)" },
                    ["updatedWeeks"] = new()
                    {
                        Type = "array",
                        Description = "Optional: replacement weeks array (same structure as create_program weeks). If provided, replaces all existing weeks.",
                        Items = new GeminiSchemaProperty
                        {
                            Type = "object",
                            Properties = new Dictionary<string, GeminiSchemaProperty>
                            {
                                ["focus"] = new() { Type = "string", Description = "Week focus" },
                                ["isDeload"] = new() { Type = "boolean", Description = "Whether this is a deload week" },
                                ["sessions"] = new()
                                {
                                    Type = "array",
                                    Description = "Sessions for this week",
                                    Items = new GeminiSchemaProperty
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, GeminiSchemaProperty>
                                        {
                                            ["dayNumber"] = new() { Type = "integer", Description = "Day number 1-7" },
                                            ["sessionName"] = new() { Type = "string", Description = "Session name" },
                                            ["focus"] = new() { Type = "string", Description = "Session focus" },
                                            ["notes"] = new() { Type = "string", Description = "Coach notes" },
                                            ["exercises"] = new()
                                            {
                                                Type = "array",
                                                Description = "Exercises for this session",
                                                Items = new GeminiSchemaProperty
                                                {
                                                    Type = "object",
                                                    Properties = new Dictionary<string, GeminiSchemaProperty>
                                                    {
                                                        ["exerciseName"] = new() { Type = "string", Description = "Exercise name" },
                                                        ["exerciseCategory"] = new() { Type = "string", Description = "Category" },
                                                        ["sets"] = new() { Type = "integer", Description = "Number of sets" },
                                                        ["repsOrDuration"] = new() { Type = "string", Description = "Reps or duration" },
                                                        ["intensityGuidance"] = new() { Type = "string", Description = "Intensity guidance" },
                                                        ["restSeconds"] = new() { Type = "integer", Description = "Rest in seconds" },
                                                        ["notes"] = new() { Type = "string", Description = "Notes" },
                                                        ["supersetGroup"] = new() { Type = "string", Description = "Superset group" }
                                                    },
                                                    Required = ["exerciseName", "sets", "repsOrDuration"]
                                                }
                                            }
                                        },
                                        Required = ["dayNumber", "sessionName", "exercises"]
                                    }
                                }
                            },
                            Required = ["sessions"]
                        }
                    }
                },
                Required = ["adjustmentType", "reason"]
            }
        },
        new()
        {
            Name = "swap_exercise",
            Description = "Swap a specific exercise in the user's active program. Use when user can't perform an exercise due to equipment, pain, or preference.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["oldExercise"] = new() { Type = "string", Description = "Exercise name to replace" },
                    ["newExercise"] = new() { Type = "string", Description = "Replacement exercise name" },
                    ["newCategory"] = new() { Type = "string", Description = "Category of the new exercise (optional)" },
                    ["reason"] = new() { Type = "string", Description = "Why the swap is being made" }
                },
                Required = ["oldExercise", "newExercise", "reason"]
            }
        },
        new()
        {
            Name = "set_program_status",
            Description = "Change a program's status (active, completed, archived, draft). Only one program can be active at a time.",
            Parameters = new GeminiSchema
            {
                Properties = new Dictionary<string, GeminiSchemaProperty>
                {
                    ["programId"] = new() { Type = "integer", Description = "Program ID. If omitted, targets the current active program." },
                    ["status"] = new() { Type = "string", Description = "New status: active, completed, archived, or draft" }
                },
                Required = ["status"]
            }
        }
    ];
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}
