using Roms.Application;

namespace Roms.Web;

public sealed class AttendanceAutoClosureWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AttendanceAutoClosureWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessOnce(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessOnce(stoppingToken);
    }

    private async Task ProcessOnce(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IAttendanceAutoClosureService>();
            var result = await service.ProcessDueAsync(cancellationToken);
            if (result.Closed > 0 || result.ConcurrencySkipped > 0)
                logger.LogInformation(
                    "Attendance auto-closure examined {Examined}, closed {Closed}, concurrency-skipped {Skipped}.",
                    result.Examined, result.Closed, result.ConcurrencySkipped);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Attendance auto-closure cycle failed; the next cycle will retry safely.");
        }
    }
}
