using FreakLete.Api.Data;
using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace FreakLete.Api.Services.Embeddings;

public sealed class EmbeddingBackgroundService : BackgroundService
{
    private readonly EmbeddingChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingBackgroundService> _logger;

    public EmbeddingBackgroundService(
        EmbeddingChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmbeddingBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Embedding job failed: {Kind} user={UserId} workout={WorkoutId}",
                    job.Kind, job.UserId, job.WorkoutId);
            }
        }
    }

    private async Task ProcessAsync(EmbeddingJob job, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gemini = scope.ServiceProvider.GetRequiredService<GeminiClient>();

        if (job.Kind == EmbeddingJobKind.Workout && job.WorkoutId.HasValue)
        {
            await ProcessWorkoutAsync(db, gemini, job.UserId, job.WorkoutId.Value, ct);
        }
        else if (job.Kind == EmbeddingJobKind.UserSnapshot)
        {
            await ProcessUserSnapshotAsync(db, gemini, job.UserId, ct);
        }
    }

    private static async Task ProcessWorkoutAsync(
        AppDbContext db, GeminiClient gemini, int userId, int workoutId, CancellationToken ct)
    {
        var workout = await db.Workouts
            .Include(w => w.ExerciseEntries)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.UserId == userId, ct);
        var user = await db.Users.FindAsync([userId], ct);
        if (workout is null || user is null) return;

        var text = EmbeddingTextFormatter.FormatWorkout(workout, user);
        var floats = await gemini.EmbedAsync(text, ct);
        if (floats is null || floats.Length != 768) return;

        var existing = await db.WorkoutEmbeddings
            .FirstOrDefaultAsync(e => e.WorkoutId == workoutId, ct);

        if (existing is null)
        {
            db.WorkoutEmbeddings.Add(new WorkoutEmbedding
            {
                WorkoutId = workoutId,
                UserId = userId,
                Embedding = new Vector(floats),
                TextSnapshot = text,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Embedding = new Vector(floats);
            existing.TextSnapshot = text;
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task ProcessUserSnapshotAsync(
        AppDbContext db, GeminiClient gemini, int userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return;

        var prs = await db.PrEntries.Where(p => p.UserId == userId).ToListAsync(ct);
        var program = await db.TrainingPrograms
            .Where(p => p.UserId == userId && p.Status == "active")
            .FirstOrDefaultAsync(ct);

        var text = EmbeddingTextFormatter.FormatUserSnapshot(user, prs, program);
        var floats = await gemini.EmbedAsync(text, ct);
        if (floats is null || floats.Length != 768) return;

        var existing = await db.UserSnapshotEmbeddings
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);

        if (existing is null)
        {
            db.UserSnapshotEmbeddings.Add(new UserSnapshotEmbedding
            {
                UserId = userId,
                Embedding = new Vector(floats),
                TextSnapshot = text,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Embedding = new Vector(floats);
            existing.TextSnapshot = text;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
