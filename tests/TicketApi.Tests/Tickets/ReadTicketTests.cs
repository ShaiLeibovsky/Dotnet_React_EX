using System.Net;
using TicketApi.Configuration;

namespace TicketApi.Tests.Tickets;

public class ReadTicketTests
{
    [Theory]
    [EveryTicketStore]
    public async Task UnknownIdIsNotFound(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.GetAsync("/api/tickets/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
