using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalAirspace.Application.Features.Announcements;

namespace NationalAirspace.Infrastructure.Services;

/// <summary>
/// Periodically archives expired announcements. Public queries already hide expired items,
/// so this job only keeps the stored status consistent for administrators.
/// </summary>
public sealed class AnnouncementExpiryJob(IServiceScopeFactory scopeFactory, ILogger<AnnouncementExpiryJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<AnnouncementAdminService>();
                var archived = await service.ArchiveExpiredAsync(stoppingToken);
                if (archived > 0) logger.LogInformation("Archived {Count} expired announcement(s).", archived);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Announcement expiry job failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
