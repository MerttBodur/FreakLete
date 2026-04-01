namespace FreakLete.Api.DTOs.FreakAi;

public class TrainingProgramResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public int DaysPerWeek { get; set; }
    public int SessionDurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Sport { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsStarterTemplate { get; set; }
    public List<ProgramWeekResponse> Weeks { get; set; } = [];
}

public class ProgramWeekResponse
{
    public int Id { get; set; }
    public int WeekNumber { get; set; }
    public string Focus { get; set; } = string.Empty;
    public bool IsDeload { get; set; }
    public List<ProgramSessionResponse> Sessions { get; set; } = [];
}

public class ProgramSessionResponse
{
    public int Id { get; set; }
    public int DayNumber { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string Focus { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<ProgramExerciseResponse> Exercises { get; set; } = [];
}

public class ProgramExerciseResponse
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string ExerciseCategory { get; set; } = string.Empty;
    public int Sets { get; set; }
    public string RepsOrDuration { get; set; } = string.Empty;
    public string IntensityGuidance { get; set; } = string.Empty;
    public int? RestSeconds { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string SupersetGroup { get; set; } = string.Empty;
}

public class TrainingProgramListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DaysPerWeek { get; set; }
    public DateTime CreatedAt { get; set; }
}
