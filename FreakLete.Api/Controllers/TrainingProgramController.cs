using FreakLete.Api.Data;
using FreakLete.Api.DTOs.FreakAi;
using FreakLete.Api.Services;
using FreakLete.Api.Services.Embeddings;
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
    private readonly IUserSnapshotEventSink _snapshotSink;

    public TrainingProgramController(AppDbContext db, IUserSnapshotEventSink snapshotSink)
    {
        _db = db;
        _snapshotSink = snapshotSink;
    }

    [HttpGet]
    public async Task<ActionResult<List<TrainingProgramListResponse>>> GetAll()
    {
        var userId = User.GetUserId();

        var programs = await _db.TrainingPrograms
            .Where(p => p.UserId == userId && !p.IsStarterTemplate)
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
            .Where(p => p.UserId == userId && p.Status == "active" && !p.IsStarterTemplate)
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
            .Where(p => p.Id == id && p.UserId == userId && !p.IsStarterTemplate)
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .FirstOrDefaultAsync();

        if (program is null)
            return NotFound();

        return Ok(MapToResponse(program));
    }

    // ── Starter Template Endpoints ─────────────────────────────────────

    [HttpGet("starter")]
    [AllowAnonymous]
    public async Task<ActionResult<List<TrainingProgramListResponse>>> GetStarterTemplates()
    {
        var templates = await _db.TrainingPrograms
            .Where(p => p.IsStarterTemplate)
            .OrderBy(p => p.DaysPerWeek)
            .ThenBy(p => p.Name)
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

        return Ok(templates);
    }

    [HttpGet("starter/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TrainingProgramResponse>> GetStarterTemplateById(int id)
    {
        var program = await _db.TrainingPrograms
            .Where(p => p.Id == id && p.IsStarterTemplate)
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .FirstOrDefaultAsync();

        if (program is null)
            return NotFound();

        return Ok(MapToResponse(program));
    }

    [HttpPost("starter/{id:int}/clone")]
    public async Task<ActionResult<TrainingProgramResponse>> CloneStarterTemplate(int id)
    {
        var userId = User.GetUserId();

        var template = await _db.TrainingPrograms
            .Where(p => p.Id == id && p.IsStarterTemplate)
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Sessions)
                    .ThenInclude(s => s.Exercises)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (template is null)
            return NotFound();

        // Deep copy with new ownership
        var now = DateTime.UtcNow;
        template.Id = 0;
        template.UserId = userId;
        template.Status = "draft";
        template.IsStarterTemplate = false;
        template.CreatedAt = now;
        template.UpdatedAt = now;

        foreach (var week in template.Weeks)
        {
            week.Id = 0;
            week.TrainingProgramId = 0;
            foreach (var session in week.Sessions)
            {
                session.Id = 0;
                session.ProgramWeekId = 0;
                foreach (var exercise in session.Exercises)
                {
                    exercise.Id = 0;
                    exercise.ProgramSessionId = 0;
                }
            }
        }

        _db.TrainingPrograms.Add(template);
        await _db.SaveChangesAsync();
        _snapshotSink.OnUserUpdated(userId);

        return Ok(MapToResponse(template));
    }

    // ── Mapping ────────────────────────────────────────────────────────

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
        IsStarterTemplate = p.IsStarterTemplate,
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
