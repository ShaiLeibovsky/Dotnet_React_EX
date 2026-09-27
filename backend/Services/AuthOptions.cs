namespace TicketApi.Services;

/// <summary>Bound from the "Auth" config section. The signing key and the seeded
/// admin password come from user-secrets, never from a committed file.</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public const string Issuer = "TicketApi";
    public const string Audience = "TicketApi";

    public string SigningKey { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
