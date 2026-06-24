namespace TicketApi.Entities;

/// <summary>A customer support ticket persisted in the JSON store.</summary>
public class Ticket
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>AI-generated summary (bonus feature; empty until generated).</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>One of <see cref="TicketStatuses"/>.</summary>
    public string Status { get; set; } = TicketStatuses.New;

    public string Resolution { get; set; } = string.Empty;
    public List<TicketResponse> Responses { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Allowed values for <see cref="Ticket.Status"/>.</summary>
public static class TicketStatuses
{
    public const string New = "New";
    public const string InProgress = "In Progress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal)
        {
            New,
            InProgress,
            Resolved,
            Closed,
        };

    public static bool IsValid(string status) => All.Contains(status);
}
