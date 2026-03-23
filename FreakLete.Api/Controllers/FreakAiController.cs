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
    private readonly ILogger<FreakAiController> _logger;

    public FreakAiController(FreakAiOrchestrator orchestrator, ILogger<FreakAiController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<FreakAiChatResponse>> Chat(FreakAiChatRequest request)
    {
        var userId = User.GetUserId();

        _logger.LogInformation("FreakAI chat request from user {UserId}: {MessageLength} chars",
            userId, request.Message.Length);

        try
        {
            var reply = await _orchestrator.ChatAsync(userId, request.Message, request.History, HttpContext.RequestAborted);

            return Ok(new FreakAiChatResponse { Reply = reply });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Gemini API error"))
        {
            _logger.LogError(ex, "Gemini API error for user {UserId}", userId);
            return StatusCode(502, new { message = "AI service returned an error. Please try again in a moment." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Gemini for user {UserId}", userId);
            return StatusCode(503, new { message = "Could not reach AI service. Please check your connection and try again." });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Gemini request timed out for user {UserId}", userId);
            return StatusCode(504, new { message = "AI request timed out. Try a shorter or simpler message." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected FreakAI error for user {UserId}", userId);
            return StatusCode(500, new { message = "FreakAI is temporarily unavailable. Please try again." });
        }
    }
}
