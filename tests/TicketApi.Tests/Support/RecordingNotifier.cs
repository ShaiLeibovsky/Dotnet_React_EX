using TicketApi.Modules.Notifications;
using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Tests.Support;

public sealed class RecordingNotifier : ICustomerNotifier
{
    private readonly List<string> notifications = [];
    private readonly List<string?> replyTos = [];

    public IReadOnlyList<string> Notifications => notifications;

    public IReadOnlyList<string?> ReplyTos => replyTos;

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default)
    {
        notifications.Add($"ticket-created:{ticket.Id}");
        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        notifications.Add($"status-changed:{ticket.Id}:{previousStatus}->{ticket.Status}");
        replyTos.Add(handlingAdminEmail);
        return Task.CompletedTask;
    }

    public Task SendResolutionChangedAsync(
        Ticket ticket,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        notifications.Add($"resolution-changed:{ticket.Id}");
        replyTos.Add(handlingAdminEmail);
        return Task.CompletedTask;
    }
}
