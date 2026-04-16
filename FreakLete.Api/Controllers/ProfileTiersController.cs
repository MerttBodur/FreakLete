using FreakLete.Api.DTOs.Tier;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileTiersController : ControllerBase
{
    private readonly IExerciseTierService _service;

    public ProfileTiersController(IExerciseTierService service)
    {
        _service = service;
    }

    [HttpGet("tiers")]
    public async Task<ActionResult<List<ExerciseTierDto>>> GetTiers(CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await _service.GetTiersForUserAsync(userId, ct));
    }

    [HttpPost("tiers/recalculate")]
    public async Task<ActionResult<List<ExerciseTierDto>>> RecalculateTiers(CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _service.BackfillTiersFromPrEntriesAsync(userId, ct);
        return Ok(await _service.GetTiersForUserAsync(userId, ct));
    }
}
