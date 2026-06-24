namespace TicketApi.Entities;

/// <summary>A single reply on a ticket thread (from a customer or an admin).</summary>
public class TicketResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Author { get; set; } = string.Empty;

    /// <summary>"customer" or "admin".</summary>
    public string Role { get; set; } = ResponseRoles.Customer;

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Allowed values for <see cref="TicketResponse.Role"/>.</summary>
public static class ResponseRoles
{
    public const string Customer = "customer";
    public const string Admin = "admin";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Customer, Admin };

    public static bool IsValid(string role) => All.Contains(role);
}
