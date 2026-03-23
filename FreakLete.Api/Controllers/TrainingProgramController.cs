using FreakLete.Api.Data;
using FreakLete.Api.DTOs.FreakAi;
using FreakLete.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrainingProgramController : ControllerBase
{
    private readonly AppDbContext _db;

    public TrainingProgramController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TrainingProgramListResponse>>> GetAll()
    {
        var userId = User.GetUserId();

        var programs = await _db.TrainingPrograms
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new TrainingProgramListResponse
            {
                Id = p.Id,
                Name = p.Name,
                Goal = p.Goal,
                Status = p.Status,
                DaysPerWeek = p.DaysPerWeek,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(programs);
    }

    [HttpGet("active")]
    public async Task<ActionResult<TrainingProgramResponse>> GetActive()
    {
        var userId = User.GetUserId();

        var program = await _db.TrainingPrograms
            .Where(p => p.UserId == userId && p.Status == "active")
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        if (program is null)
            return NotFound(new { message = "No active training program." });

        return Ok(MapToResponse(program));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingProgramResponse>> GetById(int id)
    {
        var userId = User.GetUserId();

        var program = await _db.TrainingPrograms
            .Where(p => p.Id == id && p.UserId == userId)
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .FirstOrDefaultAsync();

        if (program is null)
            return NotFound();

        return Ok(MapToResponse(program));
    }

    private static TrainingProgramResponse MapToResponse(Entities.TrainingProgram p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Goal = p.Goal,
        DaysPerWeek = p.DaysPerWeek,
        SessionDurationMinutes = p.SessionDurationMinutes,
        Status = p.Status,
        Sport = p.Sport,
        Position = p.Position,
        Notes = p.Notes,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Weeks = p.Weeks.OrderBy(w => w.WeekNumber).Select(w => new ProgramWeekResponse
        {
            Id = w.Id,
            WeekNumber = w.WeekNumber,
            Focus = w.Focus,
            IsDeload = w.IsDeload,
            Sessions = w.Sessions.OrderBy(s => s.DayNumber).Select(s => new ProgramSessionResponse
            {
                Id = s.Id,
                DayNumber = s.DayNumber,
                SessionName = s.SessionName,
                Focus = s.Focus,
                Notes = s.Notes,
                Exercises = s.Exercises.OrderBy(x => x.Order).Select(x => new ProgramExerciseResponse
                {
                    Id = x.Id,
                    Order = x.Order,
                    ExerciseName = x.ExerciseName,
                    ExerciseCategory = x.ExerciseCategory,
                    Sets = x.Sets,
                    RepsOrDuration = x.RepsOrDuration,
                    IntensityGuidance = x.IntensityGuidance,
                    RestSeconds = x.RestSeconds,
                    Notes = x.Notes,
                    SupersetGroup = x.SupersetGroup
                }).ToList()
            }).ToList()
        }).ToList()
    };
}
