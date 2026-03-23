using FreakLete.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SportCatalogController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<object>> GetAll()
    {
        var result = SportCatalog.All.Select(s => new
        {
            s.Id,
            s.Name,
            s.Category,
            s.HasPositions,
            s.Positions
        });

        return Ok(result);
    }

    [HttpGet("{sportId}/positions")]
    public ActionResult<IEnumerable<string>> GetPositions(string sportId)
    {
        var sport = SportCatalog.All.FirstOrDefault(s => s.Id == sportId);
        if (sport is null)
            return NotFound(new { message = "Sport not found." });

        return Ok(sport.Positions);
    }
}
