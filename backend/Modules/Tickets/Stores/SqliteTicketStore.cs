using Microsoft.EntityFrameworkCore;
using TicketApi.Modules.Tickets.Entities;
using TicketApi.Data;

namespace TicketApi.Modules.Tickets.Stores;

/// <summary>Database-backed <see cref="ITicketStore"/>, newest ticket first.</summary>
public sealed class SqliteTicketStore : ITicketStore
{
    private readonly TicketDbContext _tickets;

    public SqliteTicketStore(TicketDbContext tickets)
    {
        _tickets = tickets;
    }

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default) =>
        await _tickets
            .Tickets.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _tickets.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _tickets.Tickets.Add(ticket);
        await _tickets.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket?> UpdateAsync(
        string id,
        Action<Ticket> mutate,
        CancellationToken ct = default
    )
    {
        var ticket = await _tickets.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
            return null;

        mutate(ticket);
        ticket.UpdatedAt = DateTime.UtcNow;
        await _tickets.SaveChangesAsync(ct);
        return ticket;
    }
}
