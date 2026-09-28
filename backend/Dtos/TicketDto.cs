using TicketApi.Entities;

namespace TicketApi.Dtos;

/// <summary>Response shape returned to the frontend (mirrors the TS Ticket type).</summary>
public record TicketDto(
    string Id,
    string Name,
    string Email,
    string Description,
    string Summary,
    string Status,
    string Resolution,
    string ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

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
            t.ImageUrl,
            t.CreatedAt,
            t.UpdatedAt
        );
}
