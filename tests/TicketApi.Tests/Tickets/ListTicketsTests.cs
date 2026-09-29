using System.Net.Http.Json;
using TicketApi.Tickets;
using TicketApi.Tickets.Stores;

namespace TicketApi.Tests.Tickets;

public class ListTicketsTests
{
    [Theory]
    [EveryTicketStore]
    public async Task ListingReturnsTheSeededTickets(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var listed = await client.GetFromJsonAsync<List<TicketDto>>("/api/tickets");

        Assert.Equal(
            SeededTickets.Load().OrderBy(ticket => ticket.Id),
            listed!.OrderBy(ticket => ticket.Id)
        );
    }
}
