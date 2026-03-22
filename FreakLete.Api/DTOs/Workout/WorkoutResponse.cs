namespace FreakLete.Api.DTOs.Workout;

public class WorkoutResponse
{
    public int Id { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public DateTime WorkoutDate { get; set; }
    public List<ExerciseEntryDto> Exercises { get; set; } = [];
}
