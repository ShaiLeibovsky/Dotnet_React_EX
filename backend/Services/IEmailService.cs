using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>
/// Customer notifications. The console implementation only logs; a real provider
/// (e.g. Gmail SMTP via MailKit) can be swapped in behind this seam on a later
/// branch without touching endpoints or the ticket service.
/// </summary>
// BONUS: real delivery on feature/email-gmail-smtp.
public interface IEmailService
{
    Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default);

    Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        CancellationToken ct = default
    );

    Task SendResolutionChangedAsync(Ticket ticket, CancellationToken ct = default);
}
