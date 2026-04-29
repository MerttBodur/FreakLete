using Pgvector;

namespace FreakLete.Api.Entities;

public class UserSnapshotEmbedding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public Vector Embedding { get; set; } = new(new float[768]);
    public string TextSnapshot { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
