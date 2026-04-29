namespace FreakLete.Api.Services.Embeddings;

public sealed class EmbeddingEventSink : IUserSnapshotEventSink, IWorkoutEmbeddingEnqueuer
{
    private readonly EmbeddingChannel _channel;
    private readonly ILogger<EmbeddingEventSink> _logger;

    public EmbeddingEventSink(EmbeddingChannel channel, ILogger<EmbeddingEventSink> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public void OnUserUpdated(int userId)
    {
        var ok = _channel.TryWrite(new EmbeddingJob(EmbeddingJobKind.UserSnapshot, userId, null));
        if (!ok)
            _logger.LogDebug("Embedding channel full; dropped UserSnapshot job for user {UserId}", userId);
    }

    public void EnqueueWorkout(int userId, int workoutId)
    {
        var ok = _channel.TryWrite(new EmbeddingJob(EmbeddingJobKind.Workout, userId, workoutId));
        if (!ok)
            _logger.LogDebug("Embedding channel full; dropped Workout job for user {UserId} workout {WorkoutId}", userId, workoutId);
    }
}
