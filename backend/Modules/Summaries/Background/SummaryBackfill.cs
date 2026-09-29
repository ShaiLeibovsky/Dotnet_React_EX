using TicketApi.Modules.Summaries.Util;
using TicketApi.Modules.Tickets.Stores;

namespace TicketApi.Modules.Summaries.Background;

/// <summary>
/// Summarises queued tickets after their create request has already answered, and writes
/// the result back. A ticket whose summary cannot be generated keeps the empty one it was
/// created with -- ADR-0003 section 5, summarising after the response.
/// </summary>
public sealed class SummaryBackfill : BackgroundService
{
    private readonly SummaryQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SummaryBackfill> _logger;

    public SummaryBackfill(
        SummaryQueue queue,
        IServiceScopeFactory scopes,
        ILogger<SummaryBackfill> logger
    )
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var ticketId in _queue.ReadAllAsync(ct))
        {
            try
            {
                await SummariseAsync(ticketId, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception failure)
            {
                _logger.LogWarning(
                    failure,
                    "Summary generation failed for ticket {TicketId}; leaving it blank.",
                    ticketId
                );
            }
        }
    }

    private async Task SummariseAsync(string ticketId, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ITicketStore>();

        var ticket = await store.GetByIdAsync(ticketId, ct);
        if (ticket is null)
            return;

        var summaries = scope.ServiceProvider.GetRequiredService<ISummaryService>();
        var summary = await summaries.SummariseAsync(ticket.Description, ct);
        if (string.IsNullOrWhiteSpace(summary))
            return;

        await store.UpdateAsync(ticketId, t => t.Summary = summary, ct);
    }
}
