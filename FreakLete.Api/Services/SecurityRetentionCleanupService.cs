using FreakLete.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreakLete.Api.Services;

/// <summary>
/// Background service that periodically deletes old security event rows.
///
/// Retention windows (configurable via env vars):
///   SecurityRetention__AuthLoginAttemptDays   — default 30
///   SecurityRetention__GooglePlayRtdnEventDays — default 90
///
/// Never runs in the Testing environment (test fixture owns data lifecycle).
/// First run is delayed 30 s after startup so migrations finish first.
/// Subsequent runs execute every 24 h.
/// </summary>
public sealed class SecurityRetentionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<SecurityRetentionCleanupService> _logger;

    public SecurityRetentionCleanupService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        IConfiguration config,
        ILogger<SecurityRetentionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Never run in Testing — test fixture manages data lifecycle.
        if (_env.IsEnvironment("Testing"))
            return;

        // Short startup delay so DB migrations complete before first cleanup run.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupAsync(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        var authDays = _config.GetValue<int>("SecurityRetention:AuthLoginAttemptDays", 30);
        var rtdnDays = _config.GetValue<int>("SecurityRetention:GooglePlayRtdnEventDays", 90);

        var authCutoff = DateTime.UtcNow.AddDays(-authDays);
        var rtdnCutoff = DateTime.UtcNow.AddDays(-rtdnDays);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var authDeleted = await db.AuthLoginAttempts
                .Where(a => a.OccurredAtUtc < authCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            var rtdnDeleted = await db.GooglePlayRtdnEvents
                .Where(e => e.ReceivedAtUtc < rtdnCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            if (authDeleted > 0 || rtdnDeleted > 0)
                _logger.LogInformation(
                    "SecurityRetention: deleted {Auth} AuthLoginAttempts older than {AuthDays}d, " +
                    "{Rtdn} GooglePlayRtdnEvents older than {RtdnDays}d",
                    authDeleted, authDays, rtdnDeleted, rtdnDays);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SecurityRetention cleanup failed");
        }
    }
}
