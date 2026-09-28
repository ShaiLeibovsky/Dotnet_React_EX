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

    [Fact]
    public async Task WithAKeyConfiguredTheSummaryComesFromTheProvidersResponse()
    {
        using var gemini = new StubHttpMessageHandler(GeminiResponse("The engine jams."));
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("The engine jams.", created!.Summary);
        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/"
                + "gemini-3.1-flash-lite:generateContent",
            gemini.LastRequestUri?.ToString()
        );

        var reread = await client.GetFromJsonAsync<TicketDto>($"/api/tickets/{created.Id}");
        Assert.Equal("The engine jams.", reread!.Summary);
    }

    [Fact]
    public async Task AnAnswerLongerThanASummaryIsDiscarded()
    {
        var essay = string.Join(" ", Enumerable.Repeat("words", 60));
        using var gemini = new StubHttpMessageHandler(GeminiResponse(essay));
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal(string.Empty, created!.Summary);
    }

    [Fact]
    public async Task AnUnusableProviderResponseLeavesTheSummaryEmpty()
    {
        using var gemini = new StubHttpMessageHandler("""{"promptFeedback":{}}""");
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal(string.Empty, created!.Summary);
    }

    [Fact]
    public async Task ARateLimitedProviderLeavesTheSummaryEmpty()
    {
        using var gemini = new StubHttpMessageHandler("{}", HttpStatusCode.TooManyRequests);
        using var api = new TicketApiFactory(summaryProvider: gemini);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", Payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.Equal(string.Empty, created!.Summary);
    }

    private static string GeminiResponse(string text) =>
        $$"""
        {
          "candidates": [
            { "content": { "parts": [ { "text": "  {{text}}  " } ], "role": "model" } }
          ]
        }
        """;

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
