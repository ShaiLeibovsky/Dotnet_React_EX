using System.Net;
using System.Net.Http.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

public class SummaryTests
{
    private static readonly object Payload = new
    {
        name = "Ada Lovelace",
        email = "ada@example.com",
        description = "The analytical engine jams on every third card.",
    };

    [Theory]
    [EveryTicketStore]
    public async Task AProviderFailureStillCreatesTheTicketWithNoSummary(
        TicketStoreProvider store
    )
    {
        using var api = new TicketApiFactory(
            store,
            new StubSummaryService(_ => throw new HttpRequestException("provider unavailable"))
        );
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(created);
        Assert.Equal(string.Empty, created.Summary);
    }

    [Theory]
    [EveryTicketStore]
    public async Task AGeneratedSummaryIsPersistedAndReturnedOnALaterRead(
        TicketStoreProvider store
    )
    {
        using var api = new TicketApiFactory(
            store,
            new StubSummaryService(description => $"Summary of: {description}")
        );
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        var expected = "Summary of: The analytical engine jams on every third card.";
        Assert.Equal(expected, created!.Summary);

        var reread = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{created.Id}");
        Assert.Equal(expected, reread!.Summary);
    }

    [Theory]
    [EveryTicketStore]
    public async Task WithNoApiKeyConfiguredTicketsAreCreatedWithNoSummary(
        TicketStoreProvider store
    )
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal(string.Empty, created!.Summary);
    }
}
