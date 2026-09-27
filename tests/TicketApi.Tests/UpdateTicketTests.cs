using System.Net;
using System.Net.Http.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

public class UpdateTicketTests
{
    [Fact]
    public async Task UnknownIdIsNotFound()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/tickets/does-not-exist",
            new { status = "In Progress", resolution = (string?)null }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StatusOutsideTheFourValuesIsRejectedAndNamesTheField()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();
        var ticketId = SeededTickets.Load()[0].Id;

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}",
            new { status = "Pending", resolution = (string?)null }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Status", await ValidationErrors.FieldNamesAsync(response));
    }

    [Fact]
    public async Task StatusAndResolutionSurviveALaterRead()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();
        var ticketId = SeededTickets.Load()[0].Id;

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}",
            new { status = "Resolved", resolution = "Replaced the cooling fan." }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reread = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{ticketId}");
        Assert.Equal("Resolved", reread?.Status);
        Assert.Equal("Replaced the cooling fan.", reread?.Resolution);
    }
}
