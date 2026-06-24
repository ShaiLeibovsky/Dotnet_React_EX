using TicketApi.Dtos;

namespace TicketApi.Services;

/// <summary>
/// Ticket business logic: validation, orchestration of the store and email
/// notifications, and entity↔DTO mapping. Endpoints stay thin and call into here.
/// A null return means "no ticket with that id" (→ 404 at the endpoint).
/// </summary>
public interface ITicketService
{
    Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken ct = default);

    Task<TicketDto?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<TicketDto> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);

    Task<TicketDto?> UpdateAsync(
        string id,
        UpdateTicketRequest request,
        CancellationToken ct = default
    );

    Task<TicketDto?> AddResponseAsync(
        string id,
        AddResponseRequest request,
        CancellationToken ct = default
    );
}
