using System.Threading.Channels;
using TicketApi.Modules.Notifications.Background;
using TicketApi.Modules.Notifications.Dto;
using TicketApi.Modules.Tickets;
using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Notifications.Util;

/// <summary>
/// Hands notifications owed to a customer from <see cref="TicketService"/> to
/// <see cref="NotificationDelivery"/>. Mirrors <see cref="ICustomerNotifier"/>, one enqueue
/// per send. Unbounded: a request enqueues at most two -- ADR-0005 section 2, a queue and
/// a worker.
/// </summary>
public sealed class NotificationQueue
{
    private readonly Channel<PendingNotification> _pending =
        Channel.CreateUnbounded<PendingNotification>(
            new UnboundedChannelOptions { SingleReader = true }
        );

    public void EnqueueTicketCreated(Ticket ticket) =>
        Enqueue(ticket, (notifier, ct) => notifier.SendTicketCreatedAsync(ticket, ct));

    public void EnqueueStatusChanged(
        Ticket ticket,
        string previousStatus,
        string? handlingAdminEmail
    ) =>
        Enqueue(
            ticket,
            (notifier, ct) =>
                notifier.SendStatusChangedAsync(ticket, previousStatus, handlingAdminEmail, ct)
        );

    public void EnqueueResolutionChanged(Ticket ticket, string? handlingAdminEmail) =>
        Enqueue(
            ticket,
            (notifier, ct) =>
                notifier.SendResolutionChangedAsync(ticket, handlingAdminEmail, ct)
        );

    public IAsyncEnumerable<PendingNotification> ReadAllAsync(CancellationToken ct) =>
        _pending.Reader.ReadAllAsync(ct);

    private void Enqueue(Ticket ticket, Func<ICustomerNotifier, CancellationToken, Task> send) =>
        _pending.Writer.TryWrite(new PendingNotification(ticket, send));
}
