using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Workout;

public class WorkoutRequest
{
    [Required, MaxLength(200)]
    public string WorkoutName { get; set; } = string.Empty;

    [Required]
    public DateTime WorkoutDate { get; set; }

    public List<ExerciseEntryDto> Exercises { get; set; } = [];
}

public class ExerciseEntryDto
{
    [Required, MaxLength(200)]
    public string ExerciseName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ExerciseCategory { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TrackingMode { get; set; } = "Strength";

    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? RIR { get; set; }
    public int? RestSeconds { get; set; }
    public double? GroundContactTimeMs { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
    public double? Metric1Value { get; set; }
    public string? Metric1Unit { get; set; }
    public double? Metric2Value { get; set; }
    public string? Metric2Unit { get; set; }
}
