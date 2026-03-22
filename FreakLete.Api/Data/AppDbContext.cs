using FreakLete.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreakLete.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<ExerciseEntry> ExerciseEntries => Set<ExerciseEntry>();
    public DbSet<PrEntry> PrEntries => Set<PrEntry>();
    public DbSet<AthleticPerformanceEntry> AthleticPerformanceEntries => Set<AthleticPerformanceEntry>();
    public DbSet<MovementGoal> MovementGoals => Set<MovementGoal>();
    public DbSet<ExerciseDefinition> ExerciseDefinitions => Set<ExerciseDefinition>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var prop in entry.Properties
                .Where(p => p.CurrentValue is DateTime && p.Metadata.ClrType == typeof(DateTime)))
            {
                var dt = (DateTime)prop.CurrentValue!;
                if (dt.Kind == DateTimeKind.Unspecified)
                    prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            foreach (var prop in entry.Properties
                .Where(p => p.CurrentValue is DateTime && p.Metadata.ClrType == typeof(DateTime?)))
            {
                var dt = (DateTime)prop.CurrentValue!;
                if (dt.Kind == DateTimeKind.Unspecified)
                    prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.SportName).HasMaxLength(100);
            e.Property(u => u.GymExperienceLevel).HasMaxLength(50);
        });

        // Workout
        modelBuilder.Entity<Workout>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.UserId);
            e.HasOne(w => w.User)
             .WithMany(u => u.Workouts)
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ExerciseEntry
        modelBuilder.Entity<ExerciseEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WorkoutId);
            e.HasOne(x => x.Workout)
             .WithMany(w => w.ExerciseEntries)
             .HasForeignKey(x => x.WorkoutId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.ExerciseName).HasMaxLength(200);
            e.Property(x => x.ExerciseCategory).HasMaxLength(100);
            e.Property(x => x.TrackingMode).HasMaxLength(20);
            e.Property(x => x.Metric1Unit).HasMaxLength(50);
            e.Property(x => x.Metric2Unit).HasMaxLength(50);
        });

        // PrEntry
        modelBuilder.Entity<PrEntry>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.UserId);
            e.HasOne(p => p.User)
             .WithMany(u => u.PrEntries)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.ExerciseName).HasMaxLength(200);
            e.Property(p => p.ExerciseCategory).HasMaxLength(100);
            e.Property(p => p.TrackingMode).HasMaxLength(20);
            e.Property(p => p.Metric1Unit).HasMaxLength(50);
            e.Property(p => p.Metric2Unit).HasMaxLength(50);
        });

        // AthleticPerformanceEntry
        modelBuilder.Entity<AthleticPerformanceEntry>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.UserId);
            e.HasOne(a => a.User)
             .WithMany(u => u.AthleticPerformanceEntries)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(a => a.MovementName).HasMaxLength(200);
            e.Property(a => a.MovementCategory).HasMaxLength(100);
            e.Property(a => a.Unit).HasMaxLength(50);
            e.Property(a => a.SecondaryUnit).HasMaxLength(50);
        });

        // MovementGoal
        modelBuilder.Entity<MovementGoal>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.UserId);
            e.HasOne(g => g.User)
             .WithMany(u => u.MovementGoals)
             .HasForeignKey(g => g.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(g => g.MovementName).HasMaxLength(200);
            e.Property(g => g.MovementCategory).HasMaxLength(100);
            e.Property(g => g.GoalMetricLabel).HasMaxLength(100);
            e.Property(g => g.Unit).HasMaxLength(50);
        });

        // ExerciseDefinition
        modelBuilder.Entity<ExerciseDefinition>(e =>
        {
            e.HasKey(d => d.CatalogId);
            e.HasIndex(d => d.Name);
            e.HasIndex(d => d.Category);
            e.Property(d => d.CatalogId).HasMaxLength(100);
            e.Property(d => d.Name).HasMaxLength(200);
            e.Property(d => d.Category).HasMaxLength(100);
            e.Property(d => d.TrackingMode).HasMaxLength(20);
        });
    }
}
