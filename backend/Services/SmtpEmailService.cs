using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>Delivers customer notifications over SMTP, e.g. Gmail submission on port 587.</summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendTicketCreatedAsync(Ticket ticket, CancellationToken ct = default) =>
        SendAsync(CustomerNotification.TicketCreated(ticket, TrackingLink(ticket)));

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        CancellationToken ct = default
    ) => SendAsync(CustomerNotification.StatusChanged(ticket, previousStatus, TrackingLink(ticket)));

    public Task SendResolutionChangedAsync(Ticket ticket, CancellationToken ct = default) =>
        SendAsync(CustomerNotification.ResolutionChanged(ticket, TrackingLink(ticket)));

    private string TrackingLink(Ticket ticket) =>
        CustomerNotification.TrackingLink(_options, ticket);

    private async Task SendAsync(CustomerNotification notification)
    {
        // ponytail: System.Net.Mail only speaks STARTTLS and password auth; MailKit if
        // implicit TLS (465) or OAuth2 is ever needed.
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.SmtpUseSsl,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword),
        };
        using var message = new MailMessage(
            from: _options.SmtpFrom ?? _options.SmtpUser!,
            to: notification.Recipient,
            subject: notification.Subject,
            body: notification.Body
        );

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception failure)
        {
            _logger.LogError(
                failure,
                "[EMAIL] Send failed. To: {Email} | Subject: {Subject}",
                notification.Recipient,
                notification.Subject
            );
        }
    }
}
