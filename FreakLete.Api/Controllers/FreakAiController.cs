using FreakLete.Api.DTOs.FreakAi;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FreakAiController : ControllerBase
{
    private readonly FreakAiOrchestrator _orchestrator;
    private readonly FreakAiToolExecutor _toolExecutor;
    private readonly QuotaService _quota;
    private readonly EntitlementService _entitlement;
    private readonly ILogger<FreakAiController> _logger;

    public FreakAiController(
        FreakAiOrchestrator orchestrator,
        FreakAiToolExecutor toolExecutor,
        QuotaService quota,
        EntitlementService entitlement,
        ILogger<FreakAiController> logger)
    {
        _orchestrator = orchestrator;
        _toolExecutor = toolExecutor;
        _quota = quota;
        _entitlement = entitlement;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<FreakAiChatResponse>> Chat(FreakAiChatRequest request)
    {
        var userId = User.GetUserId();
        var ct = HttpContext.RequestAborted;

        // ── Intent resolution ──────────────────────────────
        var intent = IntentClassifier.Classify(request.Intent, request.Message);

        _logger.LogInformation(
            "FreakAI chat from user {UserId}: {Len} chars, intent={Intent} (client={ClientIntent})",
            userId, request.Message.Length, intent, request.Intent ?? "none");

        // ── Quota enforcement (before Gemini call) ─────────
        var denied = await _quota.CheckAsync(userId, intent, ct);
        if (denied is not null)
        {
            var plan = denied.Plan;
            await _quota.RecordUsageAsync(userId, intent, plan, wasBlocked: true,
                $"Quota exceeded: {denied.Window} {denied.Used}/{denied.Max}", ct);

            _logger.LogInformation(
                "FreakAI quota blocked user {UserId}: intent={Intent}, plan={Plan}, window={Window}, {Used}/{Max}",
                userId, intent, plan, denied.Window, denied.Used, denied.Max);

            var lang = LanguageDetector.Detect(request.Message);
            return StatusCode(429, new
            {
                message = GetQuotaDeniedMessage(lang, intent, denied),
                intent,
                plan,
                window = denied.Window,
                limit = denied.Max,
                used = denied.Used,
                resetsAtUtc = denied.ResetsAtUtc
            });
        }

        // ── Gemini call ────────────────────────────────────
        try
        {
            var reply = await _orchestrator.ChatAsync(userId, request.Message, request.History, ct);

            // Promote intent if Gemini called program-mutating tools
            var recordedIntent = intent;
            if (_toolExecutor.DidMutateProgramGenerate && intent != FreakAiUsageIntent.ProgramGenerate)
            {
                _logger.LogInformation(
                    "Promoting intent {Original} -> program_generate for user {UserId} (tools: {Tools})",
                    intent, userId, string.Join(", ", _toolExecutor.ExecutedTools));
                recordedIntent = FreakAiUsageIntent.ProgramGenerate;
            }

            // Record successful usage
            var currentPlan = await _entitlement.ResolvePlanAsync(userId, ct);
            await _quota.RecordUsageAsync(userId, recordedIntent, currentPlan, wasBlocked: false, ct: ct);

            return Ok(new FreakAiChatResponse { Reply = reply });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Gemini API error"))
        {
            _logger.LogError(ex, "Gemini API error for user {UserId}", userId);
            return StatusCode(502, new { message = FreakAiOrchestrator.GetLocalizedError(request.Message, "ai_error") });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Gemini for user {UserId}", userId);
            return StatusCode(503, new { message = FreakAiOrchestrator.GetLocalizedError(request.Message, "network_error") });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Gemini request timed out for user {UserId}", userId);
            return StatusCode(504, new { message = FreakAiOrchestrator.GetLocalizedError(request.Message, "timeout") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected FreakAI error for user {UserId}", userId);
            return StatusCode(500, new { message = FreakAiOrchestrator.GetLocalizedError(request.Message, "ai_error") });
        }
    }

    // ── Localized quota denial messages ─────────────────────

    private static string GetQuotaDeniedMessage(string langCode, string intent, QuotaDenied denied)
    {
        var intentLabel = GetIntentLabel(langCode, intent);
        var resetLabel = denied.ResetsAtUtc.ToString("yyyy-MM-dd HH:mm") + " UTC";

        return langCode switch
        {
            "tr" => $"{intentLabel} kullanım limitine ulaştınız. Limit {resetLabel} tarihinde sıfırlanacak.",
            "de" => $"Sie haben das Nutzungslimit für {intentLabel} erreicht. Das Limit wird am {resetLabel} zurückgesetzt.",
            "fr" => $"Vous avez atteint la limite d'utilisation pour {intentLabel}. La limite sera réinitialisée le {resetLabel}.",
            "es" => $"Has alcanzado el límite de uso para {intentLabel}. El límite se restablecerá el {resetLabel}.",
            _ => $"You've reached the usage limit for {intentLabel}. The limit resets at {resetLabel}."
        };
    }

    private static string GetIntentLabel(string langCode, string intent)
    {
        return (langCode, intent) switch
        {
            ("tr", FreakAiUsageIntent.ProgramGenerate) => "program oluşturma",
            ("tr", FreakAiUsageIntent.ProgramAnalyze) => "program analizi",
            ("tr", FreakAiUsageIntent.NutritionGuidance) => "beslenme rehberliği",
            ("tr", FreakAiUsageIntent.GeneralChat) => "genel sohbet",
            (_, FreakAiUsageIntent.ProgramGenerate) => "program generation",
            (_, FreakAiUsageIntent.ProgramAnalyze) => "program analysis",
            (_, FreakAiUsageIntent.NutritionGuidance) => "nutrition guidance",
            (_, FreakAiUsageIntent.GeneralChat) => "general chat",
            _ => intent
        };
    }
}
