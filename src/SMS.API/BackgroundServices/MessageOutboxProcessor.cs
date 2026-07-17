using MediatR;
using SMS.Application.Features.Notifications.Commands;

namespace SMS.API.BackgroundServices;

/// <summary>
/// Periodically dispatches queued/scheduled messages so sends survive restarts and can be
/// retried, rather than being tied to the originating request.
/// </summary>
public class MessageOutboxProcessor : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<MessageOutboxProcessor> _logger;

    public MessageOutboxProcessor(IServiceProvider services, ILogger<MessageOutboxProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ProcessMessageOutboxCommand(), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Message outbox processing failed");
            }
        }
    }
}
