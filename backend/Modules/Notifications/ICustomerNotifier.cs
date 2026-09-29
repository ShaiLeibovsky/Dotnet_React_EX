using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Notifications;

/// <summary>
/// One customer notification channel. <see cref="LogNotifier"/> logs and is the default;
/// <see cref="EmailNotifier"/> delivers by email once SMTP credentials are configured.
/// The edit notifications carry the email of the admin who made the edit, so a customer
/// reply reaches that admin rather than the sending mailbox -- ADR-0002 section 6, the
/// handling admin is a Reply-To, not the sender.
/// </summary>
public interface ICustomerNotifier
{
    Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default);

    Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        string? handlingAdminEmail,
        CancellationToken ct = default
    );

    Task SendResolutionChangedAsync(
        Ticket ticket,
        string? handlingAdminEmail,
        CancellationToken ct = default
    );
}
