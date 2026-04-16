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
}
