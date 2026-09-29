using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TicketApi.Configuration;
using TicketApi.Modules.Tickets.Entities;
using TicketApi.Modules.Tickets.Stores;

namespace TicketApi.Tests.Tickets;

public class ImageUrlTests
{
    [Theory]
    [EveryTicketStore]
    public async Task SeedingKeepsTheImageUrlOfEveryTicket(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        api.CreateClient();

        var tickets = await ReadStoredTicketsAsync(api);

        Assert.NotEmpty(tickets);
        Assert.All(tickets, ticket => Assert.NotEmpty(ticket.ImageUrl));
    }

    [Theory]
    [EveryTicketStore]
    public async Task UpdatingATicketKeepsItsImageUrl(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();
        var ticketId = SeededTickets.Load()[0].Id;
        var imageUrlBefore = (await ReadStoredTicketsAsync(api))
            .Single(ticket => ticket.Id == ticketId)
            .ImageUrl;

        await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}",
            new { status = "Resolved", resolution = "Replaced the cooling fan." }
        );

        var imageUrlAfter = (await ReadStoredTicketsAsync(api))
            .Single(ticket => ticket.Id == ticketId)
            .ImageUrl;
        Assert.Equal(imageUrlBefore, imageUrlAfter);
        Assert.NotEmpty(imageUrlAfter);
    }

    private static async Task<IReadOnlyList<Ticket>> ReadStoredTicketsAsync(
        TicketApiFactory api
    )
    {
        using var scope = api.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ITicketStore>().GetAllAsync();
    }
}
