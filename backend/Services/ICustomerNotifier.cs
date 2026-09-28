using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>
/// One customer notification channel. <see cref="LogNotifier"/> logs and is the default;
/// <see cref="EmailNotifier"/> delivers by email once SMTP credentials are configured.
/// </summary>
public interface ICustomerNotifier
{
    Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default);

    Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        CancellationToken ct = default
    );

    Task SendResolutionChangedAsync(Ticket ticket, CancellationToken ct = default);
}
