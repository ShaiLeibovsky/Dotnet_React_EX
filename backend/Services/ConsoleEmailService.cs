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
        Log(
            ticket,
            "Your support ticket has been created",
            $"Hi {ticket.Name}, we received your request and will be in touch. "
                + $"Track it here: {TrackingLink(ticket)}"
        );
        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(
        Ticket ticket,
        string previousStatus,
        CancellationToken ct = default
    )
    {
        Log(
            ticket,
            $"Ticket status updated: {previousStatus} → {ticket.Status}",
            $"Hi {ticket.Name}, your ticket status is now \"{ticket.Status}\". "
                + $"Details: {TrackingLink(ticket)}"
        );
        return Task.CompletedTask;
    }

    public Task SendResolutionChangedAsync(Ticket ticket, CancellationToken ct = default)
    {
        Log(
            ticket,
            "An update on your ticket resolution",
            $"Hi {ticket.Name}, we added a resolution note: \"{ticket.Resolution}\". "
                + $"Details: {TrackingLink(ticket)}"
        );
        return Task.CompletedTask;
    }

    private string TrackingLink(Ticket ticket) =>
        $"{_options.TrackingBaseUrl.TrimEnd('/')}/{ticket.Id}";

    private void Log(Ticket ticket, string subject, string body) =>
        _logger.LogInformation(
            "[EMAIL] To: {Email} | Subject: {Subject}\n{Body}",
            ticket.Email,
            subject,
            body
        );
}
