using TicketApi.Entities;
using TicketApi.Services;

namespace TicketApi.Tests;

public class NotificationSubjectTests
{
    [Fact]
    public void EverySubjectCarriesTheTicketSummary()
    {
        var ticket = new Ticket
        {
            Email = "grace@example.com",
            Description = "A moth is lodged in relay seventy.",
            Summary = "Moth in relay seventy",
            Status = TicketStatuses.InProgress,
        };

        var subjects = new[]
        {
            CustomerNotification.TicketCreated(ticket, "link").Subject,
            CustomerNotification.StatusChanged(ticket, TicketStatuses.New, "link").Subject,
            CustomerNotification.ResolutionChanged(ticket, "link").Subject,
        };

        Assert.All(subjects, subject => Assert.Contains("Moth in relay seventy", subject));
    }

    [Fact]
    public void TheDescriptionStandsInUntilASummaryExists()
    {
        var ticket = new Ticket { Description = "A moth is lodged in relay seventy." };

        var subject = CustomerNotification.TicketCreated(ticket, "link").Subject;

        Assert.Contains("A moth is lodged in relay seventy.", subject);
    }

    [Fact]
    public void ALongTitleIsTruncatedRatherThanFillingTheSubject()
    {
        var ticket = new Ticket { Description = new string('x', 200) };

        var subject = CustomerNotification.TicketCreated(ticket, "link").Subject;

        Assert.Contains($"{new string('x', 60)}…", subject);
        Assert.DoesNotContain(new string('x', 61), subject);
    }
}
