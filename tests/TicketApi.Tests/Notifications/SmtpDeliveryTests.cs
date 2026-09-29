using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using TicketApi.Modules.Notifications;
using TicketApi.Modules.Tickets.Dto;

namespace TicketApi.Tests.Notifications;

public class SmtpDeliveryTests
{
    [Fact]
    public void WithoutCredentialsTheLogNotifierStaysTheDefault()
    {
        using var api = new TicketApiFactory { KeepConfiguredNotifier = true };

        var notifier = api.Services.GetRequiredService<ICustomerNotifier>();

        Assert.IsType<LogNotifier>(notifier);
    }

    [Fact]
    public void WithCredentialsTheEmailNotifierIsRegistered()
    {
        using var smtp = new FakeSmtpServer();
        using var api = SmtpConfiguredApi(smtp);

        var notifier = api.Services.GetRequiredService<ICustomerNotifier>();

        Assert.IsType<EmailNotifier>(notifier);
    }

    [Fact]
    public async Task CreatingATicketSendsATrackingLinkOverSmtp()
    {
        using var smtp = new FakeSmtpServer();
        using var api = SmtpConfiguredApi(smtp);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                name = "Grace Hopper",
                email = "grace@example.com",
                description = "A moth is lodged in relay seventy.",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketDto>();

        var message = Assert.Single(await DeliveredMessagesAsync(smtp, expected: 1));
        Assert.Contains("grace@example.com", message);
        Assert.Contains($"http://localhost:5173/tickets/{created!.Id}", message);
    }

    [Fact]
    public async Task AStatusChangeAndAResolutionChangeAreBothSentOverSmtp()
    {
        using var smtp = new FakeSmtpServer();
        using var api = SmtpConfiguredApi(smtp);
        var client = await api.CreateAdminClientAsync();
        var ticket = SeededTickets.Load()[0];

        var statusChange = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = "In Progress", resolution = ticket.Resolution }
        );
        var resolutionChange = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = "In Progress", resolution = "Cleaned the vents." }
        );

        Assert.Equal(HttpStatusCode.OK, statusChange.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resolutionChange.StatusCode);

        var messages = await DeliveredMessagesAsync(smtp, expected: 2);
        Assert.Contains(messages, message => message.Contains("In Progress"));
        Assert.Contains(messages, message => message.Contains("Cleaned the vents."));
    }

    [Fact]
    public async Task ASendFailureDoesNotFailTheRequestThatTriggeredIt()
    {
        using var api = SmtpConfiguredApi(ClosedLoopbackPort());
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                name = "Grace Hopper",
                email = "grace@example.com",
                description = "A moth is lodged in relay seventy.",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AnUnusableSenderAddressDoesNotFailTheRequestThatTriggeredIt()
    {
        using var smtp = new FakeSmtpServer();
        var api = SmtpConfiguredApi(smtp);
        api.ConfigurationOverrides["Email:SmtpFrom"] = "not an address";
        using (api)
        {
            var client = api.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/tickets",
                new
                {
                    name = "Grace Hopper",
                    email = "grace@example.com",
                    description = "A moth is lodged in relay seventy.",
                }
            );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task AnEditIsSentFromTheServiceMailboxAndRepliesToTheHandlingAdmin()
    {
        using var smtp = new FakeSmtpServer();
        using var api = SmtpConfiguredApi(smtp);
        var client = await api.CreateAdminClientAsync();
        var ticket = SeededTickets.Load()[0];

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            new { status = "In Progress", resolution = ticket.Resolution }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var message = Assert.Single(await DeliveredMessagesAsync(smtp, expected: 1));
        Assert.Contains("From: support@example.com", message);
        Assert.Contains($"Reply-To: {TicketApiFactory.AdminEmail}", message);
    }

    [Fact]
    public async Task ACreatedTicketCarriesNoReplyToBecauseNoAdminHandledIt()
    {
        using var smtp = new FakeSmtpServer();
        using var api = SmtpConfiguredApi(smtp);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/tickets",
            new
            {
                name = "Grace Hopper",
                email = "grace@example.com",
                description = "A moth is lodged in relay seventy.",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = Assert.Single(await DeliveredMessagesAsync(smtp, expected: 1));
        Assert.DoesNotContain("Reply-To:", message);
    }

    private static int ClosedLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static TicketApiFactory SmtpConfiguredApi(FakeSmtpServer smtp) =>
        SmtpConfiguredApi(smtp.Port);

    private static TicketApiFactory SmtpConfiguredApi(int smtpPort) =>
        new()
        {
            KeepConfiguredNotifier = true,
            ConfigurationOverrides =
            {
                ["Email:SmtpHost"] = "127.0.0.1",
                ["Email:SmtpPort"] = smtpPort.ToString(),
                ["Email:SmtpUser"] = "support@example.com",
                ["Email:SmtpPassword"] = "app-password",
                ["Email:SmtpUseStartTls"] = "false",
            },
        };

    private static async Task<IReadOnlyList<string>> DeliveredMessagesAsync(
        FakeSmtpServer smtp,
        int expected
    )
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (smtp.Messages.Count < expected && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        Assert.Equal(expected, smtp.Messages.Count);
        return [.. smtp.Messages.Select(UnfoldQuotedPrintable)];
    }

    private static string UnfoldQuotedPrintable(string message) =>
        message.Replace("=\r\n", string.Empty).Replace("=\n", string.Empty);
}
