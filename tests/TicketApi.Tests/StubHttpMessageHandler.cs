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

    private readonly TaskCompletionSource called =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Uri? LastRequestUri { get; private set; }

    /// <summary>Completes once the backfill has called the provider.</summary>
    public Task Called => called.Task;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct
    )
    {
        LastRequestUri = request.RequestUri;
        called.TrySetResult();
        return Task.FromResult(
            new HttpResponseMessage(status) { Content = new StringContent(body) }
        );
    }
}
