namespace FreakLete.Api.Entities;

public class TrainingProgram
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public int DaysPerWeek { get; set; }
    public int SessionDurationMinutes { get; set; }
    public string Status { get; set; } = "draft"; // draft, active, completed, archived
    public string Sport { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<ProgramWeek> Weeks { get; set; } = [];
}

public class ProgramWeek
{
    public int Id { get; set; }
    public int TrainingProgramId { get; set; }
    public int WeekNumber { get; set; }
    public string Focus { get; set; } = string.Empty;
    public bool IsDeload { get; set; }

    // Navigation
    public TrainingProgram TrainingProgram { get; set; } = null!;
    public ICollection<ProgramSession> Sessions { get; set; } = [];
}

public class ProgramSession
{
    public int Id { get; set; }
    public int ProgramWeekId { get; set; }
    public int DayNumber { get; set; } // 1-7
    public string SessionName { get; set; } = string.Empty;
    public string Focus { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Navigation
    public ProgramWeek ProgramWeek { get; set; } = null!;
    public ICollection<ProgramExercise> Exercises { get; set; } = [];
}

public class ProgramExercise
{
    public int Id { get; set; }
    public int ProgramSessionId { get; set; }
    public int Order { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseCategory { get; set; } = string.Empty;
    public int Sets { get; set; }
    public string RepsOrDuration { get; set; } = string.Empty; // "8-10", "30s", "5x5"
    public string IntensityGuidance { get; set; } = string.Empty; // "70% 1RM", "RPE 7", "bodyweight"
    public int? RestSeconds { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string SupersetGroup { get; set; } = string.Empty; // "A", "B" for superset grouping

    // Navigation
    public ProgramSession ProgramSession { get; set; } = null!;
}
