using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Performance;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AthleticPerformanceController : ControllerBase
{
    private readonly AppDbContext _db;

    public AthleticPerformanceController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AthleticPerformanceResponse>>> GetAll()
    {
        var userId = User.GetUserId();
        var entries = await _db.AthleticPerformanceEntries
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.RecordedAt)
            .ToListAsync();

        return Ok(entries.Select(MapToResponse).ToList());
    }

    [HttpGet("by-movement/{movementName}")]
    public async Task<ActionResult<List<AthleticPerformanceResponse>>> GetByMovement(string movementName)
    {
        var userId = User.GetUserId();
        var entries = await _db.AthleticPerformanceEntries
            .Where(a => a.UserId == userId && a.MovementName == movementName)
            .OrderByDescending(a => a.RecordedAt)
            .ToListAsync();

        return Ok(entries.Select(MapToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<AthleticPerformanceResponse>> Create(AthleticPerformanceRequest request)
    {
        var userId = User.GetUserId();
        var entry = new AthleticPerformanceEntry
        {
            UserId = userId,
            MovementName = request.MovementName,
            MovementCategory = request.MovementCategory,
            Value = request.Value,
            Unit = request.Unit,
            SecondaryValue = request.SecondaryValue,
            SecondaryUnit = request.SecondaryUnit,
            GroundContactTimeMs = request.GroundContactTimeMs,
            ConcentricTimeSeconds = request.ConcentricTimeSeconds,
            RecordedAt = DateTime.UtcNow
        };

        _db.AthleticPerformanceEntries.Add(entry);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), MapToResponse(entry));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, AthleticPerformanceRequest request)
    {
        var userId = User.GetUserId();
        var entry = await _db.AthleticPerformanceEntries
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (entry is null) return NotFound();

        entry.MovementName = request.MovementName;
        entry.MovementCategory = request.MovementCategory;
        entry.Value = request.Value;
        entry.Unit = request.Unit;
        entry.SecondaryValue = request.SecondaryValue;
        entry.SecondaryUnit = request.SecondaryUnit;
        entry.GroundContactTimeMs = request.GroundContactTimeMs;
        entry.ConcentricTimeSeconds = request.ConcentricTimeSeconds;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var entry = await _db.AthleticPerformanceEntries
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (entry is null) return NotFound();

        _db.AthleticPerformanceEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static AthleticPerformanceResponse MapToResponse(AthleticPerformanceEntry a) => new()
    {
        Id = a.Id,
        MovementName = a.MovementName,
        MovementCategory = a.MovementCategory,
        Value = a.Value,
        Unit = a.Unit,
        SecondaryValue = a.SecondaryValue,
        SecondaryUnit = a.SecondaryUnit,
        GroundContactTimeMs = a.GroundContactTimeMs,
        ConcentricTimeSeconds = a.ConcentricTimeSeconds,
        RecordedAt = a.RecordedAt
    };
}
