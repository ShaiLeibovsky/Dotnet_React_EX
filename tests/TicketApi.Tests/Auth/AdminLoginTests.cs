using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketApi.Configuration;
using TicketApi.Modules.Auth.Dto;

namespace TicketApi.Tests.Auth;

public class AdminLoginTests
{
    [Theory]
    [EveryTicketStore]
    public async Task SeededCredentialsReturnATokenAndTheAuthenticatedEmail(
        TicketStoreProvider store
    )
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TicketApiFactory.AdminEmail, password = TicketApiFactory.AdminPassword }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<AdminSessionDto>();
        Assert.Equal(TicketApiFactory.AdminEmail, session?.Email);
        Assert.False(string.IsNullOrWhiteSpace(session?.Token));
    }

    [Fact]
    public async Task TheEmailIsNotCaseSensitive()
    {
        using var api = new TicketApiFactory();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = TicketApiFactory.AdminEmail.ToUpperInvariant(),
                password = TicketApiFactory.AdminPassword,
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [EveryTicketStore]
    public async Task AWrongPasswordAndAnUnknownEmailAreRejectedIdentically(
        TicketStoreProvider store
    )
    {
        using var api = new TicketApiFactory(store);
        var client = api.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TicketApiFactory.AdminEmail, password = "not-the-password" }
        );
        var unknownEmail = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "nobody@example.com", password = TicketApiFactory.AdminPassword }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await RejectionAsync(wrongPassword), await RejectionAsync(unknownEmail));
    }

    [Fact]
    public async Task ATokenOutlivesARestartWhenNoSigningKeyIsConfigured()
    {
        var keyDirectory = Directory.CreateTempSubdirectory("ticket-api-signing-key").FullName;
        var sharedDatabase = Path.Combine(keyDirectory, "tickets.db");
        var ticket = SeededTickets.Load()[0];
        try
        {
            AuthenticationHeaderValue? issuedBeforeTheRestart;
            using (var firstRun = GeneratedSigningKeyApi(sharedDatabase))
            {
                var client = await firstRun.CreateAdminClientAsync();
                issuedBeforeTheRestart = client.DefaultRequestHeaders.Authorization;
            }

            using var secondRun = GeneratedSigningKeyApi(sharedDatabase);
            var afterTheRestart = secondRun.CreateClient();
            afterTheRestart.DefaultRequestHeaders.Authorization = issuedBeforeTheRestart;

            var response = await afterTheRestart.PutAsJsonAsync(
                $"/api/tickets/{ticket.Id}",
                new { status = "In Progress", resolution = ticket.Resolution }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Directory.Delete(keyDirectory, recursive: true);
        }
    }

    private static TicketApiFactory GeneratedSigningKeyApi(string databasePath) =>
        new()
        {
            ConfigurationOverrides =
            {
                ["Auth:SigningKey"] = null,
                ["TicketStore:DatabasePath"] = databasePath,
            },
        };

    private static async Task<string> RejectionAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return $"{problem?.Title}|{problem?.Detail}";
    }

    private sealed record ProblemDetails(string Title, string Detail);
}
