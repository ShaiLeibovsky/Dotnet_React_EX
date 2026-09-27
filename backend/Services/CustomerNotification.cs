using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>The customer-facing text of one notification, shared by every delivery channel.</summary>
public sealed record CustomerNotification(string Recipient, string Subject, string Body)
{
    public static CustomerNotification TicketCreated(Ticket ticket, string trackingLink) =>
        new(
            ticket.Email,
            "Your support ticket has been created",
            $"Hi {ticket.Name}, we received your request and will be in touch. "
                + $"Track it here: {trackingLink}"
        );

    public static CustomerNotification StatusChanged(
        Ticket ticket,
        string previousStatus,
        string trackingLink
    ) =>
        new(
            ticket.Email,
            $"Ticket status updated: {previousStatus} → {ticket.Status}",
            $"Hi {ticket.Name}, your ticket status is now \"{ticket.Status}\". "
                + $"Details: {trackingLink}"
        );

    public static CustomerNotification ResolutionChanged(Ticket ticket, string trackingLink) =>
        new(
            ticket.Email,
            "An update on your ticket resolution",
            $"Hi {ticket.Name}, we added a resolution note: \"{ticket.Resolution}\". "
                + $"Details: {trackingLink}"
        );
}
