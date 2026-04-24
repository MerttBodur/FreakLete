namespace FreakLete.Api.DTOs.Workout;

public class ExerciseSetDto
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double? Weight { get; set; }
    public int? RIR { get; set; }
    public int? RestSeconds { get; set; }
    public double? ConcentricTimeSeconds { get; set; }
}
