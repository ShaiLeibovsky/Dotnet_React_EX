using System.Net;
using System.Net.Http.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

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

    private static async Task<string> RejectionAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return $"{problem?.Title}|{problem?.Detail}";
    }

    private sealed record ProblemDetails(string Title, string Detail);
}
