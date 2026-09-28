using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>Bound from the "Email" config section.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Base URL for the customer-facing ticket tracking link.</summary>
    public string TrackingBaseUrl { get; set; } = "http://localhost:5173/tickets";

    /// <summary>SMTP host, e.g. "smtp.gmail.com". Supplied through user-secrets.</summary>
    public string? SmtpHost { get; set; }

    /// <summary>SMTP submission port. STARTTLS only; implicit TLS (465) is unsupported.</summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>SMTP account, also the sender unless <see cref="SmtpFrom"/> is set.</summary>
    public string? SmtpUser { get; set; }

    /// <summary>SMTP password or provider app-password. Supplied through user-secrets.</summary>
    public string? SmtpPassword { get; set; }

    public bool SmtpUseStartTls { get; set; } = true;

    public string? SmtpFrom { get; set; }

    /// <summary>True when SMTP delivery has everything it needs to connect and authenticate.</summary>
    public bool SmtpConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost)
        && !string.IsNullOrWhiteSpace(SmtpUser)
        && !string.IsNullOrWhiteSpace(SmtpPassword);

    /// <summary>The customer-facing URL a notification points at for this ticket.</summary>
    public string TrackingLinkFor(Ticket ticket) =>
        $"{TrackingBaseUrl.TrimEnd('/')}/{ticket.Id}";
}
