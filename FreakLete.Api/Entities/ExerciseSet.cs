namespace FreakLete.Api.Entities;

public class ExerciseSet
{
    public int Id { get; set; }
    public int ExerciseEntryId { get; set; }
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double? Weight { get; set; }

    public ExerciseEntry ExerciseEntry { get; set; } = null!;
}
