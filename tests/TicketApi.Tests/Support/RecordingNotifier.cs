using System.Collections.Concurrent;
using TicketApi.Modules.Notifications;
using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Tests.Support;

public sealed class RecordingNotifier : ICustomerNotifier
{
    private readonly ConcurrentQueue<string> notifications = new();
    private readonly ConcurrentQueue<string?> replyTos = new();

    public IReadOnlyList<string> Notifications => [.. notifications];

    public IReadOnlyList<string?> ReplyTos => [.. replyTos];

    /// <summary>Completes once the delivery worker has sent <paramref name="count"/> of them.</summary>
    public async Task SentAsync(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (notifications.Count < count && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        Assert.Equal(count, notifications.Count);
    }

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default)
    {
        notifications.Enqueue($"ticket-created:{ticket.Id}");
        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        notifications.Enqueue($"status-changed:{ticket.Id}:{previousStatus}->{ticket.Status}");
        replyTos.Enqueue(handlingAdminEmail);
        return Task.CompletedTask;
    }

    public Task SendResolutionChangedAsync(
        Ticket ticket,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        notifications.Enqueue($"resolution-changed:{ticket.Id}");
        replyTos.Enqueue(handlingAdminEmail);
        return Task.CompletedTask;
    }
}
