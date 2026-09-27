using TicketApi.Entities;
using TicketApi.Services;

namespace TicketApi.Tests;

public sealed class RecordingEmailService : IEmailService
{
    private readonly List<string> notifications = [];

    public IReadOnlyList<string> Notifications => notifications;

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default)
    {
        notifications.Add($"ticket-created:{ticket.Id}");
        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        CancellationToken ct = default
    )
    {
        notifications.Add($"status-changed:{ticket.Id}:{previousStatus}->{ticket.Status}");
        return Task.CompletedTask;
    }

    public Task SendResolutionChangedAsync(Ticket ticket, CancellationToken ct = default)
    {
        notifications.Add($"resolution-changed:{ticket.Id}");
        return Task.CompletedTask;
    }
}
