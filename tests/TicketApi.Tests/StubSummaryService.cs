namespace TicketApi.Tests;

public sealed class StubSummaryService : ISummaryService
{
    private readonly Func<string, string> summarise;

    public StubSummaryService(Func<string, string> summarise)
    {
        this.summarise = summarise;
    }

    public Task<string> SummariseAsync(string description, CancellationToken ct = default) =>
        Task.FromResult(summarise(description));
}
