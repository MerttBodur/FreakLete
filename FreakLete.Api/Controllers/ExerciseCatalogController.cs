using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Exercise;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExerciseCatalogController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExerciseCatalogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExerciseDefinitionResponse>>> GetAll()
    {
        var exercises = await _db.ExerciseDefinitions
            .OrderBy(e => e.Category)
            .ThenBy(e => e.RecommendedRank)
            .ToListAsync();

        return Ok(exercises.Select(MapToResponse).ToList());
    }

    [HttpGet("by-category/{category}")]
    public async Task<ActionResult<List<ExerciseDefinitionResponse>>> GetByCategory(string category)
    {
        var exercises = await _db.ExerciseDefinitions
            .Where(e => e.Category == category)
            .OrderBy(e => e.RecommendedRank)
            .ToListAsync();

        return Ok(exercises.Select(MapToResponse).ToList());
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<ExerciseDefinitionResponse>>> Search(
        [FromQuery] string q,
        [FromQuery] string? category = null)
    {
        var query = _db.ExerciseDefinitions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        var lowerQ = q.ToLower();
        query = query.Where(e =>
            e.Name.ToLower().Contains(lowerQ) ||
            e.DisplayName.ToLower().Contains(lowerQ) ||
            e.TurkishName.ToLower().Contains(lowerQ) ||
            e.PrimaryMusclesText.ToLower().Contains(lowerQ));

        var exercises = await query.OrderBy(e => e.RecommendedRank).ToListAsync();
        return Ok(exercises.Select(MapToResponse).ToList());
    }

    [HttpGet("{catalogId}")]
    public async Task<ActionResult<ExerciseDefinitionResponse>> GetById(string catalogId)
    {
        var exercise = await _db.ExerciseDefinitions.FindAsync(catalogId);
        if (exercise is null) return NotFound();
        return Ok(MapToResponse(exercise));
    }

    private static ExerciseDefinitionResponse MapToResponse(Entities.ExerciseDefinition e) => new()
    {
        CatalogId = e.CatalogId,
        Name = e.Name,
        DisplayName = e.DisplayName,
        TurkishName = e.TurkishName,
        EnglishName = e.EnglishName,
        Category = e.Category,
        Force = e.Force,
        Level = e.Level,
        Mechanic = e.Mechanic,
        Equipment = e.Equipment,
        PrimaryMusclesText = e.PrimaryMusclesText,
        SecondaryMusclesText = e.SecondaryMusclesText,
        InstructionsText = e.InstructionsText,
        TrackingMode = e.TrackingMode,
        PrimaryLabel = e.PrimaryLabel,
        PrimaryUnit = e.PrimaryUnit,
        SecondaryLabel = e.SecondaryLabel,
        SecondaryUnit = e.SecondaryUnit,
        SupportsGroundContactTime = e.SupportsGroundContactTime,
        SupportsConcentricTime = e.SupportsConcentricTime,
        MovementPattern = e.MovementPattern,
        AthleticQuality = e.AthleticQuality,
        NervousSystemLoad = e.NervousSystemLoad,
        RecommendedRank = e.RecommendedRank
    };
}
