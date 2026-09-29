using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Summaries;

/// <summary>
/// Generates the short restatement of a ticket description stored on
/// <see cref="Modules.Tickets.Entities.Ticket.Summary"/>. Implementations may throw; callers treat
/// summary generation as best-effort and never let a failure fail the ticket.
/// </summary>
public interface ISummaryService
{
    Task<string> SummariseAsync(string description, CancellationToken ct = default);
}
