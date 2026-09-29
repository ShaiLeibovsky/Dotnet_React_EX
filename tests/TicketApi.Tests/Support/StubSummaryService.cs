using TicketApi.Summaries;

namespace TicketApi.Tests.Support;

public sealed class StubSummaryService : ISummaryService
{
    private readonly Func<string, string> summarise;
    private readonly TaskCompletionSource called =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public StubSummaryService(Func<string, string> summarise)
    {
        this.summarise = summarise;
    }

    /// <summary>Completes once the backfill has asked for a summary.</summary>
    public Task Called => called.Task;

    public Task<string> SummariseAsync(string description, CancellationToken ct = default)
    {
        try
        {
            return Task.FromResult(summarise(description));
        }
        finally
        {
            called.TrySetResult();
        }
    }
}
