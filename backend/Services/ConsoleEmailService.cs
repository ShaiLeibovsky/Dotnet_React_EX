using Microsoft.Extensions.Options;
using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>Mock email service: logs the message to the console (no real send).</summary>
public sealed class ConsoleEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(
        IOptions<EmailOptions> options,
        ILogger<ConsoleEmailService> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default)
    {
        Log(CustomerNotification.TicketCreated(ticket, TrackingLink(ticket)));
        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        CancellationToken ct = default
    )
    {
        Log(CustomerNotification.StatusChanged(ticket, previousStatus, TrackingLink(ticket)));
        return Task.CompletedTask;
    }

    public Task SendResolutionChangedAsync(Ticket ticket, CancellationToken ct = default)
    {
        Log(CustomerNotification.ResolutionChanged(ticket, TrackingLink(ticket)));
        return Task.CompletedTask;
    }

    private string TrackingLink(Ticket ticket) =>
        CustomerNotification.TrackingLink(_options, ticket);

    private void Log(CustomerNotification notification) =>
        _logger.LogInformation(
            "[EMAIL] To: {Email} | Subject: {Subject}\n{Body}",
            notification.Recipient,
            notification.Subject,
            notification.Body
        );
}
