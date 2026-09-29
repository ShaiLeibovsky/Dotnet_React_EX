using System.Net;
using System.Net.Http.Json;
using TicketApi.Configuration;
using TicketApi.Modules.Tickets.Dto;

namespace TicketApi.Tests.Notifications;

public class NotificationTests
{
    [Theory]
    [EveryTicketStore]
    public async Task CreatingATicketTriggersExactlyOneNotification(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                name = "Grace Hopper",
                email = "grace@example.com",
                description = "A moth is lodged in relay seventy.",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal([$"ticket-created:{created!.Id}"], api.Notifier.Notifications);
    }

    [Theory]
    [EveryTicketStore]
    public async Task AStatusChangeTriggersExactlyOneNotification(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = "In Progress", resolution = ticket.Resolution }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [$"status-changed:{ticket.Id}:{ticket.Status}->In Progress"],
            api.Notifier.Notifications
        );
        Assert.Equal([TicketApiFactory.AdminEmail], api.Notifier.ReplyTos);
    }

    [Theory]
    [EveryTicketStore]
    public async Task AResolutionChangeTriggersExactlyOneNotification(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = ticket.Status, resolution = "Cleaned the vents." }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([$"resolution-changed:{ticket.Id}"], api.Notifier.Notifications);
    }

    [Theory]
    [EveryTicketStore]
    public async Task SavingWithNoActualChangeTriggersNoNotification(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = ticket.Status, resolution = ticket.Resolution }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(api.Notifier.Notifications);
    }
}
