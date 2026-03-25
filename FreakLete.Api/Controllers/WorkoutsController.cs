using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Workout;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkoutsController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkoutsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkoutResponse>>> GetAll()
    {
        var userId = User.GetUserId();
        var workouts = await _db.Workouts
            .Where(w => w.UserId == userId)
            .Include(w => w.ExerciseEntries)
            .OrderByDescending(w => w.WorkoutDate)
            .ToListAsync();

        return Ok(workouts.Select(MapToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkoutResponse>> GetById(int id)
    {
        var userId = User.GetUserId();
        var workout = await _db.Workouts
            .Include(w => w.ExerciseEntries)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workout is null) return NotFound();
        return Ok(MapToResponse(workout));
    }

    [HttpGet("by-date/{date}")]
    public async Task<ActionResult<List<WorkoutResponse>>> GetByDate(DateTime date)
    {
        var userId = User.GetUserId();
        var workouts = await _db.Workouts
            .Where(w => w.UserId == userId && w.WorkoutDate.Date == DateTime.SpecifyKind(date, DateTimeKind.Utc).Date)
            .Include(w => w.ExerciseEntries)
            .ToListAsync();

        return Ok(workouts.Select(MapToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<WorkoutResponse>> Create(WorkoutRequest request)
    {
        var userId = User.GetUserId();
        var workout = new Workout
        {
            UserId = userId,
            WorkoutName = request.WorkoutName,
            WorkoutDate = DateTime.SpecifyKind(request.WorkoutDate, DateTimeKind.Utc),
            ExerciseEntries = request.Exercises.Select(e => new ExerciseEntry
            {
                ExerciseName = e.ExerciseName,
                ExerciseCategory = e.ExerciseCategory,
                TrackingMode = e.TrackingMode,
                Sets = e.Sets,
                Reps = e.Reps,
                RIR = e.RIR,
                RestSeconds = e.RestSeconds,
                GroundContactTimeMs = e.GroundContactTimeMs,
                ConcentricTimeSeconds = e.ConcentricTimeSeconds,
                Metric1Value = e.Metric1Value,
                Metric1Unit = e.Metric1Unit,
                Metric2Value = e.Metric2Value,
                Metric2Unit = e.Metric2Unit
            }).ToList()
        };

        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = workout.Id }, MapToResponse(workout));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, WorkoutRequest request)
    {
        var userId = User.GetUserId();
        var workout = await _db.Workouts
            .Include(w => w.ExerciseEntries)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workout is null) return NotFound();

        workout.WorkoutName = request.WorkoutName;
        workout.WorkoutDate = DateTime.SpecifyKind(request.WorkoutDate, DateTimeKind.Utc);

        // Remove old exercises and add new ones
        _db.ExerciseEntries.RemoveRange(workout.ExerciseEntries);
        workout.ExerciseEntries = request.Exercises.Select(e => new ExerciseEntry
        {
            WorkoutId = workout.Id,
            ExerciseName = e.ExerciseName,
            ExerciseCategory = e.ExerciseCategory,
            TrackingMode = e.TrackingMode,
            Sets = e.Sets,
            Reps = e.Reps,
            RIR = e.RIR,
            RestSeconds = e.RestSeconds,
            GroundContactTimeMs = e.GroundContactTimeMs,
            ConcentricTimeSeconds = e.ConcentricTimeSeconds,
            Metric1Value = e.Metric1Value,
            Metric1Unit = e.Metric1Unit,
            Metric2Value = e.Metric2Value,
            Metric2Unit = e.Metric2Unit
        }).ToList();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var workout = await _db.Workouts.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (workout is null) return NotFound();

        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static WorkoutResponse MapToResponse(Workout w) => new()
    {
        Id = w.Id,
        WorkoutName = w.WorkoutName,
        WorkoutDate = w.WorkoutDate,
        Exercises = w.ExerciseEntries.Select(e => new ExerciseEntryDto
        {
            ExerciseName = e.ExerciseName,
            ExerciseCategory = e.ExerciseCategory,
            TrackingMode = e.TrackingMode,
            Sets = e.Sets,
            Reps = e.Reps,
            RIR = e.RIR,
            RestSeconds = e.RestSeconds,
            GroundContactTimeMs = e.GroundContactTimeMs,
            ConcentricTimeSeconds = e.ConcentricTimeSeconds,
            Metric1Value = e.Metric1Value,
            Metric1Unit = e.Metric1Unit,
            Metric2Value = e.Metric2Value,
            Metric2Unit = e.Metric2Unit
        }).ToList()
    };
}
