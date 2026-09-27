using System.Net.Http.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

public class ListTicketsTests
{
    [Fact]
    public async Task ListingReturnsTheSeededTickets()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();

        var listed = await client.GetFromJsonAsync<List<TicketDto>>("/api/tickets");

        Assert.Equal(
            SeededTickets.Load().OrderBy(ticket => ticket.Id),
            listed!.OrderBy(ticket => ticket.Id)
        );
    }
}
