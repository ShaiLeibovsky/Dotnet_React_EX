using System.Threading.Channels;

namespace TicketApi.Services;

/// <summary>
/// Hands the ids of tickets awaiting a summary from <see cref="TicketService"/> to
/// <see cref="SummaryBackfill"/>. Unbounded: a ticket is only ever queued once, by the
/// request that created it -- ADR-0003 section 5, summarising after the response.
/// </summary>
public sealed class SummaryQueue
{
    private readonly Channel<string> _ticketIds = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true }
    );

    public void Enqueue(string ticketId) => _ticketIds.Writer.TryWrite(ticketId);

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct) =>
        _ticketIds.Reader.ReadAllAsync(ct);
}
