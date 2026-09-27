using System.Net;
using Microsoft.Extensions.Options;

namespace TicketApi.Tests;

public class GeminiSummaryServiceTests
{
    [Fact]
    public async Task TheGeneratedTextIsReadOutOfTheFirstCandidate()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [ { "text": "  The engine jams on every third card.  " } ],
                    "role": "model"
                  }
                }
              ]
            }
            """
        );
        var service = Summariser(handler);

        var summary = await service.SummariseAsync("The analytical engine jams.");

        Assert.Equal("The engine jams on every third card.", summary);
        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent",
            handler.LastRequestUri?.ToString()
        );
    }

    [Fact]
    public async Task AnErrorStatusThrows()
    {
        var service = Summariser(
            new StubHttpMessageHandler("{}", HttpStatusCode.TooManyRequests)
        );

        await Assert.ThrowsAsync<HttpRequestException>(() => service.SummariseAsync("Anything."));
    }

    [Fact]
    public async Task AResponseWithoutCandidatesThrows()
    {
        var service = Summariser(new StubHttpMessageHandler("""{"promptFeedback":{}}"""));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SummariseAsync("Anything."));
    }

    private static GeminiSummaryService Summariser(StubHttpMessageHandler handler)
    {
        var options = new SummaryOptions { ApiKey = "test-key" };
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseAddress),
        };
        return new GeminiSummaryService(client, Options.Create(options));
    }
}
