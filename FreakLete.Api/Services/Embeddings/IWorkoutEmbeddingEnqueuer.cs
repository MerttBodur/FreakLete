namespace FreakLete.Api.Services.Embeddings;

public interface IWorkoutEmbeddingEnqueuer
{
    void EnqueueWorkout(int userId, int workoutId);
}
