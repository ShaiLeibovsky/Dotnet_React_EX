using System.Net;
using System.Net.Http.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

public class NotificationTests
{
    [Fact]
    public async Task CreatingATicketTriggersExactlyOneNotification()
    {
        using var api = new TicketApiFactory();
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

        Assert.Equal([$"ticket-created:{created!.Id}"], api.Emails.Notifications);
    }

    [Fact]
    public async Task AStatusChangeTriggersExactlyOneNotification()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = "In Progress", resolution = ticket.Resolution }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            [$"status-changed:{ticket.Id}:{ticket.Status}->In Progress"],
            api.Emails.Notifications
        );
    }

    [Fact]
    public async Task AResolutionChangeTriggersExactlyOneNotification()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = ticket.Status, resolution = "Cleaned the vents." }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([$"resolution-changed:{ticket.Id}"], api.Emails.Notifications);
    }

    [Fact]
    public async Task SavingWithNoActualChangeTriggersNoNotification()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = ticket.Status, resolution = ticket.Resolution }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(api.Emails.Notifications);
    }
}
