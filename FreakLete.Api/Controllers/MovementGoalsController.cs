using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Goal;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MovementGoalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MovementGoalsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MovementGoalResponse>>> GetAll()
    {
        var userId = User.GetUserId();
        var goals = await _db.MovementGoals
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return Ok(goals.Select(MapToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<MovementGoalResponse>> Create(MovementGoalRequest request)
    {
        var userId = User.GetUserId();
        var goal = new MovementGoal
        {
            UserId = userId,
            MovementName = request.MovementName,
            MovementCategory = request.MovementCategory,
            GoalMetricLabel = request.GoalMetricLabel,
            TargetValue = request.TargetValue,
            Unit = request.Unit,
            CreatedAt = DateTime.UtcNow
        };

        _db.MovementGoals.Add(goal);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), MapToResponse(goal));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, MovementGoalRequest request)
    {
        var userId = User.GetUserId();
        var goal = await _db.MovementGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null) return NotFound();

        goal.MovementName = request.MovementName;
        goal.MovementCategory = request.MovementCategory;
        goal.GoalMetricLabel = request.GoalMetricLabel;
        goal.TargetValue = request.TargetValue;
        goal.Unit = request.Unit;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var goal = await _db.MovementGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null) return NotFound();

        _db.MovementGoals.Remove(goal);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static MovementGoalResponse MapToResponse(MovementGoal g) => new()
    {
        Id = g.Id,
        MovementName = g.MovementName,
        MovementCategory = g.MovementCategory,
        GoalMetricLabel = g.GoalMetricLabel,
        TargetValue = g.TargetValue,
        Unit = g.Unit,
        CreatedAt = g.CreatedAt
    };
}
