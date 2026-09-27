namespace TicketApi.Entities;

/// <summary>A support staff account allowed to edit tickets -- ADR-0002.</summary>
public class AdminUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
