using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Performance;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using FreakLete.Api.Services.Embeddings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/pr-entries")]
public class PrEntriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IExerciseTierService _tierService;
    private readonly IUserSnapshotEventSink _snapshotSink;

    public PrEntriesController(
        AppDbContext db,
        IExerciseTierService tierService,
        IUserSnapshotEventSink snapshotSink)
    {
        _db = db;
        _tierService = tierService;
        _snapshotSink = snapshotSink;
    }

    [HttpGet]
    public async Task<ActionResult<List<PrEntryResponse>>> GetAll()
    {
        var userId = User.GetUserId();
        var entries = await _db.PrEntries
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(entries.Select(MapToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PrEntryResponse>> GetById(int id)
    {
        var userId = User.GetUserId();
        var entry = await _db.PrEntries.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (entry is null) return NotFound();
        return Ok(MapToResponse(entry));
    }

    [HttpPost]
    public async Task<ActionResult<PrEntryResponse>> Create(PrEntryRequest request)
    {
        var userId = User.GetUserId();
        var entry = new PrEntry
        {
            UserId = userId,
            ExerciseName = request.ExerciseName,
            ExerciseCategory = request.ExerciseCategory,
            TrackingMode = request.TrackingMode,
            Weight = request.Weight,
            Reps = request.Reps,
            RIR = request.RIR,
            Metric1Value = request.Metric1Value,
            Metric1Unit = request.Metric1Unit,
            Metric2Value = request.Metric2Value,
            Metric2Unit = request.Metric2Unit,
            GroundContactTimeMs = request.GroundContactTimeMs,
            ConcentricTimeSeconds = request.ConcentricTimeSeconds,
            CreatedAt = DateTime.UtcNow
        };

        _db.PrEntries.Add(entry);
        await _db.SaveChangesAsync();
        _snapshotSink.OnUserUpdated(userId);

        var tier = await _tierService.RecalculateTierAsync(
            userId,
            request.CatalogId,
            request.ExerciseName,
            request.TrackingMode,
            request.Weight,
            request.Reps,
            request.RIR,
            athleticRawValue: request.Metric1Value);

        var response = MapToResponse(entry);
        response.Tier = tier;
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, PrEntryRequest request)
    {
        var userId = User.GetUserId();
        var entry = await _db.PrEntries.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (entry is null) return NotFound();

        entry.ExerciseName = request.ExerciseName;
        entry.ExerciseCategory = request.ExerciseCategory;
        entry.TrackingMode = request.TrackingMode;
        entry.Weight = request.Weight;
        entry.Reps = request.Reps;
        entry.RIR = request.RIR;
        entry.Metric1Value = request.Metric1Value;
        entry.Metric1Unit = request.Metric1Unit;
        entry.Metric2Value = request.Metric2Value;
        entry.Metric2Unit = request.Metric2Unit;
        entry.GroundContactTimeMs = request.GroundContactTimeMs;
        entry.ConcentricTimeSeconds = request.ConcentricTimeSeconds;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var entry = await _db.PrEntries.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (entry is null) return NotFound();

        _db.PrEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static PrEntryResponse MapToResponse(PrEntry p) => new()
    {
        Id = p.Id,
        ExerciseName = p.ExerciseName,
        ExerciseCategory = p.ExerciseCategory,
        TrackingMode = p.TrackingMode,
        Weight = p.Weight,
        Reps = p.Reps,
        RIR = p.RIR,
        Metric1Value = p.Metric1Value,
        Metric1Unit = p.Metric1Unit,
        Metric2Value = p.Metric2Value,
        Metric2Unit = p.Metric2Unit,
        GroundContactTimeMs = p.GroundContactTimeMs,
        ConcentricTimeSeconds = p.ConcentricTimeSeconds,
        CreatedAt = p.CreatedAt
    };
}
