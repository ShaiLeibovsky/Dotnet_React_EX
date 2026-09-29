using Microsoft.Extensions.Options;
using TicketApi.Tickets;

namespace TicketApi.Notifications;

/// <summary>Logs each customer notification instead of delivering it. The default channel.</summary>
public sealed class LogNotifier : ICustomerNotifier
{
    private readonly EmailOptions _options;
    private readonly ILogger<LogNotifier> _logger;

    public LogNotifier(
        IOptions<EmailOptions> options,
        ILogger<LogNotifier> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default)
    {
        Log(CustomerNotification.TicketCreated(ticket, _options.TrackingLinkFor(ticket)));
        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        Log(
            CustomerNotification.StatusChanged(
                ticket,
                previousStatus,
                _options.TrackingLinkFor(ticket),
                handlingAdminEmail
            )
        );
        return Task.CompletedTask;
    }

    public Task SendResolutionChangedAsync(
        Ticket ticket,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        Log(
            CustomerNotification.ResolutionChanged(
                ticket,
                _options.TrackingLinkFor(ticket),
                handlingAdminEmail
            )
        );
        return Task.CompletedTask;
    }

    private void Log(CustomerNotification notification) =>
        _logger.LogInformation(
            "[NOTIFICATION] To: {Email} | Reply-To: {ReplyTo} | Subject: {Subject}\n{Body}",
            notification.Recipient,
            notification.ReplyTo,
            notification.Subject,
            notification.Body
        );
}
