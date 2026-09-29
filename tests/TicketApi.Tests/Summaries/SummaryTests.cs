using System.Net;
using System.Net.Http.Json;
using TicketApi.Configuration;
using TicketApi.Modules.Tickets.Dto;

namespace TicketApi.Tests.Summaries;

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
    public async Task AProviderFailureLeavesTheTicketWithNoSummary(TicketStoreProvider store)
    {
        var gemini = new StubSummaryService(
            _ => throw new HttpRequestException("provider unavailable")
        );
        using var api = new TicketApiFactory(store, gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(created);
        Assert.Equal(string.Empty, await SummaryAfterBackfillAsync(client, gemini.Called, created.Id));
    }

    [Theory]
    [EveryTicketStore]
    public async Task AGeneratedSummaryReachesTheTicketAfterItIsCreated(
        TicketStoreProvider store
    )
    {
        var gemini = new StubSummaryService(description => $"Summary of: {description}");
        using var api = new TicketApiFactory(store, gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(string.Empty, created!.Summary);
        Assert.Equal(
            "Summary of: The analytical engine jams on every third card.",
            await SummaryAfterBackfillAsync(client, gemini.Called, created.Id)
        );
    }

    [Fact]
    public async Task WithAKeyConfiguredTheSummaryComesFromTheProvidersResponse()
    {
        using var gemini = new StubHttpMessageHandler(GeminiResponse("The engine jams."));
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            "The engine jams.",
            await SummaryAfterBackfillAsync(client, gemini.Called, created!.Id)
        );
        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/"
                + "gemini-3.1-flash-lite:generateContent",
            gemini.LastRequestUri?.ToString()
        );
    }

    [Fact]
    public async Task AnAnswerLongerThanASummaryIsDiscarded()
    {
        var essay = string.Join(" ", Enumerable.Repeat("words", 60));
        using var gemini = new StubHttpMessageHandler(GeminiResponse(essay));
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            string.Empty,
            await SummaryAfterBackfillAsync(client, gemini.Called, created!.Id)
        );
    }

    [Fact]
    public async Task AnUnusableProviderResponseLeavesTheSummaryEmpty()
    {
        using var gemini = new StubHttpMessageHandler("""{"promptFeedback":{}}""");
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            string.Empty,
            await SummaryAfterBackfillAsync(client, gemini.Called, created!.Id)
        );
    }

    [Fact]
    public async Task ARateLimitedProviderLeavesTheSummaryEmpty()
    {
        using var gemini = new StubHttpMessageHandler("{}", HttpStatusCode.TooManyRequests);
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            string.Empty,
            await SummaryAfterBackfillAsync(client, gemini.Called, created!.Id)
        );
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

    /// <summary>
    /// Waits for the provider call the backfill makes, then for the write that may follow
    /// it, and returns whatever summary the ticket ended up with.
    /// </summary>
    private static async Task<string> SummaryAfterBackfillAsync(
        HttpClient client,
        Task providerCalled,
        string ticketId
    )
    {
        await providerCalled.WaitAsync(TimeSpan.FromSeconds(10));

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(1);
        while (DateTime.UtcNow < deadline)
        {
            var ticket = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{ticketId}");
            if (!string.IsNullOrEmpty(ticket!.Summary))
                return ticket.Summary;
            await Task.Delay(20);
        }

        var settled = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{ticketId}");
        return settled!.Summary;
    }

    private static string GeminiResponse(string text) =>
        $$"""
        {
          "candidates": [
            { "content": { "parts": [ { "text": "  {{text}}  " } ], "role": "model" } }
          ]
        }
        """;
}
