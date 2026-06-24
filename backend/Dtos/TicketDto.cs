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
    IReadOnlyList<TicketResponseDto> Responses,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TicketResponseDto(
    string Id,
    string Author,
    string Role,
    string Body,
    DateTime CreatedAt
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
            t.Responses.Select(r => r.ToDto()).ToList(),
            t.CreatedAt,
            t.UpdatedAt
        );

    public static TicketResponseDto ToDto(this TicketResponse r) =>
        new(r.Id, r.Author, r.Role, r.Body, r.CreatedAt);
}
