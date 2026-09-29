using TicketApi.Modules.Auth.Util;

namespace TicketApi.Configuration;

/// <summary>Bound from the "Auth" config section, never from a committed file: the seeded
/// admin's email and password come from user-secrets, and the signing key from there or,
/// unset, from <see cref="DevelopmentSigningKey"/>.</summary>
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
