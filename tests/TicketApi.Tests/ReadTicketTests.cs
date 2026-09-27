using System.Net;

namespace TicketApi.Tests;

public class ReadTicketTests
{
    [Fact]
    public async Task UnknownIdIsNotFound()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();

        var response = await client.GetAsync("/api/tickets/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
