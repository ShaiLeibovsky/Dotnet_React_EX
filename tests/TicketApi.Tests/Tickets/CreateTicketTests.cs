using System.Net;
using System.Net.Http.Json;
using TicketApi.Configuration;
using TicketApi.Modules.Tickets.Dto;

namespace TicketApi.Tests.Tickets;

public class CreateTicketTests
{
    [Theory]
    [EveryTicketStore]
    public async Task ValidPayloadReturnsTheCreatedTicketWithAGeneratedId(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                name = "Ada Lovelace",
                email = "ada@example.com",
                description = "The analytical engine jams on every third card.",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(created);
        Assert.NotEmpty(created.Id);
        Assert.Equal("Ada Lovelace", created.Name);
        Assert.Equal("ada@example.com", created.Email);
        Assert.Equal("The analytical engine jams on every third card.", created.Description);
        Assert.Equal("New", created.Status);

        var reread = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{created.Id}");
        Assert.Equal(created.Id, reread?.Id);
    }

    [Theory]
    [EveryTicketStore]
    public async Task EachCreatedTicketGetsItsOwnId(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();
        var payload = new
        {
            name = "Ada Lovelace",
            email = "ada@example.com",
            description = "The analytical engine jams on every third card.",
        };

        var first = await client.PostAsJsonAsync("/api/tickets", payload);
        var second = await client.PostAsJsonAsync("/api/tickets", payload);

        var firstId = (await first.Content.ReadFromJsonAsync<TicketDto>())!.Id;
        var secondId = (await second.Content.ReadFromJsonAsync<TicketDto>())!.Id;
        Assert.NotEqual(firstId, secondId);
    }

    [Theory]
    [EveryTicketStore]
    public async Task MissingNameIsRejectedAndNamesTheField(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new { email = "ada@example.com", description = "No name supplied." }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Name", await ValidationErrors.FieldNamesAsync(response));
    }

    [Theory]
    [EveryTicketStore]
    public async Task MalformedEmailIsRejectedAndNamesTheField(TicketStoreProvider store)
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                name = "Ada Lovelace",
                email = "not-an-email",
                description = "The address above is nonsense.",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Email", await ValidationErrors.FieldNamesAsync(response));
    }
}
