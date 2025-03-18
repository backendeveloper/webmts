using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Contract.Requests;

namespace NotificationService.Business.Consumers;

public class PendingNotificationsProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PendingNotificationsProcessor> _logger;
    private readonly TimeSpan _interval;

    public PendingNotificationsProcessor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PendingNotificationsProcessor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interval = TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pending notifications processor is starting");

        stoppingToken.Register(() =>
            _logger.LogInformation("Pending notifications processor is stopping"));

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingNotificationsAsync();
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessPendingNotificationsAsync()
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var command = new ProcessPendingNotificationsRequest { BatchSize = 20 };
                var result = await mediator.Send(command);

                if (result)
                    _logger.LogInformation("Successfully processed pending notifications batch");
                else
                    _logger.LogWarning("Error processing pending notifications batch");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing pending notifications");
        }
    }
}