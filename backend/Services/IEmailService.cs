using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>
/// Customer notifications. <see cref="ConsoleEmailService"/> only logs and is the default;
/// <see cref="SmtpEmailService"/> delivers by email once SMTP credentials are configured.
/// </summary>
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
