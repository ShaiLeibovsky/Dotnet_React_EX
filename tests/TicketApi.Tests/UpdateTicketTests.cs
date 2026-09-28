using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

public class UpdateTicketTests
{
    [Theory]
    [EveryTicketStore]
    public async Task UnknownIdIsNotFound(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();

        var response = await client.PutAsJsonAsync(
            "/api/tickets/does-not-exist",
            new { status = "In Progress", resolution = (string?)null }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [EveryTicketStore]
    public async Task StatusOutsideTheFourValuesIsRejectedAndNamesTheField(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();
        var ticketId = SeededTickets.Load()[0].Id;

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}",
            new { status = "Pending", resolution = (string?)null }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Status", await ValidationErrors.FieldNamesAsync(response));
    }

    [Theory]
    [EveryTicketStore]
    public async Task StatusAndResolutionSurviveALaterRead(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = await api.CreateAdminClientAsync();
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

    [Theory]
    [EveryTicketStore]
    public async Task WithoutATokenTheUpdateIsRejected(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();
        var ticketId = SeededTickets.Load()[0].Id;

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}",
            new { status = "Resolved", resolution = "Should never be stored." }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var unchanged = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{ticketId}");
        Assert.NotEqual("Should never be stored.", unchanged?.Resolution);
    }

    [Theory]
    [EveryTicketStore]
    public async Task AnInvalidTokenIsRejected(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "not-a-real-token"
        );

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{SeededTickets.Load()[0].Id}",
            new { status = "Resolved", resolution = (string?)null }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
