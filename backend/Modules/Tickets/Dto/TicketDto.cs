using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Tickets.Dto;

/// <summary>Response shape returned to the frontend (mirrors the TS Ticket type).</summary>
public record TicketDto(
    string Id,
    string Name,
    string Email,
    string Description,
    string Summary,
    string Status,
    string Resolution,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
