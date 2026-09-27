using System.Net;

namespace TicketApi.Tests;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly string body;
    private readonly HttpStatusCode status;

    public StubHttpMessageHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        this.body = body;
        this.status = status;
    }

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct
    )
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(
            new HttpResponseMessage(status) { Content = new StringContent(body) }
        );
    }
}
