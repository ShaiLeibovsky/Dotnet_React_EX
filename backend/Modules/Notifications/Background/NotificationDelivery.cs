using TicketApi.Modules.Notifications.Util;

namespace TicketApi.Modules.Notifications.Background;

/// <summary>
/// Sends queued notifications after the request that owed them has already answered. One at
/// a time, in the order they were queued, so a status change and the resolution change saved
/// with it arrive in that order -- ADR-0005 section 2, a queue and a worker. A notification
/// that cannot be delivered is logged and dropped -- section 3.
/// </summary>
public sealed class NotificationDelivery : BackgroundService
{
    private readonly NotificationQueue _queue;
    private readonly ICustomerNotifier _notifier;
    private readonly ILogger<NotificationDelivery> _logger;

    public NotificationDelivery(
        NotificationQueue queue,
        ICustomerNotifier notifier,
        ILogger<NotificationDelivery> logger
    )
    {
        _queue = queue;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var pending in _queue.ReadAllAsync(ct))
        {
            try
            {
                // ADR-0005 section 3, a send outlives the shutdown that stopped the queue.
                await pending.Send(_notifier, CancellationToken.None);
            }
            catch (Exception failure)
            {
                _logger.LogError(
                    failure,
                    "Notification failed for ticket {TicketId}; dropping it.",
                    pending.Ticket.Id
                );
            }
        }
    }
}
