using System.ComponentModel.DataAnnotations;

namespace TicketApi.Dtos;

/// <summary>Body for POST /api/tickets.</summary>
public record CreateTicketRequest(
    [property: Required, StringLength(120)] string Name,
    [property: Required, EmailAddress] string Email,
    [property: Required, StringLength(4000, MinimumLength = 1)] string Description
);

/// <summary>Body for PUT /api/tickets/{id}.</summary>
public record UpdateTicketRequest(
    [property: Required] string Status,
    string? Resolution
);

/// <summary>Body for POST /api/tickets/{id}/responses.</summary>
public record AddResponseRequest(
    [property: Required, StringLength(120)] string Author,
    [property: Required] string Role,
    [property: Required, StringLength(4000, MinimumLength = 1)] string Body
);
