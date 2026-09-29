namespace TicketApi.Modules.Summaries;

/// <summary>Registered when no API key is configured: every ticket gets no summary.</summary>
public sealed class NullSummaryService : ISummaryService
{
    public Task<string> SummariseAsync(string description, CancellationToken ct = default) =>
        Task.FromResult(string.Empty);
}
