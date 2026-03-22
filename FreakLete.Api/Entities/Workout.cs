namespace FreakLete.Api.Entities;

public class Workout
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public DateTime WorkoutDate { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ExerciseEntry> ExerciseEntries { get; set; } = [];
}
