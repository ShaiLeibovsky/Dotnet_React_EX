using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Notifications.Dto;

/// <summary>One queued notification: the ticket it is about, and the call that sends it.</summary>
public sealed record PendingNotification(
    Ticket Ticket,
    Func<ICustomerNotifier, CancellationToken, Task> Send
);
