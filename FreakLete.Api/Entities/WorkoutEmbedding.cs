using Pgvector;

namespace FreakLete.Api.Entities;

public class WorkoutEmbedding
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int UserId { get; set; }
    public Vector Embedding { get; set; } = new(new float[768]);
    public string TextSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Workout Workout { get; set; } = null!;
    public User User { get; set; } = null!;
}
