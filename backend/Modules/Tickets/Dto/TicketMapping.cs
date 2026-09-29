using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Tickets.Dto;

/// <summary>Entity → DTO mapping (keeps transport shape decoupled from storage).</summary>
public static class TicketMapping
{
    public static TicketDto ToDto(this Ticket t) =>
        new(
            t.Id,
            t.Name,
            t.Email,
            t.Description,
            t.Summary,
            t.Status,
            t.Resolution,
            t.CreatedAt,
            t.UpdatedAt
        );
}
