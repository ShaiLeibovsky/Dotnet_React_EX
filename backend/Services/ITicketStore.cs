using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>
/// Persistence boundary for tickets. The only place that touches the JSON file —
/// endpoints and the ticket service never read/write it directly.
/// </summary>
public interface ITicketStore
{
    Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default);

    Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default);

    /// <summary>
    /// Atomically loads the ticket, applies <paramref name="mutate"/>, and persists.
    /// Returns the updated ticket, or null if no ticket with <paramref name="id"/> exists.
    /// </summary>
    Task<Ticket?> UpdateAsync(
        string id,
        Action<Ticket> mutate,
        CancellationToken ct = default
    );
}
