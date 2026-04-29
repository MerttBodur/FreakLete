namespace FreakLete.Api.Services.Embeddings;

public enum EmbeddingJobKind
{
    UserSnapshot,
    Workout
}

public sealed record EmbeddingJob(EmbeddingJobKind Kind, int UserId, int? WorkoutId);
