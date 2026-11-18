using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.Api.Services;

public sealed class DemoCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoCleanupService> _logger;

    public DemoCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<DemoCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CleanupOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal del servidor.
        }
    }

    private async Task CleanupOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider
                .GetRequiredService<DemoAccountService>();
            var deleted = await service.DeleteExpiredAsync(cancellationToken);
            if (deleted > 0)
                _logger.LogInformation("Eliminadas {Count} cuentas demo caducadas.", deleted);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Apagado normal del servidor.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza de cuentas demo.");
        }
    }
}
