namespace FreakLete.Api.Entities;

public class UserExerciseTier
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CatalogId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string TierLevel { get; set; } = string.Empty;
    public double RawValue { get; set; }
    public double? BasisValue { get; set; }
    public double? Ratio { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
