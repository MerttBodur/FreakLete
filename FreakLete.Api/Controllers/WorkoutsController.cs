using FreakLete.Api.Data;
using FreakLete.Api.DTOs.Workout;
using FreakLete.Api.Entities;
using FreakLete.Api.Services;
using FreakLete.Api.Services.Embeddings;
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
    private readonly IUserSnapshotEventSink _snapshotSink;
    private readonly IWorkoutEmbeddingEnqueuer _workoutSink;

    public WorkoutsController(
        AppDbContext db,
        IUserSnapshotEventSink snapshotSink,
        IWorkoutEmbeddingEnqueuer workoutSink)
    {
        _db = db;
        _snapshotSink = snapshotSink;
        _workoutSink = workoutSink;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkoutResponse>>> GetAll()
    {
        var userId = User.GetUserId();
        var workouts = await _db.Workouts
            .Where(w => w.UserId == userId)
            .Include(w => w.ExerciseEntries)
                .ThenInclude(e => e.Sets)
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
                .ThenInclude(e => e.Sets)
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
                .ThenInclude(e => e.Sets)
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
            ExerciseEntries = request.Exercises.Select(e => MapToExerciseEntry(e)).ToList()
        };

        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();
        _workoutSink.EnqueueWorkout(userId, workout.Id);
        _snapshotSink.OnUserUpdated(userId);

        return CreatedAtAction(nameof(GetById), new { id = workout.Id }, MapToResponse(workout));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, WorkoutRequest request)
    {
        var userId = User.GetUserId();
        var workout = await _db.Workouts
            .Include(w => w.ExerciseEntries)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workout is null) return NotFound();

        workout.WorkoutName = request.WorkoutName;
        workout.WorkoutDate = DateTime.SpecifyKind(request.WorkoutDate, DateTimeKind.Utc);

        // Remove old exercises and add new ones
        _db.ExerciseEntries.RemoveRange(workout.ExerciseEntries);
        workout.ExerciseEntries = request.Exercises.Select(e => MapToExerciseEntry(e, workout.Id)).ToList();

        await _db.SaveChangesAsync();
        _workoutSink.EnqueueWorkout(userId, workout.Id);
        _snapshotSink.OnUserUpdated(userId);

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
            SetsCount = e.SetsCount,
            Reps = e.Reps,
            RIR = e.RIR,
            RestSeconds = e.RestSeconds,
            GroundContactTimeMs = e.GroundContactTimeMs,
            ConcentricTimeSeconds = e.ConcentricTimeSeconds,
            Metric1Value = e.Metric1Value,
            Metric1Unit = e.Metric1Unit,
            Metric2Value = e.Metric2Value,
            Metric2Unit = e.Metric2Unit,
            Sets = e.Sets
                .OrderBy(s => s.SetNumber)
                .Select(s => new ExerciseSetDto
                {
                    SetNumber = s.SetNumber,
                    Reps = s.Reps,
                    Weight = s.Weight,
                    RIR = s.RIR,
                    RestSeconds = s.RestSeconds,
                    ConcentricTimeSeconds = s.ConcentricTimeSeconds
                })
                .ToList()
        }).ToList()
    };

    private static ExerciseEntry MapToExerciseEntry(ExerciseEntryDto dto, int? workoutId = null)
    {
        var sets = dto.Sets
            .Select((s, i) => new ExerciseSet
            {
                SetNumber = s.SetNumber > 0 ? s.SetNumber : i + 1,
                Reps = s.Reps,
                Weight = s.Weight,
                RIR = s.RIR,
                RestSeconds = s.RestSeconds,
                ConcentricTimeSeconds = s.ConcentricTimeSeconds
            })
            .OrderBy(s => s.SetNumber)
            .ToList();

        var entry = new ExerciseEntry
        {
            ExerciseName = dto.ExerciseName,
            ExerciseCategory = dto.ExerciseCategory,
            TrackingMode = dto.TrackingMode,
            Sets = sets,
            SetsCount = sets.Count > 0 ? sets.Count : dto.SetsCount,
            Reps = sets.Count > 0 ? sets[^1].Reps : dto.Reps,
            RIR = sets.Count > 0 ? sets[^1].RIR : dto.RIR,
            RestSeconds = sets.Count > 0 ? sets[^1].RestSeconds : dto.RestSeconds,
            GroundContactTimeMs = dto.GroundContactTimeMs,
            ConcentricTimeSeconds = sets.Count > 0 ? sets[^1].ConcentricTimeSeconds : dto.ConcentricTimeSeconds,
            Metric1Value = sets.Count > 0 ? MaxWeightOrNull(sets) : dto.Metric1Value,
            Metric1Unit = dto.Metric1Unit,
            Metric2Value = dto.Metric2Value,
            Metric2Unit = dto.Metric2Unit
        };

        if (workoutId.HasValue)
            entry.WorkoutId = workoutId.Value;

        return entry;
    }

    private static double? MaxWeightOrNull(List<ExerciseSet> sets)
    {
        double maxWeight = sets
            .Where(s => s.Weight.HasValue)
            .Select(s => s.Weight!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return maxWeight > 0 ? maxWeight : null;
    }
}
